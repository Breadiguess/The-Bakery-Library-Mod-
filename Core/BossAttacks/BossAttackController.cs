using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BreadLibrary.Core.BossAttacks
{
    /// <summary>
    /// Runtime attack runner for a boss.
    /// </summary>
    public class BossAttackController<TAttack, TBoss, TState>
        where TAttack : BossAttack<TAttack, TBoss, TState>
        where TState : struct, Enum
    {
        /// <summary>
        /// Implicitly construct a controller from an attack state.
        /// Creates the underlying attack instance via the registry but does not call Enter.
        /// This allows convenient initialization of a controller with a preset state when a boss instance
        /// is not yet available.
        /// </summary>
        /// <param name="state">The attack state to create the controller for.</param>
        public static implicit operator BossAttackController<TAttack, TBoss, TState>(TState state)
        {
            var controller = new BossAttackController<TAttack, TBoss, TState>
            {
                CurrentState = state,
                CurrentAttack = BossAttackRegistry<TAttack, TBoss, TState>.Create(state)
            };
            return controller;
        }

        /// <summary>
        /// Creates a new, empty controller. No attack is entered; <see cref="HasAttack"/> will be false.
        /// </summary>
        public BossAttackController()
        {
            CurrentState = default;
            CurrentAttack = default;
        }

        /// <summary>
        /// Creates a new controller and immediately enters the provided <paramref name="initialState"/>.
        /// The controller will call <see cref="BossAttack{TAttack, TBoss, TState}.Enter"/> on the created attack.
        /// </summary>
        /// <param name="boss">The boss instance used to enter the initial state.</param>
        /// <param name="initialState">The initial attack state to enter.</param>
        public BossAttackController(TBoss boss, TState initialState)
        {
            // Delegate to SetState to ensure correct Enter/Exit semantics and registry creation.
            SetState(boss, initialState);
        }

        /// <summary>
        /// The currently active attack instance.
        /// </summary>
        public TAttack CurrentAttack { get; private set; }

        /// <summary>
        /// The currently active attack state.
        /// </summary>
        public TState CurrentState { get; private set; }

        /// <summary>
        /// Whether the controller has entered an attack yet.
        /// </summary>
        public bool HasAttack => CurrentAttack is not null;

        /// <summary>
        /// Changes to a new attack state.
        /// </summary>
        public void SetState(TBoss boss, TState state)
        {
            CurrentAttack?.Exit(boss);

            CurrentState = state;
            CurrentAttack = BossAttackRegistry<TAttack, TBoss, TState>.Create(state);

            CurrentAttack.Enter(boss);
        }

        /// <summary>
        /// Updates the current attack. If no attack exists yet, enters fallbackState first.
        /// </summary>
        public void Update(TBoss boss, TState fallbackState)
        {
            if (CurrentAttack is null)
                SetState(boss, fallbackState);

            CurrentAttack.Update(boss);
        }

        /// <summary>
        /// Draws the current attack.
        /// </summary>
        public void Draw(TBoss boss, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            CurrentAttack?.Draw(boss, spriteBatch, screenPos, drawColor);
        }

        /// <summary>
        /// Checks whether the controller is currently in the given state.
        /// </summary>
        public bool IsState(TState state)
        {
            return CurrentAttack is not null &&
                   EqualityComparer<TState>.Default.Equals(CurrentState, state);
        }
    }
}