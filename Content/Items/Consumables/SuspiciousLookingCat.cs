using MewoMod.Content.NPCs.LordMewo;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;


namespace MewoMod.Content.Items.Consumables
{
    public class SuspiciousLookingCat : ModItem 
    {

        public override void SetStaticDefaults() {
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 13;
        }

        public override void SetDefaults() {
            Item.width = 32;
            Item.height = 32;

            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.maxStack = Item.CommonMaxStack;

            Item.value = Item.buyPrice(silver: 1);
			Item.rare = ItemRarityID.Blue;
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup) {
			itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossSpawners;
		}

        public override bool CanUseItem(Player player) {
			// If you decide to use the below UseItem code, you have to include !NPC.AnyNPCs(id), as this is also the check the server does when receiving MessageID.SpawnBoss.
			// If you want more constraints for the summon item, combine them as boolean expressions:
			//    return !Main.IsItDay() && !NPC.AnyNPCs(ModContent.NPCType<MinionBossBody>()); would mean "not daytime and no MinionBossBody currently alive"
			return !NPC.AnyNPCs(ModContent.NPCType<LordMewo>());
		}

		public override bool? UseItem(Player player) {
			if (player.whoAmI == Main.myPlayer) {
				// If the player using the item is the client
				// (explicitly excluded serverside here)
				SoundEngine.PlaySound(SoundID.Roar, player.position);

				int type = ModContent.NPCType<LordMewo>();

				if (Main.netMode != NetmodeID.MultiplayerClient) {
					// If the player is not in multiplayer, spawn directly
					NPC.SpawnOnPlayer(player.whoAmI, type);
				}
				else {
					// If the player is in multiplayer, request a spawn
					// This will only work if NPCID.Sets.MPAllowedEnemies[type] is true, which we set in MinionBossBody
					NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);
				}
			}

			return true;
		}

        public override void AddRecipes() {
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.LicenseCat, 1);
			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
    }
} 