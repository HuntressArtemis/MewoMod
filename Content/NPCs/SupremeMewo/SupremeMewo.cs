using MewoMod.Common.Systems;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using MewoMod.Content.Items.Consumables;
using MewoMod.Content.Tiles;

namespace MewoMod.Content.NPCs.SupremeMewo
{
	// The main part of the boss, usually referred to as "body"
	[AutoloadBossHead] // This attribute looks for a texture called "ClassName_Head_Boss" and automatically registers it as the NPC boss head icon
	public class SupremeMewo : ModNPC
	{

		// This code here is called a property: It acts like a variable, but can modify other things. In this case it uses the NPC.ai[] array that has four entries.
		// We use properties because it makes code more readable ("if (SecondStage)" vs "if (NPC.ai[0] == 1f)").
		// We use NPC.ai[] because in combination with NPC.netUpdate we can make it multiplayer compatible. Otherwise (making our own fields) we would have to write extra code to make it work (not covered here)
		public bool SecondStage {
			get => NPC.ai[0] == 1f;
			set => NPC.ai[0] = value ? 1f : 0f;
		}
		// If your boss has more than two stages, and since this is a boolean and can only be two things (true, false), consider using an integer or enum

		// More advanced usage of a property, used to wrap around to floats to act as a Vector2
		public Vector2 FirstStageDestination {
			get => new Vector2(NPC.ai[1], NPC.ai[2]);
			set {
				NPC.ai[1] = value.X;
				NPC.ai[2] = value.Y;
			}
		}

		// public int MinionMaxHealthTotal {
		// 	get => (int)NPC.ai[3];
		// 	set => NPC.ai[3] = value;
		// }

		// public int MinionHealthTotal { get; set; }

		// Auto-implemented property, acts exactly like a variable by using a hidden backing field
		public Vector2 LastFirstStageDestination { get; set; } = Vector2.Zero;

		private const int FirstStageTimerMax = 90;
		// This is a reference property. It lets us write FirstStageTimer as if it's NPC.localAI[1], essentially giving it our own name
		public ref float FirstStageTimer => ref NPC.localAI[1];

		// We could also repurpose FirstStageTimer since it's unused in the second stage, or write "=> ref FirstStageTimer", but then we have to reset the timer when the state switch happens
		public ref float SecondStageTimer => ref NPC.localAI[3];

		// Do NOT try to use NPC.ai[4]/NPC.localAI[4] or higher indexes, it only accepts 0, 1, 2 and 3!
		// If you choose to go the route of "wrapping properties" for NPC.ai[], make sure they don't overlap (two properties using the same variable in different ways), and that you don't accidently use NPC.ai[] directly


		public override void SetStaticDefaults() {

			// Add this in for bosses that have a summon item, requires corresponding code in the item (See MinionBossSummonItem.cs)
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			// Automatically group with other bosses
			NPCID.Sets.BossBestiaryPriority.Add(Type);

			// Specify the debuffs it is immune to. Most NPCs are immune to Confused.
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
			// This boss also becomes immune to OnFire and all buffs that inherit OnFire immunity during the second half of the fight. See the ApplySecondStageBuffImmunities method.

			// Influences how the NPC looks in the Bestiary
			NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers() {
				CustomTexturePath = "MewoMod/Assets/Textures/Bestiary/MewoBoss_Preview",
				PortraitScale = 0.6f, // Portrait refers to the full picture when clicking on the icon in the bestiary
				PortraitPositionYOverride = 0f,
			};
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
		}

