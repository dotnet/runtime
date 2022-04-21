// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Net;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Captures a state of a DFA explored during matching.</summary>
    internal sealed class DfaMatchingState<TSet> where TSet : IComparable<TSet>
    {
        internal DfaMatchingState(SymbolicRegexNode<TSet> node, uint prevCharKind)
        {
            Node = node;
            PrevCharKind = prevCharKind;
        }

        internal SymbolicRegexNode<TSet> Node { get; }

        internal uint PrevCharKind { get; }

        internal int Id { get; set; }

        internal bool IsInitialState { get; set; }

        /// <summary>This is a deadend state</summary>
        internal bool IsDeadend => Node.IsNothing;

        /// <summary>The node must be nullable here</summary>
        internal int FixedLength
        {
            get
            {
                if (Node._kind == SymbolicRegexNodeKind.FixedLengthMarker)
                {
                    return Node._lower;
                }

                if (Node._kind == SymbolicRegexNodeKind.Or)
                {
                    Debug.Assert(Node._alts is not null);
                    return Node._alts._maximumLength;
                }

                return -1;
            }
        }

        /// <summary>If true then the state is a dead-end, rejects all inputs.</summary>
        internal bool IsNothing => Node.IsNothing;

        /// <summary>If true then state starts with a ^ or $ or \Z</summary>
        internal bool StartsWithLineAnchor => Node._info.StartsWithLineAnchor;

        /// <summary>
        /// Translates a minterm set to a character kind, which is a general categorization of characters used
        /// for cheaply deciding the nullability of anchors.
        /// </summary>
        /// <remarks>
        /// An empty set is handled as a special case to indicate the very last \n.
        /// </remarks>
        /// <param name="minterm">the minterm to translate</param>
        /// <returns>the character kind of the minterm</returns>
        private uint GetNextCharKind(ref TSet minterm)
        {
            ISolver<TSet> solver = Node._builder._solver;
            TSet wordLetterPredicate = Node._builder._wordLetterForBoundariesSet;
            TSet newLinePredicate = Node._builder._newLineSet;

            // minterm == solver.False is used to represent the very last \n
            uint nextCharKind = CharKind.General;
            if (solver.Empty.Equals(minterm))
            {
                nextCharKind = CharKind.NewLineS;
                minterm = newLinePredicate;
            }
            else if (newLinePredicate.Equals(minterm))
            {
                // If the previous state was the start state, mark this as the very FIRST \n.
                // Essentially, this looks the same as the very last \n and is used to nullify
                // rev(\Z) in the conext of a reversed automaton.
                nextCharKind = PrevCharKind == CharKind.BeginningEnd ?
                    CharKind.NewLineS :
                    CharKind.Newline;
            }
            else if (!solver.IsEmpty(solver.And(wordLetterPredicate, minterm)))
            {
                nextCharKind = CharKind.WordLetter;
            }
            return nextCharKind;
        }

        /// <summary>
        /// Compute the target state for the given input minterm.
        /// If <paramref name="minterm"/> is False this means that this is \n and it is the last character of the input.
        /// </summary>
        /// <param name="minterm">minterm corresponding to some input character or False corresponding to last \n</param>
        internal DfaMatchingState<TSet> Next(TSet minterm)
        {
            uint nextCharKind = GetNextCharKind(ref minterm);

            // Combined character context
            uint context = CharKind.Context(PrevCharKind, nextCharKind);

            // Compute the derivative of the node for the given context
            SymbolicRegexNode<TSet> derivative = Node.CreateDerivative(minterm, context);

            // nextCharKind will be the PrevCharKind of the target state
            // use an existing state instead if one exists already
            // otherwise create a new new id for it
            return Node._builder.CreateState(derivative, nextCharKind, capturing: false);
        }

        /// <summary>
        /// Compute a set of transitions for the given minterm.
        /// </summary>
        /// <param name="minterm">minterm corresponding to some input character or False corresponding to last \n</param>
        /// <returns>an enumeration of the transitions as pairs of the target state and a list of effects to be applied</returns>
        internal List<(DfaMatchingState<TSet> State, DerivativeEffect[] Effects)> NfaNextWithEffects(TSet minterm)
        {
            uint nextCharKind = GetNextCharKind(ref minterm);

            // Combined character context
            uint context = CharKind.Context(PrevCharKind, nextCharKind);

            // Compute the transitions for the given context
            List<(SymbolicRegexNode<TSet>, DerivativeEffect[])> nodesAndEffects = Node.CreateNfaDerivativeWithEffects(minterm, context);

            var list = new List<(DfaMatchingState<TSet> State, DerivativeEffect[] Effects)>();
            foreach ((SymbolicRegexNode<TSet> node, DerivativeEffect[]? effects) in nodesAndEffects)
            {
                // nextCharKind will be the PrevCharKind of the target state
                // use an existing state instead if one exists already
                // otherwise create a new new id for it
                DfaMatchingState<TSet> state = Node._builder.CreateState(node, nextCharKind, capturing: true);
                if (!state.IsDeadend)
                    list.Add((state, effects));
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsNullable(uint nextCharKind)
        {
            Debug.Assert(nextCharKind is 0 or CharKind.BeginningEnd or CharKind.Newline or CharKind.WordLetter or CharKind.NewLineS);
            uint context = CharKind.Context(PrevCharKind, nextCharKind);
            return Node.IsNullableFor(context);
        }

        public override bool Equals(object? obj) =>
            obj is DfaMatchingState<TSet> s && PrevCharKind == s.PrevCharKind && Node.Equals(s.Node);

        public override int GetHashCode() => (PrevCharKind, Node).GetHashCode();

        public override string ToString() =>
            PrevCharKind == 0 ? Node.ToString() :
             $"({CharKind.DescribePrev(PrevCharKind)},{Node})";

#if DEBUG
        internal string DgmlView
        {
            get
            {
                string info = CharKind.DescribePrev(PrevCharKind);
                if (info != string.Empty)
                {
                    info = $"Previous: {info}&#13;";
                }

                string deriv = WebUtility.HtmlEncode(Node.ToString());
                if (deriv == string.Empty)
                {
                    deriv = "()";
                }

                return $"{info}{deriv}";
            }
        }
#endif
    }
}
