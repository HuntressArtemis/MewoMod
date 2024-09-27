using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MewoMod.Content.Projectiles
{
	public class LordMewoProjectile : ModProjectile
	{
		// Store the target NPC using Projectile.ai[2]
		private Player HomingTarget {
			get => Projectile.ai[2] == 0 ? null : Main.player[(int)Projectile.ai[2] - 1];
			set {
				Projectile.ai[2] = value == null ? 0 : value.whoAmI + 1;
			}
		}

		



		public override void SetDefaults() {
			Projectile.width = 14; // The width of projectile hitbox
			Projectile.height = 14; // The height of projectile hitbox

			Projectile.friendly = false; // Can the projectile deal damage to enemies?
			Projectile.hostile = true; // Can the projectile deal damage to the player?
			Projectile.ignoreWater = true; // Does the projectile's speed be influenced by water?
			Projectile.light = 1f; // How much light emit around the projectile
			Projectile.timeLeft = 600; // The live time for the projectile (60 = 1 second, so 600 is 10 seconds)

			

		}

		// Custom AI
        
		bool HasStartTimer;
		int HomingTimer;
		float DelayTimer = 0f;
		int MaxHomingTimer = 60;
	

		public override void AI() {

			float maxDetectRadius = 1000f; // The maximum radius at which a projectile can detect a target

			// First, we find a homing target if we don't have one
			if (HomingTarget == null) {
				HomingTarget = FindClosestNPC(maxDetectRadius);
			}


			// If we don't have a target, don't adjust trajectory
			if (HomingTarget == null)
				return;

			if (Projectile.ai[0] != 0) {	
				if (HasStartTimer == false) {
					HomingTimer -= (int)Projectile.ai[0];
					MaxHomingTimer += 60;
				}

				Projectile.velocity = Vector2.Zero;
				Projectile.ai[0]--;
				HasStartTimer = true;

				Projectile.netUpdate = true;
			}


			if (HasStartTimer && Projectile.ai[0] == 0) {
				Projectile.velocity = Vector2.Normalize(HomingTarget.Center - Projectile.Center) * Projectile.ai[1];
				HasStartTimer = false;
			}


			// A short delay to homing behavior after being fired
			if (DelayTimer < 10) {
				DelayTimer += 1;
				Projectile.netUpdate = true;
				return;
			}


			// If found, we rotate the projectile velocity in the direction of the target.
			// We only rotate by 3 degrees an update to give it a smooth trajectory. Increase the rotation speed here to make tighter turns

            if (HomingTimer < MaxHomingTimer) {
                float length = Projectile.velocity.Length();
                float targetAngle = Projectile.AngleTo(HomingTarget.Center);
                Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(targetAngle, MathHelper.ToRadians(1.5f)).ToRotationVector2() * length;
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            HomingTimer++;
		}

		// Finding the closest NPC to attack within maxDetectDistance range
		// If not found then returns null
		public Player FindClosestNPC(float maxDetectDistance) {
			Player closestNPC = null;

			// Using squared values in distance checks will let us skip square root calculations, drastically improving this method's speed.
			float sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;

			// Loop through all NPCs
			foreach (var target in Main.player) {
				// The DistanceSquared function returns a squared distance between 2 points, skipping relatively expensive square root calculations
				float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);
				// Check if it is within the radius
				if (sqrDistanceToTarget < sqrMaxDetectDistance) {
					sqrMaxDetectDistance = sqrDistanceToTarget;
					closestNPC = target;
				}
				
			}

			return closestNPC;
		}

		public bool IsValidTarget(Player target) {
			// This method checks that the NPC is:
			// 1. active (alive)
			// 2. chaseable (e.g. not a cultist archer)
			// 3. max life bigger than 5 (e.g. not a critter)
			// 4. can take damage (e.g. moonlord core after all it's parts are downed)
			// 5. hostile (!friendly)
			// 6. not immortal (e.g. not a target dummy)
			// 7. doesn't have solid tiles blocking a line of sight between the projectile and NPC
			return Collision.CanHit(Projectile.Center, 1, 1, target.position, target.width, target.height);
		}
		public override bool CanHitPlayer(Player target) => !HasStartTimer;
	}
}