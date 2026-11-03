//Name:			Hud.cs
//Description:	Player hud, shows the player's current health and element
//				(hud scene is a child of the player scene)

using Godot;

public partial class Hud : CanvasLayer
{
	public override void _Ready()
	{
		var healthBar = GetNode<ProgressBar>("PlayerHealthBar");
		var elementLabel = GetNode<Label>("ElementLabel");
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
	}
}
