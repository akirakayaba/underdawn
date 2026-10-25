using Godot;
using System;

public partial class MainMenu : Control
{
    private void StartGame()
    {
        SceneManager.Instance.ChangeScene("res://scenes/levels/test_area.tscn");
    }

    private void Options()
    {
        throw new NotImplementedException();
    }

    private void QuitGame()
    {
        GetTree().Quit();
    }
}
