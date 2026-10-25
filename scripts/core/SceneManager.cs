//Name:			SceneManager.cs
//Description:	Gives the ability to change scenes in the game

using Godot;
using System;

public partial class SceneManager : Node
{
    public void ChangeScene(string scene)
    {
        GetTree().ChangeSceneToFile(scene);
    }
}
