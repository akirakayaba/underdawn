//Name:			OptionsMenu.cs
//Description:	Options screen (opened from the title screen and the pause menu):
//				music / sfx volume, fullscreen, remapping every control,
//				resetting the controls and resetting the save.
//				everything is saved right away (SaveManager.cs)

using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class OptionsMenu : CanvasLayer
{
	private const string ScenePath = "res://scenes/ui/options_menu.tscn";
	private static readonly Color DuplicateKeyColor = new(1f, 0.45f, 0.4f);

	public static bool IsOpen { get; private set; }

	private Control returnFocus;
	private GridContainer controlsGrid;
	private readonly Dictionary<string, Button> keyButtons = new();
	private string waitingAction;

	/*opens the menu on top of everything, focus goes back to returnFocusTo when it closes*/
	public static void Open(Control returnFocusTo)
	{
		var menu = GD.Load<PackedScene>(ScenePath).Instantiate<OptionsMenu>();
		menu.returnFocus = returnFocusTo;
		returnFocusTo.GetTree().Root.AddChild(menu);
	}

	public override void _Ready()
	{
		IsOpen = true;
		var save = SaveManager.Instance;
		const string Box = "Center/Panel/Margin/Box/";

		var music = GetNode<HSlider>(Box + "MusicRow/MusicSlider");
		music.Value = save.MusicVolume * 100;
		music.ValueChanged += value => save.MusicVolume = (float)value / 100f;
		var sfx = GetNode<HSlider>(Box + "SfxRow/SfxSlider");
		sfx.Value = save.SfxVolume * 100;
		sfx.ValueChanged += value => save.SfxVolume = (float)value / 100f;
		var fullscreen = GetNode<CheckButton>(Box + "FullscreenCheck");
		fullscreen.ButtonPressed = save.Fullscreen;
		fullscreen.Toggled += on => save.Fullscreen = on;

		controlsGrid = GetNode<GridContainer>(Box + "Scroll/ControlsGrid");
		BuildControls();

		GetNode<Button>(Box + "Buttons/ResetControlsButton").Pressed += () =>
		{
			save.ResetControls();
			RefreshControls();
		};
		var confirm = GetNode<ConfirmationDialog>("ResetSaveConfirm");
		GetNode<Button>(Box + "Buttons/ResetSaveButton").Pressed += () => confirm.PopupCentered();
		confirm.Confirmed += save.ResetProgress;
		GetNode<Button>(Box + "Buttons/BackButton").Pressed += Close;

		music.GrabFocus();
	}

	public override void _ExitTree()
	{
		IsOpen = false;
	}

	private void BuildControls()
	{
		foreach (var (label, action) in SaveManager.RemappableActions)
		{
			if (!InputMap.HasAction(action))
				continue;
			controlsGrid.AddChild(new Label { Text = label, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
			var button = new Button { CustomMinimumSize = new Vector2(140, 0) };
			button.Pressed += () => StartRebind(action);
			controlsGrid.AddChild(button);
			keyButtons[action] = button;
		}
		RefreshControls();
	}

	/*shows the key for each action, keys used by more than one action are red*/
	private void RefreshControls()
	{
		var keys = keyButtons.Keys.ToDictionary(action => action, SaveManager.KeyName);
		foreach (var (action, button) in keyButtons)
		{
			bool duplicate = keys.Count(pair => pair.Value == keys[action]) > 1;
			button.Text = keys[action];
			button.Modulate = duplicate ? DuplicateKeyColor : Colors.White;
			button.TooltipText = duplicate ? "This key is used by more than one action" : "";
		}
	}

	private void StartRebind(string action)
	{
		waitingAction = action;
		keyButtons[action].Text = "Press a key... (Esc = cancel)";
	}

	/*runs before the pause manager (added later to the tree) so Esc closes this menu, not the pause menu*/
	public override void _Input(InputEvent @event)
	{
		if (waitingAction != null && @event is InputEventKey { Pressed: true, Echo: false } key)
		{
			if (key.PhysicalKeycode != Key.Escape)
				SaveManager.Instance.Rebind(waitingAction, key);
			waitingAction = null;
			RefreshControls();
			GetViewport().SetInputAsHandled();
		}
		else if (waitingAction == null && @event.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	private void Close()
	{
		QueueFree();
		if (IsInstanceValid(returnFocus))
			returnFocus.GrabFocus();
	}
}
