using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MewoMod.Content.Items.Accessories
{ 
    [AutoloadEquip(EquipType.Wings)]
	public class Mewings : ModItem
	{

        public override void SetStaticDefaults()
        {
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(3600, 30f, 5f, true, 1f, 1f);
        }

		public override void SetDefaults()
		{
			Item.width = 40;
			Item.height = 40;

			Item.value = Item.buyPrice(silver: 1);
			Item.rare = ItemRarityID.Blue;
            Item.accessory = true;


		}

        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling = 0.85f; // Falling glide speed
			ascentWhenRising = 0.15f; // Rising speed
			maxCanAscendMultiplier = 1f;
			maxAscentMultiplier = 3f;
			constantAscend = 0.135f;
        }


        public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.DirtBlock, 30);
			recipe.AddTile(TileID.WorkBenches);
			recipe.Register();
		}
	}
}
