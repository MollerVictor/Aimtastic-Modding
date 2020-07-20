using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;


//Replace namespace name with your unique name, to avoid conflicts with other mods
namespace ExamplePlugin
{

    public class ExamplePlugin : BaseMod
    {
        public override void OnLoaded()
        {
			var room = ScriptableObject.CreateInstance(typeof(MyRoom)) as MyRoom;
			room.RoomSetting = new NewRoomSetting {
				SceneName = "CustomGameModeTest",
				DisplayName = "My Cústom Game Mode 123",
				ImageName = "360tracking",
		};
			room.CountdownStartTargetPrefab = ModHelper.Instance.GetPrefab("CountdownTarget");
			room.CountdownStartTargetSpawnPosition = new Vector3(0, 1, 1);
			room.TeleportPosition = new Vector3(0, 1, -4);
			room.UseCountdownTimer = true;
			room.UseTimer = true;
			room.RoomUIDisplayType = RoomUIDisplayTypes.ScoreTimeLeft;
			
			WebUIManager.Instance.AddCustomRoom(room);
		}
	}


	public class MyRoom : CustomRoom
	{
		//SettingsItemAttribute helps setting up the gamemode settings, default value, step size, min and max value.
		//Follow this pattern to add your own values that should being able to be changed by players
		[SettingsItemAttribute(50, 5, 10, 150)]
		protected SettingsItemInt TargetSize = new SettingsItemInt("TargetSize");

		[SettingsItemAttribute(5, 1, 1, 10)]
		protected SettingsItemInt NumberOfTargets = new SettingsItemInt("NumberOfTargets");

		[SettingsItemAttribute(1f, 0.1f, 0.1f, 5)]
		protected SettingsItemFloat Speed = new SettingsItemFloat("Speed");

		[SettingsItemAttribute(1.4f, 0.1f, 0.4f, 5f)]
		protected SettingsItemFloat MaxTimeBeforeChangingDirection = new SettingsItemFloat("MaxTimeBeforeChangingDirection");

		[SettingsItemAttribute(2, 1, 1, 10)]
		protected SettingsItemInt TargetHealth = new SettingsItemInt("TargetHealth");

		GameObject _targetPrefab = ModHelper.Instance.GetPrefab("MovingSphereTarget");

		Vector3 _bottenLeftSpawnCorner = new Vector3(-6, 1, 2);
		Vector3 _topRightSpawnCorner = new Vector3(6, 5, 3);

		List<GameObject> _currentTargets = new List<GameObject>();


		// Use this for initialization
		void Start()
		{
			Restart();
		}


		protected override void OnKilledEnemy(Entity entity, BulletHitInfo bulletHitInfo)
		{
			SpawnNewTarget();
		}


		protected override void OnRestart()
		{

			foreach (GameObject target in _currentTargets)
			{
				Destroy(target);
			}
			_currentTargets.Clear();
		}

		public override void OnStartRoom()
		{
			for (int i = 0; i < NumberOfTargets.GetValue(this); i++)
			{
				SpawnNewTarget();
			}
		}


		//Generates a random position inside a cube, and make sure they don't spawn inside objects
		Vector3 GenerateNewPosition()
		{
			Vector3 spawnPos = new Vector3();

			for (int i = 0; i < 15; i++)
			{
				spawnPos = new Vector3(Random.Range(_bottenLeftSpawnCorner.x, _topRightSpawnCorner.x),
				Random.Range(_bottenLeftSpawnCorner.y, _topRightSpawnCorner.y),
				Random.Range(_bottenLeftSpawnCorner.z, _topRightSpawnCorner.z));

				//Make sure you don't spawn targets inside eachothers
				if (!Physics.CheckSphere(spawnPos, 1 - (i / 22), ~0, QueryTriggerInteraction.Ignore))
				{
					return spawnPos;
				}

			}
			return spawnPos;
		}

		public override void SpawnNewTarget()
		{
			if (!Running)
				return;


			Vector3 spawnPos = GenerateNewPosition();


			GameObject target = Instantiate(_targetPrefab, spawnPos, Quaternion.identity) as GameObject;
			Vector3 scale = target.transform.localScale;
			scale.x = TargetSize.GetValue(this) / (float)100;
			scale.y = TargetSize.GetValue(this) / (float)100;
			scale.z = TargetSize.GetValue(this) / (float)100;
			target.transform.localScale = scale;


			Vector3 size = new Vector3(Mathf.Abs(_topRightSpawnCorner.x - _bottenLeftSpawnCorner.x), Mathf.Abs(_topRightSpawnCorner.y - _bottenLeftSpawnCorner.y), Mathf.Abs(_topRightSpawnCorner.z - _bottenLeftSpawnCorner.z));
			Vector3 center = new Vector3(_bottenLeftSpawnCorner.x + size.x / 2, _bottenLeftSpawnCorner.y + size.y / 2, _bottenLeftSpawnCorner.z + size.z / 2);


			
			target.GetComponent<Entity>().SetHealth(TargetHealth.GetValue(this));
			

			var comp = target.GetComponent<MoveAroundInsideBox>();


			if (comp != null)
			{
				comp.SetBounds(center, size, MaxTimeBeforeChangingDirection.GetValue(this));
				
				comp._maxSpeed = comp._maxSpeed * Speed.GetValue(this);
				comp._acceleration = comp._acceleration * Speed.GetValue(this);				
			}

			_currentTargets.Add(target);
		}

		public override void OnTimeEnded()
		{
			TimeLeft = 0;
			_currentRoomState = RoomState.Idle;

			//Make sure we clean up all targets that we spawned
			foreach (GameObject target in _currentTargets)
			{
				Destroy(target);
			}

			int totalShots = Hits + Misses;
			float totalHitProcent = (float)Hits / totalShots * 100;

			float hitsPerSec = Hits / (float)RunTime.GetValue(this);

			//Pushing the stats to the UI
			Settings.Instance._statsUI.ClearList();
			Settings.Instance._statsUI.AddNewStatsItem("Score:", Score.ToString("#,0"));
			Settings.Instance._statsUI.AddNewStatsItem("Shots:", totalShots.ToString());
			Settings.Instance._statsUI.AddNewStatsItem("Hits:", Hits.ToString());
			Settings.Instance._statsUI.AddNewStatsItem("Misses:", Misses.ToString());
			Settings.Instance._statsUI.AddNewStatsItem("Hit Percent:", totalHitProcent.ToString("F1") + "%");
			Settings.Instance._statsUI.AddNewStatsItem("Biggest Combo:", BiggestCombo.ToString());
			Settings.Instance._statsUI.AddNewStatsItem("Hits/Second:", hitsPerSec.ToString("F2"));
		}
	}

}