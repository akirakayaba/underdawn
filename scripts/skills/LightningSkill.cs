//Name:			LightningSkill.cs
//Description:	Chain lightning: zaps the closest enemy in front of the player,
//				then jumps to the next closest enemy, up to MaxTargets enemies,
//				paralyzing each one

using System.Collections.Generic;
using System.Linq;
using Godot;

public static class LightningSkill
{
	private const float FirstRange = 170f;
	private const float JumpRange = 110f;
	private const int MaxTargets = 4;
	private const int Damage = 14;

	public static void Cast(Player player, Vector2 direction)
	{
		var parent = player.GetParent();
		var from = player.GlobalPosition + new Vector2(0, -10);
		var hit = new List<Enemy>();

		/*the first target has to be roughly in front of the player*/
		var target = Skills.EnemiesNear(player, player.GlobalPosition, FirstRange)
			.Where(enemy => (enemy.GlobalPosition - player.GlobalPosition).Normalized().Dot(direction) > 0.3f)
			.OrderBy(enemy => enemy.GlobalPosition.DistanceTo(player.GlobalPosition))
			.FirstOrDefault();

		if (target == null)
		{
			/*nothing to hit, just a short zap so the cast still shows*/
			DrawBolt(parent, from, from + direction * 60f);
			return;
		}

		while (target != null && hit.Count < MaxTargets)
		{
			var to = target.GlobalPosition + new Vector2(0, -10);
			DrawBolt(parent, from, to);
			Combat.Hit(from, ElementType.LIGHTNING, target, Damage);
			hit.Add(target);
			from = to;
			var last = target;
			target = Skills.EnemiesNear(player, last.GlobalPosition, JumpRange)
				.Where(enemy => !hit.Contains(enemy))
				.OrderBy(enemy => enemy.GlobalPosition.DistanceTo(last.GlobalPosition))
				.FirstOrDefault();
		}
		HitEffects.Shake(player.GetViewport().GetCamera2D(), 2f, 0.12f);
	}

	/*jagged line with a wider faded glow line behind it*/
	private static void DrawBolt(Node parent, Vector2 from, Vector2 to)
	{
		var points = new List<Vector2> { from };
		const int Segments = 7;
		var side = (to - from).Orthogonal().Normalized();
		for (int i = 1; i < Segments; i++)
			points.Add(from.Lerp(to, (float)i / Segments) + side * (float)GD.RandRange(-6.0, 6.0));
		points.Add(to);

		var color = ElementVisuals.GetColor(ElementType.LIGHTNING);
		var glow = new Line2D { Points = points.ToArray(), Width = 6f, DefaultColor = new Color(color, 0.35f), ZIndex = 3 };
		var bolt = new Line2D { Points = points.ToArray(), Width = 2f, DefaultColor = Colors.White, ZIndex = 3 };
		glow.AddChild(bolt);
		parent.AddChild(glow);

		var tween = glow.CreateTween();
		tween.TweenInterval(0.08);
		tween.TweenProperty(glow, "modulate:a", 0.0, 0.2);
		tween.TweenCallback(Callable.From(glow.QueueFree));
	}
}
