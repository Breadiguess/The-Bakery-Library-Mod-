using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace BreadLibrary.Core.BossAttacks
{
    /// <summary>
    /// Base class for ModNPC bosses that use the generic boss attack system.
    /// </summary>
    /// <typeparam name="TAttack">
    /// The boss-specific attack base type.
    /// For example: <see cref="SlimeBossAttack"/>
    /// </typeparam>
    /// <typeparam name="TBoss">
    /// The concrete boss type.
    /// This should be the same class that inherits from this host.
    /// </typeparam>
    /// <typeparam name="TState">
    /// The enum type used for attack states.
    /// </typeparam>
    /// <remarks>
    /// This class replaces the need to manually add a BossAttackController field to every boss.
    /// It stores the active attack, handles attack transitions, and exposes helper methods
    /// for updating and drawing the current attack.
    /// </remarks>
    public abstract class BossAttackHost<TAttack, TBoss, TState> : ModNPC
        where TAttack : BossAttack<TAttack, TBoss, TState>
        where TBoss : BossAttackHost<TAttack, TBoss, TState>
        where TState : struct, Enum
    {
        /// <summary>
        /// Gets the currently active runtime attack instance.
        /// </summary>
        public TAttack CurrentAttack { get; private set; }

        /// <summary>
        /// Whether this host has entered an attack yet.
        /// </summary>
        public bool HasAttack => CurrentAttack is not null;

        /// <summary>
        /// Gets or sets the boss's current attack state.
        /// </summary>
        /// <remarks>
        /// you can (and should) store this in an NPC.ai[] slot for easy multiplayer sync and persistence.
        /// </remarks>
        public abstract TState CurrentState { get; set; }

        /// <summary>
        /// Gets this boss instance as its concrete boss type.
        /// </summary>
        protected TBoss Self => (TBoss)this;

        /// <summary>
        /// Changes the boss to a new attack state.
        /// </summary>
        /// <param name="state">The state to enter.</param>
        /// <remarks>
        /// This exits the previous attack, creates a fresh attack instance for the new state,
        /// stores the new state, and calls the new attack's Enter method.
        /// </remarks>
        public virtual void SetAttackState(TState state)
        {
            CurrentAttack?.Exit(Self);

            CurrentState = state;
            CurrentAttack = BossAttackRegistry<TAttack, TBoss, TState>.Create(state);

            CurrentAttack.Enter(Self);
        }

        /// <summary>
        /// Updates the currently active attack.
        /// </summary>
        /// <remarks>
        /// Call this from your boss's AI method.
        /// If no attack has been entered yet, this enters <see cref="CurrentState"/> first.
        /// you're welcome, L-man.
        /// </remarks>
        protected void UpdateCurrentAttack()
        {
            if (CurrentAttack is null)
                SetAttackState(CurrentState);

            CurrentAttack.Update(Self);
        }

        protected void DrawCurrentAttack(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            CurrentAttack?.Draw(Self, spriteBatch, screenPos, drawColor);
        }

        public bool IsAttackState(TState state)
        {
            return EqualityComparer<TState>.Default.Equals(CurrentState, state);
        }

     
        public abstract void MoveToNextState();
    }
}