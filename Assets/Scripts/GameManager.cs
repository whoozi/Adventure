using System;

using UnityEngine;

public class GameManager : Singleton<GameManager>
{
	private static Camera _mainCamera;

	public static Camera mainCamera
	{
		get
		{
			if (_mainCamera == null)
				_mainCamera = Camera.main;

			return _mainCamera;
		}
	}

	public Player GetLocalPlayer()
	{
		foreach (Player player in FindObjectsOfType<Player>())
		{
			if (player.isLocalPlayer)
				return player;
		}

		throw new Exception("Can not locate local player");
	}

	public Item swordItem, shieldItem;
}
