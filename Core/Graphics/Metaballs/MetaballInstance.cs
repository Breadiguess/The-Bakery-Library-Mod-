using Microsoft.Xna.Framework;

namespace BreadLibrary.Core.Graphics.Metaballs
{
    /// <summary>
    /// A single runtime metaball/blob.
    /// This should mostly be data. Type-specific behavior belongs in Metaball.UpdateInstance.
    /// </summary>
    public struct MetaballInstance
    {
        public Vector2 Center;
        public Vector2 Velocity;

        public float InitialRadius;
        public float Radius;
        public float Strength;
        public float Opacity;

        /// <summary>
        /// Lifetime in ticks. If negative, the instance is treated as infinite.
        /// </summary>
        public int TimeLeft;

        public int MaxTimeLeft;



        public float[] ai = new float[4];

        public bool Active => TimeLeft != 0 && Radius > 0f && Opacity > 0f && Strength > 0f;

        public float LifetimeCompletion
        {
            get
            {
                if (MaxTimeLeft <= 0)
                    return 0f;

                return 1f - TimeLeft / (float)MaxTimeLeft;
            }
        }

        public float LifetimeRemaining
        {
            get
            {
                if (MaxTimeLeft <= 0)
                    return 1f;

                return TimeLeft / (float)MaxTimeLeft;
            }
        }

        public MetaballInstance(
            Vector2 center,
            float radius,
            float strength = 1f,
            float opacity = 1f,
            int timeLeft = 1,
            Vector2 velocity = default)
        {
            Center = center;
            Velocity = velocity;

            InitialRadius = radius;
            Radius = radius;
            Strength = strength;
            Opacity = opacity;

            TimeLeft = timeLeft;
            MaxTimeLeft = timeLeft;
            ai = new float[4];
            for(int i = 0; i < 4; i++)
            {
                ai[i] = 0;
            }
        }

        public void DefaultUpdate()
        {
            Center += Velocity;

            if (MaxTimeLeft > 0)
                Radius = InitialRadius * LifetimeRemaining;

            if (TimeLeft > 0)
                TimeLeft--;
        }

        public Vector4 ToShaderData(Vector2 screenPosition)
        {
            Vector2 screenCenter = Center - screenPosition;
            return new Vector4(screenCenter.X, screenCenter.Y, Radius, Strength * Opacity);
        }


        public bool IntersectsScreen(Vector2 screenPosition, Vector2 screenSize, float padding = 0f)
        {
            Vector2 screenCenter = Center - screenPosition;

            return screenCenter.X + Radius + padding >= 0f &&
                   screenCenter.X - Radius - padding <= screenSize.X &&
                   screenCenter.Y + Radius + padding >= 0f &&
                   screenCenter.Y - Radius - padding <= screenSize.Y;
        }
    }
    public delegate void MetaballInstanceInitializer(ref MetaballInstance instance);
}