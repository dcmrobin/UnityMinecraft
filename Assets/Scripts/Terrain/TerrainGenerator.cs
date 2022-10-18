﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Extensions;
using System.Threading;
using System;
using System.IO;
using UnityEngine.UI;
public class TerrainGenerator : MonoBehaviour
{
	/**
	* Terrain is generated based on Simplex & Perlin noise.
	* Based on the "renderDistance" and player position, generate individual chunks and place them in PCTerrain.
	*/

	// Private instance of the FastNoise library by Jordan Peck.
	//public int seed = 1337;
	public enum Biomes{Forest, Plains, Village, Mountains, Desert}
    public Biomes biomes;
	public Text renderText;
	private FastNoise noise;

	/// <summary>
	/// Stores the previous player position.
	/// </summary>
	private ChunkPosition previousPlayerPosition;

	private int chunkMatrixSize = 5;

	public int chunkRenderDistance = 5;

	/// <summary>
	/// Used to notify the main thread when a chunk has been generated to rebuild chunk meshes.
	/// </summary>
	private delegate void ChunkBatchGenerated();

	/// <summary>
	/// Handlers that subscribed to the ChunkGenerated event.
	/// </summary>
	private ChunkBatchGenerated chunkBatchGeneratedHandlers;

	void Start()
	{
		this.noise = new FastNoise();
		this.noise.SetSeed(1337);//USE THIS TO SET THE SEED
		//this.noise.SetSeed(UnityEngine.Random.Range(0, 9999));//USE THIS TO RANDOMIZE THE SEED

		this.GenerateStartingTerrain();
		this.previousPlayerPosition = Player.instance.GetVoxelChunk();
		this.chunkBatchGeneratedHandlers += this.ChunkGenerationCompleted;
	}

	void Update()
	{
		ChunkPosition currentPlayerPosition = Player.instance.GetVoxelChunk();

		if (this.previousPlayerPosition == currentPlayerPosition)
			return;
		
		chunkRenderDistance = int.Parse(renderText.text);

		GameObject[] chunks = GameObject.FindGameObjectsWithTag("chunk");
		foreach (GameObject _chunk in chunks)
		{
			if (
				Mathf.Abs(_chunk.transform.position.x / Chunk.chunkSize - currentPlayerPosition.x) >= this.chunkRenderDistance ||
				Mathf.Abs(_chunk.transform.position.z / Chunk.chunkSize - currentPlayerPosition.z) >= this.chunkRenderDistance
			)
			{
				ChunkPosition chunkPos = new ChunkPosition(
					(int)_chunk.transform.position.x / Chunk.chunkSize, 
					(int)_chunk.transform.position.z / Chunk.chunkSize
				);

				if (PCTerrain.GetInstance().chunks.ContainsKey(chunkPos))
					PCTerrain.GetInstance().chunks.Remove(chunkPos);

				GameObject.Destroy(_chunk);
			}
		}

		List<ChunkPosition> chunksToAdd = new List<ChunkPosition>();
		for (int i = -this.chunkRenderDistance / 2; i < this.chunkRenderDistance / 2; i++)
			for (int k = -this.chunkRenderDistance / 2; k < this.chunkRenderDistance / 2; k++)
			{
				ChunkPosition chunkPos = new ChunkPosition(currentPlayerPosition.x - i, currentPlayerPosition.z - k);
				if (!PCTerrain.GetInstance().chunks.ContainsKey(chunkPos))
					chunksToAdd.Add(chunkPos);
			}

		StartCoroutine(this.GenerateChunkPoolAsync(chunksToAdd.ToArray()));

		this.previousPlayerPosition = currentPlayerPosition;
	}

	/// <summary>
	/// Called when a thread finished generating a chunk.
	/// </summary>
	private void ChunkGenerationCompleted() {}

