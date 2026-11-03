//Name:			PoisonSkill.cs
//Description:	Poison cloud: leaves a cloud on the ground in front of the
//				player for a few seconds, enemies inside keep getting poisoned

using Godot;

public static class PoisonSkill
{
	private const float Distance = 60f;
	private const float Radius = 44f;
	private const float Duration = 4f;
	private const int Damage = 2;

	public static void Cast(Player player, Vector2 direction)
	{
		var color = ElementVisuals.GetColor(ElementType.POISON);
		var cloud = new SkillProjectile
		{
			Lifetime = Duration,
			HitRadius = Radius,
			Pierce = true,
			RehitTime = 0.5f,
			OnHit = (self, enemy) => Combat.Hit(self.GlobalPosition, ElementType.POISON, enemy, Damage)
		};

		var ground = new Polygon2D { Polygon = Squashed(Skills.Circle(Radius, 20)), Color = new Color(0.55f, 0.15f, 0.85f, 0.35f) };
		cloud.AddChild(ground);
		cloud.AddChild(new CpuParticles2D
		{
			Amount = 30,
			Lifetime = 1.2,
			EmissionShape = CpuParticles2D.EmissionShapeEnum.Sphere,
			EmissionSphereRadius = Radius * 0.8f,
			Gravity = new Vector2(0, -15),
			ScaleAmountMin = 4f,
			ScaleAmountMax = 8f,
			Color = new Color(0.65f, 0.3f, 0.95f, 0.7f),
			ColorRamp = new Gradient { Colors = new[] { new Color(1, 1, 1, 0), Colors.White, new Color(1, 1, 1, 0) } }
		});

		/*fades in, stays, fades out at the end*/
		cloud.Modulate = new Color(1, 1, 1, 0);
		var tween = cloud.CreateTween();
		tween.TweenProperty(cloud, "modulate:a", 1.0, 0.3);
		tween.TweenInterval(Duration - 0.8);
		tween.TweenProperty(cloud, "modulate:a", 0.0, 0.5);

		player.GetParent().AddChild(cloud);
		cloud.GlobalPosition = player.GlobalPosition + direction * Distance;
	}

	/*flattens a circle so it looks like it is lying on the ground*/
	private static Vector2[] Squashed(Vector2[] circle)
	{
		for (int i = 0; i < circle.Length; i++)
			circle[i].Y *= 0.6f;
		return circle;
	}
}
