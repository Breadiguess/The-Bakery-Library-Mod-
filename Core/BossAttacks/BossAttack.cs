using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace BreadLibrary.Core.BossAttacks
{
    /// <summary>
    /// Represents a single autoloaded boss attack/state behavior.
    /// </summary>
    /// <typeparam name="TAttack">
    /// The boss-specific abstract attack family type.
    /// For example: SlimeBossAttack.
    /// </typeparam>
    /// <typeparam name="TBoss">
    /// The concrete boss type that owns and executes this attack.
    /// For example: SlimeBoss.
    /// </typeparam>
    /// <typeparam name="TState">
    /// The enum type used to identify this boss's attack states.
    /// For example: SlimeBossState.
    /// </typeparam>
    public abstract class BossAttack<TAttack, TBoss, TState> : ModType
        where TAttack : BossAttack<TAttack, TBoss, TState>
        where TState : struct, Enum
    {
        /// <summary>
        /// The state ID handled by this attack.
        /// </summary>
        public abstract TState ID { get; }

        /// <summary>
        /// Whether this attack should register itself automatically.
        /// </summary>
        public virtual bool AutoloadAttack => true;

        /// <summary>
        /// Registers this attack type into the attack registry during tModLoader loading.
        /// </summary>
        protected sealed override void Register()
        {
            if (!AutoloadAttack)
                return;

            BossAttackRegistry<TAttack, TBoss, TState>.Register(ID, GetType());
        }

        /// <summary>
        /// Called when this attack becomes active.
        /// </summary>
        public virtual void Enter(TBoss boss) { }

        /// <summary>
        /// Called every AI update while this attack is active.
        /// </summary>
        public abstract void Update(TBoss boss);

        /// <summary>
        /// Called when this attack stops being active.
        /// </summary>
        public virtual void Exit(TBoss boss) { }

        /// <summary>
        /// Called when this attack should draw extra visuals.
        /// </summary>
        public virtual void Draw(TBoss boss, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) { }

        /// <summary>
        /// Ends this attack and asks the owning boss to move to its next state.
        /// </summary>
        protected void Finish(TBoss boss)
        {
            if (boss is IBossAttackHost<TState> host)
            {
                host.MoveToNextState();
                return;
            }

            throw new InvalidOperationException(
                $"{typeof(TBoss).Name} must implement IBossAttackHost<{typeof(TState).Name}> to use Finish()."
            );
        }

        /// <summary>
        /// Ends this attack and moves directly to the provided state.
        /// </summary>
        protected void FinishInto(TBoss boss, TState state)
        {
            if (boss is IBossAttackHost<TState> host)
            {
                host.SetAttackState(state);
                return;
            }

            throw new InvalidOperationException(
                $"{typeof(TBoss).Name} must implement IBossAttackHost<{typeof(TState).Name}> to use FinishInto()."
            );
        }
    }
}