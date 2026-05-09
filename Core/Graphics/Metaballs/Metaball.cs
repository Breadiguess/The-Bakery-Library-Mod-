using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace BreadLibrary.Core.Graphics.Metaballs
{
  
    //please note; this is still heavily work in progress. all is subject to change on a whim.
    public abstract class Metaball : ModType
    {

        public string Key => $"{Mod.Name}:{GetType().FullName}";

        public virtual Color Color => Color.White;

        public virtual float Threshold => 1f;

       
        public virtual float EdgeSoftness => 0.04f;

        public virtual int MaxShaderInstances => 128;
        public MetaballLayer Layer => MetaballLayer.AboveTiles;

       
        public virtual Effect PostProcessEffect => null;

        public virtual void ApplyPostProcessParameters(Effect effect, MetaballDrawContext context)
        {
        }

        public virtual void ApplyShaderParameters(Effect effect, MetaballDrawContext context)
        {
        }


        public virtual void UpdateInstance(ref MetaballInstance instance, MetaballUpdateContext context)
        {
            instance.DefaultUpdate();
        }


        public sealed override void SetupContent()
        {
            SetStaticDefaults();
        }

        protected sealed override void Register()
        {
            ModTypeLookup<Metaball>.Register(this);
            MetaballRegistry.Register(this);
        }
    }
}