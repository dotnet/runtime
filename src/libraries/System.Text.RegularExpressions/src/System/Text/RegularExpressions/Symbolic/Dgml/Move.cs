// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if DEBUG
using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions.Symbolic.DGML
{
    /// <summary>
    /// Represents a move of a symbolic finite automaton.
    /// The value default(L) is reserved to represent the label of an epsilon move.
    /// Thus if S is a reference type the label of an epsilon move is null.
    /// </summary>
    /// <typeparam name="TLabel">the type of the labels on moves</typeparam>
    internal sealed class Move<TLabel>
    {
        /// <summary>
        /// Source state of the move
        /// </summary>
        public readonly int SourceState;
        /// <summary>
        /// Target state of the move
        /// </summary>
        public readonly int TargetState;
        /// <summary>
        /// Label of the move
        /// </summary>
        public readonly TLabel? Label;

        /// <summary>
        /// Transition of an automaton.
        /// </summary>
        /// <param name="sourceState">source state of the transition</param>
        /// <param name="targetState">target state of the transition</param>
        /// <param name="lab">label of the transition</param>
        public Move(int sourceState, int targetState, TLabel? lab)
        {
            SourceState = sourceState;
            TargetState = targetState;
            Label = lab;
        }

        /// <summary>
        /// Creates a move. Creates an epsilon move if label is default(L).
        /// </summary>
        public static Move<TLabel> Create(int sourceState, int targetState, TLabel condition) => new Move<TLabel>(sourceState, targetState, condition);

        /// <summary>
        /// Creates an epsilon move. Same as Create(sourceState, targetState, default(L)).
        /// </summary>
        public static Move<TLabel> Epsilon(int sourceState, int targetState) => new Move<TLabel>(sourceState, targetState, default);

        /// <summary>
        /// Returns true if label equals default(S).
        /// </summary>
        public bool IsEpsilon => Equals(Label, default(TLabel));

        /// <summary>
        /// Returns true if the source state and the target state are identical
        /// </summary>
        public bool IsSelfLoop => SourceState == TargetState;

        /// <summary>
        /// Returns true if obj is a move with the same source state, target state, and label.
        /// </summary>
        public override bool Equals([NotNullWhen(false)] object? obj) =>
            obj is Move<TLabel> t &&
            t.SourceState == SourceState &&
            t.TargetState == TargetState &&
            (t.Label is null ? Label is null : t.Label.Equals(Label));

        public override int GetHashCode() => (SourceState, Label, TargetState).GetHashCode();

        public override string ToString() => $"({SourceState},{(Equals(Label, default(TLabel)) ? "" : Label + ",")}{TargetState})";
    }
}
#endif
