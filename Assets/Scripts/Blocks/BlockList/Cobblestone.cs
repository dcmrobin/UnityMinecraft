﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cobblestone: Block
{
    public Cobblestone(): base()
	{
		this.blockName 			= "cobblestone";
		this.hardness 			= 6 * 40;
		this.smeltable 			= true;
		this.smeltedResult		= new CraftingResult("stone", 1);
		this.toolTypeRequired 	= ToolType.PICKAXE;
	}
}
