using Godot;

public partial class AutoCollisions : Node2D
{
	public enum ShapeType { Circle, Rectangle }

	[Export] public ShapeType Shape = ShapeType.Rectangle;

	// Size of the collision as a fraction of the sprite's size
	[Export] public float WidthRatio = 0.8f;
	[Export] public float HeightRatio = 0.4f;

	// Only used for Circle: radius as a fraction of sprite width
	[Export] public float RadiusRatio = 0.12f;

	public override void _Ready()
	{	
		foreach (Node child in GetChildren())
		{
			if (child is Sprite2D sprite && sprite.Texture != null)
			{
				AddCollision(sprite);
			}
		}
	}

	private void AddCollision(Sprite2D sprite)
	{
		Vector2 size = sprite.RegionEnabled ? sprite.RegionRect.Size : sprite.Texture.GetSize();

		var collider = new CollisionShape2D();
		float bottomOffset;

		if (Shape == ShapeType.Circle)
		{
			float radius = size.X * RadiusRatio;
			collider.Shape = new CircleShape2D { Radius = radius };
			bottomOffset = radius;
		}
		else
		{
			var rect = new Vector2(size.X * WidthRatio, size.Y * HeightRatio);
			collider.Shape = new RectangleShape2D { Size = rect };
			bottomOffset = rect.Y / 2f;
		}

		// Bottom-center of the sprite in its local space
		Vector2 bottomCenter = sprite.Centered
			? new Vector2(0, size.Y / 2f)
			: new Vector2(size.X / 2f, size.Y);

		collider.Position = bottomCenter + sprite.Offset - new Vector2(0, bottomOffset);

		var body = new StaticBody2D();
		body.AddChild(collider);
		sprite.AddChild(body);
	}
}
