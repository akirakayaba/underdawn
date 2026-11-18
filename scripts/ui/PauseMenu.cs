//Name:			PauseMenu.cs
//Description:	Functionality for pause menu buttons

using System.Threading.Tasks;
using Godot;

public partial class PauseMenu : CanvasLayer
{
	private void ResumeGame()
	{
		PauseManager.Instance.Pauser();
	}

	/*saves the playthrough (SaveManager.cs), the button says Saved! for a moment*/
	private async void SaveGame()
	{
		var button = GetNode<Button>("VBoxContainer/SaveButton");
		if (GetTree().GetFirstNodeInGroup(Player.GroupName) is not Player player || player.IsDead)
		{
			button.Text = "Can't save now";
		}
		else
		{
			SaveManager.Instance.SaveRun(player);
			button.Text = "Saved!";
		}
		await ToSignal(GetTree().CreateTimer(1.5, true, false, true), SceneTreeTimer.SignalName.Timeout);
		if (IsInstanceValid(button))
			button.Text = "Save Game";
	}

	private void Options()
	{
		OptionsMenu.Open(GetNode<Button>("VBoxContainer/OptionsButton"));
	}

	private void MainMenu()
	{
		PauseManager.Instance.MainMenu();
	}
}
