using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	private static T _instance;

	private static object _lock = new object();

	private static bool isApplicationQuitting;

	public static T instance
	{
		get
		{
			if (isApplicationQuitting)
				return null;

			lock (_lock)
			{
				if (!_instance)
					_instance = FindObjectOfType<T>();

				if (!_instance)
					_instance = new GameObject(typeof(T).ToString()).AddComponent<T>();

				return _instance;
			}
		}
	}

	private void Awake()
	{
		DontDestroyOnLoad(gameObject);
	}

	private void OnDestroy()
	{
		isApplicationQuitting = true;
	}
}
