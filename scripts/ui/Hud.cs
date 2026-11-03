//Name:			Hud.cs
//Description:	Player hud: health, element, the skill bar (dash + element
//				skill with their cooldowns) and the game over screen
//				(hud scene is a child of the player scene)

using Godot;

public partial class Hud : CanvasLayer
{
	private static readonly Color DashColor = new(0.6f, 0.8f, 1f);

	private Player player;
	private SkillSlot dashSlot;
	private SkillSlot elementSkillSlot;

	public override void _Ready()
	{
		var healthBar = GetNode<ProgressBar>("PlayerHealthBar");
		var elementLabel = GetNode<Label>("ElementLabel");
		var gameOver = GetNode<Control>("GameOver");
		var restartButton = GetNode<Button>("GameOver/Center/Menu/RestartButton");
		dashSlot = GetNode<SkillSlot>("SkillBar/DashSlot");
		elementSkillSlot = GetNode<SkillSlot>("SkillBar/ElementSkillSlot");
		player = GetParent<Player>();

		dashSlot.SetSkill("Dash", DashColor, true);

		player.HealthChanged += (health, maxHealth) =>
		{
			healthBar.MaxValue = maxHealth;
			healthBar.Value = health;
		};
		player.ElementChanged += element =>
		{
			elementLabel.Text = "Element: " + element.ToString().Capitalize();
			elementLabel.Modulate = ElementVisuals.GetColor(element);
			elementSkillSlot.SetSkill(Skills.GetName(element), ElementVisuals.GetColor(element), element != ElementType.NONE);
		};
		player.Died += () =>
		{
			gameOver.Visible = true;
			Input.MouseMode = Input.MouseModeEnum.Visible;
			restartButton.GrabFocus();
		};

		restartButton.Pressed += Restart;
		GetNode<Button>("GameOver/Center/Menu/TitleButton").Pressed += () => PauseManager.Instance.MainMenu();
	}

	public override void _Process(double delta)
	{
		dashSlot.SetCooldown(player.DashCooldownLeft, player.DashCooldown);
		elementSkillSlot.SetCooldown(player.SkillCooldownLeft, Skills.GetCooldown(player.CurrentElement));
	}

	/*closes the pause menu first if it was opened after dying*/
	private void Restart()
	{
		if (PauseManager.Instance.isPaused)
			PauseManager.Instance.Pauser();
		GetTree().ReloadCurrentScene();
	}
}
