// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Net;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Captures a state of a DFA explored during matching.</summary>
    internal sealed class DfaMatchingState<TSet> where TSet : IComparable<TSet>, IEquatable<TSet>
    {
        internal DfaMatchingState(SymbolicRegexNode<TSet> node, uint prevCharKind)
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
        /// For patterns with no anchors this will always be set to <see cref="CharKind.General"/>, which can reduce the
        /// number of states created.
        /// </remarks>
        internal uint PrevCharKind { get; }

        /// <summary>
        /// A unique identifier for this state, which is used in <see cref="SymbolicRegexMatcher{TSet}"/> to index into
        /// state information and transition arrays.
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

        public override bool Equals(object? obj) =>
            obj is DfaMatchingState<TSet> s && PrevCharKind == s.PrevCharKind && Node.Equals(s.Node);

        public override int GetHashCode() => (PrevCharKind, Node).GetHashCode();

#if DEBUG
        public override string ToString() =>
            PrevCharKind == 0 ? Node.ToString() :
             $"({CharKind.DescribePrev(PrevCharKind)},{Node})";
#endif
    }
}
