//Name:			FireSkill.cs
//Description:	Fireball: flies forward and explodes when it hits an enemy
//				or reaches max range, the explosion burns everything around it

using Godot;

public static class FireSkill
{
	private const float Speed = 260f;
	private const float Range = 0.8f;
	private const float ExplosionRadius = 40f;
	private const int Damage = 18;

	public static void Cast(Player player, Vector2 direction)
	{
		var color = ElementVisuals.GetColor(ElementType.FIRE);
		var fireball = new SkillProjectile
		{
			Velocity = direction * Speed,
			Lifetime = Range,
			HitRadius = 12f,
			OnEnd = Explode
		};

		/*drawn a bit up so it looks like it comes from the player's hands*/
		fireball.AddChild(new Polygon2D { Polygon = Skills.Circle(7f), Color = color, Position = new Vector2(0, -10) });
		fireball.AddChild(new Polygon2D { Polygon = Skills.Circle(3.5f), Color = new Color(1f, 0.95f, 0.6f), Position = new Vector2(0, -10) });
		fireball.AddChild(new CpuParticles2D
		{
			Amount = 24,
			Lifetime = 0.35,
			LocalCoords = false,
			Position = new Vector2(0, -10),
			Gravity = new Vector2(0, -40),
			ScaleAmountMin = 2f,
			ScaleAmountMax = 4f,
			Color = color,
			ColorRamp = new Gradient { Colors = new[] { Colors.White, new Color(1, 0.3f, 0.1f, 0) } }
		});

		player.GetParent().AddChild(fireball);
		fireball.GlobalPosition = player.GlobalPosition + direction * 12f;
	}

	private static void Explode(SkillProjectile fireball)
	{
		var position = fireball.GlobalPosition;
		Skills.HitArea(fireball, position, ExplosionRadius, Damage, ElementType.FIRE);
		Skills.Burst(fireball.GetParent(), position + new Vector2(0, -6), ElementVisuals.GetColor(ElementType.FIRE), 30, 140f, 0.45f);
		HitEffects.Shake(fireball.GetViewport().GetCamera2D(), 3f, 0.15f);
	}
}
