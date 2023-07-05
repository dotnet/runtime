// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Net;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Captures a state explored during matching.</summary>
    internal sealed class MatchingState<TSet> where TSet : IComparable<TSet>, IEquatable<TSet>
    {
        internal MatchingState(SymbolicRegexNode<TSet> node, uint prevCharKind)
        {
            Node = node;
            PrevCharKind = prevCharKind;
        }

        /// <summary>The regular expression that labels this state and gives it its semantics.</summary>
        internal SymbolicRegexNode<TSet> Node { get; }

        /// <summary>
        /// The kind of the previous character in the input. The <see cref="SymbolicRegexMatcher{TSet}"/> is responsible
        /// for ensuring that in all uses of this state this invariant holds by both selecting initial states accordingly
        /// and transitioning on each character to states that match that character's kind.
        /// </summary>
        /// <remarks>
        /// Tracking this information is an optimization that allows each transition taken in the matcher to only depend
        /// on the next character (and its kind). In general, the transitions from a state with anchors in its pattern
        /// depend on both the previous and the next character. Creating distinct states for each kind of the previous
        /// character embeds the necessary information about the previous character into the state space of the automaton.
        /// However, this does incur a memory overhead due to the duplication of states. For patterns with no anchors
        /// this will always be set to <see cref="CharKind.General"/>, which can reduce the number of states created.
        ///
        /// The performance effect of this optimization has not been investigated. If this optimization were removed, the
        /// transition logic would in turn have to become more complicated for derivatives that depend on the nullability
        /// of anchors. Care should be taken to not slow down transitions without anchors involved.
        /// </remarks>
        internal uint PrevCharKind { get; }

        /// <summary>
        /// A unique identifier for this state, which is used in <see cref="SymbolicRegexMatcher{TSet}"/> to index into
        /// state information and transition arrays. Valid IDs are always >= 1.
        /// </summary>
        internal int Id { get; set; }

        /// <summary>Whether this state is known to be a dead end, i.e. no nullable states are reachable from here.</summary>
        internal bool IsDeadend(ISolver<TSet> solver) => Node.IsNothing(solver);

        /// <summary>
        /// Returns the fixed length that any match ending with this state must have, or -1 if there is no such
        /// fixed length, <see cref="SymbolicRegexNode{TSet}.ResolveFixedLength(uint)"/>. The context is defined
        /// by <see cref="PrevCharKind"/> of this state and the given nextCharKind. The node must be nullable here.
        /// </summary>
        internal int FixedLength(uint nextCharKind)
        {
            Debug.Assert(IsNullableFor(nextCharKind));
            Debug.Assert(CharKind.IsValidCharKind(nextCharKind));
            uint context = CharKind.Context(PrevCharKind, nextCharKind);
            return Node.ResolveFixedLength(context);
        }

        /// <summary>If true then state starts with a ^ or $ or \Z</summary>
        internal bool StartsWithLineAnchor => Node._info.StartsWithLineAnchor;

        /// <summary>
        /// Compute the target state for the given input minterm.
        /// If <paramref name="minterm"/> is False this means that this is \n and it is the last character of the input.
        /// </summary>
        /// <param name="builder">the builder that owns <see cref="Node"/></param>
        /// <param name="minterm">minterm corresponding to some input character or False corresponding to last \n</param>
        /// <param name="nextCharKind"></param>
        internal SymbolicRegexNode<TSet> Next(SymbolicRegexBuilder<TSet> builder, TSet minterm, uint nextCharKind)
        {
            // Combined character context
            uint context = CharKind.Context(PrevCharKind, nextCharKind);

            // Compute the derivative of the node for the given context
            return Node.CreateDerivativeWithoutEffects(builder, minterm, context);
        }

        /// <summary>
        /// Compute a set of transitions for the given minterm.
        /// </summary>
        /// <param name="builder">the builder that owns <see cref="Node"/></param>
        /// <param name="minterm">minterm corresponding to some input character or False corresponding to last \n</param>
        /// <param name="nextCharKind"></param>
        /// <returns>an enumeration of the transitions as pairs of the target state and a list of effects to be applied</returns>
        internal List<(SymbolicRegexNode<TSet> Node, DerivativeEffect[] Effects)> NfaNextWithEffects(SymbolicRegexBuilder<TSet> builder, TSet minterm, uint nextCharKind)
        {
            // Combined character context
            uint context = CharKind.Context(PrevCharKind, nextCharKind);

            // Compute the transitions for the given context
            return Node.CreateNfaDerivativeWithEffects(builder, minterm, context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsNullableFor(uint nextCharKind)
        {
            Debug.Assert(CharKind.IsValidCharKind(nextCharKind));
            uint context = CharKind.Context(PrevCharKind, nextCharKind);
            return Node.IsNullableFor(context);
        }

        /// <summary>
        /// Builds a <see cref="StateFlags"/> with the relevant flags set.
        /// </summary>
        /// <param name="solver">a solver for <typeparamref name="TSet"/></param>
        /// <param name="isInitial">whether this state is an initial state</param>
        /// <returns>the flags for this matching state</returns>
        internal StateFlags BuildStateFlags(ISolver<TSet> solver, bool isInitial)
        {
            StateFlags info = 0;

            if (isInitial)
            {
                info |= StateFlags.IsInitialFlag;
            }

            if (IsDeadend(solver))
            {
                info |= StateFlags.IsDeadendFlag;
            }

            if (Node.CanBeNullable)
            {
                info |= StateFlags.CanBeNullableFlag;
                if (Node.IsNullable)
                {
                    info |= StateFlags.IsNullableFlag;
                }
            }

            if (Node.Kind != SymbolicRegexNodeKind.DisableBacktrackingSimulation)
            {
                info |= StateFlags.SimulatesBacktrackingFlag;
            }

            return info;
        }

        public override bool Equals(object? obj) =>
            obj is MatchingState<TSet> s && PrevCharKind == s.PrevCharKind && Node.Equals(s.Node);

        public override int GetHashCode() => HashCode.Combine(PrevCharKind, Node);

#if DEBUG
        public override string ToString() =>
            PrevCharKind == 0 ? Node.ToString() :
             $"({CharKind.DescribePrev(PrevCharKind)},{Node})";
#endif
    }
}
