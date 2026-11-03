//Name:			SkillProjectile.cs
//Description:	Moving (or still) thing a skill creates that hits enemies it
//				touches. used for the fireball, tornado, poison cloud...
//				Pierce = keeps going after hitting, RehitTime = can hit the
//				same enemy again after that many seconds (0 = only once)

using System.Collections.Generic;
using Godot;

public partial class SkillProjectile : Node2D
{
	public Vector2 Velocity { get; set; }
	public float Lifetime { get; set; } = 1f;
	public float HitRadius { get; set; } = 12f;
	public bool Pierce { get; set; }
	public float RehitTime { get; set; }
	public System.Action<SkillProjectile, Enemy> OnHit { get; set; }
	public System.Action<SkillProjectile> OnEnd { get; set; }

	private readonly Dictionary<Enemy, float> nextHitTime = new();
	private float elapsed;
	private bool ended;

	public override void _PhysicsProcess(double delta)
	{
		float time = (float)delta;
		Position += Velocity * time;
		elapsed += time;

		foreach (var enemy in Skills.EnemiesNear(this, GlobalPosition, HitRadius))
		{
			if (nextHitTime.TryGetValue(enemy, out float next) && elapsed < next)
				continue;
			nextHitTime[enemy] = RehitTime > 0 ? elapsed + RehitTime : float.MaxValue;
			OnHit?.Invoke(this, enemy);
			if (!Pierce)
			{
				End();
				return;
			}
		}

		if (elapsed >= Lifetime)
			End();
	}

	public void End()
	{
		if (ended)
			return;
		ended = true;
		OnEnd?.Invoke(this);
		QueueFree();
	}
}
