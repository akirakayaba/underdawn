//Name:			Hud.cs
//Description:	Player hud, shows the player's current health
//				(hud scene is a child of the player scene)

using Godot;

public partial class Hud : CanvasLayer
{
	public override void _Ready()
	{
		var healthBar = GetNode<ProgressBar>("PlayerHealthBar");
		GetParent<Player>().HealthChanged += (health, maxHealth) =>
		{
			healthBar.MaxValue = maxHealth;
			healthBar.Value = health;
		};
	}
}
