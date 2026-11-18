//Name:			MusicManager.cs
//Description:	Autoload that loops the background music on the Music bus
//				(volume is set in the options menu, see SaveManager.cs)
//				to change the song, replace assets/audio/music/grassland_theme.ogg
//				(tools/make_music.py makes the current placeholder song)

using Godot;

public partial class MusicManager : Node
{
	private const string Song = "res://assets/audio/music/grassland_theme.ogg";

	public static MusicManager Instance { get; private set; }

	private AudioStreamPlayer player;

	public override void _Ready()
	{
		Instance = this;
		/*keeps playing while the game is paused*/
		ProcessMode = ProcessModeEnum.Always;

		var song = GD.Load<AudioStreamOggVorbis>(Song);
		if (song == null)
			return;
		song.Loop = true;
		player = new AudioStreamPlayer { Stream = song, Bus = "Music" };
		AddChild(player);
		player.Play();
	}

	public bool IsPlaying => player != null && player.Playing;
}
