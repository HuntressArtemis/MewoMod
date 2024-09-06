using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MewoMod.Content.Buffs
{
	public class MewoPilled : ModBuff
	{
		public static readonly int DamageBonus = 200;
        public static readonly int RegenerationBonus = 10;

		public override void Update(Player player, ref int buffIndex) {
			player.lifeRegen += RegenerationBonus;
            player.GetDamage(DamageClass.Generic) += DamageBonus / 100f;
		}
	}
}