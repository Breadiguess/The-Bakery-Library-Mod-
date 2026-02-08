using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Core.BaseClasses.Whip
{
    public abstract class BaseWhipItem : ModItem
    {
        /// <summary>
        /// override to change the whipProjectile
        /// </summary>
        public virtual int WhipProj {  get; private set; }

        public override void SetDefaults()
        {
            SetWhip();
        }

        protected void SetWhip()
        {
            Item.DefaultToWhip(WhipProj, Item.damage, Item.knockBack, Item.shootSpeed, Item.useAnimation);
        }

        public sealed override bool MeleePrefix() => true;
    }
}
