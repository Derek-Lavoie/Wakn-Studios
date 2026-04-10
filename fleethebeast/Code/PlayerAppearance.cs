public sealed class PlayerAppearance : Component
{
	[Property] public SkinnedModelRenderer ModelRenderer { get; set; }
	[Property] public Model SurvivorModel { get; set; }
	[Property] public Model BeastModel { get; set; }
	[Property] public float SurvivorSpeed { get; set; } = 210f;
	[Property] public float BeastSpeed { get; set; } = 230f;

	private bool wasBeast = false;

	protected override void OnUpdate()
	{
		var gm = Scene.GetAll<GameManager>().FirstOrDefault();
		if ( gm == null || ModelRenderer == null ) return;

		bool isBeast = gm.BeastId == GameObject.Id;

		var controller = Components.Get<PlayerController>();
		if ( controller is not null )
		{
			float speed = isBeast ? BeastSpeed : SurvivorSpeed;
			var ability = Components.Get<BeastAbility>( FindMode.EverythingInSelfAndDescendants );
			if ( ability is not null && ability.IsBoostActive )
				speed = ability.BoostSpeed;
			var role = Components.Get<PlayerRole>( FindMode.EverythingInSelfAndDescendants );
			if ( role is not null && role.IsKnockedOut )
				speed = 0f;
			controller.WalkSpeed = speed;
			controller.RunSpeed = speed;
		}

		if ( isBeast == wasBeast ) return;
		wasBeast = isBeast;

		ModelRenderer.Model = isBeast ? BeastModel : SurvivorModel;
	}
}
