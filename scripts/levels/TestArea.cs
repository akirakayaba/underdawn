//Name:			TestArea.cs
//Description:	Test keys for the test area so health, damage, status effects
//				and the element hud can be shown before combat is hooked up
//				K - damage player and enemies
//				B - bonus damage to player and enemies
//				S - next status effect on player and enemies (cycles back to none)
//				R - next player element (cycles back to none)

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
			case Key.K:
				player.TakeDamage(10);
				enemies.ForEach(enemy => enemy.TakeDamage(3));
				break;
			case Key.B:
				player.TakeDamage(20, true);
				enemies.ForEach(enemy => enemy.TakeDamage(6, true));
				break;
			case Key.S:
				statusIndex = (statusIndex + 1) % ElementCount;
				player.ApplyStatus((ElementType)statusIndex);
				enemies.ForEach(enemy => enemy.ApplyStatus((ElementType)statusIndex));
				break;
			case Key.R:
				elementIndex = (elementIndex + 1) % ElementCount;
				player.SetElement((ElementType)elementIndex);
				break;
		}
	}
}
