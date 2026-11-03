//Name:			ElementVisuals.cs
//Description:	Colors and status effect names for each element
//				(status effect details are in elementtypematchups.txt)

using Godot;

public static class ElementVisuals
{
	public static Color GetColor(ElementType element) => element switch
	{
		ElementType.FIRE => new Color(1f, 0.5f, 0.35f),
		ElementType.EARTH => new Color(0.8f, 0.65f, 0.4f),
		ElementType.WATER => new Color(0.4f, 0.55f, 1f),
		ElementType.LIGHTNING => new Color(1f, 1f, 0.4f),
		ElementType.WIND => new Color(0.6f, 1f, 0.7f),
		ElementType.ICE => new Color(0.7f, 1f, 1f),
		ElementType.POISON => new Color(0.75f, 0.45f, 1f),
		_ => Colors.White
	};

	public static string GetStatusName(ElementType element) => element switch
	{
		ElementType.FIRE => "Burning!",
		ElementType.EARTH => "Tremor!",
		ElementType.WATER => "Drenched!",
		ElementType.LIGHTNING => "Paralyzed!",
		ElementType.WIND => "Blown Away!",
		ElementType.ICE => "Frozen!",
		ElementType.POISON => "Poisoned!",
		_ => ""
	};
}
