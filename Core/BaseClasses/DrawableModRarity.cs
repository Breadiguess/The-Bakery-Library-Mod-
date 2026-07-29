using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.UI.Chat;

namespace BreadLibrary.Core.BaseClasses
{
    /// <summary>
    /// An implementable class that allows you to control the drawing of an item's rarity from within the ModRarity itself,
    /// without needing to create another rarity-specific GlobalItem that handles the drawing.
    /// </summary>
    public abstract class DrawableModRarity : ModRarity
    {
        private static readonly Dictionary<int, DrawableModRarity> RaritiesByType = [];

        internal static bool TryGetRarity(int rarityType, out DrawableModRarity rarity)
        {
            return RaritiesByType.TryGetValue(rarityType, out rarity);
        }

        /// <summary>
        /// Override this to set the rarity color of the item.
        /// </summary>
        public virtual Color GetRarityColor()
        {
            return Color.White;
        }

        /// <summary>
        /// Override this to set the outline color of the rarity text.
        /// </summary>
        public virtual Color BorderColor()
        {
            return Color.Black;
        }

        /// <summary>
        /// Override this to set the font used by this rarity.
        /// </summary>
        public virtual Asset<DynamicSpriteFont> GetFont()
        {
            return FontAssets.MouseText;
        }

        /// <summary>
        /// Override this if the rarity text is drawn larger or smaller than vanilla tooltip text.
        /// </summary>
        public virtual float GetScale()
        {
            return 1f;
        }

        /// <summary>
        /// Override this when your custom text draw has glow, wave offsets, trails, or other visual spillover.
        /// X affects tooltip width. Y can be used by custom draw code/yOffset, but width is usually the main issue.
        /// </summary>
        public virtual Vector2 GetLayoutPadding()
        {
            return Vector2.Zero;
        }

        /// <summary>
        /// Override this if the text you draw differs from item.AffixName().
        /// </summary>
        public virtual string GetNameText(Item item)
        {
            return item.AffixName();
        }

        public sealed override Color RarityColor => GetRarityColor();

        public sealed override void SetStaticDefaults()
        {
            RaritiesByType[Type] = this;
            SetRarityStaticDefaults();
        }

        public sealed override void Unload()
        {
            RaritiesByType.Remove(Type);
            UnloadRarity();
        }

        protected virtual void SetRarityStaticDefaults()
        {
        }

        protected virtual void UnloadRarity()
        {
        }

        internal void ModifyTooltipLayout(Item item, List<TooltipLine> tooltips)
        {
            for (int i = 0; i < tooltips.Count; i++)
            {
                TooltipLine line = tooltips[i];

                if (!IsItemNameLine(line))
                    continue;

                string realText = GetNameText(item);
                Vector2 desiredSize = MeasureItemName(item, realText);

                // Terraria measures the tooltip box before PreDrawTooltipLine runs.
                // Since we manually draw the real text later, this pads the measured vanilla text width
                // without changing what the player actually sees.
                line.Text = CreateVanillaMeasuredText(realText, desiredSize.X);

                return;
            }
        }

        public virtual bool PreDrawRarityTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (!IsItemNameLine(line))
                return true;

            string realText = GetNameText(item);
            Vector2 position = new(line.X, line.Y);

            DrawItemName(item, line, position, realText);

            return false;
        }

        protected virtual void DrawItemName(Item item, DrawableTooltipLine line, Vector2 position, string text)
        {
            Utils.DrawBorderStringFourWay(Main.spriteBatch, GetFont().Value, text, position.X, position.Y, GetRarityColor(), BorderColor(), Vector2.Zero, GetScale());
        }

        protected virtual Vector2 MeasureItemName(Item item, string text)
        {
            return ChatManager.GetStringSize(GetFont().Value, text, Vector2.One * GetScale()) + GetLayoutPadding();
        }

        private static bool IsItemNameLine(TooltipLine line)
        {
            return line.Mod == "Terraria" && line.Name == "ItemName";
        }

        private static bool IsItemNameLine(DrawableTooltipLine line)
        {
            return line.Mod == "Terraria" && line.Name == "ItemName";
        }

        private static string CreateVanillaMeasuredText(string visibleText, float desiredWidth)
        {
            DynamicSpriteFont vanillaFont = FontAssets.MouseText.Value;

            float currentWidth = ChatManager.GetStringSize(vanillaFont, visibleText, Vector2.One ).X;

            float missingWidth = desiredWidth - currentWidth;

            if (missingWidth <= 0f)
                return visibleText;

            float spaceWidth = ChatManager.GetStringSize(vanillaFont, " ", Vector2.One).X;

            if (spaceWidth <= 0f)
                return visibleText;

            int spacesNeeded = (int)MathF.Ceiling(missingWidth / spaceWidth);

            return visibleText + new string(' ', spacesNeeded);
        }
    }

    internal sealed class DrawableModRarityGlobalItem : GlobalItem
    {
        public override bool InstancePerEntity => false;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (!DrawableModRarity.TryGetRarity(item.rare, out DrawableModRarity rarity))
                return;

            rarity.ModifyTooltipLayout(item, tooltips);
        }

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (!DrawableModRarity.TryGetRarity(item.rare, out DrawableModRarity rarity))
                return true;

            return rarity.PreDrawRarityTooltipLine(item, line, ref yOffset);
        }
    }
}

