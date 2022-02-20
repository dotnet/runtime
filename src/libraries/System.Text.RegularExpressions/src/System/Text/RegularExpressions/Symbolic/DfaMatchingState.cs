// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Net;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Captures a state of a DFA explored during matching.</summary>
    internal sealed class DfaMatchingState<T> where T : notnull
    {
        internal DfaMatchingState(SymbolicRegexNode<T> node, uint prevCharKind)
        {
            Node = node;
            PrevCharKind = prevCharKind;
        }

        internal SymbolicRegexNode<T> Node { get; }

        internal uint PrevCharKind { get; }

        internal int Id { get; set; }

        internal bool IsInitialState { get; set; }

        /// <summary>State is lazy</summary>
        internal bool IsLazy => Node._info.IsLazy;

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

        /// <summary>If true then state starts with a ^ or $ or \A or \z or \Z</summary>
        internal bool StartsWithLineAnchor => Node._info.StartsWithLineAnchor;

        /// <summary>
        /// Translates a minterm predicate to a character kind, which is a general categorization of characters used
        /// for cheaply deciding the nullability of anchors.
        /// </summary>
        /// <remarks>
        /// A False predicate is handled as a special case to indicate the very last \n.
        /// </remarks>
        /// <param name="minterm">the minterm to translate</param>
        /// <returns>the character kind of the minterm</returns>
        private uint GetNextCharKind(ref T minterm)
        {
            ICharAlgebra<T> alg = Node._builder._solver;
            T wordLetterPredicate = Node._builder._wordLetterPredicateForAnchors;
            T newLinePredicate = Node._builder._newLinePredicate;

            // minterm == solver.False is used to represent the very last \n
            uint nextCharKind = CharKind.General;
            if (alg.False.Equals(minterm))
            {
                nextCharKind = CharKind.NewLineS;
                minterm = newLinePredicate;
            }
            else if (newLinePredicate.Equals(minterm))
            {
                // If the previous state was the start state, mark this as the very FIRST \n.
                // Essentially, this looks the same as the very last \n and is used to nullify
                // rev(\Z) in the conext of a reversed automaton.
                nextCharKind = PrevCharKind == CharKind.StartStop ?
                    CharKind.NewLineS :
                    CharKind.Newline;
            }
            else if (alg.IsSatisfiable(alg.And(wordLetterPredicate, minterm)))
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
        internal DfaMatchingState<T> Next(T minterm)
        {
            uint nextCharKind = GetNextCharKind(ref minterm);

            // Combined character context
            uint context = CharKind.Context(PrevCharKind, nextCharKind);

            // Compute the derivative of the node for the given context
            SymbolicRegexNode<T> derivative = Node.CreateDerivativeWithEffects(eager: true).TransitionOrdered(minterm, context);

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
        internal List<(DfaMatchingState<T> derivative, List<DerivativeEffect> effects)> AntimirovEagerNextWithEffects(T minterm)
        {
            uint nextCharKind = GetNextCharKind(ref minterm);

            // Combined character context
            uint context = CharKind.Context(PrevCharKind, nextCharKind);

            // Compute the transitions for the given context
            IEnumerable<(SymbolicRegexNode<T>, List<DerivativeEffect>)> derivativesAndEffects =
                Node.CreateDerivativeWithEffects(eager: true).TransitionsWithEffects(minterm, context);

            var list = new List<(DfaMatchingState<T> derivative, List<DerivativeEffect> effects)>();
            foreach ((SymbolicRegexNode<T> derivative, List<DerivativeEffect> effects) in derivativesAndEffects)
            {
                // nextCharKind will be the PrevCharKind of the target state
                // use an existing state instead if one exists already
                // otherwise create a new new id for it
                list.Add((Node._builder.CreateState(derivative, nextCharKind, capturing: true), effects));
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsNullable(uint nextCharKind)
        {
            Debug.Assert(nextCharKind is 0 or CharKind.StartStop or CharKind.Newline or CharKind.WordLetter or CharKind.NewLineS);
            uint context = CharKind.Context(PrevCharKind, nextCharKind);
            return Node.IsNullableFor(context);
        }

        public override bool Equals(object? obj) =>
            obj is DfaMatchingState<T> s && PrevCharKind == s.PrevCharKind && Node.Equals(s.Node);

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

    /// <summary>Encapsulates either a DFA state in Brzozowski mode or an NFA state set in Antimirov mode. Used by reference only.</summary>
    internal struct CurrentState<T> where T : notnull
    {
        // TBD: Consider SparseIntMap instead of HashSet
        // TBD: OrderedOr

        // _dfaMatchingState == null means Antimirov mode
        private DfaMatchingState<T>? _dfaMatchingState;

        private readonly SymbolicRegexBuilder<T> _builder;

        // used in Antimirov mode only
        private readonly HashSet<int> _nfaStates = new();
        private readonly List<int> _nfaStatesList = new();

        public CurrentState(DfaMatchingState<T> dfaMatchingState)
        {
            _builder = dfaMatchingState.Node._builder;
            if (_builder._antimirov)
            {
                // Create NFA state set if the builder is in Antimirov mode
                Debug.Assert(dfaMatchingState.Node.Kind == SymbolicRegexKind.Or && dfaMatchingState.Node._alts is not null);
                foreach (SymbolicRegexNode<T> member in dfaMatchingState.Node._alts)
                {
                    // Create (possibly new) NFA states for all the members
                    // add their IDs to the current set of NFA states and into the list
                    int nfaState = _builder.MkNfaState(member, dfaMatchingState.PrevCharKind);
                    if (_nfaStates.Add(nfaState))
                        // the list maintains the original order in which states are added but avoids duplicates
                        // TBD: OrderedOr may need to rely on that order
                        _nfaStatesList.Add(nfaState);
                }
                // Antimirov mode
                _dfaMatchingState = null;
            }
            else
                // Brzozowski mode
                _dfaMatchingState = dfaMatchingState;
        }
        public bool StartsWithLineAnchor
        {
            get
            {
                if (_dfaMatchingState is null)
                {
                    // in Antimirov mode check if some underlying core state starts with line anchor
                    for (int i = 0; i < _nfaStatesList.Count; i++)
                        if (_builder.GetCoreState(_nfaStatesList[i]).StartsWithLineAnchor)
                            return true;
                    return false;
                }
                else
                {
                    // Brzozowski mode
                    return _dfaMatchingState.StartsWithLineAnchor;
                }
            }
        }
        public bool IsNullable(uint nextCharKind)
        {
            if (_dfaMatchingState is null)
            {
                // in Antimirov mode check if some underlying core state is nullable
                for (int i = 0; i < _nfaStatesList.Count; i++)
                    if (_builder.GetCoreState(_nfaStatesList[i]).IsNullable(nextCharKind))
                        return true;
                return false;
            }
            else
            {
                return _dfaMatchingState.IsNullable(nextCharKind);
            }
        }

        /// <summary>In Antimirov mode an empty set of states means that it is a deadend</summary>
        public bool IsDeadend => (_dfaMatchingState is null ? _nfaStates.Count == 0 : _dfaMatchingState.IsDeadend);
        /// <summary>In Antimirov mode an empty set of states means that it is nothing</summary>
        public bool IsNothing => (_dfaMatchingState is null ? _nfaStates.Count == 0 : _dfaMatchingState.IsNothing);
        /// <summary>In Antimirov mode there are no watchdogs</summary>
        public int WatchDog => (_dfaMatchingState is null ? -1 : _dfaMatchingState.WatchDog);
        /// <summary>In Antimirov mode a set of states does not qualify as an initial state</summary>
        public bool IsInitialState => (_dfaMatchingState is null ? false : _dfaMatchingState.IsInitialState);

        /// <summary>
        /// Take the transition to the next state. This may cause a shift from  Brzozowski to Antimirov mode.
        /// </summary>
        public static void TakeTransition(ref CurrentState<T> state, int mintermId, T minterm)
        {
            if (state._dfaMatchingState is null)
            {
                // take a snapshot of the current set of nfa states
                int[] sourceStates = state._nfaStatesList.ToArray();

                // transition into the new set of target nfa states
                state._nfaStates.Clear();
                state._nfaStatesList.Clear();
                for (int i = 0; i < sourceStates.Length; i++)
                {
                    int source = sourceStates[i];
                    // Calculate the offset into the nfa transition table
                    int nfaoffset = (source << state._builder._mintermsCount) | mintermId;
                    List<int> targets = Volatile.Read(ref state._builder._antimirovDelta[nfaoffset]) ?? state._builder.CreateNewNfaTransition(source, mintermId, minterm, nfaoffset);
                    for (int j = 0; j < targets.Count; j++)
                        if (state._nfaStates.Add(targets[j]))
                            state._nfaStatesList.Add(targets[j]);
                }
            }
            else
            {
                Debug.Assert(state._builder._delta is not null);

                int offset = (state._dfaMatchingState.Id << state._builder._mintermsCount) | mintermId;
                state._dfaMatchingState = Volatile.Read(ref state._builder._delta[offset]) ?? state._builder.CreateNewTransition(state._dfaMatchingState, minterm, offset);

                if (state._builder._antimirov)
                {
                    // CreateNewTransition switched from Brzozowski to Antimirov mode
                    // update the state representation accordingly
                    // TBD: OrderedOr
                    Debug.Assert(state._dfaMatchingState.Node.Kind == SymbolicRegexKind.Or);
                    state = new CurrentState<T>(state._dfaMatchingState);
                }
            }
        }
    }
}
