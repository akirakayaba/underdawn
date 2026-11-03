//Name:			ElementOrb.cs
//Description:	Orb an enemy drops when it dies, walking into it steals the
//				enemy's element (player's element and skill change to it)
//				pulls towards the player when close, fades away after a while

using Godot;

public partial class ElementOrb : Node2D
{
	private const float PickupRange = 16f;
	private const float MagnetRange = 60f;
	private const float MagnetSpeed = 160f;
	private const float Lifetime = 12f;

	public ElementType Element { get; set; }

	private float timeLeft = Lifetime;

	public static void Drop(Node parent, Vector2 globalPosition, ElementType element)
	{
		if (element == ElementType.NONE)
			return;
		var orb = new ElementOrb { Element = element };
		parent.CallDeferred(Node.MethodName.AddChild, orb);
		orb.Position = globalPosition;
	}

	public override void _Ready()
	{
		var color = ElementVisuals.GetColor(Element);
		AddChild(new Polygon2D { Polygon = Circle(6f), Color = color });
		AddChild(new Polygon2D { Polygon = Circle(3f), Color = Colors.White });
		AddChild(new CpuParticles2D
		{
			Amount = 8,
			Lifetime = 0.8,
			EmissionShape = CpuParticles2D.EmissionShapeEnum.Sphere,
			EmissionSphereRadius = 6f,
			Gravity = new Vector2(0, -30),
			ScaleAmountMin = 1f,
			ScaleAmountMax = 2f,
			Color = color
		});

		/*bobs up and down so it stands out*/
		var tween = CreateTween().SetLoops();
		tween.TweenProperty(this, "scale", Vector2.One * 1.25f, 0.4);
		tween.TweenProperty(this, "scale", Vector2.One, 0.4);
	}

	public override void _PhysicsProcess(double delta)
	{
		timeLeft -= (float)delta;
		if (timeLeft <= 0)
		{
			QueueFree();
			return;
		}
		Modulate = new Color(1, 1, 1, Mathf.Min(timeLeft, 1f));

		var player = GetTree().GetFirstNodeInGroup(Player.GroupName) as Player;
		if (player == null || player.IsDead)
			return;

		float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
		if (distance <= PickupRange)
		{
			player.SetElement(Element);
			FloatingText.Show(player, "+" + Element.ToString().Capitalize(), ElementVisuals.GetColor(Element), 14f);
			QueueFree();
		}
		else if (distance <= MagnetRange)
		{
			GlobalPosition = GlobalPosition.MoveToward(player.GlobalPosition, MagnetSpeed * (float)delta);
		}
	}

	private static Vector2[] Circle(float radius)
	{
		var points = new Vector2[12];
		for (int i = 0; i < points.Length; i++)
			points[i] = Vector2.Right.Rotated(Mathf.Tau * i / points.Length) * radius;
		return points;
	}
}
