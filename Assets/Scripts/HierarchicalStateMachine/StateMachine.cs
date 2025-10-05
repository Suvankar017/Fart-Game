using System.Collections.Generic;

namespace HierarchicalStateMachine
{
    public class StateMachine
    {
        public readonly State Root;
        public readonly TransitionSequencer Sequencer;

        private bool hasStarted;

        public StateMachine(State root)
        {
            Root = root;
            Sequencer = new TransitionSequencer(this);
            hasStarted = false;
        }

        public void Start()
        {
            if (hasStarted)
                return;

            hasStarted = true;
            Root.Enter();
        }

        public void Tick(float deltaTime)
        {
            if (!hasStarted)
                Start();

            // InternalTick(deltaTime);
            Sequencer.Tick(deltaTime);
        }

        internal void InternalTick(float deltaTime) => Root.Update(deltaTime);

        // Perform the actual switch from 'from' to 'to' by exiting up to the shared ancestor, then entering down to the target.
        public void ChangeState(State from, State to)
        {
            if (from == to || from == null || to == null)
                return;

            State lca = TransitionSequencer.LCA(from, to);

            // Exit states from 'from' up to (but not including) 'LCA'.
            for (State s = from; s != lca; s = s.Parent)
                s.Exit();

            // Enter target states from 'LCA' down to 'to'.
            Stack<State> stack = new();
            for (State s = to; s != lca; s = s.Parent)
                stack.Push(s);
            while (stack.Count > 0)
                stack.Pop().Enter();
        }
    }
}
