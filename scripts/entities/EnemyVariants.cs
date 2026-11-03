//Name:			EnemyVariants.cs
//Description:	Extra enemy behaviour (same Enemy class as Enemy.cs, split out
//				so that file doesnt get too long)
//				second attack	enemies with an "attack2" animation sometimes use it,
//								it can hit more than once (attack2HitTimes)
//				enrage			below 40% health the enemy gets faster, attacks more
//								often and uses its second attack more
//				elite			bigger and stronger version with a gold glow,
//								the spawner makes some enemies elite

using Godot;

public abstract partial class Enemy
{
	[Export] protected int attack2Damage = 10;
	[Export] protected float[] attack2HitTimes = System.Array.Empty<float>();
	[Export] protected float attack2Chance = 0.3f;

	[Export] public bool IsElite { get; set; }
	public bool IsEnraged { get; private set; }

	private const float EnrageHealth = 0.4f;
	private const float EliteScale = 1.3f;
	private static readonly Color EliteColor = new(1f, 0.8f, 0.2f);
	private static readonly Color EnrageColor = new(1f, 0.25f, 0.2f);

	/*picks the attack to use: animation name, damage and when each hit lands*/
	private (string name, int damage, float[] hitTimes) ChooseAttack()
	{
		bool hasSecondAttack = animatedSprite != null && animatedSprite.SpriteFrames.HasAnimation("attack2") && attack2HitTimes.Length > 0;
		float chance = IsEnraged ? attack2Chance * 2.3f : attack2Chance;
		if (hasSecondAttack && GD.Randf() < chance)
			return ("attack2", attack2Damage, attack2HitTimes);
		return ("attack", attackDamage, new[] { attackHitTime });
	}

	/*called before the enemy sets up its health, so the elite stats are used*/
	private void ApplyElite()
	{
		if (!IsElite)
			return;
		maxHealth = Mathf.RoundToInt(maxHealth * 2.5f);
		attackDamage = Mathf.RoundToInt(attackDamage * 1.5f);
		attack2Damage = Mathf.RoundToInt(attack2Damage * 1.5f);
		speed *= 1.1f;
		shadowWidth *= EliteScale;
		shadowOffsetY *= EliteScale;

		var spriteNode = GetNodeOrNull<Node2D>("AnimatedSprite2D");
		if (spriteNode != null)
			spriteNode.Scale *= EliteScale;
		/*move the health bar up so it isnt on top of the bigger sprite*/
		var bar = GetNodeOrNull<Control>("HealthProgressBar");
		if (bar != null)
		{
			float raise = Mathf.Abs(bar.OffsetTop) * (EliteScale - 1f);
			bar.OffsetTop -= raise;
			bar.OffsetBottom -= raise;
		}
	}

	private void CheckEnrage()
	{
		if (IsEnraged || IsDead || health > maxHealth * EnrageHealth)
			return;
		IsEnraged = true;
		speed *= 1.35f;
		attackCooldown *= 0.7f;
		FloatingText.Show(this, "Enraged!", EnrageColor, 14f);
		SpawnAura(EnrageColor);
	}

	/*pulsing glow under the enemy's feet*/
	private void SpawnAura(Color color)
	{
		var aura = Shadow.AddTo(this, shadowWidth > 0 ? shadowWidth * 1.5f : 30f, shadowOffsetY);
		aura.Color = new Color(color, 0.45f);
		var tween = aura.CreateTween().SetLoops();
		tween.TweenProperty(aura, "scale", Vector2.One * 1.15f, 0.4);
		tween.TweenProperty(aura, "scale", Vector2.One * 0.9f, 0.4);
	}
}
