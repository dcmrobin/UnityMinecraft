﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodPickaxe: Item
{
    public WoodPickaxe()
    {
        this.itemName 				= "woodPickaxe";
		this.itemTextureName 		= "wooden_pickaxe";
		this.placeable				= false;
		this.miningLevel			= MiningLevel.WOOD;
		this.toolType				= ToolType.PICKAXE;
		this.breakingSpeedModifier	= 3.5f;
		this.maxStack				= 1;
		this.hasGenericMesh			= true;
		this.LoadPrefab();
    }
}
