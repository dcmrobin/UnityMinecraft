﻿using UnityEngine;
using UnityEngine.UI;
using Extensions;

public class Controller : MonoBehaviour
{
	public Text positionText;
	public enum Weather{Fair, Rainy, Foggy, Snowy, Thunder}
	public Weather weatherType;
	public GameObject player;
	public Color playercamBackgroundColor1;
	public Color playercamBackgroundColor2;

    // Start is called before the first frame update
    void Start()
    {
		this.positionText = GameObject.Find("UI/StaticWrapper/Position").GetComponent<Text>();

		GameObject.Find("Player").transform.Translate(new Vector3(512, 70, 512));

		//InventoryContainers.inventory.items[26] = new InventoryItem("woodShovel");
		//InventoryContainers.inventory.items[25] = new InventoryItem("woodPickaxe");
		//InventoryContainers.inventory.items[24] = new InventoryItem("stonePickaxe");
		//InventoryContainers.inventory.items[23] = new InventoryItem("stonePickaxe");
		//InventoryContainers.inventory.items[22] = new InventoryItem("diamondPickaxe");
		//InventoryContainers.inventory.items[21] = new InventoryItem("diamondPickaxe");
		//InventoryContainers.inventory.items[20] = new InventoryItem("ironPickaxe");
		//InventoryContainers.inventory.items[19] = new InventoryItem("ironPickaxe");
		//InventoryContainers.inventory.items[18] = new InventoryItem("goldPickaxe");
		//InventoryContainers.inventory.items[17] = new InventoryItem("goldPickaxe");

		MineCraftGUI.hotbar.UpdateGUI();
    }

    // Update is called once per frame
    void Update()
    {
		if (weatherType == Weather.Foggy)
		{

			RenderSettings.fog = true;

			//GameObject.FindObjectOfType<Light>().shadows = LightShadows.None;

			player.GetComponentInChildren<Camera>().backgroundColor = playercamBackgroundColor1;
		} else
		{

			RenderSettings.fog = false;

			//GameObject.FindObjectOfType<Light>().shadows = LightShadows.None;

			player.GetComponentInChildren<Camera>().backgroundColor = playercamBackgroundColor2;
		}
		Vector3Int p = Player.instance.GetVoxelPosition();
		ChunkPosition cp = Player.instance.GetVoxelChunk();
		this.positionText.text = System.String.Format("({0},{1},{2}) ({3},{4})", p.x, p.y, p.z, cp.x, cp.z);
    }

	/// <summary>
	/// Calls the given function after the given delay (in seconds).
	/// </summary>
	public void RunAfterDelay(System.Action callback, float delay)
	{
		StartCoroutine(DelayedRun(callback, delay));
	}

	private System.Collections.IEnumerator DelayedRun(System.Action callback, float delay)
	{
		yield return new WaitForSeconds(delay);

		callback();
	}
}
