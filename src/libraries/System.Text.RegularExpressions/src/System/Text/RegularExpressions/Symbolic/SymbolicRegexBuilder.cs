// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// Builder of symbolic regexes over TElement.
    /// TElement is the type of elements of an effective Boolean algebra.
    /// Used to convert .NET regexes to symbolic regexes.
    /// </summary>
    internal sealed class SymbolicRegexBuilder<TElement> where TElement : notnull
    {
        internal readonly ICharAlgebra<TElement> _solver;

        internal readonly SymbolicRegexNode<TElement> _epsilon;
        internal readonly SymbolicRegexNode<TElement> _nothing;
        internal readonly SymbolicRegexNode<TElement> _startAnchor;
        internal readonly SymbolicRegexNode<TElement> _endAnchor;
        internal readonly SymbolicRegexNode<TElement> _endAnchorZ;
        internal readonly SymbolicRegexNode<TElement> _endAnchorZRev;
        internal readonly SymbolicRegexNode<TElement> _bolAnchor;
        internal readonly SymbolicRegexNode<TElement> _eolAnchor;
        internal readonly SymbolicRegexNode<TElement> _anyChar;
        internal readonly SymbolicRegexNode<TElement> _anyStar;
        internal readonly SymbolicRegexNode<TElement> _wbAnchor;
        internal readonly SymbolicRegexNode<TElement> _nwbAnchor;
        internal readonly SymbolicRegexSet<TElement> _fullSet;
        internal readonly SymbolicRegexSet<TElement> _emptySet;
        internal readonly SymbolicRegexNode<TElement> _eagerEmptyLoop;

        internal TElement _wordLetterPredicateForAnchors;
        internal TElement _newLinePredicate;

        /// <summary>Partition of the input space of predicates.</summary>
        internal TElement[]? _minterms;

        private readonly Dictionary<TElement, SymbolicRegexNode<TElement>> _singletonCache = new();

        // states that have been created
        internal HashSet<DfaMatchingState<TElement>> _stateCache = new();

        internal readonly Dictionary<(SymbolicRegexKind,
            SymbolicRegexNode<TElement>?, // _left
            SymbolicRegexNode<TElement>?, // _right
            int, int, TElement?,          // _lower, _upper, _set
            SymbolicRegexSet<TElement>?,
            SymbolicRegexInfo), SymbolicRegexNode<TElement>> _nodeCache = new();

        internal readonly Dictionary<(TransitionRegexKind, // _kind
            TElement?,                                     // _test
            TransitionRegex<TElement>?,                    // _first
            TransitionRegex<TElement>?,                    // _second
            SymbolicRegexNode<TElement>?),                 // _leaf
            TransitionRegex<TElement>> _trCache = new();

        /// <summary>
        /// Maps state ids to states, initial capacity is 1024 states.
        /// Each time more states are needed the length is increased by 1024.
        /// </summary>
        internal DfaMatchingState<TElement>[]? _statearray;
        internal DfaMatchingState<TElement>[]? _delta;
        private const int InitialStateLimit = 1024;

        /// <summary>
        /// <see cref="_mintermsCount"/> is the smallest k s.t. 2^k >= minterms.Length + 1
        /// </summary>
        internal int _mintermsCount;

        /// <summary>
        /// If true then delta is used in a mode where
        /// each target state represents a set of states.
        /// </summary>
        internal bool _antimirov;

        /// <summary>Create a new symbolic regex builder.</summary>
        internal SymbolicRegexBuilder(ICharAlgebra<TElement> solver)
        {
            // Solver must be set first, else it will cause null reference exception in the following
            _solver = solver;
            _epsilon = SymbolicRegexNode<TElement>.MkEpsilon(this);
            _startAnchor = SymbolicRegexNode<TElement>.MkStartAnchor(this);
            _endAnchor = SymbolicRegexNode<TElement>.MkEndAnchor(this);
            _endAnchorZ = SymbolicRegexNode<TElement>.MkEndAnchorZ(this);
            _endAnchorZRev = SymbolicRegexNode<TElement>.MkEndAnchorZRev(this);
            _eolAnchor = SymbolicRegexNode<TElement>.MkEolAnchor(this);
            _bolAnchor = SymbolicRegexNode<TElement>.MkBolAnchor(this);
            _wbAnchor = SymbolicRegexNode<TElement>.MkWBAnchor(this);
            _nwbAnchor = SymbolicRegexNode<TElement>.MkNWBAnchor(this);
            _emptySet = SymbolicRegexSet<TElement>.CreateEmpty(this);
            _fullSet = SymbolicRegexSet<TElement>.CreateFull(this);
            _eagerEmptyLoop = SymbolicRegexNode<TElement>.MkEagerEmptyLoop(this, _epsilon);

            // minterms = null if partition of the solver is undefined and returned as null
            _minterms = solver.GetMinterms();
            if (_minterms == null)
            {
                _mintermsCount = -1;
            }
            else
            {
                _statearray = new DfaMatchingState<TElement>[InitialStateLimit];

                // the extra slot with id minterms.Length is reserved for \Z (last occurrence of \n)
                int mintermsCount = 1;
                while (_minterms.Length >= (1 << mintermsCount))
                {
                    mintermsCount++;
                }
                _mintermsCount = mintermsCount;
                _delta = new DfaMatchingState<TElement>[InitialStateLimit << _mintermsCount];
            }

            // initialized to False but updated later to the actual condition ony if \b or \B occurs anywhere in the regex
            // this implies that if a regex never uses \b or \B then the character context will never
            // update the previous character context to distinguish word and nonword letters
            _wordLetterPredicateForAnchors = solver.False;

            // initialized to False but updated later to the actual condition of \n ony if a line anchor occurs anywhere in the regex
            // this implies that if a regex never uses a line anchor then the character context will never
            // update the previous character context to mark that the previous caharcter was \n
            _newLinePredicate = solver.False;
            _nothing = SymbolicRegexNode<TElement>.MkFalse(this);
            _anyChar = SymbolicRegexNode<TElement>.MkTrue(this);
            _anyStar = SymbolicRegexNode<TElement>.MkStar(this, _anyChar);

            // --- initialize singletonCache ---
            _singletonCache[_solver.False] = _nothing;
            _singletonCache[_solver.True] = _anyChar;
        }

        /// <summary>
        /// Make a disjunction of given regexes, simplify by eliminating any regex that accepts no inputs
        /// </summary>
        internal SymbolicRegexNode<TElement> MkOr(params SymbolicRegexNode<TElement>[] regexes) =>
            SymbolicRegexNode<TElement>.MkOr(this, regexes);

        /// <summary>
        /// Make a conjunction of given regexes, simplify by eliminating regexes that accept everything
        /// </summary>
        internal SymbolicRegexNode<TElement> MkAnd(params SymbolicRegexNode<TElement>[] regexes) =>
            SymbolicRegexNode<TElement>.MkAnd(this, regexes);

        /// <summary>
        /// Make a disjunction of given regexes, simplify by eliminating any regex that accepts no inputs
        /// </summary>
        internal SymbolicRegexNode<TElement> MkOr(SymbolicRegexSet<TElement> alts) =>
            alts.IsNothing ? _nothing :
            alts.IsEverything ? _anyStar :
            alts.IsSingleton ? alts.GetSingletonElement() :
            SymbolicRegexNode<TElement>.MkOr(this, alts);

        internal SymbolicRegexNode<TElement> MkOr2(SymbolicRegexNode<TElement> x, SymbolicRegexNode<TElement> y) =>
            x == _anyStar || y == _anyStar ? _anyStar :
            x == _nothing ? y :
            y == _nothing ? x :
            SymbolicRegexNode<TElement>.MkOr(this, x, y);

        /// <summary>
        /// Make a conjunction of given regexes, simplify by eliminating any regex that accepts all inputs,
        /// returns the empty regex if the regex accepts nothing
        /// </summary>
        internal SymbolicRegexNode<TElement> MkAnd(SymbolicRegexSet<TElement> alts) =>
            alts.IsNothing ? _nothing :
            alts.IsEverything ? _anyStar :
            alts.IsSingleton ? alts.GetSingletonElement() :
            SymbolicRegexNode<TElement>.MkAnd(this, alts);

        /// <summary>
        /// Make a concatenation of given regexes, if any regex is nothing then return nothing, eliminate
        /// intermediate epsilons, if toplevel and length is fixed, add watchdog at the end
        /// </summary>
        internal SymbolicRegexNode<TElement> MkConcat(SymbolicRegexNode<TElement>[] regexes, bool topLevel)
        {
            if (regexes.Length == 0)
                return _epsilon;

            SymbolicRegexNode<TElement> sr = _epsilon;
            int length = CalculateFixedLength(regexes);
            if (topLevel && length >= 0)
                sr = MkWatchDog(length);

            //exclude epsilons from the concatenation
            for (int i = regexes.Length - 1; i >= 0; i--)
            {
                if (regexes[i] == _nothing)
                    return _nothing;

                sr = SymbolicRegexNode<TElement>.MkConcat(this, regexes[i], sr);
            }

            return sr;
        }

        internal SymbolicRegexNode<TElement> MkConcat(SymbolicRegexNode<TElement> left, SymbolicRegexNode<TElement> right) => SymbolicRegexNode<TElement>.MkConcat(this, left, right);

        private int CalculateFixedLength(SymbolicRegexNode<TElement>[] regexes)
        {
            int length = 0;
            for (int i = 0; i < regexes.Length; i++)
            {
                int k = regexes[i].GetFixedLength();
                if (k < 0)
                {
                    return -1;
                }

                length += k;
            }

            return length;
        }


        /// <summary>
        /// Make loop regex
        /// </summary>
        internal SymbolicRegexNode<TElement> MkLoop(SymbolicRegexNode<TElement> regex, bool isLazy, int lower = 0, int upper = int.MaxValue)
        {
            if (lower == 1 && upper == 1)
            {
                return regex;
            }

            if (lower == 0 && upper == 0)
            {
                return isLazy ? _epsilon : _eagerEmptyLoop;
            }

            if (!isLazy && lower == 0 && upper == int.MaxValue && regex._kind == SymbolicRegexKind.Singleton)
            {
                Debug.Assert(regex._set is not null);
                if (_solver.AreEquivalent(_solver.True, regex._set))
                {
                    return _anyStar;
                }
            }

            return SymbolicRegexNode<TElement>.MkLoop(this, regex, lower, upper, isLazy);
        }

        /// <summary>
        /// Make a singleton sequence regex
        /// </summary>
        internal SymbolicRegexNode<TElement> MkSingleton(TElement set)
        {
            if (!_singletonCache.TryGetValue(set, out SymbolicRegexNode<TElement>? res))
            {
                _singletonCache[set] = res = SymbolicRegexNode<TElement>.MkSingleton(this, set);
            }

            return res;
        }

        /// <summary>
        /// Make end of sequence marker
        /// </summary>
        internal SymbolicRegexNode<TElement> MkWatchDog(int length) => SymbolicRegexNode<TElement>.MkWatchDog(this, length);

        /// <summary>
        /// Make a sequence regex, i.e., a concatenation of singletons, with a watchdog at the end
        /// </summary>
        internal SymbolicRegexNode<TElement> MkSequence(TElement[] seq, bool topLevel)
        {
            int k = seq.Length;
            if (k == 0)
            {
                return _epsilon;
            }
            else if (k == 1)
            {
                return topLevel ?
                    SymbolicRegexNode<TElement>.MkConcat(this, MkSingleton(seq[0]), MkWatchDog(1)) :
                    MkSingleton(seq[0]);
            }
            else
            {
                var singletons = new SymbolicRegexNode<TElement>[seq.Length];
                for (int i =0; i < singletons.Length; i++)
                {
                    singletons[i] = MkSingleton(seq[i]);
                }
                return MkConcat(singletons, topLevel);
            }
        }

        /// <summary>
        /// Make a complemented node
        /// </summary>
        /// <param name="node">node to be complemented</param>
        /// <returns></returns>
        internal SymbolicRegexNode<TElement> MkNot(SymbolicRegexNode<TElement> node) => SymbolicRegexNode<TElement>.MkNot(this, node);

        internal SymbolicRegexNode<T> Transform<T>(SymbolicRegexNode<TElement> sr, SymbolicRegexBuilder<T> builderT, Func<TElement, T> predicateTransformer) where T : notnull
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(Transform, sr, builderT, predicateTransformer);
            }

            switch (sr._kind)
            {
                case SymbolicRegexKind.StartAnchor:
                    return builderT._startAnchor;

                case SymbolicRegexKind.EndAnchor:
                    return builderT._endAnchor;

                case SymbolicRegexKind.EndAnchorZ:
                    return builderT._endAnchorZ;

                case SymbolicRegexKind.EndAnchorZRev:
                    return builderT._endAnchorZRev;

                case SymbolicRegexKind.BOLAnchor:
                    return builderT._bolAnchor;

                case SymbolicRegexKind.EOLAnchor:
                    return builderT._eolAnchor;

                case SymbolicRegexKind.WBAnchor:
                    return builderT._wbAnchor;

                case SymbolicRegexKind.NWBAnchor:
                    return builderT._nwbAnchor;

                case SymbolicRegexKind.WatchDog:
                    return builderT.MkWatchDog(sr._lower);

                case SymbolicRegexKind.Epsilon:
                    return builderT._epsilon;

                case SymbolicRegexKind.Singleton:
                    Debug.Assert(sr._set is not null);
                    return builderT.MkSingleton(predicateTransformer(sr._set));

                case SymbolicRegexKind.Loop:
                    Debug.Assert(sr._left is not null);
                    return builderT.MkLoop(Transform(sr._left, builderT, predicateTransformer), sr.IsLazy, sr._lower, sr._upper);

                case SymbolicRegexKind.Or:
                    Debug.Assert(sr._alts is not null);
                    return builderT.MkOr(sr._alts.Transform(builderT, predicateTransformer));

                case SymbolicRegexKind.And:
                    Debug.Assert(sr._alts is not null);
                    return builderT.MkAnd(sr._alts.Transform(builderT, predicateTransformer));

                case SymbolicRegexKind.Concat:
                    {
                        List<SymbolicRegexNode<TElement>> sr_elems = sr.ToList();
                        SymbolicRegexNode<T>[] sr_elems_trasformed = new SymbolicRegexNode<T>[sr_elems.Count];
                        for (int i = 0; i < sr_elems.Count; i++)
                        {
                            sr_elems_trasformed[i] = Transform(sr_elems[i], builderT, predicateTransformer);
                        }
                        return builderT.MkConcat(sr_elems_trasformed, false);
                    }

                default:
                    Debug.Assert(sr._kind == SymbolicRegexKind.Not);
                    Debug.Assert(sr._left is not null);
                    return builderT.MkNot(Transform(sr._left, builderT, predicateTransformer));
            }
        }

        /// <summary>
        /// Make a state with given node and previous character context
        /// </summary>
        public DfaMatchingState<TElement> MkState(SymbolicRegexNode<TElement> node, uint prevCharKind, bool antimirov = false)
        {
            //first prune the anchors in the node
            TElement WLpred = _wordLetterPredicateForAnchors;
            TElement startSet = node.GetStartSet();

            //true if the startset of the node overlaps with some wordletter or the node can be nullable
            bool contWithWL = node.CanBeNullable || _solver.IsSatisfiable(_solver.And(WLpred, startSet));

            //true if the startset of the node overlaps with some nonwordletter or the node can be nullable
            bool contWithNWL = node.CanBeNullable || _solver.IsSatisfiable(_solver.And(_solver.Not(WLpred), startSet));
            SymbolicRegexNode<TElement> pruned_node = node.PruneAnchors(prevCharKind, contWithWL, contWithNWL);
            var s = new DfaMatchingState<TElement>(pruned_node, prevCharKind);
            if (!_stateCache.TryGetValue(s, out DfaMatchingState<TElement>? state))
            {
                // do not cache set of states as states in antimirov mode
                if (antimirov && pruned_node.Kind == SymbolicRegexKind.Or)
                {
                    s.Id = -1; // mark the Id as invalid
                    state = s;
                }
                else
                {
                    state = MakeNewState(s);
                }
            }

            return state;
        }

        private DfaMatchingState<TElement> MakeNewState(DfaMatchingState<TElement> state)
        {
            lock (this)
            {
                state.Id = _stateCache.Count;
                _stateCache.Add(state);

                Debug.Assert(_statearray is not null);

                if (state.Id == _statearray.Length)
                {
                    int newsize = _statearray.Length + 1024;
                    Array.Resize(ref _statearray, newsize);
                    Array.Resize(ref _delta, newsize << _mintermsCount);
                }
                _statearray[state.Id] = state;
                return state;
            }
        }
    }
}
