using System;

using UnityEngine;
using UnityEngine.Networking;

using PowerTools;

public class Mob : NetworkBehaviour
{
	protected CharacterController controller { get; private set; }
	public SpriteAnim animator { get; private set; }

	public float moveSpeed = 3.5f;
	public float moveSmoothTime = 0.025f;
	public float airSmoothTime = 0.25f;
	public float jumpHeight = 1.3f;

	private Vector3 _moveDirection;
	private float moveAngle = -180f;
	protected Vector3 velocity;
	private Vector3 smoothVelocity;

	protected Direction direction;

	public Vector3 moveDirection
	{
		get { return _moveDirection; }
		set { _moveDirection = Vector3.ClampMagnitude(value, 1f); }
	}

	protected virtual void Start()
	{
		controller = GetComponent<CharacterController>();
		animator = GetComponentInChildren<SpriteAnim>();
	}

	protected virtual void FixedUpdate()
	{
		if (!hasAuthority)
			return;

		Move();

		Animate();
	}

	protected virtual void Update()
	{
		transform.rotation = Quaternion.LookRotation(GameManager.mainCamera.transform.forward.OnlyXZ());
	}

	protected virtual void Animate()
	{
		direction = CalculateDirection();
		transform.localScale = new Vector3(direction == Direction.Left ? -1f : 1f, 1f, 1f);
	}

	protected void Move()
	{
		velocity += Physics.gravity * Time.fixedDeltaTime;
		velocity = Vector3.SmoothDamp(velocity.OnlyXZ(), moveDirection.OnlyXZ() * moveSpeed, ref smoothVelocity, controller.isGrounded ? moveSmoothTime : airSmoothTime, Mathf.Infinity, Time.fixedDeltaTime) + Vector3.up * velocity.y;

		CollisionFlags flags = controller.Move(velocity * Time.fixedDeltaTime);

		if (controller.isGrounded || (velocity.y > 0f && (flags & CollisionFlags.CollidedAbove) != 0))
			velocity.y = 0f;

		if (!moveDirection.IsZero())
			moveAngle = Mathf.Round(-Vector3.SignedAngle(moveDirection, Vector3.forward, Vector3.up));
	}

	protected void Jump()
	{
		velocity.y = Mathf.Sqrt(-2f * jumpHeight * Physics.gravity.y);
	}

	private Direction CalculateDirection()
	{
		float calcAngle = moveAngle - GameManager.mainCamera.transform.eulerAngles.y;

		if (calcAngle > 180f)
			calcAngle -= 360f;
		else if (calcAngle < -180f)
			calcAngle += 360f;

		calcAngle = Mathf.Round(calcAngle);

		if (calcAngle <= -135f || calcAngle >= 135f)
			return Direction.Down;
		else if (calcAngle >= -45f && calcAngle <= 45f)
			return Direction.Up;
		else if (calcAngle < -45f && calcAngle > -135f)
			return Direction.Left;
		else if (calcAngle > 45f && calcAngle < 135f)
			return Direction.Right;

		return direction;
	}

	public T SwitchForDirection<T>(T down, T up, T side)
	{
		switch (direction)
		{
			case Direction.Down:
				return down;

			case Direction.Up:
				return up;

			case Direction.Left:
			case Direction.Right:
				return side;

			default:
				throw new Exception("Invalid Direction: " + direction);
		}
	}
}
