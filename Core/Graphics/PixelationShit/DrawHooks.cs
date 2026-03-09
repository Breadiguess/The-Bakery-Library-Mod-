using MonoMod.Cil;
using System.Reflection;
using Terraria.Graphics.Renderers;

namespace BreadLibrary.Core.Graphics.PixelationShit
{
    public enum PixelLayer
    {
        AbovePlayer = 0,
        AboveProjectiles = 1,
        AboveNPCs = 2,
        AboveTiles = 3,
        BehindTiles = 4
    }

    /// <summary>
    /// World draw stage hooks. These are the insertion points the RT system will draw back into.
    /// </summary>
    public sealed class DrawHooks : ILoadable
    {
        private static readonly FieldInfo MainPlayersBehindNPCsField =
            typeof(Main).GetField("_playersThatDrawBehindNPCs", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo MainPlayersAboveProjectilesField =
            typeof(Main).GetField("_playersThatDrawAfterProjectiles", BindingFlags.NonPublic | BindingFlags.Instance);

        public static List<Player> PlayersBehindNPCs;
        public static List<Player> PlayersAboveProjectiles;

        public static event Action DrawBehindWallsEvent;
        public static event Action DrawBehindNonSolidTilesEvent;
        public static event Action DrawBehindSolidTilesAndBackgroundNPCsEvent;
        public static event Action DrawBehindTilesEvent;
        public static event Action DrawAboveTilesEvent;
        public static event Action DrawAboveNPCsEvent;
        public static event Action DrawAboveProjectilesEvent;
        public static event Action<bool> PreDrawPlayersEvent;
        public static event Action<bool> DrawAbovePlayersEvent;

        public static event Action<Player, Vector2, float, float, float> ModifyPlayerDrawingEvent;
        public static event Action<PlayerDrawSet> ModifyDrawSetAfterTransformsEvent;
        public static event Func<Player, bool> ShouldPlayerDrawBehindNPCsEvent;

        public void Load(Mod mod)
        {
            if (Main.dedServ)
                return;

            IL_Main.DoDraw_WallsTilesNPCs += AddDrawHooks;
            On_Main.DoDraw_WallsAndBlacks += DrawBehindWallsHook;
            On_Main.DoDraw_Tiles_Solid += DrawBehindTilesHook;
            On_Main.DoDraw_DrawNPCsOverTiles += DrawAboveNPCsHook;
            On_Main.DrawProjectiles += DrawAboveProjectilesHook;
            On_Main.DrawPlayers_BehindNPCs += DrawPlayersBehindNPCsHook;
            On_Main.DrawPlayers_AfterProjectiles += DrawPlayersAfterProjectilesHook;
            On_Main.RefreshPlayerDrawOrder += RefreshPlayerDrawOrderHook;
            On_LegacyPlayerRenderer.DrawPlayerInternal += ModifyPlayerInternalHook;
            On_PlayerDrawLayers.DrawPlayer_RenderAllLayers += ModifyDrawSetHook;

            PlayersBehindNPCs = MainPlayersBehindNPCsField?.GetValue(Main.instance) as List<Player>;
            PlayersAboveProjectiles = MainPlayersAboveProjectilesField?.GetValue(Main.instance) as List<Player>;
        }

        public void Unload()
        {
            PlayersBehindNPCs = null;
            PlayersAboveProjectiles = null;

            DrawBehindWallsEvent = null;
            DrawBehindNonSolidTilesEvent = null;
            DrawBehindSolidTilesAndBackgroundNPCsEvent = null;
            DrawBehindTilesEvent = null;
            DrawAboveTilesEvent = null;
            DrawAboveNPCsEvent = null;
            DrawAboveProjectilesEvent = null;
            PreDrawPlayersEvent = null;
            DrawAbovePlayersEvent = null;
            ModifyPlayerDrawingEvent = null;
            ModifyDrawSetAfterTransformsEvent = null;
            ShouldPlayerDrawBehindNPCsEvent = null;
        }

        private void RefreshPlayerDrawOrderHook(On_Main.orig_RefreshPlayerDrawOrder orig, Main self)
        {
            orig(self);

            if (Main.gameMenu || Main.dedServ || ShouldPlayerDrawBehindNPCsEvent is null)
                return;

            if (PlayersAboveProjectiles is null || PlayersBehindNPCs is null)
                return;

            for (int i = PlayersAboveProjectiles.Count - 1; i >= 0; i--)
            {
                Player player = PlayersAboveProjectiles[i];
                if (player is null)
                    continue;

                if (ShouldPlayerDrawBehindNPCsEvent.Invoke(player))
                {
                    PlayersBehindNPCs.Add(player);
                    PlayersAboveProjectiles.RemoveAt(i);
                }
            }
        }

        private void ModifyPlayerInternalHook(
            On_LegacyPlayerRenderer.orig_DrawPlayerInternal orig,
            LegacyPlayerRenderer self,
            Terraria.Graphics.Camera camera,
            Player drawPlayer,
            Vector2 position,
            float rotation,
            Vector2 rotationOrigin,
            float shadow,
            float alpha,
            float scale,
            bool headOnly)
        {
            if (!headOnly)
                ModifyPlayerDrawingEvent?.Invoke(drawPlayer, position, rotation, scale, shadow);

            orig(self, camera, drawPlayer, position, rotation, rotationOrigin, shadow, alpha, scale, headOnly);
        }

        private void ModifyDrawSetHook(On_PlayerDrawLayers.orig_DrawPlayer_RenderAllLayers orig, ref PlayerDrawSet drawinfo)
        {
            ModifyDrawSetAfterTransformsEvent?.Invoke(drawinfo);
            orig(ref drawinfo);
        }

        private void AddDrawHooks(ILContext il)
        {
            ILCursor cursor = new(il);

            // Before non-solid tiles.
            if (cursor.TryGotoNext(MoveType.Before,
                i => i.MatchLdarg(0),
                i => i.MatchCall<Main>("DoDraw_Tiles_NonSolid")))
            {
                cursor.EmitDelegate(InvokeBehindNonSolidTiles);
            }

            // Before DrawPlayers_BehindNPCs.
            if (cursor.TryGotoNext(MoveType.Before,
                i => i.MatchLdsfld<Main>("player"),
                i => i.MatchLdsfld<Main>("myPlayer"),
                i => i.MatchLdelemRef(),
                i => i.MatchLdfld<Player>("detectCreature")))
            {
                cursor.EmitDelegate(InvokeBehindSolidTilesAndBackgroundNPCs);
            }

            // Right after DrawPlayers_BehindNPCs.
            if (cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchCall<Main>("DrawPlayers_BehindNPCs")))
            {
                cursor.EmitDelegate(InvokeAboveTiles);
            }
        }

