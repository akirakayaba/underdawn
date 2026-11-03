//Name:			WaterSkill.cs
//Description:	Tidal wave: a wave sweeps out in front of the player, hitting
//				every enemy in the cone, pushing them back and drenching them

using Godot;

public static class WaterSkill
{
	private const float Range = 90f;
	private const float HalfAngle = 0.9f;
	private const int Damage = 10;
	private const float Push = 220f;

	public static void Cast(Player player, Vector2 direction)
	{
		var center = player.GlobalPosition;
		foreach (var enemy in Skills.EnemiesNear(player, center, Range))
		{
			var toEnemy = enemy.GlobalPosition - center;
			if (Mathf.Abs(direction.AngleTo(toEnemy)) > HalfAngle)
				continue;
			Combat.Hit(center, ElementType.WATER, enemy, Damage);
			enemy.ApplyKnockback(toEnemy.Normalized() * Push);
		}

		/*three arcs growing outward one after the other*/
		for (int i = 0; i < 3; i++)
			SpawnArc(player.GetParent(), center, direction, i * 0.05f);
		Skills.Burst(player.GetParent(), center + direction * 40f, ElementVisuals.GetColor(ElementType.WATER), 20, 160f, 0.4f);
	}

	private static void SpawnArc(Node parent, Vector2 center, Vector2 direction, float delay)
	{
		const int Points = 12;
		var points = new Vector2[Points];
		for (int i = 0; i < Points; i++)
		{
			float angle = Mathf.Lerp(-HalfAngle, HalfAngle, (float)i / (Points - 1));
			points[i] = direction.Rotated(angle) * Range;
		}

		var color = ElementVisuals.GetColor(ElementType.WATER);
		var arc = new Line2D { Points = points, Width = 5f, DefaultColor = new Color(color, 0.8f), Scale = Vector2.One * 0.2f };
		arc.AddChild(new Line2D { Points = points, Width = 1.5f, DefaultColor = Colors.White });
		parent.AddChild(arc);
		arc.GlobalPosition = center;

		var tween = arc.CreateTween();
		tween.TweenInterval(delay);
		tween.SetParallel();
		tween.TweenProperty(arc, "scale", Vector2.One, 0.25);
		tween.TweenProperty(arc, "modulate:a", 0.0, 0.3);
		tween.Chain().TweenCallback(Callable.From(arc.QueueFree));
	}
}
