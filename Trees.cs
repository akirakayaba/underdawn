using Godot;

public partial class Trees : Node2D
{
	// Tweak these to fit your trunks
	[Export] public float TrunkRadiusRatio = 0.12f; // radius as a fraction of sprite width
	[Export] public float TrunkHeightRatio = 0.0f;  // extra lift above the bottom edge, as a fraction of height

	public override void _Ready()
	{
		foreach (Node child in GetChildren())
		{
			if (child is Sprite2D tree && tree.Texture != null)
			{
				AddCollision(tree);
			}
		}
	}

	private void AddCollision(Sprite2D tree)
	{
		Vector2 size = tree.RegionEnabled ? tree.RegionRect.Size : tree.Texture.GetSize();
		float radius = size.X * TrunkRadiusRatio;

		var circle = new CircleShape2D { Radius = radius };
		var shape = new CollisionShape2D { Shape = circle };

		// Bottom-center of the sprite in its local space
		Vector2 bottomCenter = tree.Centered
			? new Vector2(0, size.Y / 2f)
			: new Vector2(size.X / 2f, size.Y);

		shape.Position = bottomCenter + tree.Offset - new Vector2(0, radius + size.Y * TrunkHeightRatio);

		var body = new StaticBody2D();
		body.AddChild(shape);
		tree.AddChild(body);
	}
}
