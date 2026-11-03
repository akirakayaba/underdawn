//Name:			MaskCollision.cs
//Description:	Makes collisions from a mask image, anything not transparent
//				in the mask is blocked (used for the black pit in grassland)
//				the mask can be smaller than the map, MaskScale stretches it
//				to fit (grassland_pit_mask.png is 1/4 the size of the ground)

using Godot;

public partial class MaskCollision : Node2D
{
	[Export] public Texture2D Mask;
	[Export] public float MaskScale = 4f;

	/*higher = simpler collision shapes but less exact edges*/
	[Export] public float Simplify = 1.5f;

	public override void _Ready()
	{
		if (Mask == null)
			return;

		var bitmap = new Bitmap();
		bitmap.CreateFromImageAlpha(Mask.GetImage());
		var polygons = bitmap.OpaqueToPolygons(new Rect2I(Vector2I.Zero, bitmap.GetSize()), Simplify);

		var body = new StaticBody2D();
		foreach (Vector2[] polygon in polygons)
		{
			for (int i = 0; i < polygon.Length; i++)
				polygon[i] *= MaskScale;
			body.AddChild(new CollisionPolygon2D { Polygon = polygon });
		}
		AddChild(body);
	}
}
