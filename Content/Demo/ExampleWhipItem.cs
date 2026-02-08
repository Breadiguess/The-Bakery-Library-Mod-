namespace BreadLibrary.Content.Demo
{
    public class ExampleWhipItem : ModItem
    {
        public override string LocalizationCategory => "Items.Weapons.Summons.Whip";
        public override void SetDefaults()
        {
            Item.DefaultToWhip(ModContent.ProjectileType<ExampleWhipProjectile>(), 16, 5, 5);
            Item.useAnimation = 34;
            Item.useTime = 34;
            Item.shootSpeed = 5.6f;
            Item.rare = ItemRarityID.Green;
            Item.channel = true;
            Item.autoReuse = true;
        }

        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[Item.shoot] < 1;
        }
        public override bool MeleePrefix() => true;
    }
}
