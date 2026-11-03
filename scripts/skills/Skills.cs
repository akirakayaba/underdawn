//Name:			Skills.cs
//Description:	Element skills (R key), the player's current element decides
//				which skill is cast. each skill is in its own file, this has
//				the names, cooldowns and helpers they share

using System.Collections.Generic;
using System.Linq;
using Godot;

public static class Skills
{
	public static string GetName(ElementType element) => element switch
	{
		ElementType.FIRE => "Fireball",
		ElementType.ICE => "Ice Spikes",
		ElementType.LIGHTNING => "Chain Lightning",
		ElementType.WIND => "Tornado",
		ElementType.EARTH => "Earth Spikes",
		ElementType.WATER => "Tidal Wave",
		ElementType.POISON => "Poison Cloud",
		_ => "No Skill"
	};

	/*seconds before the skill can be cast again*/
	public static float GetCooldown(ElementType element) => element switch
	{
		ElementType.FIRE => 2.5f,
		ElementType.ICE => 4f,
		ElementType.LIGHTNING => 3f,
		ElementType.WIND => 4f,
		ElementType.EARTH => 3f,
		ElementType.WATER => 3f,
		ElementType.POISON => 6f,
		_ => 0f
	};

	/*returns false if that element has no skill*/
	public static bool Cast(Player player, ElementType element, Vector2 direction)
	{
		switch (element)
		{
			case ElementType.FIRE:
				FireSkill.Cast(player, direction);
				return true;
			case ElementType.ICE:
				IceSkill.Cast(player, direction);
				return true;
			case ElementType.LIGHTNING:
				LightningSkill.Cast(player, direction);
				return true;
			default:
				return false;
		}
	}

	public static List<Enemy> EnemiesNear(Node node, Vector2 position, float radius) =>
		node.GetTree().GetNodesInGroup(Enemy.GroupName).OfType<Enemy>()
			.Where(enemy => enemy.GlobalPosition.DistanceTo(position) <= radius)
			.ToList();

	/*hits every enemy in the circle with the element (bonus damage + status effect)*/
	public static void HitArea(Node node, Vector2 position, float radius, int damage, ElementType element)
	{
		foreach (var enemy in EnemiesNear(node, position, radius))
			Combat.Hit(position, element, enemy, damage);
	}

	/*one shot particle burst at a position, removes itself when done*/
	public static void Burst(Node parent, Vector2 globalPosition, Color color, int amount, float speed, float lifetime = 0.5f)
	{
		var burst = new CpuParticles2D
		{
			Amount = amount,
			OneShot = true,
			Explosiveness = 1f,
			Lifetime = lifetime,
			Spread = 180f,
			Gravity = Vector2.Zero,
			InitialVelocityMin = speed * 0.5f,
			InitialVelocityMax = speed,
			DampingMin = speed,
			DampingMax = speed * 1.5f,
			ScaleAmountMin = 2f,
			ScaleAmountMax = 4f,
			Color = color,
			ColorRamp = new Gradient { Colors = new[] { Colors.White, new Color(1, 1, 1, 0) } },
			ZIndex = 2
		};
		parent.AddChild(burst);
		burst.GlobalPosition = globalPosition;
		burst.Finished += burst.QueueFree;
		burst.Emitting = true;
	}

	public static Vector2[] Circle(float radius, int points = 12)
	{
		var circle = new Vector2[points];
		for (int i = 0; i < points; i++)
			circle[i] = Vector2.Right.Rotated(Mathf.Tau * i / points) * radius;
		return circle;
	}
}
