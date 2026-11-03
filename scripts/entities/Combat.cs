//Name:			Combat.cs
//Description:	Hitting the player or enemies: element bonus damage,
//				status effects and knockback
//				(element strengths/weaknesses are from elementtypematchups.txt)

using System.Collections.Generic;
using System.Linq;
using Godot;

/*anything that can be hit (player and enemies)*/
public interface ICombatant
{
	ElementType Element { get; }
	Vector2 GlobalPosition { get; }
	bool IsDead { get; }
	void TakeDamage(int amount, bool isBonus = false);
	void ApplyStatus(ElementType element);
	void ApplyKnockback(Vector2 force);
}

public static class Combat
{
	public const float BonusMultiplier = 1.5f;
	public const float KnockbackForce = 120f;

	private static readonly Dictionary<ElementType, ElementType[]> StrongAgainst = new()
	{
		{ ElementType.FIRE, new[] { ElementType.EARTH, ElementType.ICE } },
		{ ElementType.EARTH, new[] { ElementType.LIGHTNING } },
		{ ElementType.WATER, new[] { ElementType.FIRE } },
		{ ElementType.LIGHTNING, new[] { ElementType.WATER } },
		{ ElementType.WIND, new[] { ElementType.POISON, ElementType.EARTH } },
		{ ElementType.ICE, new[] { ElementType.WIND, ElementType.WATER } },
		{ ElementType.POISON, new[] { ElementType.EARTH } }
	};

	private static readonly Dictionary<ElementType, ElementType[]> WeakAgainst = new()
	{
		{ ElementType.FIRE, new[] { ElementType.WATER } },
		{ ElementType.EARTH, new[] { ElementType.WIND, ElementType.POISON } },
		{ ElementType.WATER, new[] { ElementType.LIGHTNING, ElementType.ICE } },
		{ ElementType.LIGHTNING, new[] { ElementType.EARTH } },
		{ ElementType.WIND, new[] { ElementType.ICE } },
		{ ElementType.ICE, new[] { ElementType.FIRE } },
		{ ElementType.POISON, new[] { ElementType.WIND, ElementType.LIGHTNING } }
	};

	/*the strong and weak lists in the txt don't fully match each other,
	  so it's bonus damage if either list says so*/
	public static bool IsStrongAgainst(ElementType attacker, ElementType defender) =>
		(StrongAgainst.TryGetValue(attacker, out var strong) && strong.Contains(defender))
		|| (WeakAgainst.TryGetValue(defender, out var weak) && weak.Contains(attacker));

	/*one hit from an attacker: knockback, the attacker's status effect, then damage
	  (x1.5 damage if the attacker's element is strong against the target)*/
	public static void Hit(Vector2 attackerPosition, ElementType attackerElement, ICombatant target, int baseDamage)
	{
		if (target.IsDead)
			return;

		bool isBonus = IsStrongAgainst(attackerElement, target.Element);
		int damage = isBonus ? Mathf.RoundToInt(baseDamage * BonusMultiplier) : baseDamage;

		target.ApplyKnockback((target.GlobalPosition - attackerPosition).Normalized() * KnockbackForce);
		if (attackerElement != ElementType.NONE)
			target.ApplyStatus(attackerElement);
		target.TakeDamage(damage, isBonus);
	}
}
