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
        private readonly Dictionary<(SymbolicRegexNode<TSet> Node, uint PrevCharKind), MatchingState<TSet>> _stateCache = new();

        /// <summary>
        /// Maps state ids to states, initial capacity is given by <see cref="InitialDfaStateCapacity"/>.
        /// Each time more states are needed the length is doubled.
        /// The first valid state is at index 1.
        /// </summary>
        private MatchingState<TSet>?[] _stateArray;

        /// <summary>
        /// Maps state IDs to context-independent information for all states in <see cref="_stateArray"/>.
        /// The first valid entry is at index 1.
        /// </summary>
        private StateFlags[] _stateFlagsArray;

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
        private int[] _dfaDelta;

        /// <summary>
        /// Maps each NFA state id to the state id of the MatchingState stored in _stateArray.
        /// This map is used to compactly represent NFA state ids in NFA mode in order to utilize
        /// the property that all NFA states are small integers in one interval.
        /// The valid entries are 0 to the size of <see cref="_nfaIdByCoreId"/> - 1.
        /// </summary>
        private int[] _nfaCoreIdArray = Array.Empty<int>();

        /// <summary>
        /// Maps the id of a MatchingState to the NFA state id that it is being identifed with in the NFA.
        /// It is the inverse of used entries in _nfaStateArray.
        /// The range of this map is 0 to its size - 1.
        /// </summary>
        private readonly Dictionary<int, int> _nfaIdByCoreId = new();

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
        /// reads/writes), as when, e.g., <see cref="_dfaDelta"/> is found to not have an entry the array is checked again
        /// after a lock on the matcher has been acquired. However, in a highly threaded use case it still seems better
        /// to avoid unnecessarily causing other threads to acquire the lock.
        /// </remarks>
        private static void ArrayResizeAndVolatilePublish<T>(ref T[] array, int newSize)
        {
            Debug.Assert(newSize >= array.Length);
            T[] newArray = new T[newSize];
            Array.Copy(array, newArray, array.Length);
            Volatile.Write(ref array, newArray);
        }

        private int DeltaOffset(int stateId, int mintermId) => (stateId << _mintermsLog) | mintermId;

        /// <summary>Returns the span from <see cref="_dfaDelta"/> that may contain transitions for the given state</summary>
        private Span<int> GetDeltasFor(MatchingState<TSet> state)
        {
            Debug.Assert(Monitor.IsEntered(this));

            int numMinterms = _minterms.Length;
            if (state.StartsWithLineAnchor)
            {
                numMinterms++;
            }

            return _dfaDelta.AsSpan(state.Id << _mintermsLog, numMinterms);
        }

        /// <summary>Returns the span from <see cref="_nfaDelta"/> that may contain transitions for the given state</summary>
        private Span<int[]?> GetNfaDeltasFor(MatchingState<TSet> state)
        {
            Debug.Assert(Monitor.IsEntered(this));

            if (!_nfaIdByCoreId.TryGetValue(state.Id, out int nfaState))
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

        /// <summary>
        /// Create a state with given node and previous character context.
        /// </summary>
        /// <param name="node">the pattern that this state will represent</param>
        /// <param name="prevCharKind">the kind of the character that led to this state</param>
        /// <returns></returns>
        private MatchingState<TSet> GetOrCreateState(SymbolicRegexNode<TSet> node, uint prevCharKind)
        {
            Debug.Assert(Monitor.IsEntered(this));
            return GetOrCreateState_NoLock(node, prevCharKind);
        }

        /// <summary>
        /// Create a state with given node and previous character context.
        /// </summary>
        /// <param name="node">the pattern that this state will represent</param>
        /// <param name="prevCharKind">the kind of the character that led to this state</param>
        /// <param name="isInitialState">whether to mark the state as an initial state or not</param>
        /// <returns></returns>
        private MatchingState<TSet> GetOrCreateState_NoLock(SymbolicRegexNode<TSet> node, uint prevCharKind, bool isInitialState = false)
        {
            SymbolicRegexNode<TSet> prunedNode = node.PruneAnchors(_builder, prevCharKind);
            (SymbolicRegexNode<TSet> Node, uint PrevCharKind) key = (prunedNode, prevCharKind);
            if (!_stateCache.TryGetValue(key, out MatchingState<TSet>? state))
            {
                state = new MatchingState<TSet>(key.Node, key.PrevCharKind);
                _stateCache.Add(key, state); // Add to cache first to make 1 the first state ID
                state.Id = _stateCache.Count;

                Debug.Assert(_stateArray is not null);

                if (state.Id == _stateArray.Length)
                {
                    // The growth factor 2 matches that of List<T>
                    int newsize = _stateArray.Length * 2;
                    ArrayResizeAndVolatilePublish(ref _stateArray, newsize);
                    ArrayResizeAndVolatilePublish(ref _dfaDelta, newsize << _mintermsLog);
                    ArrayResizeAndVolatilePublish(ref _stateFlagsArray, newsize);
                }
                _stateArray[state.Id] = state;
                _stateFlagsArray[state.Id] = state.BuildStateFlags(Solver, isInitialState);
            }

            return state;
        }

        /// <summary>
        /// Make an NFA state for the given node and previous character kind. NFA states include a "core state" of a
        /// <see cref="MatchingState{TSet}"/> allocated with <see cref="GetOrCreateState(SymbolicRegexNode{TSet}, uint)"/>,
        /// which stores the pattern and previous character kind and can be used for creating further NFA transitions.
        /// In addition to the ID of the core state, NFA states are allocated a new NFA mode specific ID, which is
        /// used to index into NFA mode transition arrays (e.g. <see cref="_nfaDelta"/>).
        /// </summary>
        /// <remarks>
        /// Using an ID numbering for NFA mode that is separate from DFA mode allows the IDs to be smaller, which saves
        /// space both in the NFA mode arrays and in the <see cref="SparseIntMap{T}"/> instances used during matching for
        /// sets of NFA states.
        /// The core state ID can be looked up by the NFA ID with <see cref="GetCoreStateId(int)"/>.
        /// </remarks>
        /// <returns>the NFA ID of the new state, or null if the state is a dead end</returns>
        private int? CreateNfaState(SymbolicRegexNode<TSet> node, uint prevCharKind)
        {
            Debug.Assert(Monitor.IsEntered(this));
            Debug.Assert(node.Kind != SymbolicRegexNodeKind.Alternate);

            // First make the core state for the node, which is used for creating further transitions out of this state
            MatchingState<TSet> coreState = GetOrCreateState(node, prevCharKind);

            // If the state is a dead end then don't create an NFA state, as dead ends in NFA mode are represented
            // as empty lists of states.
            if (coreState.IsDeadend(Solver))
            {
                return null;
            }

            // The NFA state itself is an ID that can be mapped back to the ID of the MatchingState. These NFA states are
            // allocated separately from the IDs used in DFA mode to avoid large values, which helps save memory in the
            // SparseIntMap data structures used in NFA matching modes.
            if (!_nfaIdByCoreId.TryGetValue(coreState.Id, out int nfaStateId))
            {
                // No NFA state already exists, so make a new one. NFA state IDs are allocated sequentially from zero by
                // giving each new state an ID equal to the number of existing NFA states.
                nfaStateId = _nfaIdByCoreId.Count;

                // If the next ID is past the end of the NFA state array, increase the sizes of the NFA arrays
                if (nfaStateId == _nfaCoreIdArray.Length)
                {
                    // The growth factor 2 matches that of List<T>
                    int newsize = Math.Max(_nfaCoreIdArray.Length * 2, InitialNfaStateCapacity);
                    ArrayResizeAndVolatilePublish(ref _nfaCoreIdArray, newsize);
                    ArrayResizeAndVolatilePublish(ref _nfaDelta, newsize << _mintermsLog);
                    ArrayResizeAndVolatilePublish(ref _capturingNfaDelta, newsize << _mintermsLog);
                }

                // Store the mapping from NFA state ID to core state ID
                Debug.Assert(nfaStateId < _nfaCoreIdArray.Length);
                _nfaCoreIdArray[nfaStateId] = coreState.Id;

                // Store the mapping from core state ID to NFA state ID
                // Adding an entry here increments the ID that will be given to the next NFA state
                _nfaIdByCoreId.Add(coreState.Id, nfaStateId);
            }

            return nfaStateId;
        }

        /// <summary>Gets the <see cref="MatchingState{TSet}"/> corresponding to the given state ID.</summary>
        private MatchingState<TSet> GetState(int stateId)
        {
            Debug.Assert(stateId > 0);
            MatchingState<TSet>? state = _stateArray[stateId];
            Debug.Assert(state is not null);
            return state;
        }

        /// <summary>Gets the core state Id corresponding to the NFA state</summary>
        private int GetCoreStateId(int nfaStateId)
        {
            Debug.Assert(nfaStateId < _nfaCoreIdArray.Length);
            Debug.Assert(_nfaCoreIdArray[nfaStateId] < _stateArray.Length);
            return _nfaCoreIdArray[nfaStateId];
        }

        /// <summary>Gets or creates a new DFA transition.</summary>
        /// <remarks>This function locks the matcher for safe concurrent use of the <see cref="_builder"/></remarks>
        private bool TryCreateNewTransition(
            MatchingState<TSet> sourceState, int mintermId, int offset, bool checkThreshold, [NotNullWhen(true)] out MatchingState<TSet>? nextState)
        {
            Debug.Assert(offset < _dfaDelta.Length);

            lock (this)
            {
                // check if meanwhile delta[offset] has become defined possibly by another thread
                MatchingState<TSet>? targetState = _stateArray[_dfaDelta[offset]];
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
                    Volatile.Write(ref _dfaDelta[offset], targetState.Id);
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
                    int coreTargetId = _dfaDelta[coreOffset];
                    MatchingState<TSet> coreState = GetState(coreId);
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
                    MatchingState<TSet> coreState = GetState(GetCoreStateId(nfaStateId));
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
