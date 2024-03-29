﻿using System.Collections.Generic;

public class OreCoal: Block
{
    public OreCoal(): base()
	{
		this.blockName 			= "oreCoal";
		this.textureName 		= "coal_ore";
		this.hardness 			= 6 * 20;
		this.maxStack 			= 64;
		this.dropsItself 		= false;
		this.toolTypeRequired 	= ToolType.PICKAXE;
		
		this.drops = new List<Drop>();
		this.drops.Add(new Drop("coal", 1, 1.0f));
	}
}
