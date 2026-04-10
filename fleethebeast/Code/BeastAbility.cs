/// <summary>
/// Add to the player prefab. Gives the Beast a Q-activated speed burst.
/// Requires "ability1" mapped to Q in your project's input action manifest.
/// </summary>
public sealed class BeastAbility : Component
{
	[Property] public float BoostSpeed { get; set; } = 320f;
	[Property] public float BoostDuration { get; set; } = 5f;
	[Property] public float Cooldown { get; set; } = 30f;

	public bool IsBoostActive { get; private set; }
	public float CooldownRemaining { get; private set; }
	private float boostTimer;

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		var gm = Scene.GetAll<GameManager>().FirstOrDefault();
		if ( gm is null || gm.BeastId != GameObject.Id ) return;

		if ( CooldownRemaining > 0f )
			CooldownRemaining -= Time.Delta;

		if ( IsBoostActive )
		{
			boostTimer -= Time.Delta;
			if ( boostTimer <= 0f )
				IsBoostActive = false;
		}

		if ( Input.Pressed( "ability1" ) && CooldownRemaining <= 0f && !IsBoostActive )
		{
			IsBoostActive = true;
			boostTimer = BoostDuration;
			CooldownRemaining = Cooldown;
		}

		DrawAbilityHud();
	}

	private void DrawAbilityHud()
	{
		var cam = Scene.Camera;
		if ( cam is null ) return;
		var hud = cam.Hud;

		float screenW = Screen.Size.x;
		float screenH = Screen.Size.y;

		// Position: bottom-right corner
		float barWidth = 160f;
		float barHeight = 18f;
		float barX = screenW - barWidth - 24f;
		float barY = screenH - 60f;

		// Key icon
		hud.DrawRect( new Rect( barX - 36f, barY - 2f, 28f, 22f ), new Color( 0.15f, 0.15f, 0.15f, 0.9f ), 5f );
		hud.DrawText( new TextRendering.Scope( "Q", new Color( 0.9f, 0.9f, 0.9f ), 16, "Roboto" ), new Vector2( barX - 29f, barY ) );

		if ( IsBoostActive )
		{
			// Show remaining boost time as a draining bar (orange)
			float fraction = boostTimer / BoostDuration;
			var activeColor = new Color( 1f, 0.55f, 0f );

			hud.DrawRect( new Rect( barX, barY, barWidth, barHeight ), new Color( 0f, 0f, 0f, 0.8f ), 4f );
			hud.DrawRect( new Rect( barX + 2f, barY + 2f, ( barWidth - 4f ) * fraction, barHeight - 4f ), activeColor, 3f );

			int ms = (int)( boostTimer * 10f );
			hud.DrawText(
				new TextRendering.Scope( $"SPRINT  {boostTimer:F1}s", activeColor, 13, "Poppins" ),
				new Vector2( barX, barY - 20f )
			);
		}
		else if ( CooldownRemaining > 0f )
		{
			// Show cooldown progress (red, filling up)
			float fraction = 1f - ( CooldownRemaining / Cooldown );
			var cooldownColor = new Color( 0.6f, 0.15f, 0.15f );
			var fillColor = new Color( 0.85f, 0.25f, 0.25f );

			hud.DrawRect( new Rect( barX, barY, barWidth, barHeight ), new Color( 0f, 0f, 0f, 0.8f ), 4f );
			hud.DrawRect( new Rect( barX + 2f, barY + 2f, ( barWidth - 4f ) * fraction, barHeight - 4f ), fillColor, 3f );

			hud.DrawText(
				new TextRendering.Scope( $"Cooldown  {CooldownRemaining:F1}s", cooldownColor, 13, "Poppins" ),
				new Vector2( barX, barY - 20f )
			);
		}
		else
		{
			// Ready — green
			var readyColor = new Color( 0.2f, 1f, 0.35f );

			hud.DrawRect( new Rect( barX, barY, barWidth, barHeight ), new Color( 0f, 0f, 0f, 0.8f ), 4f );
			hud.DrawRect( new Rect( barX + 2f, barY + 2f, barWidth - 4f, barHeight - 4f ), readyColor, 3f );

			hud.DrawText(
				new TextRendering.Scope( "READY", readyColor, 13, "Poppins" ),
				new Vector2( barX + barWidth / 2f - 18f, barY - 20f )
			);
		}
	}
}
