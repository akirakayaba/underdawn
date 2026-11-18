//Name:			Tutorial.cs
//Description:	Step by step hints at the top of the screen the first time the
//				player is in the first level. each step waits until the player
//				has done it, the keys shown follow the player's controls.
//				explains the element stealing (one element at a time) system.
//				added by Hud.cs, marked done in the save (SaveManager.cs)

using System;
using System.Linq;
using Godot;

public partial class Tutorial : PanelContainer
{
	public const string FirstLevel = "res://grassland.tscn";

	private Player player;
	private Label stepLabel;
	private Label textLabel;
	private int step;
	private Vector2 startPosition;
	private bool jumped;
	private bool dashed;
	private float doneTimer = -1f;

	private (string text, Func<bool> done)[] steps;

	public static bool ShouldShow(SceneTree tree) =>
		!SaveManager.Instance.TutorialDone && tree.CurrentScene?.SceneFilePath == FirstLevel;

	public Tutorial(Player player)
	{
		this.player = player;
	}

	/*godot needs an empty constructor for nodes*/
	public Tutorial()
	{
	}

	public override void _Ready()
	{
		startPosition = player.GlobalPosition;
		steps = new (string, Func<bool>)[]
		{
			($"Move with {Key("ui_up")} {Key("ui_left")} {Key("ui_down")} {Key("ui_right")}",
				() => player.GlobalPosition.DistanceTo(startPosition) > 80f),
			($"Attack with {Key("attack1")} and {Key("attack2")}.\nKeep holding the key after an attack for a charged hit.",
				() => Input.IsActionJustPressed("attack1") || Input.IsActionJustPressed("attack2")),
			($"Jump with {Key("jump")} and dash with {Key("dash")}.\nYou can't be hit while dashing.",
				() => jumped && dashed),
			("Defeat an enemy.\nEnemies drop an orb with the element they use.",
				() => GetTree().GetNodesInGroup(ElementOrb.GroupName).Count > 0 || player.CurrentElement != ElementType.NONE),
			($"Stand on the orb and press {Key("interact")} to steal its element.\nYou can only hold one element: absorbing a new one replaces it.",
				() => player.CurrentElement != ElementType.NONE),
			($"Press {Key("skill")} to use your element's skill.",
				() => player.SkillCooldownLeft > 0),
			($"Press {Key("burst")} to release your element in a big burst.\nYou lose the element, so go steal a new one!",
				() => player.CurrentElement == ElementType.NONE)
		};

		AnchorLeft = 0.5f;
		AnchorRight = 0.5f;
		OffsetLeft = -230;
		OffsetRight = 230;
		OffsetTop = 12;
		var style = new StyleBoxFlat { BgColor = new Color(0.08f, 0.08f, 0.1f, 0.85f), ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 8 };
		style.SetCornerRadiusAll(4);
		AddThemeStyleboxOverride("panel", style);

		var box = new VBoxContainer();
		AddChild(box);
		var header = new HBoxContainer();
		box.AddChild(header);
		stepLabel = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill, Modulate = new Color(1f, 0.85f, 0.4f) };
		header.AddChild(stepLabel);
		var skip = new Button { Text = "Skip Tutorial", FocusMode = FocusModeEnum.None };
		skip.Pressed += Finish;
		header.AddChild(skip);
		textLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(430, 0) };
		box.AddChild(textLabel);
		ShowStep();
	}

	public override void _Process(double delta)
	{
		if (doneTimer >= 0)
		{
			doneTimer -= (float)delta;
			if (doneTimer < 0)
				Finish();
			return;
		}
		jumped |= Input.IsActionJustPressed("jump");
		dashed |= Input.IsActionJustPressed("dash");
		if (!steps[step].done())
			return;

		step++;
		if (step < steps.Length)
		{
			ShowStep();
			return;
		}
		stepLabel.Text = "Tutorial complete";
		textLabel.Text = "Good luck!";
		doneTimer = 3f;
	}

	private void ShowStep()
	{
		stepLabel.Text = $"Tutorial {step + 1}/{steps.Length}";
		textLabel.Text = steps[step].text;
	}

	private void Finish()
	{
		SaveManager.Instance.TutorialDone = true;
		QueueFree();
	}

	private static string Key(string action) => "[" + SaveManager.KeyName(action) + "]";
}
