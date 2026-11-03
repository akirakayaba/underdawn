//Name:			WindSkill.cs
//Description:	Tornado: slowly moves forward, every enemy it touches takes
//				small hits over and over and gets pushed along with it

using Godot;

public static class WindSkill
{
	private const float Speed = 110f;
	private const float Duration = 1.6f;
	private const int Damage = 4;
	private const float Push = 160f;

	public static void Cast(Player player, Vector2 direction)
	{
		var color = ElementVisuals.GetColor(ElementType.WIND);
		var tornado = new SkillProjectile
		{
			Velocity = direction * Speed,
			Lifetime = Duration,
			HitRadius = 22f,
			Pierce = true,
			RehitTime = 0.25f,
			/*hit from behind the enemy so the knockback pushes it the way the tornado moves*/
			OnHit = (self, enemy) => Combat.Hit(enemy.GlobalPosition - direction, ElementType.WIND, enemy, Damage)
		};
		tornado.OnHit += (self, enemy) => enemy.ApplyKnockback(direction * Push);

		tornado.AddChild(new CpuParticles2D
		{
			Amount = 40,
			Lifetime = 0.6,
			EmissionShape = CpuParticles2D.EmissionShapeEnum.Sphere,
			EmissionSphereRadius = 8f,
			Gravity = new Vector2(0, -70),
			OrbitVelocityMin = 1.5f,
			OrbitVelocityMax = 2.5f,
			ScaleAmountMin = 2f,
			ScaleAmountMax = 3.5f,
			Color = color,
			ColorRamp = new Gradient { Colors = new[] { Colors.White, new Color(1, 1, 1, 0) } }
		});
		/*spinning ovals stacked up to look like a funnel*/
		for (int i = 0; i < 3; i++)
		{
			float size = 8f + i * 5f;
			var ring = new Line2D
			{
				Points = Oval(size),
				Width = 1.5f,
				DefaultColor = new Color(color, 0.7f),
				Position = new Vector2(0, -4 - i * 9)
			};
			tornado.AddChild(ring);
			var spin = ring.CreateTween().SetLoops();
			spin.TweenProperty(ring, "scale:x", -1f, 0.15);
			spin.TweenProperty(ring, "scale:x", 1f, 0.15);
		}

		player.GetParent().AddChild(tornado);
		tornado.GlobalPosition = player.GlobalPosition + direction * 16f;
	}

	private static Vector2[] Oval(float width)
	{
		var points = new Vector2[13];
		for (int i = 0; i < points.Length; i++)
		{
			var point = Vector2.Right.Rotated(Mathf.Tau * i / (points.Length - 1));
			points[i] = new Vector2(point.X * width, point.Y * width * 0.35f);
		}
		return points;
	}
}
