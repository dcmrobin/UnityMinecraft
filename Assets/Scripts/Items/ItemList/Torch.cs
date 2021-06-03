﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Torch: Item
{
    public Torch()
    {
        this.itemName 				= "torch";
		this.itemTextureName 		= "torch";
		this.placeable				= true;
		this.placeableOnlyOnTop 	= true;
		this.placeableOnOtherItems 	= false;
		this.LoadPrefab();
    }
}
