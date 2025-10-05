using System.Collections.Generic;

namespace HierarchicalStateMachine
{
    public abstract class State
    {
        public readonly StateMachine Machine;
        public readonly State Parent;
        public State ActiveChild;

        public State(StateMachine machine, State parent = null)
        {
            Machine = machine;
            Parent = parent;
        }

        protected virtual State GetInitialState() => null;
        protected virtual State GetTransition() => null;

        protected virtual void OnEnter() { }
        protected virtual void OnExit() { }
        protected virtual void OnUpdate(float deltaTime) { }

        internal void Enter()
        {
            if (Parent != null) Parent.ActiveChild = this;
            OnEnter();
            State init = GetInitialState();
            init?.Enter();
        }

        internal void Exit()
        {
            ActiveChild?.Exit();
            ActiveChild = null;
            OnExit();
        }

        internal void Update(float deltaTime)
        {
            State t = GetTransition();
            if (t != null)
            {
                Machine.Sequencer.RequestTransition(this, t);
                return;
            }

            ActiveChild?.Update(deltaTime);
            OnUpdate(deltaTime);
        }

        // Returns the deepest currently-active descendant state (the leaf of the active path).
        public State Leaf()
        {
            State s = this;
            while (s.ActiveChild != null) s = s.ActiveChild;
            return s;
        }

        // Yields this state and then each ancestor up to the root (self -> parent -> ... -> root).
        public IEnumerable<State> PathToRoot()
        {
            for (State s = this; s != null; s = s.Parent) yield return s;
        }
    }
}
