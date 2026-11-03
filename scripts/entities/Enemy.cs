//Name:			Enemy.cs
//Description:	Abstract class of what a basic enemy should have

using Godot;

public abstract partial class Enemy : CharacterBody2D
{
	protected AnimatedSprite2D animatedSprite;

	/*using string to store last input of user*/
	protected string lastDirection = "down";

	/*speed key*/
	protected float speed = 125f;

	protected int attackDamage = 10;

	protected ElementType elementType { get; }

	/*health of the enemy, bar is optional so enemies without one still work*/
	[Export] protected int maxHealth = 20;
	protected int health;
	private ProgressBar healthBar;

	public override void _Ready()
	{
		health = maxHealth;
		healthBar = GetNodeOrNull<ProgressBar>("HealthProgressBar");
		UpdateHealthBar();
	}

	/*isBonus is for when the attack's element is strong against this enemy*/
	public void TakeDamage(int amount, bool isBonus = false)
	{
		health = Mathf.Max(health - amount, 0);
		UpdateHealthBar();
		FloatingText.Show(this, amount.ToString(), isBonus ? FloatingText.BonusColor : FloatingText.DamageColor);

		if (health == 0)
			Die();
	}

	protected virtual void Die() => QueueFree();

	private void UpdateHealthBar()
	{
		if (healthBar == null)
			return;
		healthBar.MaxValue = maxHealth;
		healthBar.Value = health;
	}
}