		public override void SetDefaults() {
			NPC.width = 168;
			NPC.height = 100;
			NPC.damage = 30;
			NPC.defense = 10;
			NPC.lifeMax = 3500;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.value = Item.buyPrice(gold: 5);
			NPC.SpawnWithHigherTime(30);
			NPC.boss = true;
			NPC.npcSlots = 10f; // Take up open spawn slots, preventing random NPCs from spawning during the fight

			// Default buff immunities should be set in SetStaticDefaults through the NPCID.Sets.ImmuneTo{X} arrays.
			// To dynamically adjust immunities of an active NPC, NPC.buffImmune[] can be changed in AI: NPC.buffImmune[BuffID.OnFire] = true;
			// This approach, however, will not preserve buff immunities. To preserve buff immunities, use the NPC.BecomeImmuneTo and NPC.ClearImmuneToBuffs methods instead, as shown in the ApplySecondStageBuffImmunities method below.

			// Custom AI, 0 is "bound town NPC" AI which slows the NPC down and changes sprite orientation towards the target
			NPC.aiStyle = -1;

			// The following code assigns a music track to the boss in a simple way.
			if (!Main.dedServ) {
				Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/LordMewoMusic");
			}
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			// Sets the description of this NPC that is listed in the bestiary
			bestiaryEntry.Info.AddRange(new List<IBestiaryInfoElement> {
				new MoonLordPortraitBackgroundProviderBestiaryInfoElement(), // Plain black background
				new FlavorTextBestiaryInfoElement("Lord Mewo, the mightiest of all cats.")
			});
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			// Do NOT misuse the ModifyNPCLoot and OnKill hooks: the former is only used for registering drops, the latter for everything else

			// The order in which you add loot will appear as such in the Bestiary. To mirror vanilla boss order:
			// 1. Trophy
			// 2. Classic Mode ("not expert")
			// 3. Expert Mode (usually just the treasure bag)
			// 4. Master Mode (relic first, pet last, everything else inbetween)

			// Trophies are spawned with 1/10 chance
			//npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Placeables.Furniture.MewoBossTrophy>(), 10));

			// All the Classic Mode drops here are based on "not expert", meaning we use .OnSuccess() to add them into the rule, which then gets added
			LeadingConditionRule notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());

			// Notice we use notExpertRule.OnSuccess instead of npcLoot.Add so it only applies in normal mode
			// Boss masks are spawned with 1/7 chance
			//notExpertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<MinionBossMask>(), 7));

			// This part is not required for a boss and is just showcasing some advanced stuff you can do with drop rules to control how items spawn
			// We make 12-15 ExampleItems spawn randomly in all directions, like the lunar pillar fragments. Hereby we need the DropOneByOne rule,
			// which requires these parameters to be defined
			int itemType = ModContent.ItemType<Items.Placeables.MewoBar>();
			var parameters = new DropOneByOne.Parameters() {
				ChanceNumerator = 1,
				ChanceDenominator = 1,
				MinimumStackPerChunkBase = 1,
				MaximumStackPerChunkBase = 1,
				MinimumItemDropsCount = 12,
				MaximumItemDropsCount = 15,
			};

			notExpertRule.OnSuccess(new DropOneByOne(itemType, parameters));

			// Finally add the leading rule
			npcLoot.Add(notExpertRule);

