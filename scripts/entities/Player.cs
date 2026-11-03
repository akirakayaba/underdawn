//Name:			Player.cs
//Description:	Controls for player movement, inputs,
//				attacking, and animations

using Godot;

public partial class Player : CharacterBody2D
{
	private AnimatedSprite2D animatedSprite;

	/*using string to store last input of user*/
	private string lastDirection = "down";

	/*speed key*/
	private float speed = 150f;

	private bool isAttacking = false;

	/*health of the player, the hud listens to HealthChanged to update the bar*/
	[Signal] public delegate void HealthChangedEventHandler(int health, int maxHealth);
	[Export] private int maxHealth = 100;
	private int health;


	public override void _Ready()
	{
		animatedSprite = GetNode<AnimatedSprite2D>("player_animation");
		animatedSprite.AnimationFinished += OnAnimationFinished;
		animatedSprite.Play("idle_" + lastDirection);

		health = maxHealth;
		EmitSignal(SignalName.HealthChanged, health, maxHealth);
	}

	/*isBonus is for when the enemy's element is strong against the player*/
	public void TakeDamage(int amount, bool isBonus = false)
	{
		health = Mathf.Max(health - amount, 0);
		EmitSignal(SignalName.HealthChanged, health, maxHealth);
		FloatingText.Show(this, amount.ToString(), isBonus ? FloatingText.BonusColor : FloatingText.DamageColor);
	}

	/*onAnimatonFinsished : to check if the attack is still going through*/
	private void OnAnimationFinished() => isAttacking = false;
	public override void _PhysicsProcess(double delta)
	{
		/*movement for player*/
		Vector2 direction = Input.GetVector(
		   "ui_left",
		   "ui_right",
		   "ui_up",
		   "ui_down"
	   );

		Velocity = direction * speed;
		MoveAndSlide();

		/*attack and attack animation for player*/
		if (!isAttacking)
		{
			if (Input.IsActionJustPressed("attack1"))
			{
				isAttacking = true;
				animatedSprite.Play("attack1_" + lastDirection);
			}
			if (Input.IsActionJustPressed("attack2"))
			{
				isAttacking = true;
				animatedSprite.Play("attack2_" + lastDirection);
			}
		}

		/*freeze movement temprorly until attack animation finshes*/
		if (isAttacking)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}
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
}
