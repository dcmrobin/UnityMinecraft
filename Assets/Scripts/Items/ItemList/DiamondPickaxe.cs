﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiamondPickaxe: Item
{
    public DiamondPickaxe()
    {
        this.itemName 				= "diamondPickaxe";
		this.itemTextureName 		= "diamond_pickaxe";
		this.placeable				= false;
		this.miningLevel			= MiningLevel.DIAMOND;
		this.toolType				= ToolType.PICKAXE;
		this.breakingSpeedModifier	= 8.0f;
		this.maxStack				= 1;
		this.hasGenericMesh			= true;
		this.LoadPrefab();
    }
}
