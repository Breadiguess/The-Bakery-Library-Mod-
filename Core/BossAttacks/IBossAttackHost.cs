using System;

namespace BreadLibrary.Core.BossAttacks
{
    /// <summary>
    /// Minimal interface for NPCs that can host boss attacks.
    /// </summary>
    /// <typeparam name="TState">The enum type used for attack states.</typeparam>
    public interface IBossAttackHost<TState>
        where TState : struct, Enum
    {
        /// <summary>
        /// The currently active attack state.
        /// </summary>
        TState CurrentState { get; set; }

        /// <summary>
        /// Moves this boss to a specific attack state.
        /// </summary>
        void SetAttackState(TState state);

        /// <summary>
        /// Chooses and enters the next attack state.
        /// </summary>
        void MoveToNextState();
    }
}