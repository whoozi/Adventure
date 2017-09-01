using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using PowerTools;

[NetworkSettings(channel = 1, sendInterval = 0.3f)]
public class Player : Mob
{
	public struct InputState
	{
		public uint state;
		public float horizontalInput;
		public float verticalInput;
		public bool jumpInput;
		public sbyte camera;
	}

	public struct TransformState
	{
		public uint state;
		public Vector3 position;
	}

	public PlayerCamera playerCamera { get; private set; }
	public SpriteAnimNodes animatorNodes { get; private set; }

	public AnimationClip idleDown, idleUp, idleSide, moveDown, moveUp, moveSide;

	private Item mainHandItem, offHandItem;

	private InputState currentInputState;
	private List<InputState> queuedInputStates;
	[SyncVar(hook = "OnServerStateChanged")]
	private TransformState serverState;
	private uint currentState, lastClientState, lastClientRecState;
	private float nextSendTime;

	protected override void Start()
	{
		base.Start();

		if (!isLocalPlayer)
			return;

		playerCamera = GameManager.mainCamera.GetComponent<PlayerCamera>();
		animatorNodes = GetComponentInChildren<SpriteAnimNodes>();

		queuedInputStates = new List<InputState>();
		playerCamera.target = this;

		// Spawn equipped items for testing purposes
		mainHandItem = Instantiate(GameManager.instance.swordItem, transform, false);
		offHandItem = Instantiate(GameManager.instance.shieldItem, transform, false);
	}

	protected override void FixedUpdate()
	{
		if (isLocalPlayer)
		{
			currentState++;

			currentInputState = new InputState()
			{
				state = currentState,
				horizontalInput = Input.GetAxisRaw("Horizontal"),
				verticalInput = Input.GetAxisRaw("Vertical"),
				jumpInput = Input.GetButton("Jump"),
				camera = playerCamera.side,
			};

			queuedInputStates.Add(currentInputState);

			PerformMovement(currentInputState);

			while (queuedInputStates.Count > 50)
				queuedInputStates.RemoveAt(0);

			if (isServer || nextSendTime < Time.time)
			{
				CmdUpdateServerState(queuedInputStates.ToArray());
				nextSendTime = Time.time + GetNetworkSendInterval();
			}
		}

		Animate();
	}

	protected override void Update()
	{
		base.Update();

		if (!isLocalPlayer)
			return;

		if (Input.GetKeyDown(KeyCode.Q))
			playerCamera.side--;

		if (Input.GetKeyDown(KeyCode.E))
			playerCamera.side++;

		if (Input.GetKeyDown(KeyCode.X))
			transform.position = new Vector3(Random.Range(-3f, 3f), 3f, Random.Range(-3f, 3f));
	}

	private void LateUpdate()
	{
		if (mainHandItem)
		{
			Vector3 localPosition = (Vector3)animatorNodes.GetPositionRaw(0) * 0.1f;
			localPosition.y = -localPosition.y;
			localPosition.z += (direction == Direction.Left || direction == Direction.Up) ? 0.0001f : -0.0001f;

			mainHandItem.transform.localPosition = localPosition;
			mainHandItem.transform.localRotation = Quaternion.Euler(0f, 0f, animatorNodes.GetAngleRaw(0));
			mainHandItem.transform.localScale = new Vector3(direction == Direction.Down ? -1f : 1f, 1f, 1f);
		}

		if (offHandItem)
		{
			Vector3 localPosition = (Vector3)animatorNodes.GetPositionRaw(1) * 0.1f;
			localPosition.y = -localPosition.y;
			localPosition.z += (direction == Direction.Right || direction == Direction.Up) ? 0.0001f : -0.0001f;

			offHandItem.transform.localPosition = localPosition;
			offHandItem.transform.localRotation = Quaternion.Euler(0f, 0f, animatorNodes.GetAngleRaw(1));
			offHandItem.transform.localScale = new Vector3(direction == Direction.Up ? -1f : 1f, 1f, 1f);
		}
	}

	protected override void Animate()
	{
		base.Animate();

		if (controller.isGrounded && moveDirection.IsZero())
			animator.Play(SwitchForDirection(idleDown, idleUp, idleSide));
		else
			animator.Play(SwitchForDirection(moveDown, moveUp, moveSide), Mathf.Clamp(controller.velocity.magnitude / moveSpeed, 0.3f, 2.5f));
	}

	private void PerformMovement(InputState state)
	{
		moveDirection = new Vector3(state.horizontalInput, 0f, state.verticalInput).relativeTo(Quaternion.Euler(0f, state.camera * 90f, 0f));

		if (state.jumpInput)
			if (controller.isGrounded)
				Jump();

		Move();
	}

	private void PerformReconciliation(TransformState state)
	{
		if (lastClientRecState < state.state)
		{
			lastClientRecState = state.state;

			while (queuedInputStates.Count > 0)
			{
				if (queuedInputStates[0].state <= lastClientRecState)
					queuedInputStates.RemoveAt(0);
				else
					break;
			}

			Vector3 oldPosition = transform.position;
			Vector3 oldVelocity = velocity;

			transform.position = state.position;

			// Replay all queued input based on recieved server position
			for (int i = 0; i < queuedInputStates.Count; i++)
				PerformMovement(queuedInputStates[i]);

			// Check if distance between the old position and the new, predicted position is valid, if so, restore old position and velocity
			if (Vector3.Distance(oldPosition, transform.position) < 0.6f)
			{
				transform.position = oldPosition;
				velocity = oldVelocity;
			}
		}
	}

	[Command(channel = 1)]
	private void CmdUpdateServerState(InputState[] states)
	{
		for (int i = 0; i < states.Length; i++)
		{
			if (states[i].state <= lastClientState)
				continue;

			lastClientState = states[i].state;

			if (!isLocalPlayer)
				PerformMovement(states[i]);
		}

		TransformState state = new TransformState()
		{
			state = lastClientState,
			position = transform.position,
		};

		serverState = state;

		if (isLocalPlayer)
			PerformReconciliation(state);
	}

	private void OnServerStateChanged(TransformState state)
	{
		if (isLocalPlayer)
			PerformReconciliation(state);
		else
			transform.position = state.position;
	}
}
