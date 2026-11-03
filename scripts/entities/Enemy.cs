//Name:			Enemy.cs
//Description:	Abstract class of what a basic enemy should have
//				(health, element, status effects, chasing and attacking the player)
//				if the enemy has an AnimatedSprite2D it plays these animations:
//				idle, walk, attack, hurt, death (missing ones are skipped)

using System.Linq;
using Godot;

public abstract partial class Enemy : CharacterBody2D, ICombatant
{
	public const string GroupName = "enemies";

	protected AnimatedSprite2D animatedSprite;

	/*speed key*/
	[Export] protected float speed = 70f;

	[Export] protected int attackDamage = 10;

	[Export] protected ElementType elementType { get; set; }
	public ElementType Element => elementType;

	/*health of the enemy, bar is optional so enemies without one still work*/
	[Export] protected int maxHealth = 20;
	protected int health;
	private ProgressBar healthBar;
	public bool IsDead => health <= 0;
	public bool IsInvincible => false;

	/*how close the player has to be for the enemy to chase / attack them*/
	[Export] protected float detectRange = 200f;
	[Export] protected float attackRange = 36f;
	[Export] protected float attackCooldown = 1.2f;

	/*seconds into the attack animation when the hit lands (enemy hitbox),
	  the player gets hit if they are still in range and in front of the enemy*/
	[Export] protected float attackHitTime = 0.3f;

	/*set this if the sprite sheet is drawn facing left*/
	[Export] protected bool spriteFacesLeft = false;

	/*shadow under the enemy's feet, 0 width = no shadow*/
	[Export] protected float shadowWidth = 0f;
	[Export] protected float shadowOffsetY = 16f;

	private float attackTimer;
	private bool isAttacking;
	private bool isHurt;
	private bool facingLeft;
	private float spriteScaleX = 1f;

	protected StatusEffectHandler status;
	private CanvasItem sprite;

	public override void _Ready()
	{
		AddToGroup(GroupName);
		/*top down, so no floors: in the default (platformer) mode a body moving
		  under another one drags it along like a moving platform*/
		MotionMode = MotionModeEnum.Floating;
		health = maxHealth;
		healthBar = GetNodeOrNull<ProgressBar>("HealthProgressBar");
		UpdateHealthBar();

		animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (animatedSprite != null)
		{
			spriteScaleX = Mathf.Abs(animatedSprite.Scale.X);
			animatedSprite.AnimationFinished += OnAnimationFinished;
			PlayAnimation("idle");
		}

		/*tints the enemy's first sprite so the health bar keeps its color*/
		sprite = GetChildren().FirstOrDefault(child => child is Sprite2D or AnimatedSprite2D) as CanvasItem;
		status = new StatusEffectHandler { Target = this, Sprite = sprite };
		AddChild(status);

		ShowElement();
		if (shadowWidth > 0)
			Shadow.AddTo(this, shadowWidth, shadowOffsetY);
	}

	/*basic enemy behaviour: walk to the player and hit them when close.
	  enemies with their own movement can override this, but should still use
	  status.GetVelocity / status.CanAttack so status effects work on them*/
	public override void _PhysicsProcess(double delta)
	{
		if (IsDead)
			return;

		attackTimer -= (float)delta;
		Vector2 desiredVelocity = Vector2.Zero;

		var player = GetTree().GetFirstNodeInGroup(Player.GroupName) as Player;
		if (player != null && !player.IsDead && !isAttacking)
		{
			float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
			if (distance <= attackRange)
			{
				Face(player.GlobalPosition.X - GlobalPosition.X);
				TryAttack(player);
			}
			else if (distance <= detectRange)
			{
				desiredVelocity = GlobalPosition.DirectionTo(player.GlobalPosition) * speed;
			}
		}

		Velocity = status.GetVelocity(desiredVelocity, delta);
		MoveAndSlide();
		UpdateMoveAnimation();
	}

