using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace MewoMod.Content.Items.Placeables
{
    public class MewOre : ModItem 
    {

        public override void SetStaticDefaults()
        {
            ItemID.Sets.SortingPriorityMaterials[Item.type] = 60;
        }

        public override void SetDefaults()
        {

            Item.DefaultToPlaceableTile(ModContent.TileType<Tiles.MewOreTile>());

			Item.width = 12;
			Item.height = 12;

			Item.value = 3000;
			Item.rare = ItemRarityID.Blue;
        

        }
    }
} 