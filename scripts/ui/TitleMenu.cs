//Name:			TitleMenu.cs
//Description:	Functionality for title menu buttons
//				(continue only shows up when there is a saved playthrough)

using Godot;

public partial class TitleMenu : Control
{
	private const string FirstLevel = "res://grassland.tscn";

	public override void _Ready()
	{
		var continueButton = GetNode<Button>("VBoxContainer/ContinueButton");
		continueButton.Visible = SaveManager.Instance.HasRun;
		(continueButton.Visible ? continueButton : GetNode<Button>("VBoxContainer/StartButton")).GrabFocus();
	}

	private void ContinueGame()
	{
		SaveManager.Instance.ContinueRun();
	}

	/*a new game throws away the saved playthrough*/
	private void StartGame()
	{
		SaveManager.Instance.DeleteRun();
		SceneManager.Instance.ChangeScene(FirstLevel);
	}

	private void Options()
	{
		OptionsMenu.Open(GetNode<Button>("VBoxContainer/OptionsButton"));
	}

	private void QuitGame()
	{
		GetTree().Quit();
	}
}
