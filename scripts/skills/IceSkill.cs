//Name:			IceSkill.cs
//Description:	Ice spikes: two rings of spikes burst out of the ground around
//				the player, freezing every enemy they hit

using Godot;

public static class IceSkill
{
	private const float InnerRadius = 30f;
	private const float OuterRadius = 56f;
	private const int Damage = 12;

	public static void Cast(Player player, Vector2 direction)
	{
		var center = player.GlobalPosition;
		Skills.HitArea(player, center, OuterRadius + 8f, Damage, ElementType.ICE);
		SpawnRing(player.GetParent(), center, InnerRadius, 8, 0f);
		SpawnRing(player.GetParent(), center, OuterRadius, 12, 0.08f);
		HitEffects.Shake(player.GetViewport().GetCamera2D(), 2f, 0.15f);
	}

	private static void SpawnRing(Node parent, Vector2 center, float radius, int count, float delay)
	{
		for (int i = 0; i < count; i++)
		{
			var offset = Vector2.Right.Rotated(Mathf.Tau * i / count) * radius;
			/*spikes are squashed vertically so the ring looks flat on the ground*/
			SpawnSpike(parent, center + new Vector2(offset.X, offset.Y * 0.6f), delay);
		}
	}

	private static void SpawnSpike(Node parent, Vector2 position, float delay)
	{
		var spike = new Polygon2D
		{
			Polygon = new[] { new Vector2(-5, 0), new Vector2(0, -18), new Vector2(5, 0) },
			Color = ElementVisuals.GetColor(ElementType.ICE),
			Scale = new Vector2(1, 0)
		};
		spike.AddChild(new Polygon2D
		{
			Polygon = new[] { new Vector2(-1, -2), new Vector2(0, -14), new Vector2(2, -2) },
			Color = Colors.White
		});
		parent.AddChild(spike);
		spike.GlobalPosition = position;

		var tween = spike.CreateTween();
		tween.TweenInterval(delay);
		tween.TweenProperty(spike, "scale", Vector2.One, 0.08);
		tween.TweenInterval(0.45);
		tween.TweenProperty(spike, "modulate:a", 0.0, 0.25);
		tween.TweenCallback(Callable.From(spike.QueueFree));
	}
}
