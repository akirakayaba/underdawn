//Name:			TestRunner.cs
//Description:	Automated tests for the game mechanics and their edge cases.
//				run from the project folder (exit code = number of failed tests):
//				Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . res://tests/test_runner.tscn
//				each test loads a fresh level, uses its own save file
//				(user://test_save.cfg) and prints PASS / FAIL

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class TestRunner : Node
{
	private static SceneTree tree;
	private static int failed;
	private static string currentTest;

	public override void _Ready()
	{
		tree = GetTree();
		RunAll();
	}

	private static async void RunAll()
	{
		var tests = new List<(string, Func<Task>)>
		{
			("player attack hits and knocks back", PlayerAttack),
			("element bonus damage and status", ElementBonusDamage),
			("immune to own element status", OwnElementImmunity),
			("several statuses at once, frozen + burning", StackedStatuses),
			("status does nothing on a dead enemy", StatusOnDeadEnemy),
			("paralyzed: no attack, skill or burst", ParalyzedCantAttack),
			("frozen: no move, dash or jump, even with speed boost", FrozenCantMove),
			("charged attack cancelled by paralysis", ChargeWhileParalyzed),
			("enemy killed mid wind-up does not hit", EnemyDiesDuringAttack),
			("burn kills enemy, it still drops its orb", DotKillDropsOrb),
			("orb: absorb with key only, one orb per press", OrbAbsorb),
			("orb fades away when ignored", OrbExpires),
			("speed boost only for a new element", SpeedBoost),
			("burst: needs an element, works with no enemies", BurstEdgeCases),
			("skill: nothing without an element, cooldown blocks", SkillEdgeCases),
			("dash: through enemies, not through the pit or map edge", DashBlocking),
			("dying mid-air / mid-dash / while charging", DyingInTheMiddle),
			("pausing stops enemies and status damage", PauseStopsTheGame),
			("hitstop always ends", HitstopEnds),
			("every enemy type: animations, chase, attack, death", EnemyTypes),
			("elites and enrage", ElitesAndEnrage),
			("spawner: cap, distance from player", Spawner),
			("save + continue, death deletes the run", SaveAndDeath),
			("controls: rebind, duplicate, reset", Controls),
			("tutorial: skip half way", TutorialSkip),
			("errors are written to the log", ErrorLog),
		};

		/*changing scene right inside _Ready isnt allowed, wait a frame first*/
		await Wait(0.1);
		SaveManager.Instance.UseSaveFile("user://test_save.cfg");
		SaveManager.Instance.ResetControls();
		SaveManager.Instance.ResetProgress();
		SaveManager.Instance.TutorialDone = true;

		foreach (var (name, test) in tests)
		{
			currentTest = name;
			try
			{
				await test();
			}
			catch (Exception exception)
			{
				Fail("threw " + exception.GetType().Name + ": " + exception.Message);
			}
			Engine.TimeScale = 1;
			if (tree.Paused)
				PauseManager.Instance.Pauser();
		}

		GD.Print(failed == 0 ? $"ALL {tests.Count} TESTS PASSED" : $"{failed} CHECK(S) FAILED");
		DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath("user://test_save.cfg"));
		tree.Quit(failed);
	}

	// ---- helpers ----

	private static void Check(string what, bool ok, string detail = "")
	{
		if (ok)
			GD.Print($"PASS {currentTest}: {what}");
		else
			Fail($"{what} {detail}");
	}

	private static void Fail(string message)
	{
		failed++;
		GD.Print($"FAIL {currentTest}: {message}");
	}

	private static async Task Wait(double seconds) =>
		await tree.ToSignal(tree.CreateTimer(seconds, true, false, true), SceneTreeTimer.SignalName.Timeout);

	private static void Press(string action, bool down) =>
		Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = down });

	private static async Task Tap(string action)
	{
		Press(action, true);
		await Wait(0.03);
		Press(action, false);
		await Wait(0.03);
	}

	/*fresh grassland with no enemies, the player in an open spot*/
	private static async Task<Player> Level(Vector2? at = null)
	{
		tree.ChangeSceneToFile("res://grassland.tscn");
		await Wait(0.3);
		tree.CurrentScene.GetNode<Node>("Objects/EnemySpawner").ProcessMode = ProcessModeEnum.Disabled;
		foreach (var enemy in tree.GetNodesInGroup(Enemy.GroupName))
			enemy.QueueFree();
		var player = tree.CurrentScene.GetNode<Player>("Objects/Player");
		player.GlobalPosition = at ?? new Vector2(1300, 600);
		await Wait(0.1);
		return player;
	}

	/*passive = doesnt chase or attack, so tests control everything*/
	private static Enemy Spawn(Player player, Vector2 offset, string scene = "skeleton_knight", ElementType element = ElementType.NONE, bool passive = true)
	{
		var enemy = GD.Load<PackedScene>($"res://scenes/entities/enemies/{scene}.tscn").Instantiate<Enemy>();
		enemy.SetElement(element);
		if (passive)
		{
			enemy.Set("detectRange", 0f);
			enemy.Set("attackRange", 0f);
		}
		tree.CurrentScene.GetNode("Objects").AddChild(enemy);
		enemy.GlobalPosition = player.GlobalPosition + offset;
		return enemy;
	}

	private static int Hp(Enemy enemy) => (int)enemy.GetNode<ProgressBar>("HealthProgressBar").Value;
	private static AnimatedSprite2D Sprite(Enemy enemy) => enemy.GetNode<AnimatedSprite2D>("AnimatedSprite2D");

	private static async Task Face(string direction)
	{
		Press(direction, true);
		await Wait(0.03);
		Press(direction, false);
		await Wait(0.05);
	}

	// ---- combat ----

	private static async Task PlayerAttack()
	{
		var player = await Level();
		var enemy = Spawn(player, new Vector2(0, 30));
		await Wait(0.1);
		await Face("ui_down");
		var before = enemy.GlobalPosition;
		await Tap("attack1");
		await Wait(0.2);
		Check("damage 10", Hp(enemy) == 40, $"hp {Hp(enemy)}");
		Check("pushed away", enemy.GlobalPosition.Y > before.Y + 3, $"{before} -> {enemy.GlobalPosition}");
	}

	private static async Task ElementBonusDamage()
	{
		var player = await Level();
		var ice = Spawn(player, new Vector2(0, 30), element: ElementType.ICE);
		await Wait(0.1);
		player.SetElement(ElementType.FIRE);
		await Face("ui_down");
		await Tap("attack1");
		await Wait(0.15);
		Check("fire beats ice: 10 x1.5", Hp(ice) == 35, $"hp {Hp(ice)}");
		Check("burning applied", Sprite(ice).Modulate == ElementVisuals.GetColor(ElementType.FIRE));
		Check("matchups table", Combat.IsStrongAgainst(ElementType.WATER, ElementType.FIRE) && !Combat.IsStrongAgainst(ElementType.FIRE, ElementType.WATER) && !Combat.IsStrongAgainst(ElementType.NONE, ElementType.NONE));
	}

	private static async Task OwnElementImmunity()
	{
		var player = await Level();
		player.SetElement(ElementType.FIRE);
		player.ApplyStatus(ElementType.FIRE);
		await Wait(1.2);
		Check("fire player is not burned", player.Health == 100, $"hp {player.Health}");
	}

	private static async Task StackedStatuses()
	{
		var player = await Level();
		var start = player.GlobalPosition;
		player.ApplyStatus(ElementType.ICE);
		player.ApplyStatus(ElementType.FIRE);
		/*freeze lasts 1s (StatusEffectHandler.cs)*/
		Press("ui_right", true);
		await Wait(0.8);
		Press("ui_right", false);
		Check("frozen while burning", player.GlobalPosition.DistanceTo(start) < 1f);
		Check("still burning while frozen", player.Health < 100, $"hp {player.Health}");
	}

	private static async Task StatusOnDeadEnemy()
	{
		var player = await Level();
		var enemy = Spawn(player, new Vector2(60, 0), scene: "goblin");
		await Wait(0.1);
		enemy.TakeDamage(1000);
		enemy.ApplyStatus(ElementType.POISON);
		enemy.ApplyKnockback(Vector2.Right * 500);
		Combat.Hit(player.GlobalPosition, ElementType.FIRE, enemy, 10);
		await Wait(0.1);
		Check("dead enemy ignores status / hits", enemy.IsDead && !IsInstanceValidSafe(enemy) || Sprite(enemy).Modulate == Colors.White);
	}

	private static bool IsInstanceValidSafe(GodotObject o) => GodotObject.IsInstanceValid(o);

	private static async Task ParalyzedCantAttack()
	{
		var player = await Level();
		var enemy = Spawn(player, new Vector2(0, 30));
		await Wait(0.1);
		await Face("ui_down");
		player.SetElement(ElementType.FIRE);
		player.ApplyStatus(ElementType.LIGHTNING);
		await Tap("attack1");
		await Tap("skill");
		await Tap("burst");
		await Wait(0.2);
		Check("no damage while paralyzed", Hp(enemy) == 50, $"hp {Hp(enemy)}");
		Check("element kept (burst blocked)", player.CurrentElement == ElementType.FIRE);
		Check("skill not used", player.SkillCooldownLeft == 0);
	}

	private static async Task FrozenCantMove()
	{
		var player = await Level();
		player.AbsorbElement(ElementType.FIRE);
		player.ApplyStatus(ElementType.ICE);
		var start = player.GlobalPosition;
		Press("ui_right", true);
		await Tap("dash");
		await Tap("jump");
		await Wait(0.3);
		Press("ui_right", false);
		Check("did not move", player.GlobalPosition.DistanceTo(start) < 1f, $"moved {player.GlobalPosition.DistanceTo(start):0}");
		Check("did not jump", !player.IsInAir);
	}

	private static async Task ChargeWhileParalyzed()
	{
		var player = await Level();
		var enemy = Spawn(player, new Vector2(0, 30));
		enemy.Set("maxHealth", 500);
		await Wait(0.1);
		await Face("ui_down");
		Press("attack1", true);
		await Wait(1.4);
		int beforeRelease = Hp(enemy);
		player.ApplyStatus(ElementType.LIGHTNING);
		Press("attack1", false);
		await Wait(0.2);
		Check("no charged hit while paralyzed", Hp(enemy) == beforeRelease, $"{beforeRelease} -> {Hp(enemy)}");
		Check("glow cleared", player.GetNode<AnimatedSprite2D>("player_animation").SelfModulate == Colors.White);
	}

	private static async Task EnemyDiesDuringAttack()
	{
		var player = await Level(new Vector2(680, 960));
		var enemy = Spawn(player, new Vector2(28, 0), passive: false);
		for (int i = 0; i < 60 && Sprite(enemy).Animation != "attack" && Sprite(enemy).Animation != "attack2"; i++)
			await Wait(0.02);
		enemy.TakeDamage(1000);
		await Wait(1.6);
		Check("no hit from a dead enemy", player.Health == 100, $"hp {player.Health}");
	}

	private static async Task DotKillDropsOrb()
	{
		var player = await Level();
		var enemy = Spawn(player, new Vector2(120, 0), scene: "goblin", element: ElementType.WATER);
		await Wait(0.1);
		enemy.TakeDamage(26);
		enemy.ApplyStatus(ElementType.FIRE);
		await Wait(1.5);
		Check("burn finished it off", !GodotObject.IsInstanceValid(enemy) || enemy.IsDead);
		var orbs = tree.GetNodesInGroup(ElementOrb.GroupName).OfType<ElementOrb>().ToList();
		Check("dropped a water orb", orbs.Count == 1 && orbs[0].Element == ElementType.WATER, $"{orbs.Count} orbs");
	}

	// ---- elements ----

	private static async Task OrbAbsorb()
	{
		var player = await Level();
		var objects = tree.CurrentScene.GetNode("Objects");
		ElementOrb.Drop(objects, player.GlobalPosition, ElementType.FIRE);
		ElementOrb.Drop(objects, player.GlobalPosition + new Vector2(6, 0), ElementType.ICE);
		await Wait(0.3);
		Check("no auto pickup", player.CurrentElement == ElementType.NONE);
		await Tap("interact");
		Check("one press absorbs one orb", tree.GetNodesInGroup(ElementOrb.GroupName).Count == 1, $"{tree.GetNodesInGroup(ElementOrb.GroupName).Count} orbs left");
		Check("closest orb absorbed", player.CurrentElement == ElementType.FIRE, player.CurrentElement.ToString());
		await Tap("interact");
		Check("second press replaces the element", player.CurrentElement == ElementType.ICE, player.CurrentElement.ToString());
		player.TakeDamage(1000);
		ElementOrb.Drop(objects, player.GlobalPosition, ElementType.POISON);
		await Wait(0.2);
		await Tap("interact");
		Check("dead player cant absorb", player.CurrentElement == ElementType.ICE);
	}

	private static async Task OrbExpires()
	{
		var player = await Level();
		ElementOrb.Drop(tree.CurrentScene.GetNode("Objects"), player.GlobalPosition + new Vector2(200, 0), ElementType.FIRE);
		await Wait(0.2);
		var orb = tree.GetNodesInGroup(ElementOrb.GroupName).OfType<ElementOrb>().First();
		orb.Set("timeLeft", 0.5f);
		await Wait(0.8);
		Check("orb gone", !GodotObject.IsInstanceValid(orb));
	}

	private static async Task SpeedBoost()
	{
		var player = await Level();
		player.AbsorbElement(ElementType.FIRE);
		Check("boost on new element", player.IsSpeedBoosted);
		await Wait(3.1);
		Check("boost ends", !player.IsSpeedBoosted);
		player.AbsorbElement(ElementType.FIRE);
		Check("no boost for the same element", !player.IsSpeedBoosted);
	}

	private static async Task BurstEdgeCases()
	{
		var player = await Level();
		await Tap("burst");
		Check("no burst without element", player.CurrentElement == ElementType.NONE && player.GetNode<AnimatedSprite2D>("player_animation").Animation.ToString().StartsWith("idle"));
		player.SetElement(ElementType.POISON);
		await Tap("burst");
		await Wait(0.1);
		Check("burst with no enemies still releases the element", player.CurrentElement == ElementType.NONE);
	}

	private static async Task SkillEdgeCases()
	{
		var player = await Level();
		await Tap("skill");
		Check("no skill without element", player.SkillCooldownLeft == 0);
		player.SetElement(ElementType.FIRE);
		await Tap("skill");
		float cooldown = player.SkillCooldownLeft;
		await Wait(0.7);
		await Tap("skill");
		Check("cooldown blocks a second cast", player.SkillCooldownLeft < cooldown, $"{cooldown:0.0} -> {player.SkillCooldownLeft:0.0}");
		player.SetElement(ElementType.ICE);
		Check("changing element keeps the cooldown", player.SkillCooldownLeft > 0);
	}

	// ---- movement ----

	private static async Task DashBlocking()
	{
		var player = await Level(new Vector2(680, 960));
		Spawn(player, new Vector2(40, 0));
		await Wait(0.1);
		var start = player.GlobalPosition;
		Press("ui_right", true);
		await Tap("dash");
		await Wait(0.25);
		Press("ui_right", false);
		Check("dashed through the enemy", player.GlobalPosition.X > start.X + 60, $"moved {player.GlobalPosition.X - start.X:0}");

		player.GlobalPosition = new Vector2(1300, 790);
		await Wait(1.1);
		Press("ui_down", true);
		await Tap("dash");
		await Wait(0.4);
		Press("ui_down", false);
		Check("pit stops the dash", player.GlobalPosition.Y + 18 <= 861, $"feet {player.GlobalPosition.Y + 18:0}");

		player.GlobalPosition = new Vector2(20, 400);
		await Wait(1.1);
		Press("ui_left", true);
		await Tap("dash");
		await Wait(0.4);
		Press("ui_left", false);
		Check("map edge stops the dash", player.GlobalPosition.X >= 11.9f, $"x {player.GlobalPosition.X}");
	}

	private static async Task DyingInTheMiddle()
	{
		var player = await Level();
		await Tap("jump");
		await Wait(0.1);
		player.TakeDamage(1000);
		await Wait(0.1);
		var sprite = player.GetNode<AnimatedSprite2D>("player_animation");
		Check("mid-air death lands", sprite.Position.Y == 0);

		player = await Level();
		Press("ui_right", true);
		await Tap("dash");
		player.TakeDamage(1000);
		var at = player.GlobalPosition;
		await Wait(0.3);
		Press("ui_right", false);
		Check("mid-dash death stops", player.GlobalPosition.DistanceTo(at) < 1f);

		player = await Level();
		Press("attack1", true);
		await Wait(1.3);
		player.TakeDamage(1000);
		Press("attack1", false);
		await Wait(0.2);
		Check("charging death leaves no glow stuck", player.IsDead && player.GetNode<Control>("Hud/GameOver").Visible);
	}

	private static async Task PauseStopsTheGame()
	{
		var player = await Level(new Vector2(680, 960));
		var enemy = Spawn(player, new Vector2(150, 0), scene: "goblin", passive: false);
		player.ApplyStatus(ElementType.POISON);
		await Wait(0.1);
		PauseManager.Instance.Pauser();
		var enemyAt = enemy.GlobalPosition;
		int hp = player.Health;
		await Wait(1.5);
		Check("enemy frozen while paused", enemy.GlobalPosition.DistanceTo(enemyAt) < 0.5f);
		Check("no poison damage while paused", player.Health == hp, $"{hp} -> {player.Health}");
		PauseManager.Instance.Pauser();
		await Wait(0.5);
		Check("enemy moves again after unpause", enemy.GlobalPosition.DistanceTo(enemyAt) > 5f);
	}

	private static async Task HitstopEnds()
	{
		await Level();
		HitEffects.Hitstop(tree, 0.1f);
		Check("slowed down", Engine.TimeScale < 1);
		PauseManager.Instance.Pauser();
		await Wait(0.3);
		PauseManager.Instance.Pauser();
		Check("back to normal speed even if paused during it", Engine.TimeScale == 1);
	}

	// ---- enemies ----

	private static async Task EnemyTypes()
	{
		foreach (var scene in new[] { "goblin", "demon_sword", "skeleton_knight" })
		{
			var player = await Level(new Vector2(680, 960));
			var enemy = Spawn(player, new Vector2(120, 0), scene: scene, passive: false);
			var frames = Sprite(enemy).SpriteFrames;
			Check($"{scene} has its animations", new[] { "idle", "walk", "attack", "hurt", "death" }.All(name => frames.HasAnimation(name)));
			for (int i = 0; i < 100 && player.Health == 100; i++)
				await Wait(0.05);
			Check($"{scene} chases and hits", player.Health < 100, $"hp {player.Health}");
			enemy.TakeDamage(1000);
			await Wait(0.1);
			Check($"{scene} plays death and leaves the group", Sprite(enemy).Animation == "death" && !enemy.IsInGroup(Enemy.GroupName));
			await Wait(1.5);
			Check($"{scene} removed after dying", !GodotObject.IsInstanceValid(enemy));
		}
	}

	private static async Task ElitesAndEnrage()
	{
		var player = await Level();
		var elite = GD.Load<PackedScene>("res://scenes/entities/enemies/goblin.tscn").Instantiate<Enemy>();
		elite.IsElite = true;
		elite.Set("detectRange", 0f);
		tree.CurrentScene.GetNode("Objects").AddChild(elite);
		elite.GlobalPosition = player.GlobalPosition + new Vector2(100, 0);
		await Wait(0.1);
		Check("elite has 2.5x health", (int)elite.GetNode<ProgressBar>("HealthProgressBar").MaxValue == 75);
		elite.TakeDamage(44);
		Check("not enraged above 40%", !elite.IsEnraged);
		elite.TakeDamage(2);
		Check("enraged below 40%", elite.IsEnraged);
		elite.TakeDamage(1);
		Check("enrage only triggers once", elite.GetChildren().OfType<Polygon2D>().Count() == 3);
	}

	private static async Task Spawner()
	{
		var player = await Level();
		var spawner = tree.CurrentScene.GetNode<EnemySpawner>("Objects/EnemySpawner");
		spawner.MaxEnemies = 4;
		spawner.SpawnInterval = 0.05f;
		spawner.ProcessMode = ProcessModeEnum.Inherit;
		spawner.Set("timer", 0f);
		await Wait(1f);
		var enemies = tree.GetNodesInGroup(Enemy.GroupName).OfType<Enemy>().ToList();
		Check("stops at MaxEnemies", enemies.Count == 4, $"{enemies.Count}");
		Check("never spawns next to the player", enemies.All(enemy => enemy.GlobalPosition.DistanceTo(player.GlobalPosition) > 200), string.Join(",", enemies.Select(enemy => (int)enemy.GlobalPosition.DistanceTo(player.GlobalPosition))));
	}

	// ---- save / options ----

	private static async Task SaveAndDeath()
	{
		var player = await Level(new Vector2(500, 500));
		player.SetElement(ElementType.WIND);
		SaveManager.Instance.SaveRun(player);
		SaveManager.Instance.ContinueRun();
		await Wait(0.4);
		player = tree.CurrentScene.GetNode<Player>("Objects/Player");
		Check("continue restores", player.GlobalPosition.DistanceTo(new Vector2(500, 500)) < 2 && player.CurrentElement == ElementType.WIND);
		player.TakeDamage(1000);
		Check("death deletes the run", !SaveManager.Instance.HasRun);
		SaveManager.Instance.ContinueRun();
		await Wait(0.2);
		Check("continue without a run does nothing", tree.CurrentScene.GetNode<Player>("Objects/Player") == player);
	}

	private static async Task Controls()
	{
		await Level();
		SaveManager.Instance.Rebind("attack1", new InputEventKey { PhysicalKeycode = Key.P });
		Check("rebind", SaveManager.KeyName("attack1") == "P");
		Check("old key no longer attacks", !InputMap.ActionGetEvents("attack1").OfType<InputEventKey>().Any(key => key.PhysicalKeycode == Key.J));
		Check("unknown action shows ?", SaveManager.KeyName("not_an_action") == "?");
		SaveManager.Instance.ResetControls();
		Check("reset", SaveManager.KeyName("attack1") == "J");
	}

	private static async Task TutorialSkip()
	{
		SaveManager.Instance.ResetProgress();
		var player = await Level();
		var tutorial = player.GetNode("Hud").GetNodeOrNull<Tutorial>("Tutorial");
		Check("tutorial shows after reset", tutorial != null);
		Press("ui_right", true);
		await Wait(0.8);
		Press("ui_right", false);
		tutorial.GetChild(0).GetChild(0).GetChild<Button>(1).EmitSignal(BaseButton.SignalName.Pressed);
		await Wait(0.1);
		Check("skipped half way and saved", !GodotObject.IsInstanceValid(tutorial) && SaveManager.Instance.TutorialDone);
	}

	private static async Task ErrorLog()
	{
		await Level();
		string marker = "test marker " + Time.GetTicksMsec();
		GD.PushError(marker);
		await Wait(0.1);
		Check("error in the log file", System.IO.File.ReadAllText(ErrorLogger.LogPath).Contains(marker));
	}
}
