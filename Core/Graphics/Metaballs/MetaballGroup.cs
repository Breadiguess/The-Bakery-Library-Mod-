using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace BreadLibrary.Core.Graphics.Metaballs
{
    public sealed class MetaballGroup
    {
        public Metaball Type { get; }

        public List<MetaballInstance> Instances { get; } = new();

        private readonly Vector4[] shaderBuffer;

        public MetaballGroup(Metaball type)
        {
            Type = type;

            int bufferSize = Math.Clamp(
                type.MaxShaderInstances,
                1,
                MetaballSystem.AbsoluteMaxShaderInstances
            );

            shaderBuffer = new Vector4[bufferSize];
        }

        public bool HasActiveInstances => Instances.Count > 0;

        public Vector4[] ShaderBuffer => shaderBuffer;

        public int ShaderBatchSize => shaderBuffer.Length;

        public void Add(
            Vector2 center,
            float radius,
            float strength = 1f,
            float opacity = 1f,
            int timeLeft = 1,
            Vector2 velocity = default,
            MetaballInstanceInitializer initializer = null)
        {
            MetaballInstance instance = new(center, radius, strength, opacity, timeLeft, velocity);

            initializer?.Invoke(ref instance);

            Instances.Add(instance);
        }

        public void Update()
        {
            for (int i = Instances.Count - 1; i >= 0; i--)
            {
                MetaballInstance instance = Instances[i];

                MetaballUpdateContext context = new(
                    this,
                    i,
                    Terraria.Main.GlobalTimeWrappedHourly
                );

                Type.UpdateInstance(ref instance, context);

                if (!instance.Active)
                {
                    Instances.RemoveAt(i);
                    continue;
                }

                Instances[i] = instance;
            }
        }

        public int FillShaderBuffer(Vector2 screenPosition)
        {
            int count = Math.Min(Instances.Count, shaderBuffer.Length);

            for (int i = 0; i < count; i++)
                shaderBuffer[i] = Instances[i].ToShaderData(screenPosition);

            return count;
        }
        public int FillShaderBufferVisible(
    Vector2 screenPosition,
    Vector2 screenSize,
    float padding = 16f)
        {
            int count = 0;

            for (int i = 0; i < Instances.Count && count < shaderBuffer.Length; i++)
            {
                MetaballInstance instance = Instances[i];

                if (!instance.IntersectsScreen(screenPosition, screenSize, padding))
                    continue;

                shaderBuffer[count] = instance.ToShaderData(screenPosition);
                count++;
            }

            return count;
        }
        public void Clear()
        {
            Instances.Clear();
        }
    }
}