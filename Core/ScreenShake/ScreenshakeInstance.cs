using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Core.ScreenShake
{
    public sealed class ScreenShakeInstance
    {
        public Vector2? WorldPosition;
        public float BaseStrength;
        public int TimeLeft;
        public int Lifetime;
        public float Radius;
        public float Frequency;
        public float DampingPower;
        public bool IgnoreDistance;

        private readonly float seedA;
        private readonly float seedB;

        public ScreenShakeInstance(
            float baseStrength,
            int duration,
            Vector2? worldPosition = null,
            float radius = 1200f,
            float frequency = 0.35f,
            float dampingPower = 1.6f,
            bool ignoreDistance = false)
        {
            BaseStrength = baseStrength;
            TimeLeft = duration;
            Lifetime = duration;
            WorldPosition = worldPosition;
            Radius = radius;
            Frequency = frequency;
            DampingPower = dampingPower;
            IgnoreDistance = ignoreDistance;

            seedA = Main.rand.NextFloat(0f, 1000f);
            seedB = Main.rand.NextFloat(0f, 1000f);
        }

        public bool Dead => TimeLeft <= 0 || BaseStrength <= 0f;

        public void Update()
        {
            TimeLeft--;
        }

        public Vector2 GetOffset(Player player, int index)
        {
            if (Dead)
                return Vector2.Zero;

            float progress = 1f - TimeLeft / (float)Lifetime;
            float fade = MathF.Pow(1f - progress, DampingPower);

            float distanceFactor = 1f;
            if (!IgnoreDistance && WorldPosition.HasValue)
            {
                float dist = Vector2.Distance(player.Center, WorldPosition.Value);
                if (dist >= Radius)
                    return Vector2.Zero;

                distanceFactor = 1f - dist / Radius;
                distanceFactor *= distanceFactor;
            }

            float finalStrength = BaseStrength * fade * distanceFactor;
            if (finalStrength <= 0.01f)
                return Vector2.Zero;

            float t = Main.GameUpdateCount * Frequency;

            float x = MathF.Sin(t + seedA + index * 0.731f);
            float y = MathF.Cos(t * 1.17f + seedB + index * 1.129f);

            Vector2 offset = new Vector2(x, y);
            if (offset != Vector2.Zero)
                offset = Vector2.Normalize(offset);

            return offset * finalStrength;
        }
    }
}
