using System;

public partial class GameManager : Component, Component.INetworkListener
{
	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public List<GameObject> SpawnPoints { get; set; } = new();
	[Property] public float BeastPickDelay { get; set; } = 5f;

	[Sync] public Guid BeastId { get; set; }
	[Sync] public bool RoundActive { get; set; } = false;

	private TimeSince timeSinceRoundStart;
	private bool beastPicked = false;
	private int playersNeeded = 1;

	public void OnActive( Connection connection )
	{
		if ( PlayerPrefab == null ) { Log.Error( "GameManager: PlayerPrefab is not assigned!" ); return; }
		if ( SpawnPoints.Count == 0 ) { Log.Error( "GameManager: No spawn points assigned!" ); return; }

		var spawnIndex = Game.Random.Next( SpawnPoints.Count );
		var spawn = SpawnPoints[spawnIndex];

		var player = PlayerPrefab.Clone( spawn.WorldTransform );
		player.NetworkSpawn( connection );

		Log.Info( $"Player joined: {connection.DisplayName}" );

		// Start round when enough players join
		if ( !RoundActive && Networking.IsHost )
		{
			var playerCount = Scene.GetAll<PlayerController>().Count();
			if ( playerCount >= playersNeeded )
			{
				RoundActive = true;
				beastPicked = false;
				timeSinceRoundStart = 0;
				Log.Info( "Round starting! Beast will be chosen in 5 seconds..." );
			}
		}
	}

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost ) return;
		if ( !RoundActive ) return;
		if ( beastPicked ) return;

		if ( timeSinceRoundStart >= BeastPickDelay )
		{
			PickBeast();
		}
	}

	private void PickBeast()
	{
		var players = Scene.GetAll<PlayerController>().ToList();
		if ( players.Count == 0 ) return;

		var beast = players[Game.Random.Next( players.Count )];
		BeastId = beast.GameObject.Id;
		beastPicked = true;

		var role = beast.GameObject.Components.Get<PlayerRole>();
		if ( role is { } r )
			r.IsBeast = true;

		Log.Info( $"Beast chosen: {beast.GameObject.Name}" );

		AnnounceBeast();
	}

	[Rpc.Broadcast]
	private void AnnounceBeast()
	{
		Log.Info( "THE BEAST HAS BEEN CHOSEN!" );
	}
}