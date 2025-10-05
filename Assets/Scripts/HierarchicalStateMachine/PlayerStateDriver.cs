using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using HierarchicalStateMachine;
using PlayerHSM;

public class PlayerStateDriver : MonoBehaviour
{
    public PlayerContext context = new();

    private Rigidbody2D rb;
    private StateMachine machine;
    private State root;
    private string lastPath;

    private void Awake()
    {
        if (!TryGetComponent(out rb))
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.freezeRotation = true;

        context.rb = rb;
        context.anim = GetComponentInChildren<Animator>();

        root = new PlayerRoot(null, context);
        StateMachineBuilder builder = new(root);
        machine = builder.Build();
    }

    private void Update()
    {
        machine.Tick(Time.deltaTime);

        string path = StatePath(machine.Root.Leaf());
        if (path != lastPath)
        {
            lastPath = path;
            Debug.Log(path);
        }
    }

    private static string StatePath(State state) => string.Join(" > ", state.PathToRoot().Reverse().Select(n => n.GetType().Name));
}

[Serializable]
public class PlayerContext
{
    public Vector2 move;
    public Vector2 velocity;
    public float moveSpeed = 6.0f;
    public float acceleration = 40.0f;
    public float jumpSpeed = 7.0f;
    public bool isGrounded;
    public bool jumpPressed;
    public Animator anim;
    public Rigidbody2D rb;
}

public class StateMachineBuilder
{
    private readonly State root;

    public StateMachineBuilder(State root)
    {
        this.root = root;
    }

    public StateMachine Build()
    {
        StateMachine machine = new(root);
        Wire(root, machine, new HashSet<State>());
        return machine;
    }

    private void Wire(State state, StateMachine machine, HashSet<State> visited)
    {
        if (state == null)
            return;

        // State is already wired.
        if (!visited.Add(state))
            return;

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        FieldInfo machineField = typeof(State).GetField("Machine", flags);
        machineField?.SetValue(state, machine);

        foreach (FieldInfo fieldInfo in state.GetType().GetFields(flags))
        {
            // Only consider fields that are state.
            if (!typeof(State).IsAssignableFrom(fieldInfo.FieldType))
                continue;

            // Skip back-edge to parent.
            if (string.Compare(fieldInfo.Name, "Parent") == 0)
                continue;

            State child = (State)fieldInfo.GetValue(state);
            if (child == null)
                continue;

            // Ensure it's actually our direct child
            if (!ReferenceEquals(child.Parent, state))
                continue;

            // Recurse into the child.
            Wire(child, machine, visited);
        }
    }
}