	public void GenerateStartingTerrain()
	{
		foreach(Chunk chunk in PCTerrain.GetInstance().chunks.Values)
			chunk.Destroy();

		System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

		ChunkPosition playerChunkPos = Player.instance.GetVoxelChunk();
		int halvedChunkSize = chunkMatrixSize / 2;

		for (int _i = playerChunkPos.x - halvedChunkSize; _i < playerChunkPos.x + halvedChunkSize; _i++)
			for (int _k = playerChunkPos.z - halvedChunkSize; _k < playerChunkPos.z + halvedChunkSize; _k++)
			{
				Chunk chunk = new Chunk();
				chunk.x = _i;
				chunk.z = _k;

				BaseBlock[,,] blocks;
				this.GenerateChunkBlocks(_i, _k, out blocks);

				chunk.blocks = blocks;

				StartCoroutine(chunk.BuildMeshAsync());

				PCTerrain.GetInstance().chunks[(chunk.x, chunk.z)] = chunk;
			}
		
		stopwatch.Stop();

		Debug.Log("Terrain generated in: " + stopwatch.Elapsed.TotalMilliseconds + "ms");
	}

	/// <summary>
	/// 
	/// </summary>
	
	public IEnumerator GenerateChunkPoolAsync(ChunkPosition[] chunkPositions, System.Action callback = null)
	{
		List<Coroutine> coroutinePool = new List<Coroutine>();

		foreach(ChunkPosition chunkPos in chunkPositions)
			coroutinePool.Add(StartCoroutine(this.GenerateChunkAsync(chunkPos.x, chunkPos.z)));

		foreach(Coroutine c in coroutinePool)
			yield return c;

		if (this.chunkBatchGeneratedHandlers != null)
			this.chunkBatchGeneratedHandlers();

		if (callback != null)
			callback();
	}

	private enum ThreadStatus { STARTED, FINISHED };
	private Dictionary<ChunkPosition, ThreadStatus> status = new Dictionary<ChunkPosition, ThreadStatus>();
	private IEnumerator GenerateChunkAsync(int x, int z)
	{
		Chunk chunk = new Chunk();
		chunk.x = x;
		chunk.z = z;

		ChunkPosition chunkPos = new ChunkPosition(x,z);
		this.status[chunkPos] = ThreadStatus.STARTED;
		
		BaseBlock[,,] blocks = null;

		Thread chunkBlocksGenerationThread = new Thread(() => {
			this.GenerateChunkBlocks(x, z, out blocks);
			this.status[chunkPos] = ThreadStatus.FINISHED;
		});
		chunkBlocksGenerationThread.Start();

		while(this.status[chunkPos] == ThreadStatus.STARTED)
			yield return new WaitForEndOfFrame();
		
		chunk.blocks = blocks;

		StartCoroutine(chunk.BuildMeshAsync());

		PCTerrain.GetInstance().chunks[(chunk.x, chunk.z)] = chunk;
	}

	/// <summary>
	/// Generates a chunk's blocks. The chunk is identified by an (x,y) position. Generated blocks are inserted into the `blocks` out parameter.
	/// </summary>
	private void GenerateChunkBlocks(int x, int z, out BaseBlock[,,] blocks)
	{
		System.Random random = new System.Random(x * z * 4096);

		blocks = new BaseBlock[Chunk.chunkSize, Chunk.chunkHeight, Chunk.chunkSize];

		for (int i = 0; i < Chunk.chunkSize; i++)
			for (int j = 0; j < Chunk.chunkHeight; j++)
				for (int k = 0; k < Chunk.chunkSize; k++)
				{
					BaseBlock block = this.GenerateTerrainBlockType(i + x * 16, j, k + z * 16);
					blocks[i,j,k] = block;

					if (block.blockName == "stone")
					{
						string oreBlockName = this.GenerateOres(random, j);
						if (oreBlockName != "stone")
						{
							BaseBlock oreBlock = Registry.Instantiate(oreBlockName) as BaseBlock;
							blocks[i,j,k] = oreBlock;
						} 
					}

					if (blocks[i,j,k].stateful)
						PCTerrain.GetInstance().blocks[(i,j,k).ToVector3Int()] = Registry.Instantiate(block.blockName) as Block;
				}

		if (biomes == Biomes.Forest)
		{
			this.GenerateTrees(x, z, blocks);
		}
	}

