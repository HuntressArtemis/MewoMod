using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MewoMod.Content.Items.Armor
{
	// The AutoloadEquip attribute automatically attaches an equip texture to this item.
	// Providing the EquipType.Body value here will result in TML expecting X_Arms.png, X_Body.png and X_FemaleBody.png sprite-sheet files to be placed next to the item's main texture.
	[AutoloadEquip(EquipType.Body)]
	public class MewoBreastplate : ModItem
	{
		public static readonly int SpeedBonus = 6;
		public static readonly int DamageIncrease = 30;

		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SpeedBonus, DamageIncrease);

		public override void SetDefaults() {
			Item.width = 18; // Width of the item
			Item.height = 18; // Height of the item
			Item.value = Item.sellPrice(gold: 1); // How many coins the item is worth
			Item.rare = ItemRarityID.Blue; // The rarity of the item
			Item.defense = 9; // The amount of defense the item will give when equipped
		}

		public override void UpdateEquip(Player player) {
			player.moveSpeed += SpeedBonus / 100f; 
			player.GetDamage(DamageClass.Generic) += DamageIncrease / 100f;
		}

		// Please see Content/ExampleRecipes.cs for a detailed explanation of recipe creation.
		public override void AddRecipes() {
			CreateRecipe()
                .AddIngredient(ModContent.ItemType<Placeables.MewoBar>(), 15)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}