﻿using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Extensions;

/// <summary>
/// A chunk is a cubic structure of fixed side that contains individual blocks.
/// Chunks are generated by the TerrainGenerator.
/// </summary>
[RequireComponent(typeof(TextureStitcher))]
public class Chunk
{	
	/// <summary>
	/// Individual blockNames contained within the chunk.
	/// </summary>
    public BaseBlock[,,] blocks;

	/// <summary>
	/// (x,z) size of the chunk.
	/// </summary>
	public static int chunkSize = 16;

	/// <summary>
	/// y-size of the chunk.
	/// </summary>
	public static int chunkHeight = 350;

	/// <summary>
	/// Private reference to the game object first created by BuildMesh().
	/// </summary>
	public GameObject chunkGameObject;

	/// <summary>
	/// Whether the mesh was already built or not.
	/// </summary>
	public bool meshBuilt = false;
	public Block block;

	/// <summary>
	/// x-position of the chunk.
	/// </summary>
	public int x;

	/// <summary>
	/// z-position of the chunk.
	/// </summary>
	public int z;

	/// <summary>
	/// Used to build a quad's triangles.
	/// </summary>
	private int[] identityQuad = new int[] {
		0, 1, 3,
		1, 2, 3
	};

	public Chunk()
	{
		this.blocks = new BaseBlock[chunkSize, chunkHeight, chunkSize];
	}

	/// <summary>
	/// Builds the chunk's mesh.
	/// </summary>
	public void BuildMesh()
	{
		if (this.chunkGameObject == null)
			this.SpawnGameObject();

		Mesh mesh = new Mesh();

		List<Vector3> vertices	= new List<Vector3>();
		List<Vector2> uvs		= new List<Vector2>();
		List<int> triangles 	= new List<int>();

		this.BuildMeshFaces(vertices, uvs, triangles);

		mesh.vertices 	= vertices.ToArray();
		mesh.uv 		= uvs.ToArray();
		mesh.triangles 	= triangles.ToArray();

		mesh.RecalculateNormals();

		this.chunkGameObject.GetComponent<MeshFilter>().mesh = mesh;
		this.chunkGameObject.GetComponent<MeshCollider>().sharedMesh = mesh;
		this.chunkGameObject.GetComponent<MeshCollider>().material = CachedResources.Load<PhysicMaterial>("PhysicsMaterials/FrictionLess");

		this.meshBuilt = true;
	}

	private enum ThreadStatus { STARTED, FINISHED };
	private ThreadStatus status = ThreadStatus.STARTED;
	/// <summary>
	/// Allows to build a chunk mesh almost entirely asynchronously.
	/// This has to be run on the main thread!
	/// Mesh building is done in parallel, spawning a new thread. GameObject instantiation & Unity API calls are executed by the coroutine
	/// on the main thread.
	/// </summary>
	public IEnumerator BuildMeshAsync()
	{
		if (this.chunkGameObject == null)
			this.SpawnGameObject();
		
		Mesh mesh = new Mesh();

		List<Vector3> vertices	= new List<Vector3>();
		List<Vector2> uvs		= new List<Vector2>();
		List<int> triangles 	= new List<int>();

		Thread meshBuildingThread = new Thread(() => {
			this.BuildMeshFaces(vertices, uvs, triangles);
			this.status = ThreadStatus.FINISHED;
		});
		meshBuildingThread.Start();
		
		while (this.status == ThreadStatus.STARTED)
			yield return new WaitForEndOfFrame();

		mesh.vertices 	= vertices.ToArray();
		mesh.uv 		= uvs.ToArray();
		mesh.triangles 	= triangles.ToArray();

		mesh.RecalculateNormals();

		this.chunkGameObject.GetComponent<MeshFilter>().mesh = mesh;
		this.chunkGameObject.GetComponent<MeshCollider>().sharedMesh = mesh;
		this.chunkGameObject.GetComponent<MeshCollider>().material = CachedResources.Load<PhysicMaterial>("PhysicsMaterials/FrictionLess");

		this.meshBuilt = true;
	}