	/// <summary>
	/// Given the (i,j,k) space coordinates, generates the single seeded BaseBlock.
	/// </summary>
	private BaseBlock GenerateTerrainBlockType(int i, int j, int k)
	{
		float landSimplex1;

		if (biomes == Biomes.Village)
		{
			landSimplex1 = this.noise.GetSimplex(
				i * 0.8f, 
				k * 0.8f
			) - 2;
		} else if (biomes == Biomes.Mountains) 
		{
			landSimplex1 = this.noise.GetSimplex(
				i * 0.8f, 
				k * 0.8f
			) * 24;
		}
		else {
			landSimplex1 = this.noise.GetSimplex(
				i * 0.8f, 
				k * 0.8f
			) * 10;
		}

		float landSimplex2 = this.noise.GetSimplex(
			i * 3.5f, 
			k * 3.5f
		) * 10f * (this.noise.GetSimplex(
			i * .35f,
			k * .35f
		) + .3f);

		float stoneSimplex1 = this.noise.GetSimplex(
			i * 2f,
			k * 2f
		) * 10;//makes the stone humps more defined

		float stoneSimplex2 = (this.noise.GetSimplex(
			i * 2.1f,
			k * 2.1f
		) + .7f) * 20 * this.noise.GetSimplex(
			i * .2f,
			k * .2f
		);

		float caveFractal = this.noise.GetPerlinFractal(
			i * 5f,//the smaller the number, the longer the cave gets on the x axis
			j * 3f,//the smaller the number, the longer the cave gets on the y axis
			k * 5f//the smaller the number, the longer the cave gets on the z axis
		) * 1.1f;//how common caves are

		float caveFractalMask = this.noise.GetSimplex(
			i * .45f,
			k * .45f	
		) * .3f;

		float limestoneFractal = this.noise.GetPerlinFractal(
			i * 10f,
			j * 10f,
			k * 10f
		) * .6f;

		float saltpeterFractal = this.noise.GetPerlinFractal(
			i * 10f,
			j * 10f,
			k * 10f
		) * 0.45f;

		float saltpeterFractalMask = this.noise.GetSimplex(
			i * .45f,
			k * .45f	
		) * .3f;

		float limestoneFractalMask = this.noise.GetSimplex(
			i * .45f,
			k * .45f	
		) * .3f;

		float landHoleFractal = this.noise.GetPerlinFractal(
			i * 5f,//the smaller the number, the longer the things gets on the x axis
			j * 3f,//the smaller the number, the longer the things gets on the y axis
			k * 5f//the smaller the number, the longer the things gets on the z axis
		) * 0.7f;//how common the things are

		float landHoleFractalMask = this.noise.GetSimplex(
			i * .45f,
			k * .45f	
		) * .3f;

		float baselineLandHeight 	= Chunk.chunkHeight * 0.5f + landSimplex1 + landSimplex2;
		float baselineStoneHeight 	= Chunk.chunkHeight * 0.47f + stoneSimplex1 + stoneSimplex1;
		float baselineCaveHeight 	= Chunk.chunkHeight * 0.48f;

		string blockType = "air";

		if (j <= baselineLandHeight)
		{
			blockType = "dirt";

			if (j == Mathf.FloorToInt(baselineLandHeight))
				blockType = "grass";
			//if (j == Mathf.FloorToInt(baselineLandHeight) + 1)
				//blockType = "snow";
		}

		if (j <= baselineLandHeight && biomes == Biomes.Desert)
		{
			blockType = "sand";

			if (j == Mathf.FloorToInt(baselineLandHeight))
				blockType = "sand";
		}

		if (j <= baselineStoneHeight)
			blockType = "stone";
		
		if (j >= 40 && j < 173 && blockType == "air")
			blockType = "water";

		//if (j <= baselineStoneHeight)
		//	blockType = "limestone";

		if (limestoneFractal > Mathf.Max(.2f, limestoneFractalMask) && j <= baselineCaveHeight)
			blockType = "rockLimestone";

		if (saltpeterFractal > Mathf.Max(.2f, saltpeterFractalMask) && j <= baselineCaveHeight && blockType == "rockLimestone")
			blockType = "saltpeterOre";

		if (caveFractal > Mathf.Max(.2f, caveFractalMask) && j <= baselineCaveHeight)
			blockType = "air";

		if (landHoleFractal > Mathf.Max(.2f, landHoleFractalMask) && j <= baselineLandHeight)
			blockType = "air";

		if (j <= 2)
			blockType = "bedrock";

		return Registry.Instantiate(blockType) as BaseBlock;
	}