        private void DrawBehindWallsHook(On_Main.orig_DoDraw_WallsAndBlacks orig, Main self)
        {
            DrawBehindWallsEvent?.Invoke();
            orig(self);
        }

        private void DrawBehindTilesHook(On_Main.orig_DoDraw_Tiles_Solid orig, Main self)
        {
            DrawBehindTilesEvent?.Invoke();
            orig(self);
        }

        private void DrawAboveNPCsHook(On_Main.orig_DoDraw_DrawNPCsOverTiles orig, Main self)
        {
            orig(self);
            DrawAboveNPCsEvent?.Invoke();
        }

        private void DrawAboveProjectilesHook(On_Main.orig_DrawProjectiles orig, Main self)
        {
            orig(self);
            DrawAboveProjectilesEvent?.Invoke();
        }

        private void DrawPlayersBehindNPCsHook(On_Main.orig_DrawPlayers_BehindNPCs orig, Main self)
        {
            PreDrawPlayersEvent?.Invoke(false);
            orig(self);
            DrawAbovePlayersEvent?.Invoke(false);
        }

        private void DrawPlayersAfterProjectilesHook(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
        {
            PreDrawPlayersEvent?.Invoke(true);
            orig(self);
            DrawAbovePlayersEvent?.Invoke(true);
        }

        private static void InvokeBehindNonSolidTiles() =>
            DrawBehindNonSolidTilesEvent?.Invoke();

        private static void InvokeBehindSolidTilesAndBackgroundNPCs() =>
            DrawBehindSolidTilesAndBackgroundNPCsEvent?.Invoke();

        private static void InvokeAboveTiles() =>
            DrawAboveTilesEvent?.Invoke();
    }
}