	/// <summary>
	/// Given the list of vertices, UVs and triangles, this builds the whole chunk's faces writing into the given parameters.
	/// </summary>
	void BuildMeshFaces(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
	{
		int builtFaces = 0;

		for (int i = 0; i < chunkSize; i++)
			for (int j = 0; j < chunkHeight; j++)
				for (int k = 0; k < chunkSize; k++)
				{
					if (block != null)
					{
						if (this.blocks[block.coordinates.x, block.coordinates.y, block.coordinates.z].blockName != "air")
						{
							block.blockRotation = BaseBlock.Rotation.Up;
						}
					}
					// Determine block adjacency with air. For each adjacent block face, render the face.

					// If the block itself is air, don't render anything
					if (this.blocks != null && this.blocks[i,j,k] == null || this.blocks[i,j,k].blockName == "air")
						continue;

					// Top face adjacency
					if (j >= 0 && j <= chunkHeight - 1)
					{
						if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.Up)
						{
							if (j == chunkHeight - 1 || this.blocks[i, j + 1, k]?.blockName == "air" || this.blocks[i, j + 1, k]?.blockName == "leaves" || this.blocks[i, j + 1, k]?.blockName == "glass")
								// Always render the top most face, OR if the top-adjacent block is "air".
								this.AddFace(i, j, k, builtFaces++, "top", CubeMeshFaces.top, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.North)
						{
							if (j == chunkHeight - 1 || this.blocks[i, j, k + 1]?.blockName == "air" || this.blocks[i, j, k + 1]?.blockName == "leaves" || this.blocks[i, j, k + 1]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "top", CubeMeshFaces.back, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.East)
						{
							if (j == chunkHeight - 1 || this.blocks[i, j, k + 1]?.blockName == "air" || this.blocks[i, j, k + 1]?.blockName == "leaves" || this.blocks[i, j, k + 1]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "top", CubeMeshFaces.east, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.South)
						{
							if (j == chunkHeight - 1 || this.blocks[i, j, k - 1]?.blockName == "air" || this.blocks[i, j, k - 1]?.blockName == "leaves" || this.blocks[i, j, k - 1]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "top", CubeMeshFaces.front, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.West)
						{
							if (j == chunkHeight - 1 || this.blocks[i, j, k - 1]?.blockName == "air" || this.blocks[i, j, k - 1]?.blockName == "leaves" || this.blocks[i, j, k - 1]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "top", CubeMeshFaces.west, vertices, uvs, triangles);
						}
					}

					// Bottom face adjacency
					if (j >= 0 && j < chunkHeight)
					{
						if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.Up)
						{
							if (j == 0 || this.blocks[i, j - 1, k]?.blockName == "air" || this.blocks[i, j - 1, k]?.blockName == "leaves" || this.blocks[i, j - 1, k]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "bottom", CubeMeshFaces.bottom, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.North)
						{
							if (j == 0 || this.blocks[i, j, k - 1]?.blockName == "air" || this.blocks[i, j, k - 1]?.blockName == "leaves" || this.blocks[i, j, k - 1]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "bottom", CubeMeshFaces.front, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.East)
						{
							if (j == 0 || this.blocks[i, j, k - 1]?.blockName == "air" || this.blocks[i, j, k - 1]?.blockName == "leaves" || this.blocks[i, j, k - 1]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "bottom", CubeMeshFaces.west, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.South)
						{
							if (j == 0 || this.blocks[i, j, k + 1]?.blockName == "air" || this.blocks[i, j, k + 1]?.blockName == "leaves" || this.blocks[i, j, k + 1]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "bottom", CubeMeshFaces.back, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.West)
						{
							if (j == 0 || this.blocks[i, j, k + 1]?.blockName == "air" || this.blocks[i, j, k + 1]?.blockName == "leaves" || this.blocks[i, j, k + 1]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "bottom", CubeMeshFaces.east, vertices, uvs, triangles);
						}
					}

					// West face adjacency
					if (i >= 0 && i < chunkSize)
					{
						if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.Up)
						{
							if (i == 0 || this.blocks[i - 1, j, k]?.blockName == "air" || this.blocks[i - 1, j, k]?.blockName == "leaves" || this.blocks[i - 1, j, k]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "west", CubeMeshFaces.west, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.North)
						{
							if (i == 0 || this.blocks[i - 1, j, k]?.blockName == "air" || this.blocks[i - 1, j, k]?.blockName == "leaves" || this.blocks[i - 1, j, k]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "west", CubeMeshFaces.west, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.East)
						{
							if (i == 0 || this.blocks[i, j + 1, k]?.blockName == "air" || this.blocks[i, j + 1, k]?.blockName == "leaves" || this.blocks[i, j + 1, k]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "west", CubeMeshFaces.top, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.South)
						{
							if (i == 0 || this.blocks[i - 1, j, k]?.blockName == "air" || this.blocks[i - 1, j, k]?.blockName == "leaves" || this.blocks[i - 1, j, k]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "west", CubeMeshFaces.west, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.West)
						{
							if (i == 0 || this.blocks[i, j - 1, k]?.blockName == "air" || this.blocks[i, j - 1, k]?.blockName == "leaves" || this.blocks[i, j - 1, k]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "west", CubeMeshFaces.bottom, vertices, uvs, triangles);
						}
					}

					// East face adjacency
					if (i >= 0 && i <= chunkSize)
					{
						if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.Up)
						{
							if (i == chunkSize - 1 || this.blocks[i + 1, j, k]?.blockName == "air" || this.blocks[i + 1, j, k]?.blockName == "leaves" || this.blocks[i + 1, j, k]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "east", CubeMeshFaces.east, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.North)
						{
							if (i == chunkSize - 1 || this.blocks[i + 1, j, k]?.blockName == "air" || this.blocks[i + 1, j, k]?.blockName == "leaves" || this.blocks[i + 1, j, k]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "east", CubeMeshFaces.east, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.East)
						{
							if (i == chunkSize - 1 || this.blocks[i, j - 1, k]?.blockName == "air" || this.blocks[i, j - 1, k]?.blockName == "leaves" || this.blocks[i, j - 1, k]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "east", CubeMeshFaces.bottom, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.South)
						{
							if (i == chunkSize - 1 || this.blocks[i + 1, j, k]?.blockName == "air" || this.blocks[i + 1, j, k]?.blockName == "leaves" || this.blocks[i + 1, j, k]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "east", CubeMeshFaces.east, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.West)
						{
							if (i == chunkSize - 1 || this.blocks[i, j + 1, k]?.blockName == "air" || this.blocks[i, j + 1, k]?.blockName == "leaves" || this.blocks[i, j + 1, k]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "east", CubeMeshFaces.top, vertices, uvs, triangles);
						}
					}

					// Front face adjacency
					if (k >= 0 && k < chunkSize)
					{
						if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.Up)
						{
							if (k == 0 || this.blocks[i, j, k - 1]?.blockName == "air" || this.blocks[i, j, k - 1]?.blockName == "leaves" || this.blocks[i, j, k - 1]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "front", CubeMeshFaces.front, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.North)
						{
							if (k == 0 || this.blocks[i, j + 1, k]?.blockName == "air" || this.blocks[i, j + 1, k]?.blockName == "leaves" || this.blocks[i, j + 1, k]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "front", CubeMeshFaces.top, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.East)
						{
							if (k == 0 || this.blocks[i, j, k + 1]?.blockName == "air" || this.blocks[i, j, k + 1]?.blockName == "leaves" || this.blocks[i, j, k + 1]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "front", CubeMeshFaces.front, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.South)
						{
							if (k == 0 || this.blocks[i, j - 1, k]?.blockName == "air" || this.blocks[i, j - 1, k]?.blockName == "leaves" || this.blocks[i, j - 1, k]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "front", CubeMeshFaces.bottom, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.West)
						{
							if (k == 0 || this.blocks[i, j, k + 1]?.blockName == "air" || this.blocks[i, j, k + 1]?.blockName == "leaves" || this.blocks[i, j, k + 1]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "front", CubeMeshFaces.front, vertices, uvs, triangles);
						}
					}

					// Back face adjacency
					if (k >= 0 && k <= chunkSize)
					{
						if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.Up)
						{
							if (k == chunkSize - 1 || this.blocks[i, j, k + 1]?.blockName == "air" || this.blocks[i, j, k + 1]?.blockName == "leaves" || this.blocks[i, j, k + 1]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "back", CubeMeshFaces.back, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.North)
						{
							if (k == chunkSize - 1 || this.blocks[i, j - 1, k]?.blockName == "air" || this.blocks[i, j - 1, k]?.blockName == "leaves" || this.blocks[i, j - 1, k]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "back", CubeMeshFaces.bottom, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.East)
						{
							if (k == chunkSize - 1 || this.blocks[i, j, k - 1]?.blockName == "air" || this.blocks[i, j, k - 1]?.blockName == "leaves" || this.blocks[i, j, k - 1]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "back", CubeMeshFaces.back, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.South)
						{
							if (k == chunkSize - 1 || this.blocks[i, j + 1, k]?.blockName == "air" || this.blocks[i, j + 1, k]?.blockName == "leaves" || this.blocks[i, j + 1, k]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "back", CubeMeshFaces.top, vertices, uvs, triangles);
						}
						else if (this.blocks[i, j, k].blockRotation == BaseBlock.Rotation.West)
						{
							if (k == chunkSize - 1 || this.blocks[i, j, k - 1]?.blockName == "air" || this.blocks[i, j, k - 1]?.blockName == "leaves" || this.blocks[i, j, k - 1]?.blockName == "glass")
								this.AddFace(i, j, k, builtFaces++, "back", CubeMeshFaces.back, vertices, uvs, triangles);
						}
					}
				}
	}

	/// <summary>
	/// Generates and spawns the chunk's GameObject.
	/// </summary>
	private void SpawnGameObject()
	{
		// Instantiate the GameObject (and implictly add it to the scene).
		this.chunkGameObject = new GameObject("Chunk");

		// Get the stitched texture.
		Texture2D texture = TextureStitcher.instance.StitchedTexture;

		// Add mesh filter and renderer.
		this.chunkGameObject.AddComponent<MeshFilter>();
		this.chunkGameObject.AddComponent<MeshRenderer>();
		this.chunkGameObject.AddComponent<MeshCollider>();
		this.chunkGameObject.AddComponent<ChunkObject>();

		this.chunkGameObject.tag = "chunk";

		this.chunkGameObject.GetComponent<MeshRenderer>().material = Resources.Load<Material>("ChunkCutout");
		this.chunkGameObject.GetComponent<MeshRenderer>().material.mainTexture = texture;
		this.chunkGameObject.transform.position = new Vector3(this.x * chunkSize, 0, this.z * chunkSize);
	}

	/// <summary>
	/// Given the block's (i,j,k) vector, the number of built faces, face to build, vertices, uvs and triangles references,
	/// this appends a new face mesh's data on the vertices, uvs and triangles references.
	/// </summary>
	void AddFace(int i, int j, int k, int builtFaces, string faceName, Vector3[] face, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
	{
		string textureName = this.blocks[i,j,k].blockName;

		if (this.blocks[i,j,k].textureName != "default")
			textureName = this.blocks[i,j,k].textureName;

		if (this.blocks[i,j,k].hasSidedTextures)
		{
			if (TextureStitcher.instance.TextureUVs.ContainsKey(System.String.Format("{0}_{1}", textureName, faceName)))
				textureName = System.String.Format("{0}_{1}", textureName, faceName);
			else
				textureName = System.String.Format("{0}_{1}", textureName, "side");	
		}

		vertices.AddRange(face.Add((i,j,k)));
		uvs.AddRange(TextureStitcher.instance.TextureUVs[textureName].ToArray());
		triangles.AddRange(this.identityQuad.Add(builtFaces * 4));
	}

	public void Destroy()
	{
		GameObject.Destroy(this.chunkGameObject);
	}

	/// <summary>
	/// Recalculates the chunk mesh's normals.
	/// </summary>
	public void RecalculateNormals()
	{
		this.chunkGameObject.GetComponent<MeshFilter>().mesh.RecalculateNormals();
	}
}
