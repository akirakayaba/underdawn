//Name:			SaveManager.cs
//Description:	Autoload that saves to user://save.cfg
//				settings	music / sfx volume, fullscreen
//				controls	the key for every action the player remapped
//				run			the current playthrough (level, position, health, element)
//				progress	whether the tutorial is done

using System.Collections.Generic;
using Godot;

public partial class SaveManager : Node
{
	public static SaveManager Instance { get; private set; }

	public const string SavePath = "user://save.cfg";

	/*actions the player can remap in the options menu (shown name, action)*/
	public static readonly (string Label, string Action)[] RemappableActions =
	{
		("Move Up", "ui_up"), ("Move Down", "ui_down"), ("Move Left", "ui_left"), ("Move Right", "ui_right"),
		("Attack 1", "attack1"), ("Attack 2", "attack2"), ("Skill", "skill"), ("Burst", "burst"),
		("Absorb Element", "interact"), ("Jump", "jump"), ("Dash", "dash")
	};

	private readonly ConfigFile config = new();
	private readonly Dictionary<string, Godot.Collections.Array<InputEvent>> defaultControls = new();
	private bool pendingRun;

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;
		foreach (var (_, action) in RemappableActions)
		{
			if (InputMap.HasAction(action))
				defaultControls[action] = InputMap.ActionGetEvents(action);
		}

		/*a missing save file is fine (first time playing)*/
		var error = config.Load(SavePath);
		if (error != Error.Ok && error != Error.FileNotFound)
			GD.PushError($"could not load {SavePath}: {error}");
		ApplySettings();
		ApplyControls();
	}

	private void Save()
	{
		var error = config.Save(SavePath);
		if (error != Error.Ok)
			GD.PushError($"could not save {SavePath}: {error}");
	}

	// ---- settings ----

	public float MusicVolume
	{
		get => (float)config.GetValue("settings", "music_volume", 0.8f);
		set => SetSetting("music_volume", Mathf.Clamp(value, 0f, 1f));
	}

	public float SfxVolume
	{
		get => (float)config.GetValue("settings", "sfx_volume", 0.8f);
		set => SetSetting("sfx_volume", Mathf.Clamp(value, 0f, 1f));
	}

	public bool Fullscreen
	{
		get => (bool)config.GetValue("settings", "fullscreen", false);
		set => SetSetting("fullscreen", value);
	}

	private void SetSetting(string key, Variant value)
	{
		config.SetValue("settings", key, value);
		ApplySettings();
		Save();
	}

	private void ApplySettings()
	{
		SetBusVolume("Music", MusicVolume);
		SetBusVolume("SFX", SfxVolume);
		if (DisplayServer.GetName() != "headless")
			DisplayServer.WindowSetMode(Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
	}

	private static void SetBusVolume(string bus, float volume)
	{
		int index = AudioServer.GetBusIndex(bus);
		if (index < 0)
			return;
		AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb(volume));
		AudioServer.SetBusMute(index, volume <= 0.001f);
	}

	// ---- controls ----

	/*replaces every key on the action with this one key*/
	public void Rebind(string action, InputEventKey key)
	{
		InputMap.ActionEraseEvents(action);
		InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key.PhysicalKeycode });
		config.SetValue("controls", action, (long)key.PhysicalKeycode);
		Save();
	}

	public void ResetControls()
	{
		foreach (var (action, events) in defaultControls)
		{
			InputMap.ActionEraseEvents(action);
			foreach (var inputEvent in events)
				InputMap.ActionAddEvent(action, inputEvent);
		}
		if (config.HasSection("controls"))
			config.EraseSection("controls");
		Save();
	}

	private void ApplyControls()
	{
		foreach (var (_, action) in RemappableActions)
		{
			if (!InputMap.HasAction(action) || !config.HasSectionKey("controls", action))
				continue;
			InputMap.ActionEraseEvents(action);
			InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = (Key)(long)config.GetValue("controls", action) });
		}
	}

	/*name of the first key on the action, for the hud / tutorial / options menu*/
	public static string KeyName(string action)
	{
		if (!InputMap.HasAction(action))
			return "?";
		foreach (var inputEvent in InputMap.ActionGetEvents(action))
		{
			if (inputEvent is InputEventKey key)
				return key.PhysicalKeycode != Key.None ? key.AsTextPhysicalKeycode() : key.AsTextKeycode();
		}
		return "?";
	}

	// ---- run (the current playthrough) ----

	public bool HasRun => config.HasSection("run");

	public void SaveRun(Player player)
	{
		config.SetValue("run", "scene", player.GetTree().CurrentScene.SceneFilePath);
		config.SetValue("run", "x", player.GlobalPosition.X);
		config.SetValue("run", "y", player.GlobalPosition.Y);
		config.SetValue("run", "health", player.Health);
		config.SetValue("run", "element", (int)player.CurrentElement);
		Save();
	}

	public void DeleteRun()
	{
		pendingRun = false;
		if (!HasRun)
			return;
		config.EraseSection("run");
		Save();
	}

	/*loads the saved level, the player picks the rest up in its _Ready*/
	public void ContinueRun()
	{
		if (!HasRun)
			return;
		pendingRun = true;
		SceneManager.Instance.ChangeScene((string)config.GetValue("run", "scene"));
	}

	public bool TryTakePendingRun(out Vector2 position, out int health, out ElementType element)
	{
		position = Vector2.Zero;
		health = 0;
		element = ElementType.NONE;
		if (!pendingRun || !HasRun)
			return false;
		pendingRun = false;
		position = new Vector2((float)config.GetValue("run", "x"), (float)config.GetValue("run", "y"));
		health = (int)config.GetValue("run", "health");
		element = (ElementType)(int)config.GetValue("run", "element");
		return true;
	}

	// ---- progress ----

	public bool TutorialDone
	{
		get => (bool)config.GetValue("progress", "tutorial_done", false);
		set
		{
			config.SetValue("progress", "tutorial_done", value);
			Save();
		}
	}

	/*options menu "reset save": forgets the playthrough and the tutorial, keeps settings and controls*/
	public void ResetProgress()
	{
		DeleteRun();
		if (config.HasSection("progress"))
			config.EraseSection("progress");
		Save();
	}
}
