//Name:			EnemySpawner.cs
//Description:	Spawns enemies at the Marker2D children of this node
//				(move / add markers in the editor to change where they spawn)
//				keeps spawning every SpawnInterval until MaxEnemies are alive,
//				never at a marker that is too close to the player

using System.Linq;
using Godot;

public partial class EnemySpawner : Node2D
{
	[Export] public PackedScene[] EnemyScenes = System.Array.Empty<PackedScene>();
	[Export] public int StartEnemies = 3;
	[Export] public int MaxEnemies = 6;
	[Export] public float SpawnInterval = 5f;
	[Export] public float MinDistanceFromPlayer = 250f;

	/*random element for each enemy, otherwise they keep the element from their scene*/
	[Export] public bool RandomElements = true;

	private float timer;

	public override void _Ready()
	{
		timer = SpawnInterval;
		for (int i = 0; i < StartEnemies; i++)
			Spawn();
	}

	public override void _PhysicsProcess(double delta)
	{
		timer -= (float)delta;
		if (timer > 0)
			return;
		timer = SpawnInterval;

		if (GetTree().GetNodesInGroup(Enemy.GroupName).Count < MaxEnemies)
			Spawn();
	}

	private void Spawn()
	{
		var player = GetTree().GetFirstNodeInGroup(Player.GroupName) as Node2D;
		var points = GetChildren().OfType<Marker2D>()
			.Where(point => player == null || point.GlobalPosition.DistanceTo(player.GlobalPosition) >= MinDistanceFromPlayer)
			.ToList();
		if (points.Count == 0 || EnemyScenes.Length == 0)
			return;

		var point = points[GD.RandRange(0, points.Count - 1)];
		var enemy = EnemyScenes[GD.RandRange(0, EnemyScenes.Length - 1)].Instantiate<Enemy>();
		if (RandomElements)
			enemy.SetElement((ElementType)GD.RandRange(1, 7));

		/*added next to the spawner so enemies get y-sorted with the other objects*/
		var parent = GetParent<Node2D>();
		enemy.Position = parent.ToLocal(point.GlobalPosition + new Vector2((float)GD.RandRange(-20.0, 20.0), (float)GD.RandRange(-20.0, 20.0)));
		parent.CallDeferred(Node.MethodName.AddChild, enemy);
	}
}
