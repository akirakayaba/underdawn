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

	/*combos: attack presses are remembered for a short time (buffer), and from
	  comboCancelFrame on the next attack or moving cancels the rest of the attack*/
	[Export] private float attackSpeed = 1.4f;
	[Export] private int comboCancelFrame = 4;
	[Export] private float attackBufferTime = 0.3f;
	private string bufferedAttack;
	private float bufferTimer;
	private bool attackStarted;

	/*dash: quick burst in the move direction (or the facing direction),
	  the player cant be hit while dashing and it cancels an attack*/
	[Export] private float dashSpeed = 450f;
	[Export] private float dashTime = 0.18f;
	[Export] private float dashCooldown = 0.6f;
	private Vector2 dashDirection;
	private float dashTimer;
	private float dashCooldownTimer;
	private float afterimageTimer;
	public float DashCooldown => dashCooldown;
	public float DashCooldownLeft => Mathf.Max(dashCooldownTimer, 0f);

	/*element skill (R), which skill it is depends on the current element (Skills.cs)*/
	private float skillCooldownTimer;
	public float SkillCooldownLeft => Mathf.Max(skillCooldownTimer, 0f);

	/*jump: fake height for the top down view, the sprite goes up and the shadow
	  stays on the ground. enemy attacks miss while the player is high enough,
	  one air dash per jump (gravity stops during the air dash)*/
	[Export] private float jumpVelocity = 260f;
	[Export] private float gravity = 1100f;
	[Export] private float dodgeHeight = 10f;
	private float height;
	private float heightVelocity;
	private bool canAirDash;
	public bool IsInAir => height > 0;

	/*air attack: attacking in the air slams down and hits every enemy around the landing spot*/
	[Export] private int slamDamage = 20;
	[Export] private float slamRadius = 44f;
	[Export] private float slamFallSpeed = 600f;
	[Export] private float slamKnockback = 150f;
	private bool isSlamming;

	public bool IsInvincible => dashTimer > 0 || height > dodgeHeight;

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

	/*the player cant walk past the camera limits (the edges of the map)*/
	private Camera2D camera;
	private const float EdgeMargin = 12f;

	private Polygon2D shadow;


	public override void _Ready()
	{
		AddToGroup(GroupName);
		animatedSprite = GetNode<AnimatedSprite2D>("player_animation");
		animatedSprite.AnimationFinished += OnAnimationFinished;
		animatedSprite.Play("idle_" + lastDirection);
		camera = GetNode<Camera2D>("PlayerCamera");
		shadow = Shadow.AddTo(this, 20f, 16f);

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
		HitEffects.Flash(animatedSprite);

		/*no death animation yet, so the player stops and turns gray*/
		if (IsDead)
		{
			status.Apply(ElementType.NONE);
			height = 0;
			UpdateHeight(0);
			animatedSprite.Play("idle_" + lastDirection);
			animatedSprite.Modulate = new Color(0.4f, 0.4f, 0.4f);
			Velocity = Vector2.Zero;
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
		if (Input.IsActionJustPressed("attack1"))
			BufferAttack("attack1");
		else if (Input.IsActionJustPressed("attack2"))
			BufferAttack("attack2");
		bufferTimer -= (float)delta;

		skillCooldownTimer -= (float)delta;
		if (Input.IsActionJustPressed("skill") && skillCooldownTimer <= 0 && status.CanAttack && !isAttacking)
			CastSkill(direction);

		/*jumping also cancels the end of an attack (frozen players cant jump)*/
		bool inCancelWindow = isAttacking && attackStarted && animatedSprite.Frame >= comboCancelFrame;
		if (Input.IsActionJustPressed("jump") && !IsInAir && status.CanMove && (!isAttacking || inCancelWindow))
			StartJump();

		/*frozen players cant dash, attacks pressed during a dash happen right after it*/
		dashCooldownTimer -= (float)delta;
		if (Input.IsActionJustPressed("dash") && dashCooldownTimer <= 0 && status.CanMove && (!IsInAir || canAirDash))
			StartDash(direction);
		if (dashTimer > 0)
		{
			dashTimer -= (float)delta;
			Velocity = dashDirection * dashSpeed;
			MoveAndSlide();
			KeepInsideCameraLimits();
			SpawnAfterimage((float)delta);
			return;
		}

		UpdateHeight((float)delta);

		if (bufferedAttack != null && bufferTimer > 0 && status.CanAttack && IsInAir && !isSlamming)
		{
			StartSlam(bufferedAttack);
			bufferedAttack = null;
		}
		else if (bufferedAttack != null && bufferTimer > 0 && status.CanAttack && !IsInAir && (!isAttacking || inCancelWindow))
		{
			UpdateLastDirection(direction);
			StartAttack(bufferedAttack, bufferedAttack == "attack1" ? attack1Damage : attack2Damage);
			bufferedAttack = null;
		}
		/*moving cancels the end of an attack*/
		else if (inCancelWindow && direction != Vector2.Zero)
		{
			isAttacking = false;
		}

		/*freeze movement temprorly until attack animation finshes
		  (knockback still moves the player)*/
		Velocity = status.GetVelocity(isAttacking ? Vector2.Zero : direction * speed, delta);
		MoveAndSlide();
		KeepInsideCameraLimits();

		if (isAttacking)
			return;

		/*movement animation for player*/
		if (direction != Vector2.Zero)
		{
			UpdateLastDirection(direction);
			animatedSprite.Play("run_" + lastDirection);
		}
		else
		{
			animatedSprite.Play("idle_" + lastDirection);
		}
	}

	private void UpdateLastDirection(Vector2 direction)
	{
		if (direction.X < 0)
			lastDirection = "left";
		else if (direction.X > 0)
			lastDirection = "right";
		else if (direction.Y < 0)
			lastDirection = "up";
		else if (direction.Y > 0)
			lastDirection = "down";
	}

	private void BufferAttack(string attackName)
	{
		bufferedAttack = attackName;
		bufferTimer = attackBufferTime;
	}

	private void StartJump()
	{
		heightVelocity = jumpVelocity;
		height = 0.01f;
		canAirDash = true;
		isAttacking = false;
		isSlamming = false;
	}

	/*moves the sprite up by the jump height, the shadow gets smaller the higher the player is*/
	private void UpdateHeight(float time)
	{
		if (IsInAir)
		{
			heightVelocity -= gravity * time;
			height = Mathf.Max(height + heightVelocity * time, 0f);
			if (isSlamming)
				SpawnAfterimage(time);
			if (height == 0 && isSlamming)
				Slam();
		}
		animatedSprite.Position = new Vector2(0, -height);
		shadow.Scale = Vector2.One * Mathf.Lerp(1f, 0.6f, Mathf.Min(height / 40f, 1f));
	}

	/*falls straight down fast playing the attack, Slam() happens on landing*/
	private void StartSlam(string attackName)
	{
		isSlamming = true;
		isAttacking = true;
		attackStarted = false;
		heightVelocity = -slamFallSpeed;
		animatedSprite.Stop();
		animatedSprite.Play(attackName + "_" + lastDirection, attackSpeed);
	}

	private void Slam()
	{
		isSlamming = false;
		attackStarted = true;
		bool hitSomething = false;
		foreach (var enemy in GetTree().GetNodesInGroup(Enemy.GroupName).OfType<Enemy>())
		{
			if (enemy.GlobalPosition.DistanceTo(GlobalPosition) > slamRadius)
				continue;
			/*slam pushes enemies out more than a normal hit*/
			enemy.ApplyKnockback((enemy.GlobalPosition - GlobalPosition).Normalized() * slamKnockback);
			Combat.Hit(GlobalPosition, CurrentElement, enemy, slamDamage);
			hitSomething = true;
		}

		HitEffects.Shake(camera, 5f, 0.25f);
		HitEffects.Dust(GetParent(), GlobalPosition + new Vector2(0, 16));
		if (hitSomething)
			HitEffects.Hitstop(GetTree(), 0.07f);

		/*ring on the ground that grows and fades out*/
		var ring = Shadow.AddTo(this, slamRadius * 2, 16f);
		ring.Color = new Color(1f, 1f, 1f, 0.5f);
		ring.Scale = Vector2.One * 0.3f;
		var tween = ring.CreateTween();
		tween.SetParallel();
		tween.TweenProperty(ring, "scale", Vector2.One, 0.25);
		tween.TweenProperty(ring, "modulate:a", 0.0, 0.25);
		tween.Chain().TweenCallback(Callable.From(ring.QueueFree));
	}

	/*casts toward the held direction (or the facing direction), plays attack1 as the cast animation*/
	private void CastSkill(Vector2 direction)
	{
		Vector2 aim = direction != Vector2.Zero ? direction.Normalized() : DirectionToVector(lastDirection);
		if (!Skills.Cast(this, CurrentElement, aim))
			return;
		skillCooldownTimer = Skills.GetCooldown(CurrentElement);
		UpdateLastDirection(aim);
		isAttacking = true;
		attackStarted = true;
		animatedSprite.Stop();
		animatedSprite.Play("attack1_" + lastDirection, attackSpeed);
	}

	private void StartDash(Vector2 direction)
	{
		if (IsInAir)
			canAirDash = false;
		dashDirection = direction != Vector2.Zero ? direction.Normalized() : DirectionToVector(lastDirection);
		dashTimer = dashTime;
		dashCooldownTimer = dashCooldown;
		isAttacking = false;
		UpdateLastDirection(dashDirection);
		animatedSprite.Play("run_" + lastDirection);
	}

	/*fading copies of the player sprite left behind while dashing*/
	private void SpawnAfterimage(float time)
	{
		afterimageTimer -= time;
		if (afterimageTimer > 0)
			return;
		afterimageTimer = 0.03f;

		var afterimage = new Sprite2D
		{
			Texture = animatedSprite.SpriteFrames.GetFrameTexture(animatedSprite.Animation, animatedSprite.Frame),
			Offset = animatedSprite.Offset,
			TextureFilter = TextureFilterEnum.Nearest,
			Modulate = new Color(0.6f, 0.8f, 1f, 0.5f)
		};
		GetParent().AddChild(afterimage);
		afterimage.GlobalPosition = animatedSprite.GlobalPosition;

		var tween = afterimage.CreateTween();
		tween.TweenProperty(afterimage, "modulate:a", 0.0, 0.2);
		tween.TweenCallback(Callable.From(afterimage.QueueFree));
	}

	/*plays the attack and hits the enemies in front of the player,
	  tremor (earth status) delays the attack before it starts*/
	private async void StartAttack(string attackName, int damage)
	{
		isAttacking = true;
		attackStarted = false;
		string attackDirection = lastDirection;

		if (status.AttackDelay > 0)
			await ToSignal(GetTree().CreateTimer(status.AttackDelay), SceneTreeTimer.SignalName.Timeout);
		if (IsDead)
			return;

		/*stop first so the same attack twice in a row restarts from frame 0*/
		animatedSprite.Stop();
		animatedSprite.Play(attackName + "_" + attackDirection, attackSpeed);
		attackStarted = true;
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

	private void KeepInsideCameraLimits()
	{
		GlobalPosition = GlobalPosition.Clamp(
			new Vector2(camera.LimitLeft + EdgeMargin, camera.LimitTop + EdgeMargin),
			new Vector2(camera.LimitRight - EdgeMargin, camera.LimitBottom - EdgeMargin));
	}

	private static Vector2 DirectionToVector(string direction) => direction switch
	{
		"left" => Vector2.Left,
		"right" => Vector2.Right,
		"up" => Vector2.Up,
		_ => Vector2.Down
	};
}
