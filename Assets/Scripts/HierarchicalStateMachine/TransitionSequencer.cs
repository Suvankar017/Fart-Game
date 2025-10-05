using System;
using System.Collections.Generic;

namespace HierarchicalStateMachine
{
    public class TransitionSequencer
    {
        public readonly StateMachine Machine;

        private ISequence sequence;                 // Current phase (deactivate or activate)
        private Action nextPhase;                   // switch structure between phases
        private (State from, State to)? pending;    // coalesced a single transition request
        private State lastFrom, lastTo;

        public TransitionSequencer(StateMachine machine)
        {
            Machine = machine;
        }

        // Request a transition from one state to another.
        public void RequestTransition(State from, State to)
        {
            // Machine.ChangeState(from, to);
            if (to == null || from == to)
                return;

            if (sequence != null)
            {
                pending = (from, to);
                return;
            }

            BeginTransition(from, to);
        }

        public void Tick(float deltaTime)
        {
            if (sequence == null)
            {
                Machine.InternalTick(deltaTime);
                return;
            }

            if (sequence.Update(deltaTime))
            {
                if (nextPhase != null)
                {
                    Action nextPhaseAction = nextPhase;
                    nextPhase = null;
                    nextPhaseAction();
                }
                else
                {
                    EndTransition();
                }
            }
        }

        private void BeginTransition(State from, State to)
        {
            // Placeholder for any setup needed before a transition.

            // 1. Deactivate the "old branch"
            sequence = new NoopPhase(); // TODO: Replace with actual deactivation phase
            sequence.Start();

            nextPhase = () =>
            {
                // 2. Change state
                Machine.ChangeState(from, to);

                // 3. Activate the "new branch"
                sequence = new NoopPhase(); // TODO: Replace with actual activation phase
                sequence.Start();
            };
        }

        private void EndTransition()
        {
            // Placeholder for any cleanup needed after a transition.

            sequence = null;

            if (pending.HasValue)
            {
                (State from, State to) = pending.Value;
                pending = null;
                BeginTransition(from, to);
            }
        }

        // Compute the Lowest Common Ancestor of two states.
        public static State LCANonAlloc(State from, State to)
        {
            // First, compute the depths of both states.
            int depthA = 0;
            for (State s = from; s != null; s = s.Parent)
                depthA++;
            int depthB = 0;
            for (State s = to; s != null; s = s.Parent)
                depthB++;

            // Ascend from the deeper state until both states are at the same depth.
            while (depthA > depthB)
            {
                from = from.Parent;
                depthA--;
            }
            while (depthB > depthA)
            {
                to = to.Parent;
                depthB--;
            }

            // Now ascend both states in tandem until we find the common ancestor.
            while (from != to)
            {
                from = from.Parent;
                to = to.Parent;
            }

            return from; // or b, since a == b
        }

        public static State LCA(State a, State b)
        {
            // Create a set of all parents of 'a'.
            HashSet<State> set = new();
            for (State s = a; s != null; s = s.Parent)
                set.Add(s);

            // Find the first parent of 'b' that is also a parent of 'a'.
            for (State s = b; s != null; s = s.Parent)
                if (set.Contains(s))
                    return s;

            // No common ancestor found (shouldn't happen in a well-formed state machine).
            return null;
        }
    }

    public interface ISequence
    {
        public bool IsDone { get; }

        public void Start();
        public bool Update(float deltaTime);
    }

    public class NoopPhase : ISequence
    {
        public bool IsDone { get; private set; }

        public NoopPhase()
        {
            IsDone = false;
        }

        public void Start()
        {
            IsDone = true;
        }

        public bool Update(float deltaTime) => IsDone;
    }
}
