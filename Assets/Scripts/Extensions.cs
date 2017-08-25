using UnityEngine;

public static class Extensions
{
	/// <summary>
	/// Checks whether value is near to zero within a tolerance.
	/// </summary>
	public static bool IsZero(this float value)
	{
		return Mathf.Abs(value) < 0.0000000001f;
	}

	/// <summary>
	/// Checks whether vector is near to zero within a tolerance.
	/// </summary>
	public static bool IsZero(this Vector3 vector3)
	{
		return vector3.sqrMagnitude < 9.99999943962493E-11;
	}

	/// <summary>
	/// Checks whether vector is near to zero within a tolerance.
	/// </summary>
	public static bool IsZero(this Vector2 vector2)
	{
		return vector2.sqrMagnitude < 9.99999943962493E-11;
	}

	/// <summary>
	/// Returns a copy of given vector with only X component of the vector.
	/// </summary>
	public static Vector3 OnlyX(this Vector3 vector3)
	{
		vector3.y = 0f;
		vector3.z = 0f;

		return vector3;
	}

	/// <summary>
	/// Returns a copy of given vector with only Y component of the vector.
	/// </summary>
	public static Vector3 OnlyY(this Vector3 vector3)
	{
		vector3.x = 0f;
		vector3.z = 0f;

		return vector3;
	}

	/// <summary>
	/// Returns a copy of given vector with only Z component of the vector.
	/// </summary>
	public static Vector3 OnlyZ(this Vector3 vector3)
	{
		vector3.x = 0f;
		vector3.y = 0f;

		return vector3;
	}

	/// <summary>
	/// Returns a copy of given vector with only X and Z components of the vector.
	/// </summary>
	public static Vector3 OnlyXZ(this Vector3 vector3)
	{
		vector3.y = 0f;

		return vector3;
	}

	/// <summary>
	/// Transform a given vector to be relative to target transform.
	/// Eg: Use to perform movement relative to camera's view direction.
	/// </summary>
	public static Vector3 relativeTo(this Vector3 vector3, Transform target, bool onlyXZ = true)
	{
		Vector3 forward = onlyXZ ? target.forward.OnlyXZ() : target.forward;

		return Quaternion.LookRotation(forward) * vector3;
	}

	/// <summary>
	/// Transform a given vector to be relative to target quaternion.
	/// Eg: Use to perform movement relative to camera's view direction.
	/// </summary>
	public static Vector3 relativeTo(this Vector3 vector3, Quaternion target, bool onlyXZ = true)
	{
		Vector3 forward = onlyXZ ? (target * Vector3.forward).OnlyXZ() : target * Vector3.forward;

		return Quaternion.LookRotation(forward) * vector3;
	}
}
