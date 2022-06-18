// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic
{
    internal sealed partial class SymbolicRegexMatcher<TSet>
    {
        /// <summary>
        /// Initial capacity for DFA related arrays.
        /// </summary>
        private const int InitialDfaStateCapacity = 1024;

        /// <summary>
        /// Minimum capacity for NFA related arrays when the matcher first enters NFA mode. The arrays start out empty,
        /// but are resized to this capacity upon first use.
        /// </summary>
        private const int InitialNfaStateCapacity = 64;

        /// <summary>
        /// Cache for the states that have been created. Each state is uniquely identified by its associated
        /// <see cref="SymbolicRegexNode{TSet}"/> and the kind of the previous character.
        /// </summary>
        private Dictionary<(SymbolicRegexNode<TSet> Node, uint PrevCharKind), DfaMatchingState<TSet>> _stateCache = new();

        /// <summary>
        /// Maps state ids to states, initial capacity is given by <see cref="InitialDfaStateCapacity"/>.
        /// Each time more states are needed the length is doubled.
        /// The first valid state is at index 1.
        /// </summary>
        private DfaMatchingState<TSet>?[] _stateArray;

        /// <summary>
        /// Maps state IDs to context-independent information for all states in <see cref="_stateArray"/>.
        /// The first valid entry is at index 1.
        /// </summary>
        private ContextIndependentState[] _stateInfo;

        /// <summary>Context-independent information available for every state.</summary>
        [Flags]
        private enum ContextIndependentState : byte
        {
            IsInitial = 1,
            IsDeadend = 2,
            IsNullable = 4,
            CanBeNullable = 8,
        }

        /// <summary>
        /// The transition function for DFA mode.
        /// Each state has a range of consecutive entries for each minterm ID. A range of size 2^L, where L is
        /// the number of bits required to represent the largest minterm ID <see cref="_mintermsLog"/>, is reserved
        /// for each state. This makes indexing into this array not require a multiplication
        /// <see cref="DeltaOffset(int, int)"/>, but does mean some unused space may be present.
        /// The first valid state ID is 1.
        /// </summary>
        /// <remarks>
        /// For these "delta" arrays, technically Volatile.Read should be used to read out an element,
        /// but in practice that's not needed on the runtimes in use (though that needs to be documented
        /// via https://github.com/dotnet/runtime/issues/63474), and use of Volatile.Read is
        /// contributing non-trivial overhead (https://github.com/dotnet/runtime/issues/65789).
        /// </remarks>
        private int[] _delta;

        /// <summary>
        /// Maps each NFA state id to the state id of the DfaMatchingState stored in _stateArray.
        /// This map is used to compactly represent NFA state ids in NFA mode in order to utilize
        /// the property that all NFA states are small integers in one interval.
        /// The valid entries are 0 to <see cref="NfaStateCount"/>-1.
        /// </summary>
        private int[] _nfaStateArray = Array.Empty<int>();

        /// <summary>
        /// Maps the id of a DfaMatchingState to the NFA state id that it is being identifed with in the NFA.
        /// It is the inverse of used entries in _nfaStateArray.
        /// The range of this map is 0 to <see cref="NfaStateCount"/>-1.
        /// </summary>
        private readonly Dictionary<int, int> _nfaStateArrayInverse = new();

        /// <summary>Gets <see cref="_nfaStateArrayInverse"/>.Count</summary>
        private int NfaStateCount => _nfaStateArrayInverse.Count;

        /// <summary>
        /// Transition function for NFA transitions in NFA mode.
        /// Each NFA entry maps to a list of NFA target states.
        /// Each list of target states is without repetitions.
        /// If the entry is null then the targets states have not been computed yet.
        /// </summary>
        private int[]?[] _nfaDelta = Array.Empty<int[]>();

        /// <summary>
        /// The transition function for <see cref="FindSubcaptures(ReadOnlySpan{char}, int, int, PerThreadData)"/>,
        /// which is an NFA mode with additional state to track capture start and end positions.
        /// Each entry is an array of pairs of target state and effects to be applied when taking the transition.
        /// If the entry is null then the transition has not been computed yet.
        /// </summary>
        private (int, DerivativeEffect[])[]?[] _capturingNfaDelta = Array.Empty<(int, DerivativeEffect[])[]?>();

        /// <summary>
        /// Implements a version of <see cref="Array.Resize"/> that is guaranteed to not publish an array before values
        /// have been copied over.
        /// </summary>
        /// <remarks>
        /// This may not be strictly necessary for arrays of primitive or reference types (which have atomic
        /// reads/writes), as when, e.g., <see cref="_delta"/> is found to not have an entry the array is checked again
        /// after a lock on the matcher has been acquired. However, in a highly threaded use case it still seems better
        /// to avoid unnecessarily causing other threads to acquire the lock.
        /// </remarks>
        private static void ConcurrentArrayResize<T>(ref T[] array, int newSize)
        {
            Debug.Assert(newSize >= array.Length);
            T[] newArray = new T[newSize];
            Array.Copy(array, newArray, array.Length);
            Volatile.Write(ref array, newArray);
        }

        private int DeltaOffset(int stateId, int mintermId) => (stateId << _mintermsLog) | mintermId;

        /// <summary>Returns the span from <see cref="_delta"/> that may contain transitions for the given state</summary>
        private Span<int> GetDeltasFor(DfaMatchingState<TSet> state)
        {
            int numMinterms = _minterms.Length;
            if (state.StartsWithLineAnchor)
            {
                numMinterms++;
            }

            return _delta.AsSpan(state.Id << _mintermsLog, numMinterms);
        }

        /// <summary>Returns the span from <see cref="_nfaDelta"/> that may contain transitions for the given state</summary>
        private Span<int[]?> GetNfaDeltasFor(DfaMatchingState<TSet> state)
        {
            Debug.Assert(Monitor.IsEntered(this));

            if (!_nfaStateArrayInverse.TryGetValue(state.Id, out int nfaState))
            {
                return default;
            }

            int numMinterms = _minterms.Length;
            if (state.StartsWithLineAnchor)
            {
                numMinterms++;
            }

            return _nfaDelta.AsSpan(nfaState << _mintermsLog, numMinterms);
        }

        /// <summary>Get context-independent information for the given state.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (bool IsInitial, bool IsDeadend, bool IsNullable, bool CanBeNullable) GetStateInfo(int stateId)
        {
            Debug.Assert(stateId > 0);

            ContextIndependentState info = _stateInfo[stateId];
            return ((info & ContextIndependentState.IsInitial) != 0,
                    (info & ContextIndependentState.IsDeadend) != 0,
                    (info & ContextIndependentState.IsNullable) != 0,
                    (info & ContextIndependentState.CanBeNullable) != 0);
        }

        /// <summary>
        /// Create a state with given node and previous character context.
        /// </summary>
        /// <param name="node">the pattern that this state will represent</param>
        /// <param name="prevCharKind">the kind of the character that led to this state</param>
        /// <returns></returns>
        private DfaMatchingState<TSet> GetOrCreateState(SymbolicRegexNode<TSet> node, uint prevCharKind)
        {
            Debug.Assert(Monitor.IsEntered(this));
            return GetOrCreateStateUnsafe(node, prevCharKind);
        }

        /// <summary>
        /// Create a state with given node and previous character context.
        /// </summary>
        /// <param name="node">the pattern that this state will represent</param>
        /// <param name="prevCharKind">the kind of the character that led to this state</param>
        /// <param name="isInitialState">whether to mark the state as an initial state or not</param>
        /// <returns></returns>
        private DfaMatchingState<TSet> GetOrCreateStateUnsafe(SymbolicRegexNode<TSet> node, uint prevCharKind, bool isInitialState = false)
        {
            SymbolicRegexNode<TSet> prunedNode = node.PruneAnchors(_builder, prevCharKind);
            (SymbolicRegexNode<TSet> Node, uint PrevCharKind) key = (prunedNode, prevCharKind);
            if (!_stateCache.TryGetValue(key, out DfaMatchingState<TSet>? state))
            {
                state = new DfaMatchingState<TSet>(key.Node, key.PrevCharKind);
                _stateCache.Add(key, state); // Add to cache first to make 1 the first state ID
                state.Id = _stateCache.Count;

                Debug.Assert(_stateArray is not null);

                if (state.Id == _stateArray.Length)
                {
                    // The growth factor 2 matches that of List<T>
                    int newsize = _stateArray.Length * 2;
                    ConcurrentArrayResize(ref _stateArray, newsize);
                    ConcurrentArrayResize(ref _delta, newsize << _mintermsLog);
                    ConcurrentArrayResize(ref _stateInfo, newsize);
                }
                _stateArray[state.Id] = state;
                _stateInfo[state.Id] = BuildStateInfo(state.Id, isInitialState, state.IsDeadend(Solver), state.Node.IsNullable, state.Node.CanBeNullable);
            }

            return state;

            // Assign the context-independent information for the given state
            static ContextIndependentState BuildStateInfo(int stateId, bool isInitial, bool isDeadend, bool isNullable, bool canBeNullable)
            {
                Debug.Assert(stateId > 0);
                Debug.Assert(!isNullable || canBeNullable);

                ContextIndependentState info = 0;

                if (isInitial)
                {
                    info |= ContextIndependentState.IsInitial;
                }

                if (isDeadend)
                {
                    info |= ContextIndependentState.IsDeadend;
                }

                if (canBeNullable)
                {
                    info |= ContextIndependentState.CanBeNullable;
                    if (isNullable)
                    {
                        info |= ContextIndependentState.IsNullable;
                    }
                }

                return info;
            }
        }

        /// <summary>
        /// Make an NFA state for the given node and previous character kind.
        /// </summary>
        /// <returns>the NFA ID of the new state, or null if the state is a dead end</returns>
        private int? CreateNfaState(SymbolicRegexNode<TSet> node, uint prevCharKind)
        {
            Debug.Assert(Monitor.IsEntered(this));
            Debug.Assert(node.Kind != SymbolicRegexNodeKind.Alternate);

            // First make the underlying core state
            DfaMatchingState<TSet> coreState = GetOrCreateState(node, prevCharKind);

            // If the state is a dead end then don't create an NFA state, as dead ends in NFA mode are represented
            // as empty lists of states.
            if (coreState.IsDeadend(Solver))
            {
                return null;
            }

            if (!_nfaStateArrayInverse.TryGetValue(coreState.Id, out int nfaStateId))
            {
                nfaStateId = MakeNewNfaState(coreState.Id);
            }

            return nfaStateId;
        }

        /// <summary>Creates a new NFA state for the underlying core state</summary>
        private int MakeNewNfaState(int coreStateId)
        {
            Debug.Assert(Monitor.IsEntered(this));

            if (NfaStateCount == _nfaStateArray.Length)
            {
                // The growth factor 2 matches that of List<T>
                int newsize = Math.Max(_nfaStateArray.Length * 2, InitialNfaStateCapacity);
                ConcurrentArrayResize(ref _nfaStateArray, newsize);
                ConcurrentArrayResize(ref _nfaDelta, newsize << _mintermsLog);
                ConcurrentArrayResize(ref _capturingNfaDelta, newsize << _mintermsLog);
            }

            int nfaStateId = NfaStateCount;
            _nfaStateArray[nfaStateId] = coreStateId;
            _nfaStateArrayInverse[coreStateId] = nfaStateId;
            return nfaStateId;
        }

        /// <summary>Gets the <see cref="DfaMatchingState{TSet}"/> corresponding to the given state ID.</summary>
        private DfaMatchingState<TSet> GetState(int stateId)
        {
            Debug.Assert(stateId > 0);
            DfaMatchingState<TSet>? state = _stateArray[stateId];
            Debug.Assert(state is not null);
            return state;
        }

        /// <summary>Gets the core state Id corresponding to the NFA state</summary>
        private int GetCoreStateId(int nfaStateId)
        {
            Debug.Assert(nfaStateId < _nfaStateArray.Length);
            Debug.Assert(_nfaStateArray[nfaStateId] < _stateArray.Length);
            return _nfaStateArray[nfaStateId];
        }

        /// <summary>Gets or creates a new DFA transition.</summary>
        /// <remarks>This function locks the matcher for safe concurrent use of the <see cref="_builder"/></remarks>
        private bool TryCreateNewTransition(
            DfaMatchingState<TSet> sourceState, int mintermId, int offset, bool checkThreshold, [NotNullWhen(true)] out DfaMatchingState<TSet>? nextState)
        {
            Debug.Assert(offset < _delta.Length);

            lock (this)
            {
                // check if meanwhile delta[offset] has become defined possibly by another thread
                DfaMatchingState<TSet>? targetState = _stateArray[_delta[offset]];
                if (targetState is null)
                {
                    if (checkThreshold && _stateCache.Count >= SymbolicRegexThresholds.NfaThreshold)
                    {
                        nextState = null;
                        return false;
                    }

                    TSet minterm = GetMintermFromId(mintermId);
                    uint nextCharKind = GetPositionKind(mintermId);
                    targetState = GetOrCreateState(sourceState.Next(_builder, minterm, nextCharKind), nextCharKind);
                    Volatile.Write(ref _delta[offset], targetState.Id);
                }

                nextState = targetState;
                return true;
            }
        }

        /// <summary>Gets or creates a new NFA transition.</summary>
        /// <remarks>This function locks the matcher for safe concurrent use of the <see cref="_builder"/></remarks>
        private int[] CreateNewNfaTransition(int nfaStateId, int mintermId, int nfaOffset)
        {
            Debug.Assert(nfaOffset < _nfaDelta.Length);

            lock (this)
            {
                // check if meanwhile the nfaoffset has become defined possibly by another thread
                int[]? targets = _nfaDelta[nfaOffset];
                if (targets is null)
                {
                    // Create the underlying transition from the core state corresponding to the nfa state
                    int coreId = GetCoreStateId(nfaStateId);
                    int coreOffset = (coreId << _mintermsLog) | mintermId;
                    int coreTargetId = _delta[coreOffset];
                    DfaMatchingState<TSet> coreState = GetState(coreId);
                    TSet minterm = GetMintermFromId(mintermId);
                    uint nextCharKind = GetPositionKind(mintermId);
                    SymbolicRegexNode<TSet>? targetNode = coreTargetId > 0 ?
                        GetState(coreTargetId).Node : coreState.Next(_builder, minterm, nextCharKind);

                    List<int> targetsList = new();
                    ForEachNfaState(targetNode, nextCharKind, targetsList, static (int nfaId, List<int> targetsList) =>
                        targetsList.Add(nfaId));

                    targets = targetsList.ToArray();
                    Volatile.Write(ref _nfaDelta[nfaOffset], targets);
                }

                return targets;
            }
        }

        /// <summary>Gets or creates a new capturing NFA transition.</summary>
        /// <remarks>This function locks the matcher for safe concurrent use of the <see cref="_builder"/></remarks>
        private (int, DerivativeEffect[])[] CreateNewCapturingTransition(int nfaStateId, int mintermId, int offset)
        {
            lock (this)
            {
                // Get the next state if it exists.  The caller should have already tried and found it null (not yet created),
                // but in the interim another thread could have created it.
                (int, DerivativeEffect[])[]? targets = _capturingNfaDelta[offset];
                if (targets is null)
                {
                    DfaMatchingState<TSet> coreState = GetState(GetCoreStateId(nfaStateId));
                    TSet minterm = GetMintermFromId(mintermId);
                    uint nextCharKind = GetPositionKind(mintermId);
                    List<(SymbolicRegexNode<TSet> Node, DerivativeEffect[] Effects)>? transition = coreState.NfaNextWithEffects(_builder, minterm, nextCharKind);
                    // Build the new state and store it into the array.
                    List<(int, DerivativeEffect[])> targetsList = new();
                    foreach ((SymbolicRegexNode<TSet> Node, DerivativeEffect[] Effects) entry in transition)
                    {
                        ForEachNfaState(entry.Node, nextCharKind, (targetsList, entry.Effects),
                            static (int nfaId, (List<(int, DerivativeEffect[])> Targets, DerivativeEffect[] Effects) args) =>
                                args.Targets.Add((nfaId, args.Effects)));
                    }
                    targets = targetsList.ToArray();
                    Volatile.Write(ref _capturingNfaDelta[offset], targets);
                }

                return targets;
            }
        }

        /// <summary>
        /// Iterates through the alternation branches <see cref="SymbolicRegexNode{TSet}.EnumerateAlternationBranches(SymbolicRegexBuilder{TSet})"/>
        /// and tries to create NFA states for each. The supplied action is called for each created NFA state. These never
        /// include dead ends as <see cref="CreateNfaState(SymbolicRegexNode{TSet}, uint)"/> will filter those out.
        /// </summary>
        /// <remarks>This function locks the matcher for safe concurrent use of the <see cref="_builder"/></remarks>
        /// <typeparam name="T">the type of the additional argument passed through to the action</typeparam>
        /// <param name="node">the node to break up into NFA states</param>
        /// <param name="prevCharKind">the previous character kind for each created NFA state</param>
        /// <param name="arg">an additional argument passed through to each call to the action</param>
        /// <param name="action">action to call for each NFA state</param>
        private void ForEachNfaState<T>(SymbolicRegexNode<TSet> node, uint prevCharKind, T arg, Action<int, T> action)
        {
            lock (this)
            {
                foreach (SymbolicRegexNode<TSet> nfaNode in node.EnumerateAlternationBranches(_builder))
                {
                    if (CreateNfaState(nfaNode, prevCharKind) is int nfaId)
                    {
                        action(nfaId, arg);
                    }
                }
            }
        }
    }
}
