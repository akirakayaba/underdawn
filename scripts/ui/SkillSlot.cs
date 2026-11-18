//Name:			SkillSlot.cs
//Description:	One slot in the hud's skill bar: key, skill name, border in
//				the skill's color, and a dark overlay + seconds while on cooldown

using Godot;

public partial class SkillSlot : Panel
{
	/*the input action of this slot, the key shown is whatever it is bound to*/
	[Export] public string Action = "skill";

	private static readonly Color Unavailable = new(0.35f, 0.35f, 0.35f);

	private Label nameLabel;
	private Label keyLabel;
	private Label cooldownLabel;
	private ColorRect icon;
	private ColorRect cooldownOverlay;
	private StyleBoxFlat style;

	public override void _Ready()
	{
		nameLabel = GetNode<Label>("NameLabel");
		cooldownLabel = GetNode<Label>("CooldownLabel");
		icon = GetNode<ColorRect>("Icon");
		cooldownOverlay = GetNode<ColorRect>("CooldownOverlay");
		keyLabel = GetNode<Label>("KeyLabel");
		keyLabel.Text = SaveManager.KeyName(Action);

		/*own copy of the style so each slot can have its own border color*/
		style = (StyleBoxFlat)GetThemeStylebox("panel").Duplicate();
		AddThemeStyleboxOverride("panel", style);
		SetCooldown(0, 0);
	}

	public void SetSkill(string skillName, Color color, bool available)
	{
		nameLabel.Text = skillName;
		icon.Color = available ? color.Darkened(0.35f) : Unavailable.Darkened(0.5f);
		style.BorderColor = available ? color : Unavailable;
		nameLabel.Modulate = available ? Colors.White : Unavailable;
	}

	/*overlay shrinks from the top as the cooldown runs out*/
	public void SetCooldown(float timeLeft, float cooldown)
	{
		/*keeps up with keys changed in the options menu*/
		string key = SaveManager.KeyName(Action);
		if (keyLabel.Text != key)
			keyLabel.Text = key;

		bool coolingDown = timeLeft > 0 && cooldown > 0;
		cooldownOverlay.Visible = coolingDown;
		cooldownLabel.Visible = coolingDown;
		if (!coolingDown)
			return;
		cooldownOverlay.AnchorTop = 1f - timeLeft / cooldown;
		cooldownLabel.Text = timeLeft.ToString("0.0");
	}
}
