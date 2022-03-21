// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        internal readonly SymbolicRegexNode<TElement> _nothing;
        internal readonly SymbolicRegexNode<TElement> _anyChar;
        internal readonly SymbolicRegexNode<TElement> _anyStar;

        private SymbolicRegexNode<TElement>? _epsilon;
        internal SymbolicRegexNode<TElement> Epsilon => _epsilon ??= SymbolicRegexNode<TElement>.CreateEpsilon(this);

        private SymbolicRegexNode<TElement>? _beginningAnchor;
        internal SymbolicRegexNode<TElement> BeginningAnchor => _beginningAnchor ??= SymbolicRegexNode<TElement>.CreateBeginEndAnchor(this, SymbolicRegexNodeKind.BeginningAnchor);

        private SymbolicRegexNode<TElement>? _endAnchor;
        internal SymbolicRegexNode<TElement> EndAnchor => _endAnchor ??= SymbolicRegexNode<TElement>.CreateBeginEndAnchor(this, SymbolicRegexNodeKind.EndAnchor);

        private SymbolicRegexNode<TElement>? _endAnchorZ;
        internal SymbolicRegexNode<TElement> EndAnchorZ => _endAnchorZ ??= SymbolicRegexNode<TElement>.CreateBeginEndAnchor(this, SymbolicRegexNodeKind.EndAnchorZ);

        private SymbolicRegexNode<TElement>? _endAnchorZReverse;
        internal SymbolicRegexNode<TElement> EndAnchorZReverse => _endAnchorZReverse ??= SymbolicRegexNode<TElement>.CreateBeginEndAnchor(this, SymbolicRegexNodeKind.EndAnchorZReverse);

        private SymbolicRegexNode<TElement>? _bolAnchor;
        internal SymbolicRegexNode<TElement> BolAnchor => _bolAnchor ??= SymbolicRegexNode<TElement>.CreateBeginEndAnchor(this, SymbolicRegexNodeKind.BOLAnchor);

        private SymbolicRegexNode<TElement>? _eolAnchor;
        internal SymbolicRegexNode<TElement> EolAnchor => _eolAnchor ??= SymbolicRegexNode<TElement>.CreateBeginEndAnchor(this, SymbolicRegexNodeKind.EOLAnchor);

        private SymbolicRegexNode<TElement>? _wbAnchor;
        internal SymbolicRegexNode<TElement> BoundaryAnchor => _wbAnchor ??= SymbolicRegexNode<TElement>.CreateBoundaryAnchor(this, SymbolicRegexNodeKind.BoundaryAnchor);

        private SymbolicRegexNode<TElement>? _nwbAnchor;
        internal SymbolicRegexNode<TElement> NonBoundaryAnchor => _nwbAnchor ??= SymbolicRegexNode<TElement>.CreateBoundaryAnchor(this, SymbolicRegexNodeKind.NonBoundaryAnchor);

        private SymbolicRegexSet<TElement>? _fullSet;
        internal SymbolicRegexSet<TElement> FullSet => _fullSet ??= SymbolicRegexSet<TElement>.CreateFull(this);

        private SymbolicRegexSet<TElement>? _emptySet;
        internal SymbolicRegexSet<TElement> EmptySet => _emptySet ??= SymbolicRegexSet<TElement>.CreateEmpty(this);

        private SymbolicRegexNode<TElement>? _eagerEmptyLoop;
        internal SymbolicRegexNode<TElement> EagerEmptyLoop => _eagerEmptyLoop ??= SymbolicRegexNode<TElement>.CreateEagerEmptyLoop(this, Epsilon);

        internal TElement _wordLetterPredicateForAnchors;
        internal TElement _newLinePredicate;

        /// <summary>Partition of the input space of predicates.</summary>
        internal TElement[]? _minterms;

        private readonly Dictionary<TElement, SymbolicRegexNode<TElement>> _singletonCache = new();

        // states that have been created
        internal HashSet<DfaMatchingState<TElement>> _stateCache = new();

        // capturing states that have been created
        internal HashSet<DfaMatchingState<TElement>> _capturingStateCache = new();

        internal readonly Dictionary<(SymbolicRegexNodeKind,
            SymbolicRegexNode<TElement>?, // _left
            SymbolicRegexNode<TElement>?, // _right
            int, int, TElement?,          // _lower, _upper, _set
            SymbolicRegexSet<TElement>?,
            SymbolicRegexInfo), SymbolicRegexNode<TElement>> _nodeCache = new();

