using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace MewoMod.Content.Items.Placeables
{
    public class MewoBar : ModItem 
    {

        public override void SetStaticDefaults()
        {
            ItemID.Sets.SortingPriorityMaterials[Item.type] = 61;
        }

        public override void SetDefaults()
        {

            Item.DefaultToPlaceableTile(ModContent.TileType<Tiles.MewoBarTile>());

			Item.width = 20;
			Item.height = 20;

			Item.value = 9000;
			Item.rare = ItemRarityID.Blue;
        

        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<MewOre>(), 3)
                .AddTile(TileID.Furnaces)
                .Register();
        }
    }
} 