using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
	public Player target;
	public Vector3 targetOffset = new Vector3(0f, 0.5f, 0f);
	public float distance = 7f, viewAngle = 45f;
	public sbyte side;

	private float sideAngleVelocity;

	private void Update()
	{
		if (!target)
			return;

		float sideAngle = Mathf.SmoothDampAngle(transform.rotation.eulerAngles.y, side * 90f, ref sideAngleVelocity, 0.1f, Mathf.Infinity, Time.deltaTime);

		transform.rotation = Quaternion.Euler(new Vector3(viewAngle, sideAngle, 0f));
		transform.position = target.transform.position + targetOffset - (transform.forward * distance);
	}
}
