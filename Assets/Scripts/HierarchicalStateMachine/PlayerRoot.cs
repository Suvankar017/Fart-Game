using UnityEngine;
using HierarchicalStateMachine;

namespace PlayerHSM
{
    public class PlayerRoot : State
    {
        public readonly Grounded Grounded;
        public readonly Airborne Airborne;

        private readonly PlayerContext ctx;

        public PlayerRoot(StateMachine machine, PlayerContext context) : base(machine, null)
        {
            ctx = context;
            Grounded = new Grounded(machine, this, context);
            Airborne = new Airborne(machine, this, context);
        }

        protected override State GetInitialState() => Grounded;

        protected override State GetTransition() => ctx.isGrounded ? null : Airborne;
    }

    public class Grounded : State
    {
        public readonly Idle Idle;
        public readonly Move Move;

        private readonly PlayerContext ctx;

        public Grounded(StateMachine machine, State parent, PlayerContext context) : base(machine, parent)
        {
            ctx = context;
            Idle = new Idle(machine, this, context);
            Move = new Move(machine, this, context);
        }

        protected override State GetInitialState() => Idle;

        protected override State GetTransition()
        {
            if (ctx.jumpPressed)
            {
                ctx.jumpPressed = false;
                Rigidbody2D rb = ctx.rb;

                if (rb != null)
                {
                    Vector2 v = rb.linearVelocity;
                    v.y = ctx.jumpSpeed;
                    rb.linearVelocity = v;
                }

                return ((PlayerRoot)Parent).Airborne;
            }

            return ctx.isGrounded ? null : ((PlayerRoot)Parent).Airborne;
        }
    }

    public class Idle : State
    {
        private readonly PlayerContext ctx;

        public Idle(StateMachine machine, State parent, PlayerContext context) : base(machine, parent)
        {
            ctx = context;
        }

        protected override State GetTransition() => Mathf.Abs(ctx.move.x) > 0.01f ? ((Grounded)Parent).Move : null;

        protected override void OnEnter()
        {
            ctx.velocity.x = 0.0f;
        }
    }

    public class Move : State
    {
        private readonly PlayerContext ctx;

        public Move(StateMachine machine, State parent, PlayerContext context) : base(machine, parent)
        {
            ctx = context;
        }

        protected override State GetTransition()
        {
            if (!ctx.isGrounded)
                return ((PlayerRoot)Parent).Airborne;

            return Mathf.Abs(ctx.move.x) <= 0.01f ? ((Grounded)Parent).Idle : null;
        }

        protected override void OnUpdate(float deltaTime)
        {
            float target = ctx.move.x * ctx.moveSpeed;
            ctx.velocity.x = Mathf.MoveTowards(ctx.velocity.x, target, ctx.acceleration * deltaTime);
        }
    }

    public class Airborne : State
    {
        private readonly PlayerContext ctx;

        public Airborne(StateMachine machine, State parent, PlayerContext context) : base(machine, parent)
        {
            ctx = context;
        }

        protected override State GetTransition() => ctx.isGrounded ? ((PlayerRoot)Parent).Grounded : null;

        protected override void OnEnter()
        {

        }
    }
}
