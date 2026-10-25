//Name:			SceneManager.cs
//Description:	Gives the ability to change scenes in the game

using Godot;

public partial class SceneManager : Node
{
    //singleton static reference for global use
    public static SceneManager Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
    }

    public void ChangeScene(string scenePath)
    {
        GetTree().ChangeSceneToFile(scenePath);
    }
}
