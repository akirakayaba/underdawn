//Name:			EarthSkill.cs
//Description:	Earth spikes: rocks burst out of the ground one after another
//				in a line in front of the player, each enemy gets hit once

using System.Collections.Generic;
using Godot;

public static class EarthSkill
{
	private const int Bursts = 6;
	private const float Spacing = 22f;
	private const float Delay = 0.06f;
	private const float HitRadius = 20f;
	private const int Damage = 12;

	public static void Cast(Player player, Vector2 direction)
	{
		var parent = player.GetParent();
		var start = player.GlobalPosition;
		var hit = new HashSet<Enemy>();

		for (int i = 0; i < Bursts; i++)
		{
			var position = start + direction * (20f + i * Spacing);
			player.GetTree().CreateTimer(i * Delay).Timeout += () =>
			{
				if (!GodotObject.IsInstanceValid(parent))
					return;
				foreach (var enemy in Skills.EnemiesNear(parent, position, HitRadius))
				{
					if (hit.Add(enemy))
						Combat.Hit(position, ElementType.EARTH, enemy, Damage);
				}
				SpawnRock(parent, position);
				Skills.Burst(parent, position, new Color(0.6f, 0.45f, 0.3f), 10, 80f, 0.35f);
			};
		}
		HitEffects.Shake(player.GetViewport().GetCamera2D(), 3f, Bursts * Delay + 0.1f);
	}

	private static void SpawnRock(Node parent, Vector2 position)
	{
		var rock = new Polygon2D
		{
			Polygon = new[] { new Vector2(-7, 0), new Vector2(-5, -10), new Vector2(-1, -16), new Vector2(3, -11), new Vector2(7, 0) },
			Color = ElementVisuals.GetColor(ElementType.EARTH).Darkened(0.2f),
			Scale = new Vector2(1, 0)
		};
		rock.AddChild(new Polygon2D
		{
			Polygon = new[] { new Vector2(-4, -2), new Vector2(-1, -13), new Vector2(1, -3) },
			Color = ElementVisuals.GetColor(ElementType.EARTH).Lightened(0.2f)
		});
		parent.AddChild(rock);
		rock.GlobalPosition = position;

		var tween = rock.CreateTween();
		tween.TweenProperty(rock, "scale", Vector2.One, 0.06);
		tween.TweenInterval(0.4);
		tween.TweenProperty(rock, "scale", new Vector2(1, 0), 0.15);
		tween.TweenCallback(Callable.From(rock.QueueFree));
	}
}
