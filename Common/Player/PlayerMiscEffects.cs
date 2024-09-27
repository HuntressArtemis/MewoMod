using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.GameInput;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace MewoMod.Common.Player
{
    public class Player : ModPlayer
    {

        #region Post Update Misc Effects

        public override void PostUpdateMiscEffects() {
            #region Misc Effects
            bool holdingDown = Player.controlDown && !Player.controlJump;
            bool notInLiquid = !Player.wet;
            bool notOnRope = !Player.pulley && Player.ropeCount == 0;
            bool notGrappling = Player.grappling[0] == -1;
            bool airborne = Player.velocity.Y != 0;
            if (holdingDown && notInLiquid && notOnRope && notGrappling && airborne) { //Player cannot further increase their ridiculous gravity during a Gravistar Slam
                Player.velocity.Y += Player.gravity * Player.gravDir;

                if (Player.velocity.Y * Player.gravDir > Player.maxFallSpeed) {
                    Player.velocity.Y = Player.maxFallSpeed * Player.gravDir;
                }
            }
            #endregion
        }
        #endregion
    }
}    