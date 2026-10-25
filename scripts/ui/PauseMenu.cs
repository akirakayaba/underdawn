using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
	private void ResumeGame()
	{
		PauseManager.Instance.Pauser();
	}

	private void SaveGame()
	{
		throw new NotImplementedException();
	}

	private void MainMenu()
	{
		PauseManager.Instance.MainMenu();
	}
}