#if DEBUG
        internal readonly Dictionary<(TransitionRegexKind, // _kind
            TElement?,                                     // _test
            TransitionRegex<TElement>?,                    // _first
            TransitionRegex<TElement>?,                    // _second
            SymbolicRegexNode<TElement>?,                  // _leaf
            DerivativeEffect?),                            // _effect
            TransitionRegex<TElement>> _trCache = new();
#endif

        /// <summary>
        /// Maps state ids to states, initial capacity is 1024 states.
        /// Each time more states are needed the length is increased by 1024.
        /// </summary>
        internal DfaMatchingState<TElement>[]? _stateArray;
        internal DfaMatchingState<TElement>[]? _capturingStateArray;
        /// <remarks>
        /// For these "delta" arrays, technically Volatile.Read should be used to read out an element,
        /// but in practice that's not needed on the runtimes in use (though that needs to be documented
        /// via https://github.com/dotnet/runtime/issues/63474), and use of Volatile.Read is
        /// contributing non-trivial overhead (https://github.com/dotnet/runtime/issues/65789).
        /// </remarks>
        internal DfaMatchingState<TElement>?[]? _delta;
        internal List<(DfaMatchingState<TElement>, DerivativeEffect[])>?[]? _capturingDelta;
        private const int InitialStateLimit = 1024;

        /// <summary>1 + Log2(_minterms.Length), the smallest k s.t. 2^k >= minterms.Length + 1</summary>
        internal int _mintermsLog;

        /// <summary>
        /// Maps each NFA state id to the state id of the DfaMatchingState stored in _stateArray.
        /// This map is used to compactly represent NFA state ids in NFA mode in order to utilize
        /// the property that all NFA states are small integers in one interval.
        /// The valid entries are 0 to <see cref="NfaStateCount"/>-1.
        /// </summary>
        internal int[] _nfaStateArray = Array.Empty<int>();

        /// <summary>
        /// Maps the id of a DfaMatchingState to the NFA state id that it is being identifed with in the NFA.
        /// It is the inverse of used entries in _nfaStateArray.
        /// The range of this map is 0 to <see cref="NfaStateCount"/>-1.
        /// </summary>
        internal readonly Dictionary<int, int> _nfaStateArrayInverse = new();

        /// <summary>Gets <see cref="_nfaStateArrayInverse"/>.Count</summary>
        internal int NfaStateCount => _nfaStateArrayInverse.Count;

        /// <summary>
        /// Transition function for NFA transitions in NFA mode.
        /// Each NFA entry maps to a list of NFA target states.
        /// Each list of target states is without repetitions.
        /// If the entry is null then the targets states have not been computed yet.
        /// </summary>
        internal int[]?[] _nfaDelta = Array.Empty<int[]>();

        /// <summary>Create a new symbolic regex builder.</summary>
        internal SymbolicRegexBuilder(ICharAlgebra<TElement> solver)
        {
            // Solver must be set first, else it will cause null reference exception in the following
            _solver = solver;

            // minterms = null if partition of the solver is undefined and returned as null
            _minterms = solver.GetMinterms();
            if (_minterms == null)
            {
                _mintermsLog = -1;
            }
            else
            {
                _stateArray = new DfaMatchingState<TElement>[InitialStateLimit];
                _capturingStateArray = new DfaMatchingState<TElement>[InitialStateLimit];

                // the extra +1 slot with id minterms.Length is reserved for \Z (last occurrence of \n)
                _mintermsLog = BitOperations.Log2((uint)_minterms.Length) + 1;
                _delta = new DfaMatchingState<TElement>[InitialStateLimit << _mintermsLog];
                _capturingDelta = new List<(DfaMatchingState<TElement>, DerivativeEffect[])>[InitialStateLimit << _mintermsLog];
            }

            // initialized to False but updated later to the actual condition ony if \b or \B occurs anywhere in the regex
            // this implies that if a regex never uses \b or \B then the character context will never
            // update the previous character context to distinguish word and nonword letters
            _wordLetterPredicateForAnchors = solver.False;

            // initialized to False but updated later to the actual condition of \n only if a line anchor occurs anywhere in the regex
            // this implies that if a regex never uses a line anchor then the character context will never
            // update the previous character context to mark that the previous caharcter was \n
            _newLinePredicate = solver.False;
            _nothing = SymbolicRegexNode<TElement>.CreateFalse(this);
            _anyChar = SymbolicRegexNode<TElement>.CreateTrue(this);
            _anyStar = SymbolicRegexNode<TElement>.CreateLoop(this, _anyChar, 0, int.MaxValue, isLazy: false);

            // --- initialize singletonCache ---
            _singletonCache[_solver.False] = _nothing;
            _singletonCache[_solver.True] = _anyChar;
        }

        /// <summary>Lookup the actual minterm based on its ID.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TElement GetMinterm(int mintermId)
        {
            TElement[]? minterms = _minterms;
            Debug.Assert(minterms is not null);
            return (uint)mintermId < (uint)minterms.Length ?
                minterms[mintermId] :
                _solver.False; // minterm=False represents \Z
        }

        /// <summary>
        /// Make a disjunction of given nodes, simplify by eliminating any regex that accepts no inputs
        /// </summary>
        internal SymbolicRegexNode<TElement> Or(params SymbolicRegexNode<TElement>[] nodes) =>
            SymbolicRegexNode<TElement>.Or(this, nodes);

        /// <summary>
        /// Make an ordered disjunction of given nodes, simplify by eliminating any regex that accepts no inputs
        /// </summary>
        internal SymbolicRegexNode<TElement> OrderedOr(params SymbolicRegexNode<TElement>[] nodes)
        {
            SymbolicRegexNode<TElement>? or = null;
            // Iterate backwards to avoid quadratic rebuilding of the Or nodes, which are always simplified to
            // right associative form. Concretely:
            // In (a|(b|c)) | d -> (a|(b|(c|d)) the first argument is not a subtree of the result.
            // In a | (b|(c|d)) -> (a|(b|(c|d)) the second argument is a subtree of the result.
            // The first case performs linear work for each element, leading to a quadratic blowup.
            for (int i = nodes.Length - 1; i >= 0; --i)
            {
                SymbolicRegexNode<TElement> elem = nodes[i];

                if (elem.IsNothing)
                    continue;

                or = or is null ? elem : SymbolicRegexNode<TElement>.OrderedOr(this, elem, or);

                if (elem.IsAnyStar)
                    or = elem; // .* is the absorbing element
            }

            return or ?? _nothing;
        }

        /// <summary>
        /// Make a conjunction of given nodes, simplify by eliminating nodes that accept everything
        /// </summary>
        internal SymbolicRegexNode<TElement> And(params SymbolicRegexNode<TElement>[] nodes) =>
            SymbolicRegexNode<TElement>.And(this, nodes);

        /// <summary>
        /// Make a disjunction of given set of nodes, simplify by eliminating any regex that accepts no inputs
        /// </summary>
        internal SymbolicRegexNode<TElement> Or(SymbolicRegexSet<TElement> set) =>
            set.IsNothing ? _nothing :
            set.IsEverything ? _anyStar :
            set.IsSingleton ? set.GetSingletonElement() :
            SymbolicRegexNode<TElement>.Or(this, set);

        internal SymbolicRegexNode<TElement> Or(SymbolicRegexNode<TElement> x, SymbolicRegexNode<TElement> y) =>
            x == _anyStar || y == _anyStar ? _anyStar :
            x == _nothing ? y :
            y == _nothing ? x :
            SymbolicRegexNode<TElement>.Or(this, x, y);

        /// <summary>
        /// Make a conjunction of given set, simplify by eliminating any regex that accepts all inputs,
        /// returns the empty regex if the regex accepts nothing
        /// </summary>
        internal SymbolicRegexNode<TElement> And(SymbolicRegexSet<TElement> set) =>
            set.IsNothing ? _nothing :
            set.IsEverything ? _anyStar :
            set.IsSingleton ? set.GetSingletonElement() :
            SymbolicRegexNode<TElement>.And(this, set);

        /// <summary>
        /// Make a concatenation of given nodes, if any regex is nothing then return nothing, eliminate
        /// intermediate epsilons, if tryCreateFixedLengthMarker and length is fixed, add a fixed length
        /// marker at the end.
        /// </summary>
        internal SymbolicRegexNode<TElement> CreateConcat(SymbolicRegexNode<TElement>[] nodes, bool tryCreateFixedLengthMarker)
        {
            SymbolicRegexNode<TElement> sr = Epsilon;

            if (nodes.Length != 0)
            {
                if (tryCreateFixedLengthMarker)
                {
                    int length = CalculateFixedLength(nodes);
                    if (length >= 0)
                    {
                        sr = CreateFixedLengthMarker(length);
                    }
                }

                // Iterate through all the nodes concatenating them together.  We iterate in reverse in order to
                // avoid quadratic behavior in combination with the called CreateConcat method.
                for (int i = nodes.Length - 1; i >= 0; i--)
                {
                    // If there's a nothing in the list, the whole concatenation can't match, so just return nothing.
                    if (nodes[i] == _nothing)
                    {
                        return _nothing;
                    }

                    sr = SymbolicRegexNode<TElement>.CreateConcat(this, nodes[i], sr);
                }
            }

            return sr;
        }

        internal SymbolicRegexNode<TElement> CreateConcat(SymbolicRegexNode<TElement> left, SymbolicRegexNode<TElement> right) => SymbolicRegexNode<TElement>.CreateConcat(this, left, right);

        private int CalculateFixedLength(SymbolicRegexNode<TElement>[] nodes)
        {
            int length = 0;
            foreach (SymbolicRegexNode<TElement> node in nodes)
            {
                int k = node.GetFixedLength();
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
        internal SymbolicRegexNode<TElement> CreateLoop(SymbolicRegexNode<TElement> node, bool isLazy, int lower = 0, int upper = int.MaxValue)
        {
            // If the lower and upper bound are both 1, then the node would be processed once and only once, so we can just return that node.
            if (lower == 1 && upper == 1)
            {
                return node;
            }

            // If the lower and upper bound are both 0, this is actually empty.
            if (lower == 0 && upper == 0)
            {
                return Epsilon;
            }

            // If this is equivalent to any*, return that.
            if (!isLazy && lower == 0 && upper == int.MaxValue && node._kind == SymbolicRegexNodeKind.Singleton)
            {
                Debug.Assert(node._set is not null);
                if (_solver.AreEquivalent(_solver.True, node._set))
                {
                    return _anyStar;
                }
            }

            // Otherwise, create the loop.
            return SymbolicRegexNode<TElement>.CreateLoop(this, node, lower, upper, isLazy);
        }

        /// <summary>Creates a "singleton", which matches a single character.</summary>
        internal SymbolicRegexNode<TElement> CreateSingleton(TElement set)
        {
            // We maintain a cache of singletons, under the assumption that it's likely the same one/notone/set appears
            // multiple times in the same pattern.  First consult the cache, and then create a new singleton if one didn't exist.
            ref SymbolicRegexNode<TElement>? result = ref CollectionsMarshal.GetValueRefOrAddDefault(_singletonCache, set, out _);
            return result ??= SymbolicRegexNode<TElement>.CreateSingleton(this, set);
        }

        /// <summary>Creates a fixed length marker for the end of a sequence.</summary>
        internal SymbolicRegexNode<TElement> CreateFixedLengthMarker(int length) => SymbolicRegexNode<TElement>.CreateFixedLengthMarker(this, length);

        /// <summary>
        /// Make a complemented node
        /// </summary>
        /// <param name="node">node to be complemented</param>
        /// <returns></returns>
        internal SymbolicRegexNode<TElement> Not(SymbolicRegexNode<TElement> node) => SymbolicRegexNode<TElement>.Not(this, node);

        internal SymbolicRegexNode<TElement> CreateCapture(SymbolicRegexNode<TElement> child, int captureNum) => CreateConcat(CreateCaptureStart(captureNum), CreateConcat(child, CreateCaptureEnd(captureNum)));

        internal SymbolicRegexNode<TElement> CreateCaptureStart(int captureNum) => SymbolicRegexNode<TElement>.CreateCaptureStart(this, captureNum);

        internal SymbolicRegexNode<TElement> CreateCaptureEnd(int captureNum) => SymbolicRegexNode<TElement>.CreateCaptureEnd(this, captureNum);

        internal SymbolicRegexNode<TElement> CreateDisableBacktrackingSimulation(SymbolicRegexNode<TElement> child)
        {
            if (child == _nothing)
                return _nothing;
            return SymbolicRegexNode<TElement>.CreateDisableBacktrackingSimulation(this, child);
        }

        internal SymbolicRegexNode<T> Transform<T>(SymbolicRegexNode<TElement> sr, SymbolicRegexBuilder<T> builder, Func<SymbolicRegexBuilder<T>, TElement, T> predicateTransformer) where T : notnull
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(Transform, sr, builder, predicateTransformer);
            }

            switch (sr._kind)
            {
                case SymbolicRegexNodeKind.BeginningAnchor:
                    return builder.BeginningAnchor;

                case SymbolicRegexNodeKind.EndAnchor:
                    return builder.EndAnchor;

                case SymbolicRegexNodeKind.EndAnchorZ:
                    return builder.EndAnchorZ;

                case SymbolicRegexNodeKind.EndAnchorZReverse:
                    return builder.EndAnchorZReverse;

                case SymbolicRegexNodeKind.BOLAnchor:
                    return builder.BolAnchor;

                case SymbolicRegexNodeKind.EOLAnchor:
                    return builder.EolAnchor;

                case SymbolicRegexNodeKind.BoundaryAnchor:
                    return builder.BoundaryAnchor;

                case SymbolicRegexNodeKind.NonBoundaryAnchor:
                    return builder.NonBoundaryAnchor;

                case SymbolicRegexNodeKind.FixedLengthMarker:
                    return builder.CreateFixedLengthMarker(sr._lower);

                case SymbolicRegexNodeKind.Epsilon:
                    return builder.Epsilon;

                case SymbolicRegexNodeKind.Singleton:
                    Debug.Assert(sr._set is not null);
                    return builder.CreateSingleton(predicateTransformer(builder, sr._set));

                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(sr._left is not null);
                    return builder.CreateLoop(Transform(sr._left, builder, predicateTransformer), sr.IsLazy, sr._lower, sr._upper);

                case SymbolicRegexNodeKind.Or:
                    Debug.Assert(sr._alts is not null);
                    return builder.Or(sr._alts.Transform(builder, predicateTransformer));

                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(sr._left is not null && sr._right is not null);
                    return builder.OrderedOr(Transform(sr._left, builder, predicateTransformer), Transform(sr._right, builder, predicateTransformer));

                case SymbolicRegexNodeKind.And:
                    Debug.Assert(sr._alts is not null);
                    return builder.And(sr._alts.Transform(builder, predicateTransformer));

                case SymbolicRegexNodeKind.CaptureStart:
                    return builder.CreateCaptureStart(sr._lower);

                case SymbolicRegexNodeKind.CaptureEnd:
                    return builder.CreateCaptureEnd(sr._lower);

                case SymbolicRegexNodeKind.Concat:
                    {
                        List<SymbolicRegexNode<TElement>> sr_elems = sr.ToList();
                        SymbolicRegexNode<T>[] sr_elems_trasformed = new SymbolicRegexNode<T>[sr_elems.Count];
                        for (int i = 0; i < sr_elems.Count; i++)
                        {
                            sr_elems_trasformed[i] = Transform(sr_elems[i], builder, predicateTransformer);
                        }
                        return builder.CreateConcat(sr_elems_trasformed, false);
                    }

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(sr._left is not null);
                    return builder.CreateDisableBacktrackingSimulation(Transform(sr._left, builder, predicateTransformer));

                default:
                    Debug.Assert(sr._kind == SymbolicRegexNodeKind.Not);
                    Debug.Assert(sr._left is not null);
                    return builder.Not(Transform(sr._left, builder, predicateTransformer));
            }
        }

        /// <summary>
        /// Create a state with given node and previous character context.
        /// </summary>
        /// <param name="node">the pattern that this state will represent</param>
        /// <param name="prevCharKind">the kind of the character that led to this state</param>
        /// <param name="disableCaching">if true, then state won't be cached</param>
        /// <param name="capturing">whether to use the separate space of states with capturing transitions or not</param>
        /// <returns></returns>
        public DfaMatchingState<TElement> CreateState(SymbolicRegexNode<TElement> node, uint prevCharKind, bool disableCaching = false, bool capturing = false)
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
            if (!(capturing ? _capturingStateCache : _stateCache).TryGetValue(s, out DfaMatchingState<TElement>? state))
            {
                // do not cache set of states as states in NFA mode
                if (disableCaching && pruned_node.Kind == SymbolicRegexNodeKind.Or)
                {
                    s.Id = -1; // mark the Id as invalid
                    state = s;
                }
                else
                {
                    state = MakeNewState(s, capturing);
                }
            }

            return state;
        }

        private DfaMatchingState<TElement> MakeNewState(DfaMatchingState<TElement> state, bool capturing)
        {
            lock (this)
            {
                HashSet<DfaMatchingState<TElement>> cache = capturing ? _capturingStateCache : _stateCache;
                state.Id = cache.Count;
                cache.Add(state);

                Debug.Assert(_stateArray is not null && _capturingStateArray is not null);

                const int GrowthSize = 1024;
                if (capturing)
                {
                    if (state.Id == _capturingStateArray.Length)
                    {
                        int newsize = _capturingStateArray.Length + GrowthSize;
                        Array.Resize(ref _capturingStateArray, newsize);
                        Array.Resize(ref _capturingDelta, newsize << _mintermsLog);
                    }
                    _capturingStateArray[state.Id] = state;
                }
                else
                {
                    if (state.Id == _stateArray.Length)
                    {
                        int newsize = _stateArray.Length + GrowthSize;
                        Array.Resize(ref _stateArray, newsize);
                        Array.Resize(ref _delta, newsize << _mintermsLog);
                    }
                    _stateArray[state.Id] = state;
                }
                return state;
            }
        }

        /// <summary>
        /// Make an NFA state for the given node and previous character kind.
        /// </summary>
        public int CreateNfaState(SymbolicRegexNode<TElement> node, uint prevCharKind)
        {
            // TBD: OrderedOr
            Debug.Assert(node.Kind != SymbolicRegexNodeKind.Or);

            // First make the underlying core state
            DfaMatchingState<TElement> coreState = CreateState(node, prevCharKind);

            if (!_nfaStateArrayInverse.TryGetValue(coreState.Id, out int nfaStateId))
            {
                nfaStateId = MakeNewNfaState(coreState.Id);
            }

            return nfaStateId;
        }

        /// <summary>Critical region that creates a new NFA state for the underlying core state</summary>
        private int MakeNewNfaState(int coreStateId)
        {
            lock (this)
            {
                if (NfaStateCount == _nfaStateArray.Length)
                {
                    // TBD: is 1024 reasonable?
                    int newsize = _nfaStateArray.Length + 1024;
                    Array.Resize(ref _nfaStateArray, newsize);
                    Array.Resize(ref _nfaDelta, newsize << _mintermsLog);
                    // TBD: capturing
                }

                int nfaStateId = NfaStateCount;
                _nfaStateArray[nfaStateId] = coreStateId;
                _nfaStateArrayInverse[coreStateId] = nfaStateId;
                return nfaStateId;
            }
        }

        /// <summary>Gets the core state corresponding to the NFA state</summary>
        public DfaMatchingState<TElement> GetCoreState(int nfaStateId)
        {
            Debug.Assert(_stateArray is not null);
            Debug.Assert(nfaStateId < _nfaStateArray.Length);
            Debug.Assert(_nfaStateArray[nfaStateId] < _stateArray.Length);
            return _stateArray[_nfaStateArray[nfaStateId]];
        }

        /// <summary>Critical region for defining a new core transition</summary>
        public DfaMatchingState<TElement> CreateNewTransition(DfaMatchingState<TElement> sourceState, int mintermId, int offset)
        {
            TryCreateNewTransition(sourceState, mintermId, offset, checkThreshold: false, out DfaMatchingState<TElement>? nextState);
            Debug.Assert(nextState is not null);
            return nextState;
        }

        /// <summary>Gets or creates a new DFA transition.</summary>
        public bool TryCreateNewTransition(
            DfaMatchingState<TElement> sourceState, int mintermId, int offset, bool checkThreshold, [NotNullWhen(true)] out DfaMatchingState<TElement>? nextState)
        {
            Debug.Assert(_delta is not null);
            lock (this)
            {
                Debug.Assert(offset < _delta.Length);

                // check if meanwhile delta[offset] has become defined possibly by another thread
                DfaMatchingState<TElement>? targetState = _delta[offset];
                if (targetState is null)
                {
                    if (checkThreshold && _stateCache.Count >= SymbolicRegexMatcher<TElement>.NfaThreshold)
                    {
                        nextState = null;
                        return false;
                    }

                    targetState = sourceState.Next(GetMinterm(mintermId));
                    Volatile.Write(ref _delta[offset], targetState);
                }

                nextState = targetState;
                return true;
            }
        }

        /// <summary>Gets or creates a new NFA transition.</summary>
        public int[] CreateNewNfaTransition(int nfaStateId, int mintermId, int nfaOffset)
        {
            Debug.Assert(_delta is not null);
            lock (this)
            {
                Debug.Assert(nfaOffset < _nfaDelta.Length);

                // check if meanwhile the nfaoffset has become defined possibly by another thread
                int[]? targets = _nfaDelta[nfaOffset];
                if (targets is null)
                {
                    // Create the underlying transition from the core state corresponding to the nfa state
                    DfaMatchingState<TElement> coreState = GetCoreState(nfaStateId);
                    int coreOffset = (coreState.Id << _mintermsLog) | mintermId;
                    DfaMatchingState<TElement>? coreTarget = _delta[coreOffset] ?? CreateNewTransition(coreState, mintermId, coreOffset);

                    // TBD: OrderedOr
                    if (coreTarget.Node.Kind == SymbolicRegexNodeKind.Or)
                    {
                        // Create separate NFA states for all members of a disjunction
                        // Here duplicate NFA states cannot arise because there are no duplicate nodes in the disjunction
                        SymbolicRegexSet<TElement>? alts = coreTarget.Node._alts;
                        Debug.Assert(alts is not null);

                        targets = new int[alts.Count];
                        int targetIndex = 0;
                        foreach (SymbolicRegexNode<TElement> q in alts)
                        {
                            Debug.Assert(!q.IsNothing);
                            targets[targetIndex++] = CreateNfaState(q, coreTarget.PrevCharKind);
                        }
                        Debug.Assert(targetIndex == targets.Length);
                    }
                    else if (coreTarget.IsDeadend)
                    {
                        // Omit deadend states from the target list of states
                        // target list being empty means that the NFA state itself is a deadend
                        targets = Array.Empty<int>();
                    }
                    else
                    {
                        // Add the single NFA target state correponding to the core target state
                        if (!_nfaStateArrayInverse.TryGetValue(coreTarget.Id, out int nfaTargetId))
                        {
                            nfaTargetId = MakeNewNfaState(coreTarget.Id);
                        }

                        targets = new[] { nfaTargetId };
                    }

                    Volatile.Write(ref _nfaDelta[nfaOffset], targets);
                }

                return targets;
            }
        }
    }
}
