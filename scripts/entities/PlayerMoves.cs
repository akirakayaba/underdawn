//Name:			PlayerMoves.cs
//Description:	Extra attack moves for the player (same Player class as
//				Player.cs, split out so that file doesnt get too long)
//				charged attack	hold Q or E after an attack, let go once the player glows
//				spin finisher	Q, Q, E combo: the E spins and hits all around

using System.Linq;
using Godot;

public partial class Player
{
	[Export] private float chargeTime = 0.6f;
	[Export] private int chargedDamage = 35;
	[Export] private float chargedReach = 24f;
	[Export] private float chargedRadius = 40f;
	[Export] private int spinDamage = 22;
	[Export] private float spinRadius = 42f;

	private float chargeTimer;
	private bool chargeReady;
	private int attack1Streak;

	/*starts the buffered attack, chained = it cancels the end of the last attack*/
	private void StartBufferedAttack(Vector2 direction, bool chained)
	{
		string attackName = bufferedAttack;
		bufferedAttack = null;
		UpdateLastDirection(direction);

		if (!chained)
			attack1Streak = 0;
		if (attackName == "attack2" && attack1Streak >= 2)
		{
			attack1Streak = 0;
			SpinAttack();
			return;
		}
		attack1Streak = attackName == "attack1" ? attack1Streak + 1 : 0;
		StartAttack(attackName, attackName == "attack1" ? attack1Damage : attack2Damage);
	}

	/*holding an attack key (not attacking, on the ground) builds the charge,
	  letting go with a full charge does the charged attack*/
	private void UpdateCharge(float time)
	{
		bool holding = Input.IsActionPressed("attack1") || Input.IsActionPressed("attack2");
		if (!holding)
		{
			if (chargeReady && !isAttacking && status.CanAttack)
				ChargedAttack();
			ResetCharge();
			return;
		}
		if (isAttacking || IsInAir || !status.CanAttack)
			return;

		chargeTimer += time;
		if (!chargeReady && chargeTimer >= chargeTime)
		{
			chargeReady = true;
			SpawnSlashArc(GlobalPosition, Vector2.Up, 14f, Mathf.Tau, new Color(1f, 0.9f, 0.5f));
		}
		/*glows brighter while charging, pulses when ready*/
		float glow = chargeReady ? 1.6f + Mathf.Sin(chargeTimer * 20f) * 0.4f : 1f + chargeTimer / chargeTime * 0.5f;
		animatedSprite.SelfModulate = new Color(glow, glow, glow * 0.8f);
	}

	private void ResetCharge()
	{
		if (chargeTimer > 0)
			animatedSprite.SelfModulate = Colors.White;
		chargeTimer = 0;
		chargeReady = false;
	}

	private void ChargedAttack()
	{
		BeginMove("attack2");
		var direction = DirectionToVector(lastDirection);
		var center = GlobalPosition + direction * chargedReach;
		bool hit = HitEnemiesAround(center, chargedRadius, chargedDamage, 200f);

		SpawnSlashArc(center - direction * 10f, direction, chargedRadius, 2.4f, Colors.White);
		HitEffects.Shake(camera, 4f, 0.2f);
		if (hit)
			HitEffects.Hitstop(GetTree(), 0.08f);
	}

	private void SpinAttack()
	{
		BeginMove("attack2");
		bool hit = HitEnemiesAround(GlobalPosition, spinRadius, spinDamage, 160f);
		SpawnSlashArc(GlobalPosition, Vector2.Right, spinRadius, Mathf.Tau, Colors.White);
		HitEffects.Shake(camera, 3f, 0.15f);
		if (hit)
			HitEffects.Hitstop(GetTree(), 0.06f);
	}

	/*plays an attack animation for a special move (the hit is done by the move)*/
	private void BeginMove(string attackName)
	{
		isAttacking = true;
		attackStarted = true;
		animatedSprite.Stop();
		animatedSprite.Play(attackName + "_" + lastDirection, attackSpeed);
	}

	/*hits every enemy in the circle with the player's element and an extra push
	  away from the circle, returns true if anything was hit*/
	private bool HitEnemiesAround(Vector2 center, float radius, int damage, float push)
	{
		var enemies = Skills.EnemiesNear(this, center, radius);
		foreach (var enemy in enemies)
		{
			enemy.ApplyKnockback((enemy.GlobalPosition - center).Normalized() * push);
			Combat.Hit(GlobalPosition, CurrentElement, enemy, damage);
		}
		return enemies.Any();
	}

	/*white slash arc that grows a bit and fades, angle = how much of a circle it covers*/
	private void SpawnSlashArc(Vector2 center, Vector2 direction, float radius, float angle, Color color)
	{
		const int Points = 16;
		var points = new Vector2[Points + 1];
		for (int i = 0; i <= Points; i++)
			points[i] = direction.Rotated(Mathf.Lerp(-angle / 2, angle / 2, (float)i / Points)) * radius;

		var arc = new Line2D { Points = points, Width = 4f, DefaultColor = color, Scale = Vector2.One * 0.8f, ZIndex = 3 };
		GetParent().AddChild(arc);
		arc.GlobalPosition = center + new Vector2(0, -6);

		var tween = arc.CreateTween();
		tween.SetParallel();
		tween.TweenProperty(arc, "scale", Vector2.One * 1.1f, 0.15);
		tween.TweenProperty(arc, "modulate:a", 0.0, 0.2);
		tween.Chain().TweenCallback(Callable.From(arc.QueueFree));
	}
}
