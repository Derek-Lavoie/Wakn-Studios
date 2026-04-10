using Sandbox;

/// <summary>
/// Place on the Player prefab. Handles E-press, draws circular skill check
/// and segmented progress bar at the bottom of the screen.
/// </summary>
public sealed class HackingHud : Component
{
	private HackableComputer ActiveComputer { get; set; }
	private bool IsHacking { get; set; }

	// Skill check state
	private bool SkillCheckActive { get; set; }
	private float NeedleAngle { get; set; }
	private float NeedleSpeed { get; set; } = 280f;
	private float ZoneStart { get; set; }
	private float ZoneSize { get; set; } = 50f;
	private int LoopCount { get; set; }
	private float NextSkillCheckTime { get; set; }
	private float HackCooldown { get; set; }

	// Fail ping
	private HackableComputer FailPingComputer { get; set; }
	private float FailPingTimer { get; set; }

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		if ( HackCooldown > 0f )
			HackCooldown -= Time.Delta;

		if ( FailPingTimer > 0f )
		{
			FailPingTimer -= Time.Delta;
			if ( FailPingComputer is not null )
				DrawFailPing( FailPingComputer.WorldPosition );
			if ( FailPingTimer <= 0f )
				FailPingComputer = null;
		}

		HandleInteraction();

