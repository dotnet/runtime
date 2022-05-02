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
    /// Builder of symbolic regexes over TSet.
    /// TSet is the type of the set of elements.
    /// Used to convert .NET regexes to symbolic regexes.
    /// </summary>
    internal sealed class SymbolicRegexBuilder<TSet> where TSet : IComparable<TSet>, IEquatable<TSet>
    {
        internal readonly CharSetSolver _charSetSolver;
        internal readonly ISolver<TSet> _solver;

        internal readonly SymbolicRegexNode<TSet> _nothing;
        internal readonly SymbolicRegexNode<TSet> _anyChar;
        internal readonly SymbolicRegexNode<TSet> _anyStar;
        internal readonly SymbolicRegexNode<TSet> _anyStarLazy;

        private SymbolicRegexNode<TSet>? _epsilon;
        internal SymbolicRegexNode<TSet> Epsilon => _epsilon ??= SymbolicRegexNode<TSet>.CreateEpsilon(this);

        private SymbolicRegexNode<TSet>? _beginningAnchor;
        internal SymbolicRegexNode<TSet> BeginningAnchor => _beginningAnchor ??= SymbolicRegexNode<TSet>.CreateBeginEndAnchor(this, SymbolicRegexNodeKind.BeginningAnchor);

        private SymbolicRegexNode<TSet>? _endAnchor;
        internal SymbolicRegexNode<TSet> EndAnchor => _endAnchor ??= SymbolicRegexNode<TSet>.CreateBeginEndAnchor(this, SymbolicRegexNodeKind.EndAnchor);

        private SymbolicRegexNode<TSet>? _endAnchorZ;
        internal SymbolicRegexNode<TSet> EndAnchorZ => _endAnchorZ ??= SymbolicRegexNode<TSet>.CreateBeginEndAnchor(this, SymbolicRegexNodeKind.EndAnchorZ);

        private SymbolicRegexNode<TSet>? _endAnchorZReverse;
        internal SymbolicRegexNode<TSet> EndAnchorZReverse => _endAnchorZReverse ??= SymbolicRegexNode<TSet>.CreateBeginEndAnchor(this, SymbolicRegexNodeKind.EndAnchorZReverse);

        private SymbolicRegexNode<TSet>? _bolAnchor;
        internal SymbolicRegexNode<TSet> BolAnchor => _bolAnchor ??= SymbolicRegexNode<TSet>.CreateBeginEndAnchor(this, SymbolicRegexNodeKind.BOLAnchor);

        private SymbolicRegexNode<TSet>? _eolAnchor;
        internal SymbolicRegexNode<TSet> EolAnchor => _eolAnchor ??= SymbolicRegexNode<TSet>.CreateBeginEndAnchor(this, SymbolicRegexNodeKind.EOLAnchor);

        private SymbolicRegexNode<TSet>? _wbAnchor;
        internal SymbolicRegexNode<TSet> BoundaryAnchor => _wbAnchor ??= SymbolicRegexNode<TSet>.CreateBoundaryAnchor(this, SymbolicRegexNodeKind.BoundaryAnchor);

        private SymbolicRegexNode<TSet>? _nwbAnchor;
        internal SymbolicRegexNode<TSet> NonBoundaryAnchor => _nwbAnchor ??= SymbolicRegexNode<TSet>.CreateBoundaryAnchor(this, SymbolicRegexNodeKind.NonBoundaryAnchor);

        private SymbolicRegexSet<TSet>? _fullSet;
        internal SymbolicRegexSet<TSet> FullSet => _fullSet ??= SymbolicRegexSet<TSet>.CreateFull(this);

        private SymbolicRegexSet<TSet>? _emptySet;
        internal SymbolicRegexSet<TSet> EmptySet => _emptySet ??= SymbolicRegexSet<TSet>.CreateEmpty(this);

        private SymbolicRegexNode<TSet>? _eagerEmptyLoop;
        internal SymbolicRegexNode<TSet> EagerEmptyLoop => _eagerEmptyLoop ??= SymbolicRegexNode<TSet>.CreateEagerEmptyLoop(this, Epsilon);

        internal TSet _wordLetterForBoundariesSet;
        internal TSet _newLineSet;

        /// <summary>Partition of the input space of sets.</summary>
        internal TSet[]? _minterms;

        private readonly Dictionary<TSet, SymbolicRegexNode<TSet>> _singletonCache = new();

        // states that have been created
        internal HashSet<DfaMatchingState<TSet>> _stateCache = new();

        // capturing states that have been created
        internal HashSet<DfaMatchingState<TSet>> _capturingStateCache = new();

        internal readonly Dictionary<(SymbolicRegexNodeKind,
            SymbolicRegexNode<TSet>?, // _left
            SymbolicRegexNode<TSet>?, // _right
            int, int, TSet?,          // _lower, _upper, _set
            SymbolicRegexSet<TSet>?,
            SymbolicRegexInfo), SymbolicRegexNode<TSet>> _nodeCache = new();

#if DEBUG
        internal readonly Dictionary<(TransitionRegexKind, // _kind
            TSet?,                                     // _test
            TransitionRegex<TSet>?,                    // _first
            TransitionRegex<TSet>?,                    // _second
            SymbolicRegexNode<TSet>?,                  // _leaf
            DerivativeEffect?),                            // _effect
            TransitionRegex<TSet>> _trCache = new();
#endif

        /// <summary>
        /// Maps state ids to states, initial capacity is 1024 states.
        /// Each time more states are needed the length is increased by 1024.
        /// </summary>
        internal DfaMatchingState<TSet>[]? _stateArray;
        internal DfaMatchingState<TSet>[]? _capturingStateArray;
        /// <remarks>
        /// For these "delta" arrays, technically Volatile.Read should be used to read out an element,
        /// but in practice that's not needed on the runtimes in use (though that needs to be documented
        /// via https://github.com/dotnet/runtime/issues/63474), and use of Volatile.Read is
        /// contributing non-trivial overhead (https://github.com/dotnet/runtime/issues/65789).
        /// </remarks>
        internal DfaMatchingState<TSet>?[]? _delta;
        internal List<(DfaMatchingState<TSet>, DerivativeEffect[])>?[]? _capturingDelta;
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
        internal SymbolicRegexBuilder(ISolver<TSet> solver, CharSetSolver charSetSolver)
        {
            // Solver must be set first, else it will cause null reference exception in the following
            _charSetSolver = charSetSolver;
            _solver = solver;

            // minterms = null if partition of the solver is undefined and returned as null
            _minterms = solver.GetMinterms();
            if (_minterms == null)
            {
                _mintermsLog = -1;
            }
            else
            {
                _stateArray = new DfaMatchingState<TSet>[InitialStateLimit];
                _capturingStateArray = new DfaMatchingState<TSet>[InitialStateLimit];

                // the extra +1 slot with id minterms.Length is reserved for \Z (last occurrence of \n)
                _mintermsLog = BitOperations.Log2((uint)_minterms.Length) + 1;
                _delta = new DfaMatchingState<TSet>[InitialStateLimit << _mintermsLog];
                _capturingDelta = new List<(DfaMatchingState<TSet>, DerivativeEffect[])>[InitialStateLimit << _mintermsLog];
            }

            // initialized to False but updated later to the actual condition ony if \b or \B occurs anywhere in the regex
            // this implies that if a regex never uses \b or \B then the character context will never
            // update the previous character context to distinguish word and nonword letters
            _wordLetterForBoundariesSet = solver.Empty;

            // initialized to False but updated later to the actual condition of \n only if a line anchor occurs anywhere in the regex
            // this implies that if a regex never uses a line anchor then the character context will never
            // update the previous character context to mark that the previous caharcter was \n
            _newLineSet = solver.Empty;
            _nothing = SymbolicRegexNode<TSet>.CreateFalse(this);
            _anyChar = SymbolicRegexNode<TSet>.CreateTrue(this);
            _anyStar = SymbolicRegexNode<TSet>.CreateLoop(this, _anyChar, 0, int.MaxValue, isLazy: false);
            _anyStarLazy = SymbolicRegexNode<TSet>.CreateLoop(this, _anyChar, 0, int.MaxValue, isLazy: true);

            // --- initialize singletonCache ---
            _singletonCache[_solver.Empty] = _nothing;
            _singletonCache[_solver.Full] = _anyChar;
        }

        /// <summary>Lookup the actual minterm based on its ID.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TSet GetMinterm(int mintermId)
        {
            TSet[]? minterms = _minterms;
            Debug.Assert(minterms is not null);
            return (uint)mintermId < (uint)minterms.Length ?
                minterms[mintermId] :
                _solver.Empty; // minterm=False represents \Z
        }

        /// <summary>
        /// Make a disjunction of given nodes, simplify by eliminating any regex that accepts no inputs
        /// </summary>
        internal SymbolicRegexNode<TSet> Or(params SymbolicRegexNode<TSet>[] nodes) =>
            SymbolicRegexNode<TSet>.Or(this, nodes);

        /// <summary>
        /// Make an ordered disjunction of given nodes, simplify by eliminating any regex that accepts no inputs
        /// </summary>
        internal SymbolicRegexNode<TSet> OrderedOr(params SymbolicRegexNode<TSet>[] nodes)
        {
            SymbolicRegexNode<TSet>? or = null;
            // Iterate backwards to avoid quadratic rebuilding of the Or nodes, which are always simplified to
            // right associative form. Concretely:
            // In (a|(b|c)) | d -> (a|(b|(c|d)) the first argument is not a subtree of the result.
            // In a | (b|(c|d)) -> (a|(b|(c|d)) the second argument is a subtree of the result.
            // The first case performs linear work for each element, leading to a quadratic blowup.
            for (int i = nodes.Length - 1; i >= 0; --i)
            {
                SymbolicRegexNode<TSet> elem = nodes[i];

                if (elem.IsNothing)
                    continue;

                or = or is null ? elem : SymbolicRegexNode<TSet>.OrderedOr(this, elem, or);

                if (elem.IsAnyStar)
                    or = elem; // .* is the absorbing element
            }

            return or ?? _nothing;
        }

        /// <summary>
        /// Make a conjunction of given nodes, simplify by eliminating nodes that accept everything
        /// </summary>
        internal SymbolicRegexNode<TSet> And(params SymbolicRegexNode<TSet>[] nodes) =>
            SymbolicRegexNode<TSet>.And(this, nodes);

        /// <summary>
        /// Make a disjunction of given set of nodes, simplify by eliminating any regex that accepts no inputs
        /// </summary>
        internal SymbolicRegexNode<TSet> Or(SymbolicRegexSet<TSet> set) =>
            set.IsNothing ? _nothing :
            set.IsEverything ? _anyStar :
            set.IsSingleton ? set.GetSingletonElement() :
            SymbolicRegexNode<TSet>.Or(this, set);

        internal SymbolicRegexNode<TSet> Or(SymbolicRegexNode<TSet> x, SymbolicRegexNode<TSet> y) =>
            x == _anyStar || y == _anyStar ? _anyStar :
            x == _nothing ? y :
            y == _nothing ? x :
            SymbolicRegexNode<TSet>.Or(this, x, y);

        /// <summary>
        /// Make a conjunction of given set, simplify by eliminating any regex that accepts all inputs,
        /// returns the empty regex if the regex accepts nothing
        /// </summary>
        internal SymbolicRegexNode<TSet> And(SymbolicRegexSet<TSet> set) =>
            set.IsNothing ? _nothing :
            set.IsEverything ? _anyStar :
            set.IsSingleton ? set.GetSingletonElement() :
            SymbolicRegexNode<TSet>.And(this, set);

        /// <summary>Create a concatenation of given nodes already given in reverse order.</summary>
        /// <remarks>
        /// If any regex is nothing, then return nothing.
        /// Eliminate intermediate epsilons.
        /// If tryCreateFixedLengthMarker and length is fixed, add a fixed length marker at the end.
        /// </remarks>
        internal SymbolicRegexNode<TSet> CreateConcatAlreadyReversed(IEnumerable<SymbolicRegexNode<TSet>> nodes, bool tryCreateFixedLengthMarker)
        {
            SymbolicRegexNode<TSet> result = Epsilon;

            if (tryCreateFixedLengthMarker)
            {
                int length = CalculateFixedLength(nodes);
                if (length >= 0)
                {
                    result = CreateFixedLengthMarker(length);
                }
            }

            // Iterate through all the nodes concatenating them together in reverse order.
            // Here the nodes enumeration is already reversed, so reversing it back to the original concatenation order.
            foreach (SymbolicRegexNode<TSet> node in nodes)
            {
                // If there's a nothing in the list, the whole concatenation can't match, so just return nothing.
                if (node == _nothing)
                {
                    return _nothing;
                }

                // no node may itself be a concat node in the list
                Debug.Assert(node._kind != SymbolicRegexNodeKind.Concat);

                result = SymbolicRegexNode<TSet>.CreateConcat(this, node, result);
            }

            return result;
        }

        internal SymbolicRegexNode<TSet> CreateConcat(SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right) => SymbolicRegexNode<TSet>.CreateConcat(this, left, right);

        private static int CalculateFixedLength(IEnumerable<SymbolicRegexNode<TSet>> nodes)
        {
            int length = 0;
            foreach (SymbolicRegexNode<TSet> node in nodes)
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
        internal SymbolicRegexNode<TSet> CreateLoop(SymbolicRegexNode<TSet> node, bool isLazy, int lower = 0, int upper = int.MaxValue)
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
                if (_solver.IsFull(node._set))
                {
                    return _anyStar;
                }
            }

            // Otherwise, create the loop.
            return SymbolicRegexNode<TSet>.CreateLoop(this, node, lower, upper, isLazy);
        }

        /// <summary>Creates a "singleton", which matches a single character.</summary>
        internal SymbolicRegexNode<TSet> CreateSingleton(TSet set)
        {
            // We maintain a cache of singletons, under the assumption that it's likely the same one/notone/set appears
            // multiple times in the same pattern.  First consult the cache, and then create a new singleton if one didn't exist.
            ref SymbolicRegexNode<TSet>? result = ref CollectionsMarshal.GetValueRefOrAddDefault(_singletonCache, set, out _);
            return result ??= SymbolicRegexNode<TSet>.CreateSingleton(this, set);
        }

        /// <summary>Creates a fixed length marker for the end of a sequence.</summary>
        internal SymbolicRegexNode<TSet> CreateFixedLengthMarker(int length) => SymbolicRegexNode<TSet>.CreateFixedLengthMarker(this, length);

        /// <summary>
        /// Make a complemented node
        /// </summary>
        /// <param name="node">node to be complemented</param>
        /// <returns></returns>
        internal SymbolicRegexNode<TSet> Not(SymbolicRegexNode<TSet> node) => SymbolicRegexNode<TSet>.Not(this, node);

        internal SymbolicRegexNode<TSet> CreateCapture(SymbolicRegexNode<TSet> child, int captureNum) => CreateConcat(CreateCaptureStart(captureNum), CreateConcat(child, CreateCaptureEnd(captureNum)));

        internal SymbolicRegexNode<TSet> CreateCaptureStart(int captureNum) => SymbolicRegexNode<TSet>.CreateCaptureStart(this, captureNum);

        internal SymbolicRegexNode<TSet> CreateCaptureEnd(int captureNum) => SymbolicRegexNode<TSet>.CreateCaptureEnd(this, captureNum);

        internal SymbolicRegexNode<TSet> CreateDisableBacktrackingSimulation(SymbolicRegexNode<TSet> child)
        {
            return child == _nothing ? _nothing : SymbolicRegexNode<TSet>.CreateDisableBacktrackingSimulation(this, child);
        }

        internal SymbolicRegexNode<TNewSet> Transform<TNewSet>(SymbolicRegexNode<TSet> node, SymbolicRegexBuilder<TNewSet> builder, Func<SymbolicRegexBuilder<TNewSet>, TSet, TNewSet> setTransformer)
            where TNewSet : IComparable<TNewSet>, IEquatable<TNewSet>
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                return StackHelper.CallOnEmptyStack(Transform, node, builder, setTransformer);
            }

            switch (node._kind)
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
                    return builder.CreateFixedLengthMarker(node._lower);

                case SymbolicRegexNodeKind.Epsilon:
                    return builder.Epsilon;

                case SymbolicRegexNodeKind.Singleton:
                    Debug.Assert(node._set is not null);
                    return builder.CreateSingleton(setTransformer(builder, node._set));

                case SymbolicRegexNodeKind.Loop:
                    Debug.Assert(node._left is not null);
                    return builder.CreateLoop(Transform(node._left, builder, setTransformer), node.IsLazy, node._lower, node._upper);

                case SymbolicRegexNodeKind.Or:
                    Debug.Assert(node._alts is not null);
                    return builder.Or(node._alts.Transform(builder, setTransformer));

                case SymbolicRegexNodeKind.OrderedOr:
                    Debug.Assert(node._left is not null && node._right is not null);
                    return builder.OrderedOr(Transform(node._left, builder, setTransformer), Transform(node._right, builder, setTransformer));

                case SymbolicRegexNodeKind.And:
                    Debug.Assert(node._alts is not null);
                    return builder.And(node._alts.Transform(builder, setTransformer));

                case SymbolicRegexNodeKind.CaptureStart:
                    return builder.CreateCaptureStart(node._lower);

                case SymbolicRegexNodeKind.CaptureEnd:
                    return builder.CreateCaptureEnd(node._lower);

                case SymbolicRegexNodeKind.Concat:
                    {
                        List<SymbolicRegexNode<TSet>> concatElements = node.ToList();
                        SymbolicRegexNode<TNewSet>[] reverseTransformed = new SymbolicRegexNode<TNewSet>[concatElements.Count];
                        for (int i = 0; i < reverseTransformed.Length; i++)
                        {
                            reverseTransformed[i] = Transform(concatElements[^(i + 1)], builder, setTransformer);
                        }
                        return builder.CreateConcatAlreadyReversed(reverseTransformed, tryCreateFixedLengthMarker: false);
                    }

                case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
                    Debug.Assert(node._left is not null);
                    return builder.CreateDisableBacktrackingSimulation(Transform(node._left, builder, setTransformer));

                default:
                    Debug.Assert(node._kind == SymbolicRegexNodeKind.Not);
                    Debug.Assert(node._left is not null);
                    return builder.Not(Transform(node._left, builder, setTransformer));
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
        public DfaMatchingState<TSet> CreateState(SymbolicRegexNode<TSet> node, uint prevCharKind, bool disableCaching = false, bool capturing = false)
        {
            //first prune the anchors in the node
            TSet wlbSet = _wordLetterForBoundariesSet;
            TSet startSet = node.GetStartSet();

            //true if the startset of the node overlaps with some wordletter or the node can be nullable
            bool contWithWL = node.CanBeNullable || !_solver.IsEmpty(_solver.And(wlbSet, startSet));

            //true if the startset of the node overlaps with some nonwordletter or the node can be nullable
            bool contWithNWL = node.CanBeNullable || !_solver.IsEmpty(_solver.And(_solver.Not(wlbSet), startSet));
            SymbolicRegexNode<TSet> pruned_node = node.PruneAnchors(prevCharKind, contWithWL, contWithNWL);
            var s = new DfaMatchingState<TSet>(pruned_node, prevCharKind);
            if (!(capturing ? _capturingStateCache : _stateCache).TryGetValue(s, out DfaMatchingState<TSet>? state))
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

        private DfaMatchingState<TSet> MakeNewState(DfaMatchingState<TSet> state, bool capturing)
        {
            lock (this)
            {
                HashSet<DfaMatchingState<TSet>> cache = capturing ? _capturingStateCache : _stateCache;
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
        public int CreateNfaState(SymbolicRegexNode<TSet> node, uint prevCharKind)
        {
            // TBD: OrderedOr
            Debug.Assert(node.Kind != SymbolicRegexNodeKind.Or);

            // First make the underlying core state
            DfaMatchingState<TSet> coreState = CreateState(node, prevCharKind);

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
        public DfaMatchingState<TSet> GetCoreState(int nfaStateId)
        {
            Debug.Assert(_stateArray is not null);
            Debug.Assert(nfaStateId < _nfaStateArray.Length);
            Debug.Assert(_nfaStateArray[nfaStateId] < _stateArray.Length);
            return _stateArray[_nfaStateArray[nfaStateId]];
        }

        /// <summary>Critical region for defining a new core transition</summary>
        public DfaMatchingState<TSet> CreateNewTransition(DfaMatchingState<TSet> sourceState, int mintermId, int offset)
        {
            TryCreateNewTransition(sourceState, mintermId, offset, checkThreshold: false, out DfaMatchingState<TSet>? nextState);
            Debug.Assert(nextState is not null);
            return nextState;
        }

        /// <summary>Gets or creates a new DFA transition.</summary>
        public bool TryCreateNewTransition(
            DfaMatchingState<TSet> sourceState, int mintermId, int offset, bool checkThreshold, [NotNullWhen(true)] out DfaMatchingState<TSet>? nextState)
        {
            Debug.Assert(_delta is not null);
            lock (this)
            {
                Debug.Assert(offset < _delta.Length);

                // check if meanwhile delta[offset] has become defined possibly by another thread
                DfaMatchingState<TSet>? targetState = _delta[offset];
                if (targetState is null)
                {
                    if (checkThreshold && _stateCache.Count >= SymbolicRegexMatcher<TSet>.NfaThreshold)
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
                    DfaMatchingState<TSet> coreState = GetCoreState(nfaStateId);
                    int coreOffset = (coreState.Id << _mintermsLog) | mintermId;
                    DfaMatchingState<TSet>? coreTarget = _delta[coreOffset] ?? CreateNewTransition(coreState, mintermId, coreOffset);

                    // TBD: OrderedOr
                    if (coreTarget.Node.Kind == SymbolicRegexNodeKind.Or)
                    {
                        // Create separate NFA states for all members of a disjunction
                        // Here duplicate NFA states cannot arise because there are no duplicate nodes in the disjunction
                        SymbolicRegexSet<TSet>? alts = coreTarget.Node._alts;
                        Debug.Assert(alts is not null);

                        targets = new int[alts.Count];
                        int targetIndex = 0;
                        foreach (SymbolicRegexNode<TSet> q in alts)
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
