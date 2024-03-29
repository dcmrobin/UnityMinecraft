﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Extensions;

public class PlayerInput : MonoBehaviour
{
	public bool expandFOVnow = false;
	public float tapSpeed = 0.5f; //in seconds
 
	private float lastTapTime = 0;

	/// <summary>
	/// The rate of change of the player's position over time.
	/// </summary>
	private float speed;

	/// <summary>
	/// The speed at which the player walks.
	/// </summary>
	private const float walkingSpeed = 4.317f;

	/// <summary>
	/// The speed at which the player runs.
	/// </summary>
	private const float runningSpeed = 8.612f;

	/// <summary>
	/// Height of the jump that can be performed by the player.
	/// </summary>
	private float jumpHeight = 1.125f;

	/// <summary>
	/// Determines whether the player is currently jumping or not.
	/// </summary>
	private bool jumping = false;

	/// <summary>
	/// Reference to the player's CharacterController. Used to change the player's position.
	/// </summary>
	private Rigidbody _rigidbody;
	
	/// <summary>
	/// Used to indicate whether or not a stepping sound is ready to be played.
	/// </summary>
	private bool canPlaySteppingSound = true;

    // Start is called before the first frame update
    void Start()
    {
		this._rigidbody = this.GetComponent<Rigidbody>();
		this.speed = walkingSpeed;
		lastTapTime = 0;
    }

	void Update()
	{
		if(Input.GetKeyDown(KeyCode.W)){
			if((Time.time - lastTapTime) < tapSpeed){
				this.speed = runningSpeed;

				expandFOVnow = true;
			}
			lastTapTime = Time.time;
		} else if (Input.GetKeyUp(KeyCode.W))
		{
			this.speed = walkingSpeed;
			expandFOVnow = false;
		}

		if (MineCraftGUI.isAGUIShown)
			return;

		if (Input.GetKeyDown(KeyCode.Space))
			this.jumping = true;

		if (expandFOVnow)
			gameObject.GetComponentInChildren<Camera>().fieldOfView ++;

		if (!expandFOVnow)
			gameObject.GetComponentInChildren<Camera>().fieldOfView --;

		if (gameObject.GetComponentInChildren<Camera>().fieldOfView >= 70)
			gameObject.GetComponentInChildren<Camera>().fieldOfView = 70;

		if (gameObject.GetComponentInChildren<Camera>().fieldOfView <= 60 && this.speed == walkingSpeed)
			gameObject.GetComponentInChildren<Camera>().fieldOfView = 60;
	}

    // Update is called once per frame
    void FixedUpdate()
    {
		float deltaX = Input.GetAxis("Horizontal");
		float deltaY = Input.GetAxis("Vertical");

		if (MineCraftGUI.isAGUIShown)
		{
			deltaX = 0;
			deltaY = 0;
		}

		// Reset the rigidBody velocity if the player is not moving any longer.
		// For example, when the inventory has been opened.
		if (deltaX == 0 && deltaY == 0 && !this.jumping && false)
			this._rigidbody.velocity = Vector3.zero;
		
		// Grounding logic.
		float raycastDistance = this.GetComponent<BoxCollider>().bounds.extents.y + 0.01f;
		
		RaycastHit verticalHit;
		bool grounded = Physics.Raycast(this.transform.position, -this.transform.up, out verticalHit, raycastDistance);

		if (!grounded && !this.canPlaySteppingSound)
			Player.instance.StopAllSounds();

		if (this.canPlaySteppingSound && (deltaX != 0 || deltaY != 0) && grounded)
		{
			this.PlaySteppingSound(verticalHit);
			StartCoroutine(this.StartSteppingSoundCooldown());
		}

		// Movement along the x-z axis
		Vector3 movement_xz = new Vector3(deltaX, 0.1f, deltaY);

		// When moving diagonally, ||i+k|| > speed; "clamping" the movement vector's magnitude solves the issue.
		movement_xz = Vector3.ClampMagnitude(movement_xz, this.speed);

		if (grounded && this.jumping && !MineCraftGUI.isAGUIShown)
		{
			this.jumping = false;

			// The force is applied istantaneously to the player at its center of mass.
			// Given the istantaneous nature of the force, we're thinking about an impulse.
			this._rigidbody.AddForce((
				0f, 
				Mathf.Sqrt(-2.0f * Physics.gravity.y * this.jumpHeight), 
				0f
			).ToVector3(), ForceMode.Impulse);
		}

		// Transform direction to global space coordinates.
		Vector3 movement = this.transform.TransformDirection(movement_xz);

		this._rigidbody.velocity = new Vector3(movement.x * this.speed, this._rigidbody.velocity.y, movement.z * this.speed);
    }

	/// <summary>
	/// Plays the stepping sound, changing depending on the block that the player walked on.
	/// </summary>
	private void PlaySteppingSound(RaycastHit hit)
	{
		// Move 0.5f towards the center of the block
		Vector3 hitBlockCoords = hit.point - new Vector3(0, 0.5f, 0);
		ChunkPosition chunkPosition = Player.instance.GetVoxelChunk();

		BaseBlock baseBlock = PCTerrain.GetInstance().chunks[chunkPosition].blocks[
			(int)hitBlockCoords.x % 16, 
			(int)hitBlockCoords.y, 
			(int)hitBlockCoords.z % 16
		];
		Block block = Registry.Instantiate(baseBlock.blockName) as Block;
		Player.instance.PlaySteppingSound(block.soundType);
	}

	/// <summary>
	/// Starts the stepping sound cooldown.
	/// </summary>
	private IEnumerator StartSteppingSoundCooldown()
	{
		this.canPlaySteppingSound = false;

		yield return new WaitForSeconds(0.45f);

		this.canPlaySteppingSound = true;
	}
}
