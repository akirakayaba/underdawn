//Name:			HitEffects.cs
//Description:	Effects that make hits feel stronger: white flash on the
//				sprite that got hit, camera shake, dust burst and hitstop
//				(a very short slow down of the whole game)

using Godot;

public static class HitEffects
{
	/*flashes the sprite white then fades back (uses SelfModulate so it
	  doesnt mess with the status effect tint on Modulate)*/
	public static void Flash(CanvasItem sprite)
	{
		if (sprite == null)
			return;
		sprite.SelfModulate = new Color(3f, 3f, 3f);
		sprite.CreateTween().TweenProperty(sprite, "self_modulate", Colors.White, 0.15);
	}

	public static void Shake(Camera2D camera, float strength, float time)
	{
		if (camera == null)
			return;
		var tween = camera.CreateTween();
		tween.TweenMethod(Callable.From<float>(left =>
			camera.Offset = new Vector2(GD.Randf() * 2 - 1, GD.Randf() * 2 - 1) * strength * left), 1f, 0f, time);
		tween.TweenCallback(Callable.From(() => camera.Offset = Vector2.Zero));
	}

	/*dust flying out on the ground, removes itself when done*/
	public static void Dust(Node parent, Vector2 globalPosition)
	{
		var dust = new CpuParticles2D
		{
			Amount = 18,
			OneShot = true,
			Explosiveness = 1f,
			Lifetime = 0.45,
			Direction = Vector2.Right,
			Spread = 180f,
			Gravity = Vector2.Zero,
			InitialVelocityMin = 50f,
			InitialVelocityMax = 110f,
			DampingMin = 120f,
			DampingMax = 180f,
			ScaleAmountMin = 2f,
			ScaleAmountMax = 3.5f,
			Color = new Color(0.85f, 0.78f, 0.6f, 0.8f),
			ColorRamp = new Gradient { Colors = new[] { new Color(1, 1, 1, 1), new Color(1, 1, 1, 0) } },
			ZIndex = 1
		};
		parent.AddChild(dust);
		dust.GlobalPosition = globalPosition;
		dust.Finished += dust.QueueFree;
		dust.Emitting = true;
	}

	/*slows the whole game down for a moment (timer ignores time scale)*/
	public static void Hitstop(SceneTree tree, float time)
	{
		Engine.TimeScale = 0.05;
		tree.CreateTimer(time, true, false, true).Timeout += () => Engine.TimeScale = 1;
	}
}