	/*paralyzed enemies cant attack, tremor delays the attack*/
	private async void TryAttack(Player player)
	{
		if (attackTimer > 0 || isAttacking || !status.CanAttack)
			return;
		attackTimer = attackCooldown;
		isAttacking = true;
		isHurt = false;

		if (status.AttackDelay > 0)
			await ToSignal(GetTree().CreateTimer(status.AttackDelay), SceneTreeTimer.SignalName.Timeout);
		if (IsDead)
			return;

		bool hasAttackAnimation = PlayAnimation("attack");
		await ToSignal(GetTree().CreateTimer(attackHitTime), SceneTreeTimer.SignalName.Timeout);
		if (IsDead)
			return;

		if (IsInstanceValid(player) && IsInAttackHitbox(player.GlobalPosition))
			Combat.Hit(GlobalPosition, elementType, player, attackDamage);

		/*without an attack animation there is no AnimationFinished to wait for*/
		if (!hasAttackAnimation)
			isAttacking = false;
	}

	/*the attack hits in a circle in front of the enemy (a bit bigger than
	  attackRange so a player that just stepped back still gets hit)*/
	private bool IsInAttackHitbox(Vector2 target)
	{
		Vector2 hitCenter = GlobalPosition + new Vector2(facingLeft ? -attackRange / 2 : attackRange / 2, 0);
		return target.DistanceTo(hitCenter) <= attackRange;
	}

	/*isBonus is for when the attack's element is strong against this enemy*/
	public void TakeDamage(int amount, bool isBonus = false)
	{
		if (IsDead)
			return;
		health = Mathf.Max(health - amount, 0);
		UpdateHealthBar();
		FloatingText.Show(this, amount.ToString(), isBonus ? FloatingText.BonusColor : FloatingText.DamageColor);
		HitEffects.Flash(sprite);

		if (IsDead)
			Die();
		else if (!isAttacking && PlayAnimation("hurt"))
			isHurt = true;
	}

	/*plays the death animation first if there is one*/
	protected virtual void Die()
	{
		RemoveFromGroup(GroupName);
		CollisionLayer = 0;
		CollisionMask = 0;
		if (healthBar != null)
			healthBar.Visible = false;

		/*the player can steal the element from the orb*/
		ElementOrb.Drop(GetParent(), GlobalPosition, elementType);

		if (!PlayAnimation("death"))
			QueueFree();
	}

	/*shows and applies the status effect on the enemy, NONE clears it*/
	public void ApplyStatus(ElementType element) => status.Apply(element);

	public void ApplyKnockback(Vector2 force) => status.ApplyKnockback(force);

	/*changes the enemy's element (used by the spawner)*/
	public void SetElement(ElementType element)
	{
		elementType = element;
		if (IsInsideTree())
			ShowElement();
	}

	private void OnAnimationFinished()
	{
		string name = animatedSprite.Animation;
		if (name == "death")
			QueueFree();
		else if (name == "attack")
			isAttacking = false;
		else if (name == "hurt")
			isHurt = false;
	}

	private void UpdateMoveAnimation()
	{
		if (animatedSprite == null)
			return;

		/*frozen enemies stop animating*/
		animatedSprite.SpeedScale = status.CanMove ? 1f : 0f;
		if (isAttacking || isHurt)
			return;

		if (Velocity.Length() > 5f)
		{
			Face(Velocity.X);
			PlayAnimation("walk");
		}
		else
		{
			PlayAnimation("idle");
		}
	}

	private void Face(float directionX)
	{
		if (Mathf.Abs(directionX) < 1f)
			return;
		facingLeft = directionX < 0;
		if (animatedSprite == null)
			return;
		bool flip = facingLeft != spriteFacesLeft;
		animatedSprite.Scale = new Vector2(flip ? -spriteScaleX : spriteScaleX, animatedSprite.Scale.Y);
	}

	/*returns false if the enemy doesnt have that animation*/
	private bool PlayAnimation(string name)
	{
		if (animatedSprite == null || !animatedSprite.SpriteFrames.HasAnimation(name))
			return false;
		animatedSprite.Play(name);
		return true;
	}

	/*the health bar fill and element icon are the color of the enemy's element*/
	private void ShowElement()
	{
		var color = ElementVisuals.GetColor(elementType);
		var elementIcon = GetNodeOrNull<CanvasItem>("ElementIconSprite");
		if (elementIcon != null)
			elementIcon.Modulate = color;
		if (healthBar != null)
			healthBar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = elementType == ElementType.NONE ? new Color(0.85f, 0.15f, 0.15f) : color });
	}

	private void UpdateHealthBar()
	{
		if (healthBar == null)
			return;
		healthBar.MaxValue = maxHealth;
		healthBar.Value = health;
	}
}
