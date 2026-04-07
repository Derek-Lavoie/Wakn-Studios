public sealed class PlayerAppearance : Component
{
	[Property] public SkinnedModelRenderer ModelRenderer { get; set; }
	[Property] public Model SurvivorModel { get; set; }
	[Property] public Model BeastModel { get; set; }

	private bool wasBeast = false;

	protected override void OnUpdate()
	{
		var gm = Scene.GetAll<GameManager>().FirstOrDefault();
		if ( gm == null || ModelRenderer == null ) return;

		bool isBeast = gm.BeastId == GameObject.Id;

		if ( isBeast == wasBeast ) return;
		wasBeast = isBeast;

		ModelRenderer.Model = isBeast ? BeastModel : SurvivorModel;
	}
}
