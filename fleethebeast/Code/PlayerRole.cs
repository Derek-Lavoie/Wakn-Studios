public partial class PlayerRole : Component
{
	[Sync] public bool IsBeast { get; set; } = false;
	[Sync] public bool IsKnockedOut { get; set; } = false;

	private float knockoutTimer;

	[Rpc.Host]
	public void StartKnockout()
	{
		IsKnockedOut = true;
		knockoutTimer = 30f;
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;
		if ( !IsKnockedOut ) return;

		knockoutTimer -= Time.Delta;
		if ( knockoutTimer <= 0f )
			IsKnockedOut = false;
	}
}
