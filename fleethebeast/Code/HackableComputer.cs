using Sandbox;
using System.Collections.Generic;

/// <summary>
/// Place on a computer GameObject. Players press E nearby to hack.
/// Progress saves per-player if they walk away. ~35s to complete.
/// </summary>
public partial class HackableComputer : Component
{
	[Property, Category( "Hacking" )] public float HackDuration { get; set; } = 50f;
	[Property, Category( "Hacking" )] public float InteractRange { get; set; } = 120f;

	// Per-player saved progress (host only), keyed by SteamId
	private Dictionary<long, float> PlayerProgress { get; set; } = new();

	// Currently synced state
	[Sync] public long ActiveHackerId { get; set; }
	[Sync] public float ActiveProgressSync { get; set; }
	[Sync] public bool IsHacked { get; set; }
	[Sync] public bool IsLockedOut { get; set; }

	private Connection ActiveHacker { get; set; }
	private float LockoutTimer { get; set; }

	[Rpc.Host]
	public void TriggerLockout()
	{
		if ( ActiveHacker is { } hacker )
		{
			PlayerProgress[hacker.SteamId] = ActiveProgressSync;
			ActiveHacker = null;
			ActiveHackerId = 0;
		}

		IsLockedOut = true;
		LockoutTimer = 5f;

		var model = Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants );
		if ( model is not null )
			model.Tint = new Color( 1f, 0.15f, 0.15f );
	}

	[Rpc.Host]
	public void StartHack()
	{
		var caller = Rpc.Caller;
		if ( IsHacked ) return;
		if ( IsLockedOut ) return;
		if ( ActiveHacker is not null && ActiveHacker != caller ) return;

		ActiveHacker = caller;
		ActiveHackerId = caller.SteamId;

		if ( !PlayerProgress.ContainsKey( caller.SteamId ) )
			PlayerProgress[caller.SteamId] = 0f;

		ActiveProgressSync = PlayerProgress[caller.SteamId];
	}

	[Rpc.Host]
	public void StopHack()
	{
		var caller = Rpc.Caller;
		if ( ActiveHacker != caller ) return;

		PlayerProgress[caller.SteamId] = ActiveProgressSync;
		ActiveHacker = null;
		ActiveHackerId = 0;
	}

	[Rpc.Host]
	public void AddProgress( float amount )
	{
		ActiveProgressSync += amount;
		if ( ActiveProgressSync < 0f ) ActiveProgressSync = 0f;
		if ( ActiveProgressSync > 1f ) ActiveProgressSync = 1f;
	}

	private SoundHandle _hackingLoop;

	[Rpc.Broadcast]
	public void PlayHackingSound()
	{
		if ( _hackingLoop.IsValid() )
			_hackingLoop.Stop();
		_hackingLoop = Sound.Play( "sounds/keyboard_clack.sound", WorldPosition );
	}

	[Rpc.Broadcast]
	public void StopHackingSound()
	{
		if ( _hackingLoop.IsValid() )
			_hackingLoop.Stop();
	}

	[Rpc.Broadcast]
	public void PlayFailSound()
	{
		Sound.Play( "sounds/skillcheck_fail.sound", WorldPosition );
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;

		if ( IsLockedOut )
		{
			LockoutTimer -= Time.Delta;
			if ( LockoutTimer <= 0f )
			{
				IsLockedOut = false;
				if ( Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants ) is { } m )
					m.Tint = Color.White;
			}
			return;
		}

		if ( IsHacked || ActiveHacker is null ) return;

		float rate = 1f / HackDuration;
		ActiveProgressSync += rate * Time.Delta;

		PlayerProgress[ActiveHacker.SteamId] = ActiveProgressSync;

		if ( ActiveProgressSync >= 1f )
		{
			ActiveProgressSync = 1f;
			IsHacked = true;
			ActiveHacker = null;
			ActiveHackerId = 0;
			Log.Info( $"Computer {GameObject.Name} hacked!" );
			// TODO: trigger your game event here
			var model = Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants );
			if ( model is not null )
				model.Tint = new Color( 0f, 1f, 0f );
		}
	}
}
