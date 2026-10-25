//Name:			PauseManager.cs
//Description:	Allows player to pause the game, and go to
//              the main menu correctly

using Godot;

public partial class PauseManager : Node
{
    //singleton static reference for global use
    public static PauseManager Instance { get; private set; }

    private Node PauseMenuInstance;

    private PackedScene PauseMenuScene = GD.Load<PackedScene>("res://scenes/ui/pause_ui.tscn");

    public bool isPaused = false;

    public override void _Ready()
    {
        Instance = this;

        //ensures this node will keep running when
        //using gettree().paused
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
            Pauser();
    }

    public void Pauser()
    {
        string currentScenePath = GetTree().CurrentScene.SceneFilePath;

        if (currentScenePath == "res://scenes/ui/title_screen_ui.tscn")
            return;

        if (isPaused)
        {
            isPaused = false;
            GetTree().Paused = false;

            if (PauseMenuInstance is CanvasLayer pauseCanvas)
                pauseCanvas.Visible = false;
        }
        else
        {
            isPaused = true;
            GetTree().Paused = true;

            //instantiate and show the pause menu ui
            if (PauseMenuInstance == null && PauseMenuScene != null)
            {
                PauseMenuInstance = PauseMenuScene.Instantiate();
                GetTree().Root.AddChild(PauseMenuInstance);
            }
            else if (PauseMenuInstance is CanvasLayer pauseCanvas)
                pauseCanvas.Visible = true;

            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public void MainMenu()
    {
        if (PauseMenuInstance != null)
        {
            PauseMenuInstance.QueueFree();
            PauseMenuInstance = null;
        }
        isPaused = false;
        GetTree().Paused = false;

        SceneManager.Instance.ChangeScene("res://scenes/ui/title_screen_ui.tscn");
    }
}
