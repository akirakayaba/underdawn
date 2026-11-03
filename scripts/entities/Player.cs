//Name:			Player.cs
//Description:	Controls for player movement, inputs,
//				attacking, and animations

using System.Linq;
using Godot;

public partial class Player : CharacterBody2D, ICombatant
{
	public const string GroupName = "player";

	private AnimatedSprite2D animatedSprite;

	/*using string to store last input of user*/
	private string lastDirection = "down";

	/*speed key*/
	private float speed = 150f;

	private bool isAttacking = false;

	/*attack damage, and the area in front of the player that the attack hits*/
	[Export] private int attack1Damage = 10;
	[Export] private int attack2Damage = 15;
	[Export] private float attackReach = 16f;
	[Export] private float attackRadius = 28f;

	/*health of the player, the hud listens to HealthChanged to update the bar*/
	[Signal] public delegate void HealthChangedEventHandler(int health, int maxHealth);
	[Signal] public delegate void DiedEventHandler();
	[Export] private int maxHealth = 100;
	private int health;
	public bool IsDead => health <= 0;

	/*element ability the player currently has, the hud listens to ElementChanged*/
	[Signal] public delegate void ElementChangedEventHandler(ElementType element);
	public ElementType CurrentElement { get; private set; } = ElementType.NONE;
	public ElementType Element => CurrentElement;

	private StatusEffectHandler status;


	public override void _Ready()
	{
		AddToGroup(GroupName);
		animatedSprite = GetNode<AnimatedSprite2D>("player_animation");
		animatedSprite.AnimationFinished += OnAnimationFinished;
		animatedSprite.Play("idle_" + lastDirection);

		status = new StatusEffectHandler { Target = this, Sprite = animatedSprite };
		AddChild(status);

		health = maxHealth;
		EmitSignal(SignalName.HealthChanged, health, maxHealth);
		EmitSignal(SignalName.ElementChanged, (int)CurrentElement);
	}

	public void SetElement(ElementType element)
	{
		CurrentElement = element;
		EmitSignal(SignalName.ElementChanged, (int)element);
	}

	/*shows and applies the status effect on the player, NONE clears it*/
	public void ApplyStatus(ElementType element) => status.Apply(element);

	public void ApplyKnockback(Vector2 force) => status.ApplyKnockback(force);

	/*isBonus is for when the enemy's element is strong against the player*/
	public void TakeDamage(int amount, bool isBonus = false)
	{
		if (IsDead)
			return;
		health = Mathf.Max(health - amount, 0);
		EmitSignal(SignalName.HealthChanged, health, maxHealth);
		FloatingText.Show(this, amount.ToString(), isBonus ? FloatingText.BonusColor : FloatingText.DamageColor);

		if (IsDead)
		{
			animatedSprite.Play("idle_" + lastDirection);
			EmitSignal(SignalName.Died);
		}
	}

	/*onAnimatonFinsished : to check if the attack is still going through*/
	private void OnAnimationFinished() => isAttacking = false;
	public override void _PhysicsProcess(double delta)
	{
		if (IsDead)
			return;

		/*movement for player (frozen players cant move)*/
		Vector2 direction = Input.GetVector(
		   "ui_left",
		   "ui_right",
		   "ui_up",
		   "ui_down"
	   );
		if (!status.CanMove)
			direction = Vector2.Zero;

		/*attack and attack animation for player (paralyzed players cant attack)*/
		if (!isAttacking && status.CanAttack)
		{
			if (Input.IsActionJustPressed("attack1"))
				StartAttack("attack1", attack1Damage);
			else if (Input.IsActionJustPressed("attack2"))
				StartAttack("attack2", attack2Damage);
		}

		/*freeze movement temprorly until attack animation finshes
		  (knockback still moves the player)*/
		Velocity = status.GetVelocity(isAttacking ? Vector2.Zero : direction * speed, delta);
		MoveAndSlide();

		if (isAttacking)
			return;

		/*movement animation for player*/
		if (direction != Vector2.Zero)
		{
			if (direction.X < 0)
				lastDirection = "left";
			else if (direction.X > 0)
				lastDirection = "right";
			else if (direction.Y < 0)
				lastDirection = "up";
			else if (direction.Y > 0)
				lastDirection = "down";

			animatedSprite.Play("run_" + lastDirection);
		}
		else
		{
			animatedSprite.Play("idle_" + lastDirection);
		}
	}

	/*plays the attack and hits the enemies in front of the player,
	  tremor (earth status) delays the attack before it starts*/
	private async void StartAttack(string attackName, int damage)
	{
		isAttacking = true;
		string attackDirection = lastDirection;

		if (status.AttackDelay > 0)
			await ToSignal(GetTree().CreateTimer(status.AttackDelay), SceneTreeTimer.SignalName.Timeout);
		if (IsDead)
			return;

		animatedSprite.Play(attackName + "_" + attackDirection);
		HitEnemiesInFront(attackDirection, damage);
	}

	private void HitEnemiesInFront(string attackDirection, int damage)
	{
		Vector2 hitCenter = GlobalPosition + DirectionToVector(attackDirection) * attackReach;
		foreach (var enemy in GetTree().GetNodesInGroup(Enemy.GroupName).OfType<Enemy>())
		{
			if (enemy.GlobalPosition.DistanceTo(hitCenter) <= attackRadius)
				Combat.Hit(GlobalPosition, CurrentElement, enemy, damage);
		}
	}

	private static Vector2 DirectionToVector(string direction) => direction switch
	{
		"left" => Vector2.Left,
		"right" => Vector2.Right,
		"up" => Vector2.Up,
		_ => Vector2.Down
	};
}
