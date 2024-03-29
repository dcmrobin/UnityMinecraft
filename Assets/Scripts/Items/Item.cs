﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Extensions;

public class Item: IInteractable
{
	/// <summary>
	/// The name of the item.
	/// </summary>
    public string itemName = "default";

	/// <summary>
	/// The 2D texture name of the item.
	/// </summary>
	public string itemTextureName = "default";

	/// <summary>
	/// The prefab associated with the item.
	/// </summary>
	public GameObject prefab;

	/// <summary>
	/// The Voxel world coordinates of the item to be placed.
	/// </summary>
	public Vector3 coordinates;

	/// <summary>
	/// The item's tool type.
	/// </summary>
	public ToolType toolType = ToolType.ANY;

	/// <summary>
	/// Whether the item uses the GenericItem prefab.
	/// </summary>
	public bool hasGenericMesh = false;

	/// <summary>
	/// Whether or not to render the item on the player's hand, to be animated.
	/// </summary>
	public bool renderWhenSelected = false;

	/// <summary>
	/// Whether the item is placeable or not.
	/// </summary>
	public bool placeable = false;

	/// <summary>
	/// Whether the item can be placed only on the top face of a block.
	/// </summary>
	public bool placeableOnlyOnTop = false;

	/// <summary>
	/// Determines whether this item can be placed on top of other items or not.
	/// </summary>
	public bool placeableOnOtherItems = false;

	/// <summary>
	/// Whether the item is non-empty and can be thus walked through.
	/// Non-empty items cannot be placed on top of other non-empty items.
	/// </summary>
	public bool nonEmpty = true;

	/// <summary>
	/// Whether the block drops itself when broken.
	/// </summary>
	public bool dropsItself = true;

	/// <summary>
	/// The level at which the IInteractable is breakable.
	/// </summary>
	private MiningLevel _miningLevel = MiningLevel.WOOD;
	/// <summary>
	/// The level at which the IInteractable is breakable.
	/// </summary>
	public MiningLevel miningLevel {
		get { return this._miningLevel; }
		set { this._miningLevel = value; }
	}

	/// <summary>
	/// The breaking speed multiplier. Target block hardness' gets divided by this amount.
	/// </summary>
	public float breakingSpeedModifier = 1.0f;

	/// <summary>
	/// Whether the item is interactable or not.
	/// </summary>
	private bool _interactable;
	/// <summary>
	/// Whether the item is interactable or not.
	/// </summary>
	public bool interactable { 
		get { return this._interactable; } 
		set{ this._interactable = value; }
	}

	/// <summary>
	/// Whether the item is smeltable or not.
	/// </summary>
	private bool _smeltable = false;
	/// <summary>
	/// Whether the item is smeltable or not.
	/// </summary>
	public bool smeltable {
		get { return this._smeltable; }
		set { this._smeltable = value; }
	}

	/// <summary>
	/// Item result when smelted.
	/// </summary>
	private CraftingResult? _smeltedResult;

	/// <summary>
	/// Item result when smelted.
	/// </summary>
	public CraftingResult? smeltedResult {
		get { return this._smeltedResult; }
		set { this._smeltedResult = value; }
	}

	/// <summary>
	/// Whether the item can be burned to produce heat or not.
	/// </summary>
	private bool _burnable = false;

	/// <summary>
	/// Whether the item can be burned to produce heat or not.
	/// </summary>
	public bool burnable {
		get { return this._burnable; }
		set { this._burnable = value; }
	}

	/// <summary>
	/// The number of ticks the fuel item lasts for as a burnable item.
	/// </summary>
	private int _burnTime = 300;
	/// <summary>
	/// The number of ticks the fuel item lasts for as a burnable item.
	/// </summary>
	public int burnTime {
		get { return this._burnTime; }
		set { this._burnTime = value; }
	}

	/// <summary>
	/// Maximum amount of items that can be aggregated in a single item slot.
	/// </summary>
	public int maxStack = 64;

	/// <summary>
	/// Loads the prefab associated to the item name.
	/// </summary>
	public void LoadPrefab()
	{
		if (this.hasGenericMesh)
		{
			this.prefab = Resources.Load<GameObject>("Prefabs/GenericItem");
			Texture2D texture = CachedResources.Load<Texture2D>("Textures/Items/" + itemTextureName);
			//Texture2D texture = CachedResources.Load<Texture2D>(System.String.Format("Textures/Items/{0}", this.itemTextureName));
			this.prefab.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial.mainTexture = texture;
		}
		else
			// Load the prefab.
			this.prefab = Resources.Load<GameObject>(System.String.Format("Prefabs/{0}", this.itemName));
	}

	/// <summary>
	/// Places the item on the world.
	/// </summary>
	public virtual Vector3? Place()
	{
		// Place the block in the middle.
		this.prefab.transform.position = this.coordinates + (0.5f, 0.0f, 0.5f).ToVector3();

		this.prefab.GetComponent<ItemObject>().itemName = this.itemName;

		// And instantiate the associated prefab.
		GameObject.Instantiate(this.prefab);

		return this.coordinates;
	}

	/// <summary>
	/// Breaks the item and removes it from the world.
	/// </summary>
	public virtual void Break()
	{
		if (this.dropsItself)
			Dropper.DropItem(this.itemName, this.coordinates);

		GameObject.Destroy(this.prefab);
	}

	public virtual void Interact() {}
}
