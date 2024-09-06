using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace MewoMod.Content.Items.Consumables
{
    public class CatFood : ModItem 
    {

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useTime = 30;
			Item.useAnimation = 30;
            Item.maxStack = Item.CommonMaxStack;
            //Item.UseSound = SoundID.Item4;
            Item.UseSound = SoundID.Item57;

            Item.buffType = ModContent.BuffType<Buffs.MewoPilled>();
            Item.buffTime = 3600;

            Item.value = Item.buyPrice(silver: 1);
			Item.rare = ItemRarityID.Blue;
        

        }

        public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.DirtBlock, 20);
			recipe.AddTile(TileID.WorkBenches);
			recipe.Register();
		}
    }
} 