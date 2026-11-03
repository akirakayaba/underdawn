//Name:			Hud.cs
//Description:	Player hud, shows the player's current health and element,
//				and the game over screen when the player dies
//				(hud scene is a child of the player scene)

using Godot;

public partial class Hud : CanvasLayer
{
	public override void _Ready()
	{
		var healthBar = GetNode<ProgressBar>("PlayerHealthBar");
		var elementLabel = GetNode<Label>("ElementLabel");
		var gameOver = GetNode<Control>("GameOver");
		var restartButton = GetNode<Button>("GameOver/Center/Menu/RestartButton");
		var player = GetParent<Player>();

		player.HealthChanged += (health, maxHealth) =>
		{
			healthBar.MaxValue = maxHealth;
			healthBar.Value = health;
		};
		player.ElementChanged += element =>
		{
			elementLabel.Text = "Element: " + element.ToString().Capitalize();
			elementLabel.Modulate = ElementVisuals.GetColor(element);
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

	/*closes the pause menu first if it was opened after dying*/
	private void Restart()
	{
		if (PauseManager.Instance.isPaused)
			PauseManager.Instance.Pauser();
		GetTree().ReloadCurrentScene();
	}
}
