using BreadLibrary.Core.BossAttacks;
using Terraria.ModLoader;

namespace BreadLibrary.Core.BossAttacks
{
    public abstract class SlimeBossAttack
        : BossAttack<SlimeBossAttack, SlimeBoss, SlimeBossState>
    {
    }

    public class SlimeBossAttack1 : SlimeBossAttack
    {
        public override SlimeBossState ID => SlimeBossState.Attack1;

        public override void Enter(SlimeBoss boss)
        {
        }

        public override void Update(SlimeBoss boss)
        {

        }
    }

    public enum SlimeBossState
    {
        Attack1,
        Attack2,
        Attack3
    }

    public class SlimeBoss : BossAttackHost<SlimeBossAttack, SlimeBoss, SlimeBossState>
    {
        private BossAttackController<SlimeBossAttack, SlimeBoss, SlimeBossState> Attacks;
        public override SlimeBossState CurrentState
        {
            get => (SlimeBossState)NPC.ai[0];
            set => NPC.ai[0] = (float)(int)value;
        }

        public override void SetDefaults()
        {
            NPC.width = 80;
            NPC.height = 60;
            NPC.damage = 20;
            NPC.defense = 5;
            NPC.lifeMax = 500;
            NPC.aiStyle = -1;
        }

        public override void AI()
        {
            UpdateCurrentAttack();
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            DrawCurrentAttack(spriteBatch, screenPos, drawColor);
            return true;
        }

        public override void MoveToNextState()
        {
            SlimeBossState next = CurrentState switch
            {
                SlimeBossState.Attack1 => SlimeBossState.Attack2,
                SlimeBossState.Attack2 => SlimeBossState.Attack3,
                _ => SlimeBossState.Attack1
            };

            SetAttackState(next);
        }
    }
}