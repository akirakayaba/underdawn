//Name:			ElementOrb.cs
//Description:	Orb an enemy drops when it dies. standing next to it shows a
//				prompt, pressing the absorb key (interact) steals the enemy's
//				element. the player only holds one element, so absorbing
//				replaces the current one. fades away after a while

using System.Linq;
using Godot;

public partial class ElementOrb : Node2D
{
	public const string GroupName = "element_orbs";

	private const float PickupRange = 24f;
	private const float Lifetime = 20f;

	public ElementType Element { get; set; }

	private float timeLeft = Lifetime;
	private Label prompt;

	/*one key press absorbs one orb, even if another orb becomes the closest in the same frame*/
	private static ulong lastAbsorbFrame = ulong.MaxValue;

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
		AddToGroup(GroupName);
		var color = ElementVisuals.GetColor(Element);

		/*the orb itself pulses, the prompt stays still*/
		var visual = new Node2D();
		visual.AddChild(new Polygon2D { Polygon = Circle(6f), Color = color });
		visual.AddChild(new Polygon2D { Polygon = Circle(3f), Color = Colors.White });
		visual.AddChild(new CpuParticles2D
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
		AddChild(visual);
		var tween = visual.CreateTween().SetLoops();
		tween.TweenProperty(visual, "scale", Vector2.One * 1.25f, 0.4);
		tween.TweenProperty(visual, "scale", Vector2.One, 0.4);

		prompt = new Label
		{
			Visible = false,
			ZIndex = 10,
			HorizontalAlignment = HorizontalAlignment.Center,
			Size = new Vector2(160, 30),
			Position = new Vector2(-80, -40)
		};
		prompt.AddThemeFontSizeOverride("font_size", 10);
		prompt.AddThemeConstantOverride("outline_size", 4);
		prompt.AddThemeColorOverride("font_outline_color", Colors.Black);
		AddChild(prompt);
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
		bool canAbsorb = player != null && !player.IsDead && IsClosestOrbInRange(player);
		prompt.Visible = canAbsorb;
		if (!canAbsorb)
			return;

		prompt.Text = PromptText(player.CurrentElement);
		if (Input.IsActionJustPressed("interact") && lastAbsorbFrame != Engine.GetPhysicsFrames())
		{
			lastAbsorbFrame = Engine.GetPhysicsFrames();
			player.AbsorbElement(Element);
			QueueFree();
		}
	}

	/*only the closest orb answers the key when a few are on top of each other*/
	private bool IsClosestOrbInRange(Player player)
	{
		float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
		if (distance > PickupRange)
			return false;
		return GetTree().GetNodesInGroup(GroupName).OfType<ElementOrb>()
			.All(orb => orb == this || orb.IsQueuedForDeletion() || orb.GlobalPosition.DistanceTo(player.GlobalPosition) >= distance);
	}

	private string PromptText(ElementType current)
	{
		string name = Element.ToString().Capitalize();
		string text = $"[{SaveManager.KeyName("interact")}] Absorb {name}";
		if (current == Element)
			return text + "\n(you already have it)";
		if (current != ElementType.NONE)
			return text + $"\n(replaces {current.ToString().Capitalize()})";
		return text;
	}

	private static Vector2[] Circle(float radius)
	{
		var points = new Vector2[12];
		for (int i = 0; i < points.Length; i++)
			points[i] = Vector2.Right.Rotated(Mathf.Tau * i / points.Length) * radius;
		return points;
	}
}
