//Name:			TestArea.cs
//Description:	Test keys for the test area so health, damage, status effects
//				and the element hud can be shown before everything is in a level
//				(number keys so they don't clash with WASD / Q / E)
//				1 - damage player and enemies
//				2 - bonus damage to player and enemies
//				3 - next status effect on player and enemies (cycles back to none)
//				4 - next player element (cycles back to none)
//				5 - spawn a random enemy (goblin / demon sword / skeleton knight) near the player

using System.Linq;
using Godot;

public partial class TestArea : Node2D
{
	private const int ElementCount = 8;
	private int statusIndex = 0;
	private int elementIndex = 0;

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;

		var player = GetNode<Player>("Player");
		var enemies = GetChildren().OfType<Enemy>().ToList();

		switch (key.Keycode)
		{
			case Key.Key1:
				player.TakeDamage(10);
				enemies.ForEach(enemy => enemy.TakeDamage(3));
				break;
			case Key.Key2:
				player.TakeDamage(20, true);
				enemies.ForEach(enemy => enemy.TakeDamage(6, true));
				break;
			case Key.Key3:
				statusIndex = (statusIndex + 1) % ElementCount;
				player.ApplyStatus((ElementType)statusIndex);
				enemies.ForEach(enemy => enemy.ApplyStatus((ElementType)statusIndex));
				break;
			case Key.Key4:
				elementIndex = (elementIndex + 1) % ElementCount;
				player.SetElement((ElementType)elementIndex);
				break;
			case Key.Key5:
				string[] enemyScenes = { "goblin", "demon_sword", "skeleton_knight" };
				var enemy = GD.Load<PackedScene>($"res://scenes/entities/enemies/{enemyScenes[GD.RandRange(0, 2)]}.tscn").Instantiate<Enemy>();
				enemy.Position = player.Position + Vector2.Right.Rotated(GD.Randf() * Mathf.Tau) * 120;
				AddChild(enemy);
				break;
		}
	}
}
