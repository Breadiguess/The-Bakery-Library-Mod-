using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Core.ScreenShake
{
    public class ScreenShakeSystem : ModSystem
    {
        internal static readonly List<ScreenShakeInstance> ActiveShakes = new();

        public static Vector2 CurrentOffset { get; private set; }

        public override void PreUpdateEntities()
        {
            for (int i = ActiveShakes.Count - 1; i >= 0; i--)
            {
                ActiveShakes[i].Update();

                if (ActiveShakes[i].Dead)
                    ActiveShakes.RemoveAt(i);
            }
        }

        public override void ModifyScreenPosition()
        {
            Player player = Main.LocalPlayer;

            if (player is null || !player.active || Main.gameMenu)
            {
                CurrentOffset = Vector2.Zero;
                return;
            }

            Vector2 totalOffset = Vector2.Zero;

            for (int i = 0; i < ActiveShakes.Count; i++)
                totalOffset += ActiveShakes[i].GetOffset(player, i);

            // Clamp so stacked shakes do not get absurd.
            const float maxShakeDistance = 24f;
            if (totalOffset.Length() > maxShakeDistance)
                totalOffset = Vector2.Normalize(totalOffset) * maxShakeDistance;

            CurrentOffset = totalOffset;
            Main.screenPosition += CurrentOffset;
        }

        public static void Clear()
        {
            ActiveShakes.Clear();
            CurrentOffset = Vector2.Zero;
        }

        public static void Shake(
            float strength,
            int duration,
            float frequency = 0.35f,
            float dampingPower = 1.6f)
        {
            if (Main.dedServ || strength <= 0f || duration <= 0)
                return;

            ActiveShakes.Add(new ScreenShakeInstance(
                baseStrength: strength,
                duration: duration,
                worldPosition: null,
                radius: 0f,
                frequency: frequency,
                dampingPower: dampingPower,
                ignoreDistance: true));
        }

        public static void ShakeAt(
            Vector2 worldPosition,
            float strength,
            int duration,
            float radius = 1200f,
            float frequency = 0.35f,
            float dampingPower = 1.6f)
        {
            if (Main.dedServ || strength <= 0f || duration <= 0)
                return;

            ActiveShakes.Add(new ScreenShakeInstance(
                baseStrength: strength,
                duration: duration,
                worldPosition: worldPosition,
                radius: radius,
                frequency: frequency,
                dampingPower: dampingPower,
                ignoreDistance: false));
        }
    }
}
