using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

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

[NetworkSettings(channel = 1, sendInterval = 0.3f)]
public class Player : Mob
{
	public PlayerCamera playerCamera { get; private set; }

	public AnimationClip idleDown, idleUp, idleSide, moveDown, moveUp, moveSide;

	private InputState currentInputState;
	private List<InputState> queuedInputStates = new List<InputState>();
	private uint currentState, lastClientState, lastClientRecState;
	private float nextSendTime;
	[SyncVar(hook = "OnServerStateChanged")]
	private TransformState serverState;

	protected override void Start()
	{
		base.Start();

		if (!isLocalPlayer)
			return;

		playerCamera = GameManager.mainCamera.GetComponent<PlayerCamera>();
		playerCamera.target = this;
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

	private void PerformMovement(InputState state)
	{
		moveDirection = new Vector3(state.horizontalInput, 0f, state.verticalInput).relativeTo(Quaternion.Euler(0f, state.camera * 90f, 0f));

		if (state.jumpInput)
			if (controller.isGrounded)
				Jump();

		Move();
	}

	protected override void Animate()
	{
		base.Animate();

		if (moveDirection.IsZero())
			animator.Play(SwitchForDirection(idleDown, idleUp, idleSide));
		else
			animator.Play(SwitchForDirection(moveDown, moveUp, moveSide), Mathf.Max(0.3f, controller.velocity.OnlyXZ().magnitude / moveSpeed));
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

			for (int i = 0; i < queuedInputStates.Count; i++)
				PerformMovement(queuedInputStates[i]);

			if (Vector3.Distance(transform.position.OnlyXZ(), state.position.OnlyXZ()) > 0.6f)
				transform.position = state.position;
			else
			{
				transform.position = oldPosition;
				velocity = oldVelocity;
			}

			if (Vector3.Distance(transform.position.OnlyY(), state.position.OnlyY()) > 0.6f)
				transform.position = state.position;
			else
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

		if (!isLocalPlayer)
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
