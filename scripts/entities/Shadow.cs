//Name:			Shadow.cs
//Description:	Oval shadow drawn under the player / enemies
//				(added as the first child so it draws under the sprite)

using Godot;

public static class Shadow
{
	public static Polygon2D AddTo(Node2D entity, float width, float feetY)
	{
		const int Points = 16;
		var points = new Vector2[Points];
		for (int i = 0; i < Points; i++)
		{
			float angle = Mathf.Tau * i / Points;
			points[i] = new Vector2(Mathf.Cos(angle) * width / 2, Mathf.Sin(angle) * width / 5);
		}

		var shadow = new Polygon2D
		{
			Name = "Shadow",
			Polygon = points,
			Color = new Color(0, 0, 0, 0.3f),
			Position = new Vector2(0, feetY)
		};
		entity.AddChild(shadow);
		entity.MoveChild(shadow, 0);
		return shadow;
	}
}
