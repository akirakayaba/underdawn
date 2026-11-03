//Name:			FloatingText.cs
//Description:	Text that pops up above an entity, floats up and fades out
//				(damage numbers, bonus damage, status effects being added)

using Godot;

public static class FloatingText
{
	public static readonly Color DamageColor = Colors.White;
	public static readonly Color BonusColor = Colors.Orange;

	public static void Show(Node2D target, string text, Color color)
	{
		var label = new Label
		{
			Text = text,
			Modulate = color,
			ZIndex = 10
		};
		label.AddThemeFontSizeOverride("font_size", 12);
		label.AddThemeConstantOverride("outline_size", 4);
		label.AddThemeColorOverride("font_outline_color", Colors.Black);

		/*added to the target's parent so the text stays if the target dies*/
		target.GetParent().AddChild(label);
		label.GlobalPosition = target.GlobalPosition + new Vector2(-8, -40);

		var tween = label.CreateTween();
		tween.SetParallel();
		tween.TweenProperty(label, "position:y", label.Position.Y - 20, 0.6);
		tween.TweenProperty(label, "modulate:a", 0.0, 0.6);
		tween.Chain().TweenCallback(Callable.From(label.QueueFree));
	}
}
