//Name:			Enemy.cs
//Description:	Abstract class of what a basic enemy should have

using System.Linq;
using Godot;

public abstract partial class Enemy : CharacterBody2D, ICombatant
{
	public const string GroupName = "enemies";

	protected AnimatedSprite2D animatedSprite;

	/*using string to store last input of user*/
	protected string lastDirection = "down";

	/*speed key*/
	protected float speed = 125f;

	protected int attackDamage = 10;

	[Export] protected ElementType elementType { get; set; }
	public ElementType Element => elementType;

	/*health of the enemy, bar is optional so enemies without one still work*/
	[Export] protected int maxHealth = 20;
	protected int health;
	private ProgressBar healthBar;
	public bool IsDead => health <= 0;

	/*how close the player has to be for the enemy to chase / attack them*/
	[Export] protected float detectRange = 150f;
	[Export] protected float attackRange = 36f;
	[Export] protected float attackCooldown = 1f;
	private float attackTimer;
	private bool isWindingUp;

	protected StatusEffectHandler status;

	public override void _Ready()
	{
		AddToGroup(GroupName);
		health = maxHealth;
		healthBar = GetNodeOrNull<ProgressBar>("HealthProgressBar");
		UpdateHealthBar();

		/*tints the enemy's first sprite so the health bar keeps its color*/
		var sprite = GetChildren().FirstOrDefault(child => child is Sprite2D or AnimatedSprite2D) as CanvasItem;
		status = new StatusEffectHandler { Target = this, Sprite = sprite };
		AddChild(status);

		var elementIcon = GetNodeOrNull<CanvasItem>("ElementIconSprite");
		if (elementIcon != null)
			elementIcon.Modulate = ElementVisuals.GetColor(elementType);
	}

	/*basic enemy behaviour: walk to the player and hit them when close.
	  enemies with their own movement can override this, but should still use
	  status.GetVelocity / status.CanAttack so status effects work on them*/
	public override void _PhysicsProcess(double delta)
	{
		attackTimer -= (float)delta;
		Vector2 desiredVelocity = Vector2.Zero;

		var player = GetTree().GetFirstNodeInGroup(Player.GroupName) as Player;
		if (player != null && !player.IsDead)
		{
			float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
			if (distance <= attackRange)
				TryAttack(player);
			else if (distance <= detectRange && !isWindingUp)
				desiredVelocity = GlobalPosition.DirectionTo(player.GlobalPosition) * speed;
		}

		Velocity = status.GetVelocity(desiredVelocity, delta);
		MoveAndSlide();
	}

	/*paralyzed enemies cant attack, tremor delays the attack*/
	private async void TryAttack(Player player)
	{
		if (attackTimer > 0 || isWindingUp || !status.CanAttack)
			return;
		attackTimer = attackCooldown;

		if (status.AttackDelay > 0)
		{
			isWindingUp = true;
			await ToSignal(GetTree().CreateTimer(status.AttackDelay), SceneTreeTimer.SignalName.Timeout);
			isWindingUp = false;
			if (IsDead || !IsInstanceValid(player) || GlobalPosition.DistanceTo(player.GlobalPosition) > attackRange)
				return;
		}
		Combat.Hit(GlobalPosition, elementType, player, attackDamage);
	}

	/*isBonus is for when the attack's element is strong against this enemy*/
	public void TakeDamage(int amount, bool isBonus = false)
	{
		if (IsDead)
			return;
		health = Mathf.Max(health - amount, 0);
		UpdateHealthBar();
		FloatingText.Show(this, amount.ToString(), isBonus ? FloatingText.BonusColor : FloatingText.DamageColor);

		if (IsDead)
			Die();
	}

	protected virtual void Die() => QueueFree();

	/*shows and applies the status effect on the enemy, NONE clears it*/
	public void ApplyStatus(ElementType element) => status.Apply(element);

	public void ApplyKnockback(Vector2 force) => status.ApplyKnockback(force);

	private void UpdateHealthBar()
	{
		if (healthBar == null)
			return;
		healthBar.MaxValue = maxHealth;
		healthBar.Value = health;
	}
}