			// Add the treasure bag using ItemDropRule.BossBag (automatically checks for expert mode)
			npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<LordMewoTreasureBag>()));

			// ItemDropRule.MasterModeCommonDrop for the relic
			npcLoot.Add(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<Items.Placeables.Furniture.LordMewoRelic>()));

			// ItemDropRule.MasterModeDropOnAllPlayers for the pet
			//npcLoot.Add(ItemDropRule.MasterModeDropOnAllPlayers(ModContent.ItemType<MinionBossPetItem>(), 4));
		}

		public override void OnKill() {
			// The first time this boss is killed, spawn MewOre into the world. This code is above SetEventFlagCleared because that will set DownedMewoBoss to true.
			if (!DownedBossSystem.DownedMewoBoss) {
				ModContent.GetInstance<MewOreSystem>().BlessWorldWithMewOre();
			}

			// This sets DownedMewoBoss to true, and if it was false before, it initiates a lantern night
			NPC.SetEventFlagCleared(ref DownedBossSystem.DownedMewoBoss, -1);

			// Since this hook is only ran in singleplayer and serverside, we would have to sync it manually.
			// Thankfully, vanilla sends the MessageID.WorldData packet if a BOSS was killed automatically, shortly after this hook is ran

			// If your NPC is not a boss and you need to sync the world (which includes ModSystem, check DownedBossSystem), use this code:
			/*
			if (Main.netMode == NetmodeID.Server) {
				NetMessage.SendData(MessageID.WorldData);
			}
			*/
		}

		public override void BossLoot(ref string name, ref int potionType) {
			// Here you'd want to change the potion type that drops when the boss is defeated. Because this boss is early pre-hardmode, we keep it unchanged
			// (Lesser Healing Potion). If you wanted to change it, simply write "potionType = ItemID.HealingPotion;" or any other potion type
			potionType = ItemID.HealingPotion;
		}

		public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
			cooldownSlot = ImmunityCooldownID.Bosses; // use the boss immunity cooldown counter, to prevent ignoring boss attacks by taking damage from other sources
			return true;
		}

		// public override void FindFrame(int frameHeight) {
		// 	// This NPC animates with a simple "go from start frame to final frame, and loop back to start frame" rule
		// 	// In this case: First stage: 0-1-2-0-1-2, Second stage: 3-4-5-3-4-5, 5 being "total frame count - 1"
		// 	int startFrame = 0;
		// 	int finalFrame = 1;
			


		// 	int frameSpeed = 15;
		// 	NPC.frameCounter += 0.5f;
		// 	if (NPC.frameCounter > frameSpeed) {
		// 		NPC.frameCounter = 0;
		// 		NPC.frame.Y += frameHeight;

		// 		if (NPC.frame.Y > finalFrame * frameHeight) {
		// 			NPC.frame.Y = startFrame * frameHeight;
		// 		}
		// 	}
		// }

		public override void HitEffect(NPC.HitInfo hit) {
			// If the NPC dies, spawn gore and play a sound
			if (Main.netMode == NetmodeID.Server) {
				// We don't want Mod.Find<ModGore> to run on servers as it will crash because gores are not loaded on servers
				return;
			}

			if (NPC.life <= 0) {
				// These gores work by simply existing as a texture inside any folder which path contains "Gores/"
				int backGoreType = Mod.Find<ModGore>("MewoBossBody_Back").Type;
				int frontGoreType = Mod.Find<ModGore>("MewoBossBody_Front").Type;
				

				var entitySource = NPC.GetSource_Death();

				
				Gore.NewGore(entitySource, NPC.position, new Vector2(Main.rand.Next(-6, 7), Main.rand.Next(-6, 7)), backGoreType);
				Gore.NewGore(entitySource, NPC.position, new Vector2(Main.rand.Next(-6, 7), Main.rand.Next(-6, 7)), frontGoreType);
				

				SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

				// This adds a screen shake (screenshake) similar to Deerclops
				PunchCameraModifier modifier = new PunchCameraModifier(NPC.Center, (Main.rand.NextFloat() * ((float)Math.PI * 2f)).ToRotationVector2(), 20f, 6f, 20, 1000f, FullName);
				Main.instance.CameraModifiers.Add(modifier);
			}
		}

		public override void AI() {
			// This should almost always be the first code in AI() as it is responsible for finding the proper player target
			if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active) {
				NPC.TargetClosest();
			}

			Player player = Main.player[NPC.target];

			if (player.dead) {
				// If the targeted player is dead, flee
				NPC.velocity.Y -= 0.04f;
				// This method makes it so when the boss is in "despawn range" (outside of the screen), it despawns in 10 ticks
				NPC.EncourageDespawn(10);
				return;
			}

			CheckSecondStage();


			if (SecondStage) {
				DoSecondStage(player);
			}
			else {
				DoFirstStage(player);
			}
		}



		private void CheckSecondStage() {
			if (SecondStage) {
				// No point checking if the NPC is already in its second stage
				return;
			}

			if (NPC.life < NPC.lifeMax * 0.7f) {
				// If the boss is half hp, we initiate the second stage, and notify other players that this NPC has reached its second stage
				// by setting NPC.netUpdate to true in this tick. It will send important data like position, velocity and the NPC.ai[] array to all connected clients

				// Because SecondStage is a property using NPC.ai[], it will get synced this way
				SecondStage = true;
				NPC.netUpdate = true;
			}
		}

		private void DoFirstStage(Player player) {
			// Each time the timer is 0, pick a random position a fixed distance away from the player but towards the opposite side
			// The NPC moves directly towards it with fixed speed, while displaying its trajectory as a telegraph

			FirstStageTimer++;
			if (FirstStageTimer > FirstStageTimerMax) {
				FirstStageTimer = 0;
			}

			float distance = 200; // Distance in pixels behind the player

			if (FirstStageTimer == 0) {
				Vector2 fromPlayer = NPC.Center - player.Center;

				if (Main.netMode != NetmodeID.MultiplayerClient) {
					// Important multiplayer consideration: drastic change in behavior (that is also decided by randomness) like this requires
					// to be executed on the server (or singleplayer) to keep the boss in sync

					float angle = fromPlayer.ToRotation();
					float twelfth = MathHelper.Pi / 6;

					angle += MathHelper.Pi + Main.rand.NextFloat(-twelfth, twelfth);
					if (angle > MathHelper.TwoPi) {
						angle -= MathHelper.TwoPi;
					}
					else if (angle < 0) {
						angle += MathHelper.TwoPi;
					}

					Vector2 relativeDestination = angle.ToRotationVector2() * distance;

					FirstStageDestination = player.Center + relativeDestination;
					NPC.netUpdate = true;
				}
			}

			

			// Move along the vector
			Vector2 toDestination = FirstStageDestination - NPC.Center;
			Vector2 toDestinationNormalized = toDestination.SafeNormalize(Vector2.UnitY);

			float speed = Math.Min(distance, toDestination.Length());
			NPC.velocity = toDestinationNormalized * speed / 30;

			if (FirstStageDestination != LastFirstStageDestination) {
				// If destination changed
				NPC.TargetClosest(); // Pick the closest player target again

				// "Why is this not in the same code that sets FirstStageDestination?" Because in multiplayer it's ran by the server.
				// The client has to know when the destination changes a different way. Keeping track of the previous ticks' destination is one way
				if (Main.netMode != NetmodeID.Server) {
					// For visuals regarding NPC position, netOffset has to be concidered to make visuals align properly
					NPC.position += NPC.netOffset;

					// Draw a line between the NPC and its destination, represented as dusts every 20 pixels
					Dust.QuickDustLine(NPC.Center + toDestinationNormalized * NPC.width, FirstStageDestination, toDestination.Length() / 20f, Color.Yellow);

					NPC.position -= NPC.netOffset;
				}
			}
			LastFirstStageDestination = FirstStageDestination;


			NPC.rotation = NPC.velocity.ToRotation() - MathHelper.PiOver2;
		}
		float rotationTimer;
		int SecondStagetimer;
		int ShotCounter;
		bool Dashing;
		int DashTimer;
		bool ClockWise = true;
		float r = 300f;
		// bool MovingCloser = true;
		private void DoSecondStage(Player player) {
			SecondStageTimer++;

			// if (MovingCloser) {
			// 	r--;
			// }
			// else {
			// 	r++;
			// }
			

			if (Dashing) {
				DashTimer++;
			}
			
			//1 in 6 chance to turn around every second
			if (SecondStageTimer % 60 == 0) {
				if (Main.rand.NextBool(6)) {
					ClockWise = !ClockWise;
					NPC.netUpdate = true;


				}
            
            if (SecondStageTimer % 600 == 0) {
                
            }

			}

			float RotationSpeed = Utils.Clamp((float)NPC.life / NPC.lifeMax, 0.5f, 0.75f) / 0.75f;
			//Increment rotationtimer so it takes 3 seconds (180 ticks) to complete a full circle/reach 2pi (times 1 at the start, times 0.5 at 33%health)
			//takes ClockWise variable into account

			if (ClockWise && !Dashing) {
				rotationTimer += MathHelper.TwoPi / (180f * RotationSpeed);
			}
			if (!ClockWise && !Dashing) {
				rotationTimer -= MathHelper.TwoPi / (180f * RotationSpeed);
			}
			

			if (rotationTimer > MathHelper.TwoPi) {
				rotationTimer -= MathHelper.TwoPi;
			}

			if (rotationTimer < 0) {
				rotationTimer += MathHelper.TwoPi;
			}

			if (NPC.life < NPC.lifeMax * 0.75f) {
				ApplySecondStageBuffImmunities();
			}

			// if (r == 500f) {
			// 	MovingCloser = true;
			// 	NPC.netUpdate = true;
			// }
			// else if (r == 200f) {
			// 	MovingCloser = false;
			// 	NPC.netUpdate = true;
			// }

			Vector2 ToPlayer = player.Center - NPC.Center;
			Vector2 ToPlayerNormalized = ToPlayer.SafeNormalize(Vector2.UnitY);


			

			//float DistanceToCircle = Math.Abs(ToPlayer.Length() - r);

			
			

			
			Vector2 NextPosition = player.Center + new Vector2((float)Math.Cos(rotationTimer) * r, (float)Math.Sin(rotationTimer) * r);
			Vector2 ToNextPosition = NextPosition - NPC.Center;
			Vector2 ToNPC = NPC.Center - player.Center;
			Vector2 ToNextPositionNormalized = ToNextPosition.SafeNormalize(Vector2.UnitY);
			//only circle if not dashing
			if (Dashing != true) {
				NPC.velocity = ToNextPositionNormalized * (8f / RotationSpeed);
			}


			//choose closest circle position instead if next position is too far away
			float ToNPCRotation = ToNPC.ToRotation();
			Vector2 ClosestCirclePosition = player.Center + new Vector2((float)Math.Cos(ToNPCRotation) * r, (float)Math.Sin(ToNPCRotation) * r);
			Vector2 ToClosestCirclePosition = ClosestCirclePosition - NPC.Center;
			if (ToClosestCirclePosition.Length() < ToNextPosition.Length() - 200f) {
				rotationTimer = ToNPCRotation;
			}

			

			







		


			







			//Shooting projectiles
			var entitySource = NPC.GetSource_FromAI();
			//if classic
			if (!Main.expertMode && SecondStageTimer % 90 == 0 && Main.netMode != NetmodeID.MultiplayerClient) {
				ShotCounter++;
				if (ShotCounter < 4) {
					Projectile.NewProjectile(entitySource, NPC.Center, ToPlayerNormalized * 7.5f, ModContent.ProjectileType<Projectiles.LordMewoProjectile>(), 15, 5f, Main.myPlayer);
				}
			}
			//if expert and life above 40%
			else if (Main.expertMode && NPC.life > NPC.lifeMax * 0.4f && SecondStageTimer % 90 == 0 && Main.netMode != NetmodeID.MultiplayerClient) {
				ShotCounter++;
				if (ShotCounter < 4) {
					Projectile.NewProjectile(entitySource, NPC.Center, ToPlayerNormalized * 7.5f, ModContent.ProjectileType<Projectiles.LordMewoProjectile>(), 15, 5f, Main.myPlayer);
				}
			}
			//if expert and life above 20% and below 40%
			else if (Main.expertMode && NPC.life > NPC.lifeMax * 0.2f && NPC.life < NPC.lifeMax * 0.4f && SecondStageTimer % 75 == 0 && Main.netMode != NetmodeID.MultiplayerClient) {
				ShotCounter++;
				if (ShotCounter < 4) {
					Projectile.NewProjectile(entitySource, NPC.Center, ToPlayerNormalized * 7.5f, ModContent.ProjectileType<Projectiles.LordMewoProjectile>(), 15, 5f, Main.myPlayer);
				}
			}
			//if expert and life below 20%
			else if (Main.expertMode && NPC.life < NPC.lifeMax * 0.2f && SecondStageTimer % 45 == 0 && Main.netMode != NetmodeID.MultiplayerClient) {
				ShotCounter++;
				if (ShotCounter < 4) {
					Projectile.NewProjectile(entitySource, NPC.Center, ToPlayerNormalized * 7.5f, ModContent.ProjectileType<Projectiles.LordMewoProjectile>(), 15, 5f, Main.myPlayer);
				}
			}

			//Dashing
			if (ShotCounter == 4) {
				ShotCounter = 0;
				Dashing = true;
				NPC.velocity = ToPlayerNormalized * 15f;
				rotationTimer += MathHelper.Pi;
				NPC.netUpdate = true;
			}

			if (DashTimer == 50) {
				Dashing = false;
				DashTimer = 0;
				NPC.netUpdate = true;
			}



			// // The NPC tries to go towards the offsetX position, but most likely it will never get there exactly, or close to if the player is moving
			// // This checks if the npc is "70% there", and then changes direction
			// float changeDirOffset = offsetX * 0.7f;

			// if (NPC.direction == -1 && NPC.Center.X - changeDirOffset < abovePlayer.X ||
			// 	NPC.direction == 1 && NPC.Center.X + changeDirOffset > abovePlayer.X) {
			// 	NPC.direction *= -1;
			// }

			// float speed = 8f;
			// float inertia = 40f;

			// // If the boss is somehow below the player, move faster to catch up
			// if (NPC.Top.Y > player.Bottom.Y) {
			// 	speed = 12f;
			// }

			// Vector2 moveTo = toAbovePlayerNormalized * speed;
			// NPC.velocity = (NPC.velocity * (inertia - 1) + moveTo) / inertia;

			
			NPC.rotation = ToPlayer.ToRotation() - MathHelper.PiOver2;

			if (!ClockWise) {
				NPC.rotation -= MathHelper.Pi;
			}
			
		}

		// private void DoSecondStage_SpawnEyes(Player player) {
		// 	// At 100% health, spawn every 90 ticks
		// 	// Drops down until 33% health to spawn every 30 ticks
		// 	float timerMax = Utils.Clamp((float)NPC.life / NPC.lifeMax, 0.33f, 1f) * 90;

		// 	SecondStageTimer_SpawnEyes++;
		// 	if (SecondStageTimer_SpawnEyes > timerMax) {
		// 		SecondStageTimer_SpawnEyes = 0;
		// 	}

		// 	if (NPC.HasValidTarget && SecondStageTimer_SpawnEyes == 0 && Main.netMode != NetmodeID.MultiplayerClient) {
		// 		// Spawn projectile randomly below player, based on horizontal velocity to make kiting harder, starting velocity 1f upwards
		// 		// (The projectiles accelerate from their initial velocity)

		// 		float kitingOffsetX = Utils.Clamp(player.velocity.X * 16, -100, 100);
		// 		Vector2 position = player.Bottom + new Vector2(kitingOffsetX + Main.rand.Next(-100, 100), Main.rand.Next(50, 100));

		// 		int type = ModContent.ProjectileType<MinionBossEye>();
		// 		int damage = NPC.damage / 2;
		// 		var entitySource = NPC.GetSource_FromAI();

		// 		Projectile.NewProjectile(entitySource, position, -Vector2.UnitY, type, damage, 0f, Main.myPlayer);
		// 	}
		// }

		private void ApplySecondStageBuffImmunities() {
			if (NPC.buffImmune[BuffID.OnFire]) {
				return;
			}
			// Halfway through stage 2, this boss becomes immune to the OnFire buff.
			// This code will only run once because of the !NPC.buffImmune[BuffID.OnFire] check.
			// If you make a similar check for just a life percentage in a boss, you will need to use a bool to track if the corresponding code has run yet or not.
			NPC.BecomeImmuneTo(BuffID.OnFire);

			// Finally, this boss will clear all the buffs it currently has that it is now immune to. ClearImmuneToBuffs should not be run on multiplayer clients, the server has authority over buffs.
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				NPC.ClearImmuneToBuffs(out bool anyBuffsCleared);

				if (anyBuffsCleared) {
					// Since we cleared some fire related buffs, spawn some smoke to communicate that the fire buffs have been extinguished.
					// This example is commented out because it would require a ModPacket to manually sync in order to work in multiplayer.
					/* for (int g = 0; g < 8; g++) {
						Gore gore = Gore.NewGoreDirect(NPC.GetSource_FromThis(), NPC.Center, default, Main.rand.Next(61, 64), 1f);
						gore.scale = 1.5f;
						gore.velocity += new Vector2(1.5f, 0).RotatedBy(g * MathHelper.PiOver2);
					}*/
				}
			}

			// Spawn a ring of dust to communicate the change.
			for (int loops = 0; loops < 2; loops++) {
				for (int i = 0; i < 50; i++) {
					Vector2 speed = Main.rand.NextVector2CircularEdge(1f, 1f);
					Dust d = Dust.NewDustPerfect(NPC.Center, DustID.BlueCrystalShard, speed * 10 * (loops + 1), Scale: 1.5f);
					d.noGravity = true;
				}
			}
		}
	}
}