		if ( IsHacking && ActiveComputer is not null )
		{
			// Tick skill check timer
			if ( !SkillCheckActive && !ActiveComputer.IsHacked )
			{
				NextSkillCheckTime -= Time.Delta;
				if ( NextSkillCheckTime <= 0f )
					StartSkillCheck();
			}

			// Tick needle
			if ( SkillCheckActive )
			{
				NeedleAngle += NeedleSpeed * Time.Delta;
				if ( NeedleAngle >= 360f )
				{
					NeedleAngle -= 360f;
					LoopCount++;
					if ( LoopCount >= 2 )
					{
						// Auto-fail — missed it
						FailSkillCheck();
						return;
					}
				}
			}

			DrawHackingHud( ActiveComputer.ActiveProgressSync, ActiveComputer.IsHacked );

			if ( ActiveComputer.IsHacked )
				StopHacking();
		}
	}

	private void HandleInteraction()
	{
		var role = Components.Get<PlayerRole>( FindMode.EverythingInSelfAndDescendants );
		if ( role is not null && role.IsBeast ) return;
		if ( role is not null && role.IsKnockedOut ) return;

		var cam = Scene.Camera;
		if ( cam is null ) return;

		// Find the nearest computer within range
		HackableComputer lookedAtComputer = null;
		float closestDist = float.MaxValue;

		foreach ( var comp in Scene.GetAll<HackableComputer>() )
		{
			float dist = Vector3.DistanceBetween( WorldPosition, comp.WorldPosition );
			if ( dist < comp.InteractRange && dist < closestDist )
			{
				closestDist = dist;
				lookedAtComputer = comp;
			}
		}

		// Show prompt
		if ( lookedAtComputer is not null && !lookedAtComputer.IsHacked && !lookedAtComputer.IsLockedOut && !IsHacking && HackCooldown <= 0f )
		{
			var hud = Scene.Camera.Hud;
			float cx = Screen.Size.x * 0.5f;
			float cy = Screen.Size.y * 0.5f;
			hud.DrawText(
				new TextRendering.Scope( "Press E to Hack", new Color( 0.3f, 1f, 0.3f ), 22, "Poppins" ),
				new Vector2( cx - 80f, cy + 40f )
			);
		}

		if ( Input.Pressed( "use" ) )
		{
			if ( IsHacking )
			{
				// If skill check is active, try to hit it
				if ( SkillCheckActive )
				{
					CheckNeedleHit();
					return;
				}

				StopHacking();
				return;
			}

			if ( lookedAtComputer is not null && !lookedAtComputer.IsHacked && !lookedAtComputer.IsLockedOut && HackCooldown <= 0f )
			{
				ActiveComputer = lookedAtComputer;
				ActiveComputer.StartHack();
				ActiveComputer.PlayHackingSound();
				IsHacking = true;
				NextSkillCheckTime = 3f;
			}
		}

		// Range check
		if ( IsHacking && ActiveComputer is not null )
		{
			float dist = Vector3.DistanceBetween( WorldPosition, ActiveComputer.WorldPosition );
			if ( dist > ActiveComputer.InteractRange )
				FailSkillCheck();
		}
	}

	// ─────────────────────────────────────────────
	// Skill Check Logic
	// ─────────────────────────────────────────────

	private void StartSkillCheck()
	{
		SkillCheckActive = true;
		NeedleAngle = 0f;
		LoopCount = 0;
		ZoneStart = Game.Random.Float( 60f, 280f );
		ZoneSize = 50f;
	}

	private void CheckNeedleHit()
	{
		if ( !SkillCheckActive ) return;

		float zoneEnd = ZoneStart + ZoneSize;
		bool hit;
		if ( zoneEnd > 360f )
			hit = NeedleAngle >= ZoneStart || NeedleAngle <= ( zoneEnd - 360f );
		else
			hit = NeedleAngle >= ZoneStart && NeedleAngle <= zoneEnd;

		SkillCheckActive = false;

		if ( hit )
		{
			ActiveComputer.AddProgress( 0.08f );
			Sound.Play( "sounds/skillcheck_success.sound" );
		}
		else
		{
			FailSkillCheck();
		}

		NextSkillCheckTime = Game.Random.Float( 3f, 6f );
	}

	private void FailSkillCheck()
	{
		FailPingComputer = ActiveComputer;
		FailPingTimer = 5f;
		ActiveComputer.PlayFailSound();
		ActiveComputer.TriggerLockout();
		StopHacking();
		HackCooldown = 0f; // IsLockedOut on the computer controls the wait, not a separate cooldown
	}

	private void DrawFailPing( Vector3 worldPos )
	{
		var cam = Scene.Camera;
		if ( cam is null ) return;

		var toPoint = ( worldPos - cam.WorldPosition ).Normal;
		if ( Vector3.Dot( toPoint, cam.WorldRotation.Forward ) < 0f ) return; // behind camera

		var screenPos = cam.PointToScreenPixels( worldPos );
		var hud = cam.Hud;
		float x = screenPos.x;
		float y = screenPos.y;

		// Red circle background
		hud.DrawRect( new Rect( x - 22f, y - 44f, 44f, 44f ), new Color( 0.85f, 0.1f, 0.1f, 0.92f ), 22f );

		// "!" centered
		hud.DrawText( new TextRendering.Scope( "!", Color.White, 36, "Roboto" ), new Vector2( x - 8f, y - 44f ) );
	}

	private void StopHacking()
	{
		if ( ActiveComputer is not null )
		{
			ActiveComputer.StopHackingSound();
			ActiveComputer.StopHack();
		}

		ActiveComputer = null;
		IsHacking = false;
		SkillCheckActive = false;
		HackCooldown = 3f;
	}

	// ─────────────────────────────────────────────
	// HUD Drawing
	// ─────────────────────────────────────────────

	private void DrawHackingHud( float progress, bool complete )
	{
		var hud = Scene.Camera.Hud;
		float screenW = Screen.Size.x;
		float screenH = Screen.Size.y;

		// ── Circular skill check (center of screen) ──
		if ( SkillCheckActive )
		{
			float cx = screenW * 0.5f;
			float cy = screenH * 0.45f;
			float radius = 70f;
			int segments = 64;
			float thickness = 10f;

			// Draw ring segments
			for ( int i = 0; i < segments; i++ )
			{
				float a1 = ( i / (float)segments ) * 360f;
				float a2 = ( ( i + 1 ) / (float)segments ) * 360f;

				float r1 = a1 * 3.14159f / 180f;
				float r2 = a2 * 3.14159f / 180f;

				float cos1 = (float)System.Math.Cos( r1 );
				float sin1 = (float)System.Math.Sin( r1 );
				float cos2 = (float)System.Math.Cos( r2 );
				float sin2 = (float)System.Math.Sin( r2 );

				float x1 = cx + cos1 * radius;
				float y1 = cy + sin1 * radius;
				float x2 = cx + cos2 * radius;
				float y2 = cy + sin2 * radius;

				// Check if in success zone
				bool inZone = false;
				float zoneEnd = ZoneStart + ZoneSize;
				if ( zoneEnd > 360f )
					inZone = a1 >= ZoneStart || a1 <= ( zoneEnd - 360f );
				else
					inZone = a1 >= ZoneStart && a1 <= zoneEnd;

				var lineColor = inZone
					? new Color( 0f, 1f, 0f, 0.9f )
					: new Color( 1f, 1f, 1f, 0.25f );
				float lineW = inZone ? thickness + 4f : thickness;

				hud.DrawLine( new Vector2( x1, y1 ), new Vector2( x2, y2 ), lineW, lineColor );
			}

			// Draw needle
			float needleRad = NeedleAngle * 3.14159f / 180f;
			float nCos = (float)System.Math.Cos( needleRad );
			float nSin = (float)System.Math.Sin( needleRad );

			float nx1 = cx + nCos * ( radius - 20f );
			float ny1 = cy + nSin * ( radius - 20f );
			float nx2 = cx + nCos * ( radius + 20f );
			float ny2 = cy + nSin * ( radius + 20f );

			hud.DrawLine( new Vector2( nx1, ny1 ), new Vector2( nx2, ny2 ), 5f, new Color( 1f, 0f, 0f ) );


			// App-icon style "E" in center
			float keySize = 50f;
			float rounding = 11f;
			float ex = cx - keySize * 0.5f;
			float ey = cy - keySize * 0.5f;

			// Thin outer border (outline layer)
			hud.DrawRect( new Rect( ex - 3f, ey - 3f, keySize + 6f, keySize + 6f ), new Color( 0.75f, 0.75f, 0.75f, 0.85f ), rounding + 3f );

			// Dark rounded box (inner background)
			hud.DrawRect( new Rect( ex, ey, keySize, keySize ), new Color( 0.1f, 0.1f, 0.1f, 0.97f ), rounding );

			// Bold off-white "E" centered in the box (fake bold via offsets)
			float etx = cx - 12f;
			float ety = cy - 20f;
			var eColor = new Color( 0.95f, 0.95f, 0.95f );
			hud.DrawText( new TextRendering.Scope( "E", eColor, 38, "Roboto" ), new Vector2( etx, ety ) );
			hud.DrawText( new TextRendering.Scope( "E", eColor, 38, "Roboto" ), new Vector2( etx + 1f, ety ) );
			hud.DrawText( new TextRendering.Scope( "E", eColor, 38, "Roboto" ), new Vector2( etx + 1f, ety + 1f ) );
			hud.DrawText( new TextRendering.Scope( "E", eColor, 38, "Roboto" ), new Vector2( etx, ety + 1f ) );
		}

		// ── Progress bar at bottom of screen ──
		float barWidth = 500f;
		float barHeight = 24f;
		float barX = ( screenW - barWidth ) / 2f;
		float barY = screenH - 80f;

		var green = new Color( 0.1f, 0.9f, 0.1f );
		var darkGreen = new Color( 0.05f, 0.25f, 0.05f );
		var borderColor = new Color( 0.1f, 0.6f, 0.1f, 0.8f );

	

		// Border
		hud.DrawRect( new Rect( barX - 2, barY - 2, barWidth + 4, barHeight + 4 ), borderColor );
		hud.DrawRect( new Rect( barX, barY, barWidth, barHeight ), new Color( 0f, 0f, 0f, 0.9f ) );

		// Segments
		int totalSegments = 24;
		float segmentGap = 3f;
		float totalGaps = segmentGap * ( totalSegments - 1 );
		float segmentWidth = ( barWidth - totalGaps ) / totalSegments;
		int filledSegments = (int)( progress * totalSegments );

		for ( int i = 0; i < totalSegments; i++ )
		{
			float segX = barX + i * ( segmentWidth + segmentGap );
			var segRect = new Rect( segX, barY + 2, segmentWidth, barHeight - 4 );
			hud.DrawRect( segRect, i < filledSegments ? green : darkGreen );
		}

		// Glow line
		float fillWidth = progress * barWidth;
		if ( fillWidth > 0 )
			hud.DrawRect( new Rect( barX, barY - 1, fillWidth, 2 ), new Color( 0.4f, 1f, 0.4f, 0.8f ) );

		// Percentage label above bar
		int pct = (int)( progress * 100f );
		string label = complete ? "HACKED!" : $"Hacking...  {pct}%";

		hud.DrawText(
			new TextRendering.Scope( label, green, 18, "Poppins" ),
			new Vector2( barX + barWidth / 2f - 50f, barY - 28f )
		);
	}
}
