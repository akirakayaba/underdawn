//Name:			ElementAura.cs
//Description:	Visual effect on the player that matches their current element:
//				a glowing ring under their feet and particles that move
//				differently for each element (embers rise, snow falls...)
//				hidden when the player has no element. added by Player.cs

using Godot;

public partial class ElementAura : Node2D
{
	private const float FeetY = 16f;

	private Line2D ring;
	private CpuParticles2D particles;

	public override void _Ready()
	{
		var points = new Vector2[25];
		for (int i = 0; i < points.Length; i++)
		{
			var point = Vector2.Right.Rotated(Mathf.Tau * i / (points.Length - 1));
			points[i] = new Vector2(point.X * 13f, point.Y * 5f + FeetY);
		}
		ring = new Line2D { Points = points, Width = 1.5f };
		AddChild(ring);
		var pulse = ring.CreateTween().SetLoops();
		pulse.TweenProperty(ring, "modulate:a", 0.35, 0.6);
		pulse.TweenProperty(ring, "modulate:a", 1.0, 0.6);

		particles = new CpuParticles2D
		{
			Amount = 20,
			Lifetime = 1.0,
			LocalCoords = false,
			EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
			EmissionRectExtents = new Vector2(10, 14),
			ScaleAmountMin = 2f,
			ScaleAmountMax = 3f,
			ColorRamp = new Gradient { Colors = new[] { new Color(1, 1, 1, 0), Colors.White, new Color(1, 1, 1, 0) } }
		};
		/*the ring draws under the player (this node sits before the sprite), the
		  particles go on the player itself so they draw in front of the sprite*/
		GetParent().CallDeferred(Node.MethodName.AddChild, particles);
		SetElement(ElementType.NONE);
	}

	public void SetElement(ElementType element)
	{
		Visible = element != ElementType.NONE;
		particles.Visible = Visible;
		if (!Visible)
			return;
		var color = ElementVisuals.GetColor(element);
		ring.DefaultColor = new Color(color, 0.8f);
		particles.Color = color;

		/*how the particles move for each element*/
		var (gravity, speed, orbit) = element switch
		{
			ElementType.FIRE => (new Vector2(0, -60), 10f, 0f),
			ElementType.ICE => (new Vector2(0, 15), 4f, 0f),
			ElementType.LIGHTNING => (Vector2.Zero, 60f, 0f),
			ElementType.WIND => (new Vector2(0, -10), 0f, 1.2f),
			ElementType.EARTH => (new Vector2(0, 45), 8f, 0f),
			ElementType.WATER => (new Vector2(0, 35), 3f, 0f),
			_ => (new Vector2(0, -20), 6f, 0f)
		};
		particles.Gravity = gravity;
		particles.InitialVelocityMin = speed * 0.5f;
		particles.InitialVelocityMax = speed;
		particles.Spread = 180f;
		particles.OrbitVelocityMin = orbit;
		particles.OrbitVelocityMax = orbit;
		particles.Lifetime = element == ElementType.LIGHTNING ? 0.25 : 1.0;
		particles.Restart();
	}
}
