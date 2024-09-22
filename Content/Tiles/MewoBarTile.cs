using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;


namespace MewoMod.Content.Tiles
{ 
	public class MewoBarTile : ModTile
	{

        public override void SetStaticDefaults() { 
			Main.tileShine[Type] = 975; 
			Main.tileSolid[Type] = true;
			Main.tileSolidTop[Type] = true;
            Main.tileFrameImportant[Type] = true;

			AddMapEntry(new Color(188, 0, 204), Language.GetText("MapObject.MetalBar"));

  
        }
	}
}