	/// <summary>
	/// Given a chunk's coordinates, generates all trees within the chunk.
	/// </summary>
	private void GenerateTrees(int x, int z, BaseBlock[,,] blocks)
	{
		// Seeded random number generator. Given the (x,y)-coordinates of a chunk, this will
		// generate always the same trees.
		System.Random random = new System.Random(x * z * 8192);

		float treesSimplex = this.noise.GetSimplex(x * 2.5f, z * 2.5f);

		// The current chunk has... no trees in it!
		if (treesSimplex < 0)
			return;

		int treesCount = (int)(((random.NextDouble() * 4) + 1) * (treesSimplex + 1));

		for (int treeNumber = 0; treeNumber < treesCount; treeNumber++)
		{
			int tPosX = (int)(random.NextDouble() * 12 + 2);
			int tPosZ = (int)(random.NextDouble() * 12 + 2);
			
			int groundLevel = -1;
			for (int y = 0; y < Chunk.chunkHeight; y++)
				if (blocks[tPosX,y,tPosZ].blockName == "grass")
				{
					groundLevel = y + 1;
					break;
				}

			if (groundLevel == -1)
				continue;

			bool anotherTreeTooClose = false;
			for (int a = tPosX - 2; a < tPosX + 3; a++)
				for (int b = groundLevel - 3; b < groundLevel + 7; b++)
					for (int c = tPosZ - 2; c < tPosZ + 3; c++)
						if (blocks[a, b, c].blockName == "log")
							anotherTreeTooClose = true;

			if (anotherTreeTooClose)
				continue;
			
			// Make the trunk!
			for (int i = 0; i < 5; i++)
			{
				blocks[tPosX, groundLevel + i, tPosZ] = Registry.Instantiate("log") as BaseBlock;
			}
			// First 5x5 layer of leaves
			for (int i = tPosX - 2; i <= tPosX + 2; i++)
				for (int k = tPosZ - 2; k <= tPosZ + 2; k++)
					if (blocks[i, groundLevel + 3, k]?.blockName != "log")
						blocks[i, groundLevel + 3, k] = Registry.Instantiate("leaves") as BaseBlock;

			// Second 4x2x4 layer of leaves
			for (int i = tPosX - 1; i <= tPosX + 1; i++)
				for (int j = 0; j < 2; j++)
					for (int k = tPosZ - 1; k <= tPosZ + 1; k++)
						if (blocks[i, groundLevel + 4 + j, k]?.blockName != "log")
							blocks[i, groundLevel + 4 + j, k] = Registry.Instantiate("leaves") as BaseBlock;

			// The tip!
			blocks[tPosX, groundLevel + 6, tPosZ] = Registry.Instantiate("leaves") as BaseBlock;
		}
	}

	/// <summary>
	/// Given a chunk's (x,z)-coordinates, it generates ores for it.
	/// </summary>
	private string GenerateOres(System.Random random, int y)
	{
		/**
		* Coal: spawns wherever stone can spawn. Rate: 0.25
		* Iron: spawns at y > 25. Rate: 0.18
		* Gold: spawns at y < 25 Rate: 0.10
		* Diamond & Emerald: spawns y < 14. Rate: 0.05
		*/

		float probability = (float)(random.NextDouble() * 100);
		float probabilityDiamond = (float)(random.NextDouble() * 100);

		string blockName = "stone";
		
		//if (probability <= 8)
			//blockName = "rockLimestone";
		
		//if (probability <= 7)
		//	blockName = "saltpeterOre";
		
		if (probability <= 6)
			blockName = "oreCoal";

		//if (probability <= 6)
		//	blockName = "clay"; //tis a misery, but it can't spawn cause i'm running out of probability numbers (biomes will help a lot)

		if (probability <= 5 && y > 25 && y < 150)
			blockName = "oreIron";

		if (probability <= 3 && y <= 85)
			blockName = "oreGold";

		if (probability <= 2 && y <= 95)
			blockName = "oreRedstone";

		if (probability <= 4 && y <= 105)
			blockName = "oreSulfur";

		if (probability <= 1 && y <= 95)
			if (probabilityDiamond > 47)
				blockName = "oreDiamond";
			else
				blockName = "oreEmerald";

		return blockName;
	}

	public void changeRenderDistance(float amount)
	{
		//change the rencer distance
	}
}
