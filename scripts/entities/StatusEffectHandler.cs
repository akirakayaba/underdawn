//Name:			StatusEffectHandler.cs
//Description:	Keeps track of the status effects on the player or an enemy
//				and what they do (added as a child by Player and Enemy)
//				FIRE		burning		damage over time, shorter but stronger than poison
//				POISON		poisoned	damage over time, longer but weaker than burning
//				EARTH		tremor		attacks are delayed
//				WATER		drenched	movement is slippery
//				LIGHTNING	paralyzed	cant attack
//				WIND		blown away	knocked back farther
//				ICE			frozen		cant move
//				entities are immune to the status of their own element

using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class StatusEffectHandler : Node
{
	/*how long each status effect lasts in seconds*/
	private static readonly Dictionary<ElementType, float> Durations = new()
	{
		{ ElementType.FIRE, 3f },
		{ ElementType.POISON, 8f },
		{ ElementType.EARTH, 4f },
		{ ElementType.WATER, 4f },
		{ ElementType.LIGHTNING, 2f },
		{ ElementType.WIND, 3f },
		{ ElementType.ICE, 2f }
	};

	private const int BurnDamage = 4;
	private const float BurnTickTime = 0.5f;
	private const int PoisonDamage = 2;
	private const float PoisonTickTime = 1f;
	private const float TremorAttackDelay = 0.5f;
	private const float SlipperyAcceleration = 300f;
	private const float WindKnockbackMultiplier = 2f;
	private const float KnockbackFriction = 800f;

	public ICombatant Target { get; init; }

	/*sprite that gets tinted to the status color*/
	public CanvasItem Sprite { get; init; }

	private readonly Dictionary<ElementType, float> timeLeft = new();
	private readonly Dictionary<ElementType, float> tickTimers = new();
	private ElementType shownStatus = ElementType.NONE;
	private Vector2 moveVelocity;
	private Vector2 knockbackVelocity;

	public bool Has(ElementType element) => timeLeft.ContainsKey(element);
	public bool CanMove => !Has(ElementType.ICE);
	public bool CanAttack => !Has(ElementType.LIGHTNING);
	public float AttackDelay => Has(ElementType.EARTH) ? TremorAttackDelay : 0f;

	/*adds a status effect (or refreshes how long it lasts), NONE clears all of them*/
	public void Apply(ElementType element)
	{
		if (element == ElementType.NONE)
		{
			timeLeft.Clear();
			tickTimers.Clear();
			ShowTint(ElementType.NONE);
			return;
		}
		if (element == Target.Element || Target.IsDead)
			return;

		/*only show the name when the status is new, not every time it gets refreshed*/
		if (!Has(element))
			FloatingText.Show((Node2D)Target, ElementVisuals.GetStatusName(element), ElementVisuals.GetColor(element), 14f);
		timeLeft[element] = Durations[element];
		tickTimers.TryAdd(element, 0f);
		ShowTint(element);
	}

	public void ApplyKnockback(Vector2 force) =>
		knockbackVelocity += Has(ElementType.WIND) ? force * WindKnockbackMultiplier : force;

	/*turns the velocity the entity wants to move at into what it actually
	  moves at with its status effects and knockback*/
	public Vector2 GetVelocity(Vector2 desiredVelocity, double delta)
	{
		if (!CanMove)
			moveVelocity = Vector2.Zero;
		else if (Has(ElementType.WATER))
			moveVelocity = moveVelocity.MoveToward(desiredVelocity, SlipperyAcceleration * (float)delta);
		else
			moveVelocity = desiredVelocity;

		return moveVelocity + knockbackVelocity;
	}

	public override void _PhysicsProcess(double delta)
	{
		float time = (float)delta;
		knockbackVelocity = knockbackVelocity.MoveToward(Vector2.Zero, KnockbackFriction * time);

		foreach (var element in timeLeft.Keys.ToList())
		{
			if (Target.IsDead)
				return;
			if (element == ElementType.FIRE)
				TickDamage(element, BurnDamage, BurnTickTime, time);
			else if (element == ElementType.POISON)
				TickDamage(element, PoisonDamage, PoisonTickTime, time);

			timeLeft[element] -= time;
			if (timeLeft[element] > 0)
				continue;

			timeLeft.Remove(element);
			tickTimers.Remove(element);
			if (element == shownStatus)
				ShowTint(timeLeft.Keys.LastOrDefault());
		}
	}

	private void TickDamage(ElementType element, int damage, float tickTime, float time)
	{
		/*small margin so float rounding doesnt skip the last tick*/
		tickTimers[element] += time;
		if (tickTimers[element] < tickTime - 0.001f)
			return;
		tickTimers[element] -= tickTime;
		Target.TakeDamage(damage);
	}

	private void ShowTint(ElementType element)
	{
		shownStatus = element;
		if (Sprite != null)
			Sprite.Modulate = ElementVisuals.GetColor(element);
	}
}
