using System;
using System.Collections.Generic;

namespace BreadLibrary.Core.BossAttacks
{
    /// <summary>
    /// Stores autoloaded attack types for a specific boss attack family.
    /// </summary>
    public static class BossAttackRegistry<TAttack, TBoss, TState>
        where TAttack : BossAttack<TAttack, TBoss, TState>
        where TState : struct, Enum
    {
        private static readonly Dictionary<TState, Type> AttackTypes = new();

        /// <summary>
        /// Registers an attack type for a state.
        /// </summary>
        public static void Register(TState state, Type attackType)
        {
            if (!typeof(TAttack).IsAssignableFrom(attackType))
            {
                throw new ArgumentException(
                    $"{attackType.FullName} must inherit from {typeof(TAttack).FullName}.",
                    nameof(attackType)
                );
            }

            if (AttackTypes.TryGetValue(state, out Type existingType))
            {
                throw new InvalidOperationException(
                    $"Duplicate attack registered for state {state}. " +
                    $"Existing: {existingType.FullName}. New: {attackType.FullName}."
                );
            }

            AttackTypes.Add(state, attackType);
        }

        /// <summary>
        /// Creates a fresh attack instance for the given state.
        /// </summary>
        public static TAttack Create(TState state)
        {
            if (!AttackTypes.TryGetValue(state, out Type type))
            {
                throw new KeyNotFoundException(
                    $"No {typeof(TAttack).Name} is registered for state {state}."
                );
            }

            return (TAttack)Activator.CreateInstance(type);
        }

        /// <summary>
        /// Checks whether an attack exists for the given state.
        /// </summary>
        public static bool Has(TState state)
        {
            return AttackTypes.ContainsKey(state);
        }

        /// <summary>
        /// Clears registered attacks.
        /// </summary>
        public static void Clear()
        {
            AttackTypes.Clear();
        }
    }
}