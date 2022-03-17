﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Represents a regex matching engine that performs regex matching using symbolic derivatives.</summary>
    internal abstract class SymbolicRegexMatcher
    {
#if DEBUG
        /// <summary>Unwind the regex of the matcher and save the resulting state graph in DGML</summary>
        /// <param name="writer">Writer to which the DGML is written.</param>
        /// <param name="nfa">True to create an NFA instead of a DFA.</param>
        /// <param name="addDotStar">True to prepend .*? onto the pattern (outside of the implicit root capture).</param>
        /// <param name="reverse">If true, then unwind the regex backwards.</param>
        /// <param name="maxStates">The approximate maximum number of states to include; less than or equal to 0 for no maximum.</param>
        /// <param name="maxLabelLength">maximum length of labels in nodes anything over that length is indicated with .. </param>
        public abstract void SaveDGML(TextWriter writer, bool nfa, bool addDotStar, bool reverse, int maxStates, int maxLabelLength);

        /// <summary>
        /// Generates up to k random strings matched by the regex
        /// </summary>
        /// <param name="k">upper bound on the number of generated strings</param>
        /// <param name="randomseed">random seed for the generator, 0 means no random seed</param>
        /// <param name="negative">if true then generate inputs that do not match</param>
        /// <returns></returns>
        public abstract IEnumerable<string> GenerateRandomMembers(int k, int randomseed, bool negative);
#endif
    }

    /// <summary>Represents a regex matching engine that performs regex matching using symbolic derivatives.</summary>
    /// <typeparam name="TSetType">Character set type.</typeparam>
    internal sealed class SymbolicRegexMatcher<TSetType> : SymbolicRegexMatcher where TSetType : notnull
    {
        /// <summary>Maximum number of built states before switching over to NFA mode.</summary>
        /// <remarks>
        /// By default, all matching starts out using DFAs, where every state transitions to one and only one
        /// state for any minterm (each character maps to one minterm).  Some regular expressions, however, can result
        /// in really, really large DFA state graphs, much too big to actually store.  Instead of failing when we
        /// encounter such state graphs, at some point we instead switch from processing as a DFA to processing as
        /// an NFA.  As an NFA, we instead track all of the states we're in at any given point, and transitioning
        /// from one "state" to the next really means for every constituent state that composes our current "state",
        /// we find all possible states that transitioning out of each of them could result in, and the union of
        /// all of those is our new "state".  This constant represents the size of the graph after which we start
        /// processing as an NFA instead of as a DFA.  This processing doesn't change immediately, however. All
        /// processing starts out in DFA mode, even if we've previously triggered NFA mode for the same regex.
        /// We switch over into NFA mode the first time a given traversal (match operation) results in us needing
        /// to create a new node and the graph is already or newly beyond this threshold.
        /// </remarks>
        internal const int NfaThreshold = 10_000;

        /// <summary>Sentinel value used internally by the matcher to indicate no match exists.</summary>
        private const int NoMatchExists = -2;

        /// <summary>Builder used to create <see cref="SymbolicRegexNode{S}"/>s while matching.</summary>
        /// <remarks>
        /// The builder is used to build up the DFA state space lazily, which means we need to be able to
        /// produce new <see cref="SymbolicRegexNode{S}"/>s as we match.  Once in NFA mode, we also use
        /// the builder to produce new NFA states.  The builder maintains a cache of all DFA and NFA states.
        /// </remarks>
        internal readonly SymbolicRegexBuilder<TSetType> _builder;

        /// <summary>Maps every character to its corresponding minterm ID.</summary>
        private readonly MintermClassifier _mintermClassifier;

        /// <summary><see cref="_pattern"/> prefixed with [0-0xFFFF]*</summary>
        /// <remarks>
        /// The matching engine first uses <see cref="_dotStarredPattern"/> to find whether there is a match
        /// and where that match might end.  Prepending the .* prefix onto the original pattern provides the DFA
        /// with the ability to continue to process input characters even if those characters aren't part of
        /// the match. If Regex.IsMatch is used, nothing further is needed beyond this prefixed pattern.  If, however,
        /// other matching operations are performed that require knowing the exact start and end of the match,
        /// the engine then needs to process the pattern in reverse to find where the match actually started;
        /// for that, it uses the <see cref="_reversePattern"/> and walks backwards through the input characters
        /// from where <see cref="_dotStarredPattern"/> left off.  At this point we know that there was a match,
        /// where it started, and where it could have ended, but that ending point could be influenced by the
        /// selection of the starting point.  So, to find the actual ending point, the original <see cref="_pattern"/>
        /// is then used from that starting point to walk forward through the input characters again to find the
        /// actual end point used for the match.
        /// </remarks>
        internal readonly SymbolicRegexNode<TSetType> _dotStarredPattern;

        /// <summary>The original regex pattern.</summary>
        internal readonly SymbolicRegexNode<TSetType> _pattern;

        /// <summary>The reverse of <see cref="_pattern"/>.</summary>
        /// <remarks>
        /// Determining that there is a match and where the match ends requires only <see cref="_pattern"/>.
        /// But from there determining where the match began requires reversing the pattern and running
        /// the matcher again, starting from the ending position. This <see cref="_reversePattern"/> caches
        /// that reversed pattern used for extracting match start.
        /// </remarks>
        internal readonly SymbolicRegexNode<TSetType> _reversePattern;

        /// <summary>true iff timeout checking is enabled.</summary>
        private readonly bool _checkTimeout;

        /// <summary>Timeout in milliseconds. This is only used if <see cref="_checkTimeout"/> is true.</summary>
        private readonly int _timeout;

        /// <summary>Data and routines for skipping ahead to the next place a match could potentially start.</summary>
        private readonly RegexFindOptimizations? _findOpts;

        /// <summary>The initial states for the original pattern, keyed off of the previous character kind.</summary>
        /// <remarks>If the pattern doesn't contain any anchors, there will only be a single initial state.</remarks>
        private readonly DfaMatchingState<TSetType>[] _initialStates;

        /// <summary>The initial states for the dot-star pattern, keyed off of the previous character kind.</summary>
        /// <remarks>If the pattern doesn't contain any anchors, there will only be a single initial state.</remarks>
        private readonly DfaMatchingState<TSetType>[] _dotstarredInitialStates;

        /// <summary>The initial states for the reverse pattern, keyed off of the previous character kind.</summary>
        /// <remarks>If the pattern doesn't contain any anchors, there will only be a single initial state.</remarks>
        private readonly DfaMatchingState<TSetType>[] _reverseInitialStates;

        /// <summary>Lookup table to quickly determine the character kind for ASCII characters.</summary>
        /// <remarks>Non-null iff the pattern contains anchors; otherwise, it's unused.</remarks>
        private readonly uint[]? _asciiCharKinds;

        /// <summary>Number of capture groups.</summary>
        private readonly int _capsize;

        /// <summary>Fixed-length of any possible match.</summary>
        /// <remarks>This will be null if matches may be of varying lengths or if a fixed-length couldn't otherwise be computed.</remarks>
        private readonly int? _fixedMatchLength;

        /// <summary>Gets whether the regular expression contains captures (beyond the implicit root-level capture).</summary>
        /// <remarks>This determines whether the matcher uses the special capturing NFA simulation mode.</remarks>
        internal bool HasSubcaptures => _capsize > 1;

        /// <summary>Get the minterm of <paramref name="c"/>.</summary>
        /// <param name="c">character code</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TSetType GetMinterm(int c)
        {
            Debug.Assert(_builder._minterms is not null);
            return _builder._minterms[_mintermClassifier.GetMintermID(c)];
        }

        /// <summary>Creates a new <see cref="SymbolicRegexMatcher{TSetType}"/>.</summary>
        /// <param name="captureCount">The number of captures in the regular expression.</param>
        /// <param name="findOptimizations">The find optimizations computed from the expression.</param>
        /// <param name="bddBuilder">The <see cref="BDD"/>-based builder.</param>
        /// <param name="rootBddNode">The root <see cref="BDD"/>-based node from the pattern.</param>
        /// <param name="algebra">The algebra to use.</param>
        /// <param name="matchTimeout">The match timeout to use.</param>
        public static SymbolicRegexMatcher<TSetType> Create(
            int captureCount, RegexFindOptimizations findOptimizations,
            SymbolicRegexBuilder<BDD> bddBuilder, SymbolicRegexNode<BDD> rootBddNode, ICharAlgebra<TSetType> algebra,
            TimeSpan matchTimeout)
        {
            // Use BitVector to represent a predicate
            var builder = new SymbolicRegexBuilder<TSetType>(algebra)
            {
                // The default constructor sets the following predicates to False; this update happens after the fact.
                // It depends on whether anchors where used in the regex whether the predicates are actually different from False.
                _wordLetterPredicateForAnchors = algebra.ConvertFromCharSet(CharSetSolver.Instance, bddBuilder._wordLetterPredicateForAnchors),
                _newLinePredicate = algebra.ConvertFromCharSet(CharSetSolver.Instance, bddBuilder._newLinePredicate)
            };

            // Convert the BDD-based AST to TSetType-based AST
            SymbolicRegexNode<TSetType> rootNode = bddBuilder.Transform(rootBddNode, builder, static (builder, bdd) => builder._solver.ConvertFromCharSet(CharSetSolver.Instance, bdd));
            return new SymbolicRegexMatcher<TSetType>(rootNode, captureCount, findOptimizations, matchTimeout);
        }

        /// <summary>Constructs matcher for given symbolic regex.</summary>
        private SymbolicRegexMatcher(SymbolicRegexNode<TSetType> rootNode, int captureCount, RegexFindOptimizations findOptimizations, TimeSpan matchTimeout)
        {
            Debug.Assert(rootNode._builder._solver is UInt64Algebra or BitVectorAlgebra, $"Unsupported algebra: {rootNode._builder._solver}");

            _pattern = rootNode;
            _builder = rootNode._builder;
            _checkTimeout = Regex.InfiniteMatchTimeout != matchTimeout;
            _timeout = (int)(matchTimeout.TotalMilliseconds + 0.5); // Round up, so it will be at least 1ms
            _mintermClassifier = _builder._solver is UInt64Algebra bv64 ?
                bv64._classifier :
                ((BitVectorAlgebra)(object)_builder._solver)._classifier;
            _capsize = captureCount;

            if (findOptimizations.MinRequiredLength == findOptimizations.MaxPossibleLength)
            {
                _fixedMatchLength = findOptimizations.MinRequiredLength;
            }

            if (findOptimizations.FindMode != FindNextStartingPositionMode.NoSearch &&
                findOptimizations.LeadingAnchor == 0) // If there are any anchors, we're better off letting the DFA quickly do its job of determining whether there's a match.
            {
                _findOpts = findOptimizations;
            }

            // Determine the number of initial states. If there's no anchor, only the default previous
            // character kind 0 is ever going to be used for all initial states.
            int statesCount = _pattern._info.ContainsSomeAnchor ? CharKind.CharKindCount : 1;

            // Create the initial states for the original pattern.
            var initialStates = new DfaMatchingState<TSetType>[statesCount];
            for (uint i = 0; i < initialStates.Length; i++)
            {
                initialStates[i] = _builder.CreateState(_pattern, i, capturing: HasSubcaptures);
            }
            _initialStates = initialStates;

            // Create the dot-star pattern (a concatenation of any* with the original pattern)
            // and all of its initial states.
            _dotStarredPattern = _builder.CreateConcat(_builder._anyStar, _pattern);
            var dotstarredInitialStates = new DfaMatchingState<TSetType>[statesCount];
            for (uint i = 0; i < dotstarredInitialStates.Length; i++)
            {
                // Used to detect if initial state was reentered,
                // but observe that the behavior from the state may ultimately depend on the previous
                // input char e.g. possibly causing nullability of \b or \B or of a start-of-line anchor,
                // in that sense there can be several "versions" (not more than StateCount) of the initial state.
                DfaMatchingState<TSetType> state = _builder.CreateState(_dotStarredPattern, i, capturing: false);
                state.IsInitialState = true;
                dotstarredInitialStates[i] = state;
            }
            _dotstarredInitialStates = dotstarredInitialStates;

            // Create the reverse pattern (the original pattern in reverse order) and all of its
            // initial states. Also disable backtracking simulation to ensure the reverse path from
            // the final state that was found is followed. Not doing so might cause the earliest
            // starting point to not be found.
            _reversePattern = _builder.CreateDisableBacktrackingSimulation(_pattern.Reverse());
            var reverseInitialStates = new DfaMatchingState<TSetType>[statesCount];
            for (uint i = 0; i < reverseInitialStates.Length; i++)
            {
                reverseInitialStates[i] = _builder.CreateState(_reversePattern, i, capturing: false);
            }
            _reverseInitialStates = reverseInitialStates;

            // Initialize our fast-lookup for determining the character kind of ASCII characters.
            // This is only required when the pattern contains anchors, as otherwise there's only
            // ever a single kind used.
            if (_pattern._info.ContainsSomeAnchor)
            {
                var asciiCharKinds = new uint[128];
                for (int i = 0; i < asciiCharKinds.Length; i++)
                {
                    TSetType predicate2;
                    uint charKind;

                    if (i == '\n')
                    {
                        predicate2 = _builder._newLinePredicate;
                        charKind = CharKind.Newline;
                    }
                    else
                    {
                        predicate2 = _builder._wordLetterPredicateForAnchors;
                        charKind = CharKind.WordLetter;
                    }

                    asciiCharKinds[i] = _builder._solver.And(GetMinterm(i), predicate2).Equals(_builder._solver.False) ? 0 : charKind;
                }
                _asciiCharKinds = asciiCharKinds;
            }
        }

        /// <summary>
        /// Create a PerThreadData with the appropriate parts initialized for this matcher's pattern.
        /// </summary>
        internal PerThreadData CreatePerThreadData() => new PerThreadData(_builder, _capsize);

        /// <summary>Compute the target state for the source state and input[i] character and transition to it.</summary>
        /// <param name="builder">The associated builder.</param>
        /// <param name="input">The input text.</param>
        /// <param name="i">The index into <paramref name="input"/> at which the target character lives.</param>
        /// <param name="state">The current state being transitioned from. Upon return it's the new state if the transition succeeded.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryTakeTransition<TStateHandler>(SymbolicRegexBuilder<TSetType> builder, ReadOnlySpan<char> input, int i, ref CurrentState state)
            where TStateHandler : struct, IStateHandler
        {
            int c = input[i];

            int mintermId = c == '\n' && i == input.Length - 1 && TStateHandler.StartsWithLineAnchor(ref state) ?
                builder._minterms!.Length : // mintermId = minterms.Length represents \Z (last \n)
                _mintermClassifier.GetMintermID(c);

            return TStateHandler.TakeTransition(builder, ref state, mintermId);
        }

        private List<(DfaMatchingState<TSetType>, DerivativeEffect[])> CreateNewCapturingTransitions(DfaMatchingState<TSetType> state, TSetType minterm, int offset)
        {
            Debug.Assert(_builder._capturingDelta is not null);
            lock (this)
            {
                // Get the next state if it exists.  The caller should have already tried and found it null (not yet created),
                // but in the interim another thread could have created it.
                List<(DfaMatchingState<TSetType>, DerivativeEffect[])>? p = _builder._capturingDelta[offset];
                if (p is null)
                {
                    // Build the new state and store it into the array.
                    p = state.NfaNextWithEffects(minterm);
                    Volatile.Write(ref _builder._capturingDelta[offset], p);
                }

                return p;
            }
        }

        private void DoCheckTimeout(int timeoutOccursAt)
        {
            // This logic is identical to RegexRunner.DoCheckTimeout, with the exception of check skipping. RegexRunner calls
            // DoCheckTimeout potentially on every iteration of a loop, whereas this calls it only once per transition.
            int currentMillis = Environment.TickCount;
            if (currentMillis >= timeoutOccursAt && (0 <= timeoutOccursAt || 0 >= currentMillis))
            {
                throw new RegexMatchTimeoutException(string.Empty, string.Empty, TimeSpan.FromMilliseconds(_timeout));
            }
        }

        /// <summary>Find a match.</summary>
        /// <param name="isMatch">Whether to return once we know there's a match without determining where exactly it matched.</param>
        /// <param name="input">The input span</param>
        /// <param name="startat">The position to start search in the input span.</param>
        /// <param name="perThreadData">Per thread data reused between calls.</param>
        public SymbolicMatch FindMatch(bool isMatch, ReadOnlySpan<char> input, int startat, PerThreadData perThreadData)
        {
            Debug.Assert(startat >= 0 && startat <= input.Length, $"{nameof(startat)} == {startat}, {nameof(input.Length)} == {input.Length}");
            Debug.Assert(perThreadData is not null);

            // If we need to perform timeout checks, store the absolute timeout value.
            int timeoutOccursAt = 0;
            if (_checkTimeout)
            {
                // Using Environment.TickCount for efficiency instead of Stopwatch -- as in the non-DFA case.
                timeoutOccursAt = Environment.TickCount + (int)(_timeout + 0.5);
            }

            // If we're starting at the end of the input, we don't need to do any work other than
            // determine whether an empty match is valid, i.e. whether the pattern is "nullable"
            // given the kinds of characters at and just before the end.
            if (startat == input.Length)
            {
                // TODO https://github.com/dotnet/runtime/issues/65606: Handle capture groups.
                uint prevKind = GetCharKind(input, startat - 1);
                uint nextKind = GetCharKind(input, startat);
                return _pattern.IsNullableFor(CharKind.Context(prevKind, nextKind)) ?
                    new SymbolicMatch(startat, 0) :
                    SymbolicMatch.NoMatch;
            }

            // Phase 1:
            // Determine whether there is a match by finding the first final state position.  This only tells
            // us whether there is a match but needn't give us the longest possible match. This may return -1 as
            // a legitimate value when the initial state is nullable and startat == 0. It returns NoMatchExists (-2)
            // when there is no match.  As an example, consider the pattern a{5,10}b* run against an input
            // of aaaaaaaaaaaaaaabbbc: phase 1 will find the position of the first b: aaaaaaaaaaaaaaab.
            int i = FindFinalStatePosition(input, startat, timeoutOccursAt, out int matchStartLowBoundary, out int matchStartLengthMarker, perThreadData);

            // If there wasn't a match, we're done.
            if (i == NoMatchExists)
            {
                return SymbolicMatch.NoMatch;
            }

            // A match exists. If we don't need further details, because IsMatch was used (and thus we don't
            // need the exact bounds of the match, captures, etc.), we're done.
            if (isMatch)
            {
                return SymbolicMatch.QuickMatch;
            }

            // Phase 2:
            // Match backwards through the input matching against the reverse of the pattern, looking for the earliest
            // start position.  That tells us the actual starting position of the match.  We can skip this phase if we
            // recorded a fixed-length marker for the portion of the pattern that matched, as we can then jump that
            // exact number of positions backwards.  Continuing the previous example, phase 2 will walk backwards from
            // that first b until it finds the 6th a: aaaaaaaaaab.
            int matchStart;
            if (matchStartLengthMarker >= 0)
            {
                matchStart = i - matchStartLengthMarker + 1;
            }
            else
            {
                Debug.Assert(i >= startat - 1);
                matchStart = i < startat ?
                    startat :
                    FindStartPosition(input, i, matchStartLowBoundary, perThreadData);
            }

            // Phase 3:
            // Match again, this time from the computed start position, to find the latest end position.  That start
            // and end then represent the bounds of the match.  If the pattern has subcaptures (captures other than
            // the top-level capture for the whole match), we need to do more work to compute their exact bounds, so we
            // take a faster path if captures aren't required.  Further, if captures aren't needed, and if any possible
            // match of the whole pattern is a fixed length, we can skip this phase as well, just using that fixed-length
            // to compute the ending position based on the starting position.  Continuing the previous example, phase 3
            // will walk forwards from the 6th a until it finds the end of the match: aaaaaaaaaabbb.
            if (!HasSubcaptures)
            {
                if (_fixedMatchLength.HasValue)
                {
                    return new SymbolicMatch(matchStart, _fixedMatchLength.GetValueOrDefault());
                }

                int matchEnd = FindEndPosition(input, matchStart, perThreadData);
                return new SymbolicMatch(matchStart, matchEnd + 1 - matchStart);
            }
            else
            {
                int matchEnd = FindEndPositionCapturing(input, matchStart, out Registers endRegisters, perThreadData);
                return new SymbolicMatch(matchStart, matchEnd + 1 - matchStart, endRegisters.CaptureStarts, endRegisters.CaptureEnds);
            }
        }

        /// <summary>Phase 3 of matching. From a found starting position, find the ending position of the match using the original pattern.</summary>
        /// <remarks>
        /// The ending position is known to exist; this function just needs to determine exactly what it is.
        /// We need to find the longest possible match and thus the latest valid ending position.
        /// </remarks>
        /// <param name="input">The input text.</param>
        /// <param name="i">The starting position of the match.</param>
        /// <param name="perThreadData">Per thread data reused between calls.</param>
        /// <returns>The found ending position of the match.</returns>
        private int FindEndPosition(ReadOnlySpan<char> input, int i, PerThreadData perThreadData)
        {
            // Get the starting state based on the current context.
            DfaMatchingState<TSetType> dfaStartState = _initialStates[GetCharKind(input, i - 1)];

            // If the starting state is nullable (accepts the empty string), then it's a valid
            // match and we need to record the position as a possible end, but keep going looking
            // for a better one.
            int end = input.Length; // invalid sentinel value
            if (dfaStartState.IsNullable(GetCharKind(input, i)))
            {
                // Empty match exists because the initial state is accepting.
                end = i - 1;
            }

            if ((uint)i < (uint)input.Length)
            {
                // Iterate from the starting state until we've found the best ending state.
                SymbolicRegexBuilder<TSetType> builder = dfaStartState.Node._builder;
                var currentState = new CurrentState(dfaStartState);
                while (true)
                {
                    // Run the DFA or NFA traversal backwards from the current point using the current state.
                    bool done = currentState.NfaState is not null ?
                        FindEndPositionDeltas<NfaStateHandler>(builder, input, ref i, ref currentState, ref end) :
                        FindEndPositionDeltas<DfaStateHandler>(builder, input, ref i, ref currentState, ref end);

                    // If we successfully found the ending position, we're done.
                    if (done || (uint)i >= (uint)input.Length)
                    {
                        break;
                    }

                    // We exited out of the inner processing loop, but we didn't hit a dead end or run out
                    // of input, and that should only happen if we failed to transition from one state to
                    // the next, which should only happen if we were in DFA mode and we tried to create
                    // a new state and exceeded the graph size.  Upgrade to NFA mode and continue;
                    Debug.Assert(currentState.DfaState is not null);
                    NfaMatchingState nfaState = perThreadData.NfaState;
                    nfaState.InitializeFrom(currentState.DfaState);
                    currentState = new CurrentState(nfaState);
                }
            }

            // Return the found ending position.
            Debug.Assert(end < input.Length, "Expected to find an ending position but didn't");
            return end;
        }

        /// <summary>
        /// Workhorse inner loop for <see cref="FindEndPosition"/>.  Consumes the <paramref name="input"/> character by character,
        /// starting at <paramref name="i"/>, for each character transitioning from one state in the DFA or NFA graph to the next state,
        /// lazily building out the graph as needed.
        /// </summary>
        private bool FindEndPositionDeltas<TStateHandler>(SymbolicRegexBuilder<TSetType> builder, ReadOnlySpan<char> input, ref int i, ref CurrentState currentState, ref int endingIndex)
            where TStateHandler : struct, IStateHandler
        {
            // To avoid frequent reads/writes to ref values, make and operate on local copies, which we then copy back once before returning.
            int pos = i;
            CurrentState state = currentState;

            // Repeatedly read the next character from the input and use it to transition the current state to the next.
            // We're looking for the furthest final state we can find.
            while ((uint)pos < (uint)input.Length && TryTakeTransition<TStateHandler>(builder, input, pos, ref state))
            {
                if (TStateHandler.IsNullable(ref state, GetCharKind(input, pos + 1)))
                {
                    // If the new state accepts the empty string, we found an ending state. Record the position.
                    endingIndex = pos;
                }
                else if (TStateHandler.IsDeadend(ref state))
                {
                    // If the new state is a dead end, the match ended the last time endingIndex was updated.
                    currentState = state;
                    i = pos;
                    return true;
                }

                // We successfully transitioned to the next state and consumed the current character,
                // so move along to the next.
                pos++;
            }

            // We either ran out of input, in which case we successfully recorded an ending index,
            // or we failed to transition to the next state due to the graph becoming too large.
            currentState = state;
            i = pos;
            return false;
        }

        /// <summary>Find match end position using the original pattern, end position is known to exist. This version also produces captures.</summary>
        /// <param name="input">input span</param>
        /// <param name="i">inclusive start position</param>
        /// <param name="resultRegisters">out parameter for the final register values, which indicate capture starts and ends</param>
        /// <param name="perThreadData">Per thread data reused between calls.</param>
        /// <returns>the match end position</returns>
        private int FindEndPositionCapturing(ReadOnlySpan<char> input, int i, out Registers resultRegisters, PerThreadData perThreadData)
        {
            int i_end = input.Length;
            Registers endRegisters = default;
            DfaMatchingState<TSetType>? endState = null;

            // Pick the correct start state based on previous character kind.
            DfaMatchingState<TSetType> initialState = _initialStates[GetCharKind(input, i - 1)];

            Registers initialRegisters = perThreadData.InitialRegisters;

            // Initialize registers with -1, which means "not seen yet"
            Array.Fill(initialRegisters.CaptureStarts, -1);
            Array.Fill(initialRegisters.CaptureEnds, -1);

            if (initialState.IsNullable(GetCharKind(input, i)))
            {
                // Empty match exists because the initial state is accepting.
                i_end = i - 1;
                endRegisters.Assign(initialRegisters);
                endState = initialState;
            }

            // Use two maps from state IDs to register values for the current and next set of states.
            // Note that these maps use insertion order, which is used to maintain priorities between states in a way
            // that matches the order the backtracking engines visit paths.
            Debug.Assert(perThreadData.Current is not null && perThreadData.Next is not null);
            SparseIntMap<Registers> current = perThreadData.Current, next = perThreadData.Next;
            current.Clear();
            next.Clear();
            current.Add(initialState.Id, initialRegisters);

            SymbolicRegexBuilder<TSetType> builder = _builder;

            while ((uint)i < (uint)input.Length)
            {
                Debug.Assert(next.Count == 0);

                int c = input[i];
                int normalMintermId = _mintermClassifier.GetMintermID(c);

                foreach ((int sourceId, Registers sourceRegisters) in current.Values)
                {
                    Debug.Assert(builder._capturingStateArray is not null);
                    DfaMatchingState<TSetType> sourceState = builder._capturingStateArray[sourceId];

                    // Find the minterm, handling the special case for the last \n
                    int mintermId = c == '\n' && i == input.Length - 1 && sourceState.StartsWithLineAnchor ?
                        builder._minterms!.Length :
                        normalMintermId; // mintermId = minterms.Length represents \Z (last \n)
                    TSetType minterm = builder.GetMinterm(mintermId);

                    // Get or create the transitions
                    int offset = (sourceId << builder._mintermsLog) | mintermId;
                    Debug.Assert(builder._capturingDelta is not null);
                    List<(DfaMatchingState<TSetType>, DerivativeEffect[])>? transitions =
                        builder._capturingDelta[offset] ??
                        CreateNewCapturingTransitions(sourceState, minterm, offset);

                    // Take the transitions in their prioritized order
                    for (int j = 0; j < transitions.Count; ++j)
                    {
                        (DfaMatchingState<TSetType> targetState, DerivativeEffect[] effects) = transitions[j];
                        if (targetState.IsDeadend)
                            continue;

                        // Try to add the state and handle the case where it didn't exist before. If the state already
                        // exists, then the transition can be safely ignored, as the existing state was generated by a
                        // higher priority transition.
                        if (next.Add(targetState.Id, out int index))
                        {
                            // Avoid copying the registers on the last transition from this state, reusing the registers instead
                            Registers newRegisters = j != transitions.Count - 1 ? sourceRegisters.Clone() : sourceRegisters;
                            newRegisters.ApplyEffects(effects, i);
                            next.Update(index, targetState.Id, newRegisters);
                            if (targetState.IsNullable(GetCharKind(input, i + 1)))
                            {
                                // Accepting state has been reached. Record the position.
                                i_end = i;
                                endRegisters.Assign(newRegisters);
                                endState = targetState;
                                // No lower priority transitions from this or other source states are taken because the
                                // backtracking engines would return the match ending here.
                                goto BreakNullable;
                            }
                        }
                    }
                }

            BreakNullable:
                if (next.Count == 0)
                {
                    // If all states died out some nullable state must have been seen before
                    break;
                }

                // Swap the state sets and prepare for the next character
                SparseIntMap<Registers> tmp = current;
                current = next;
                next = tmp;
                next.Clear();
                i++;
            }

            Debug.Assert(i_end != input.Length && endState is not null);
            // Apply effects for finishing at the stored end state
            endState.Node.ApplyEffects((effect, args) => args.Registers.ApplyEffect(effect, args.Pos),
                CharKind.Context(endState.PrevCharKind, GetCharKind(input, i_end + 1)), (Registers: endRegisters, Pos: i_end + 1));
            resultRegisters = endRegisters;
            return i_end;
        }

        /// <summary>
        /// Phase 2 of matching. From a found ending position, walk in reverse through the input using the reverse pattern to find the
        /// start position of match.
        /// </summary>
        /// <remarks>
        /// The start position is known to exist; this function just needs to determine exactly what it is.
        /// We need to find the earliest (lowest index) starting position that's not earlier than <paramref name="matchStartBoundary"/>.
        /// </remarks>
        /// <param name="input">The input text.</param>
        /// <param name="i">The ending position to walk backwards from. <paramref name="i"/> points at the last character of the match.</param>
        /// <param name="matchStartBoundary">The initial starting location discovered in phase 1, a point we must not walk earlier than.</param>
        /// <param name="perThreadData">Per thread data reused between calls.</param>
        /// <returns>The found starting position for the match.</returns>
        private int FindStartPosition(ReadOnlySpan<char> input, int i, int matchStartBoundary, PerThreadData perThreadData)
        {
            Debug.Assert(i >= 0, $"{nameof(i)} == {i}");
            Debug.Assert(matchStartBoundary >= 0 && matchStartBoundary < input.Length, $"{nameof(matchStartBoundary)} == {matchStartBoundary}");
            Debug.Assert(i >= matchStartBoundary, $"Expected {i} >= {matchStartBoundary}.");

            // Get the starting state for the reverse pattern. This depends on previous character (which, because we're
            // going backwards, is character number i + 1).
            var currentState = new CurrentState(_reverseInitialStates[GetCharKind(input, i + 1)]);

            // If the initial state is nullable, meaning it accepts the empty string, then we've already discovered
            // a valid starting position, and we just need to keep looking for an earlier one in case there is one.
            int lastStart = -1; // invalid sentinel value
            if (currentState.DfaState!.IsNullable(GetCharKind(input, i)))
            {
                lastStart = i + 1;
            }

            // Walk backwards to the furthest accepting state of the reverse pattern but no earlier than matchStartBoundary.
            SymbolicRegexBuilder<TSetType> builder = currentState.DfaState.Node._builder;
            while (true)
            {
                // Run the DFA or NFA traversal backwards from the current point using the current state.
                bool done = currentState.NfaState is not null ?
                    FindStartPositionDeltas<NfaStateHandler>(builder, input, ref i, matchStartBoundary, ref currentState, ref lastStart) :
                    FindStartPositionDeltas<DfaStateHandler>(builder, input, ref i, matchStartBoundary, ref currentState, ref lastStart);

                // If we found the starting position, we're done.
                if (done)
                {
                    break;
                }

                // We didn't find the starting position but we did exit out of the backwards traversal.  That should only happen
                // if we were unable to transition, which should only happen if we were in DFA mode and exceeded our graph size.
                // Upgrade to NFA mode and continue.
                Debug.Assert(i >= matchStartBoundary);
                Debug.Assert(currentState.DfaState is not null);
                NfaMatchingState nfaState = perThreadData.NfaState;
                nfaState.InitializeFrom(currentState.DfaState);
                currentState = new CurrentState(nfaState);
            }

            Debug.Assert(lastStart != -1, "We expected to find a starting position but didn't.");
            return lastStart;
        }

        /// <summary>
        /// Workhorse inner loop for <see cref="FindStartPosition"/>.  Consumes the <paramref name="input"/> character by character in reverse,
        /// starting at <paramref name="i"/>, for each character transitioning from one state in the DFA or NFA graph to the next state,
        /// lazily building out the graph as needed.
        /// </summary>
        private bool FindStartPositionDeltas<TStateHandler>(SymbolicRegexBuilder<TSetType> builder, ReadOnlySpan<char> input, ref int i, int startThreshold, ref CurrentState currentState, ref int lastStart)
            where TStateHandler : struct, IStateHandler
        {
            // To avoid frequent reads/writes to ref values, make and operate on local copies, which we then copy back once before returning.
            int pos = i;
            CurrentState state = currentState;

            // Loop backwards through each character in the input, transitioning from state to state for each.
            while (TryTakeTransition<TStateHandler>(builder, input, pos, ref state))
            {
                // We successfully transitioned.  If the new state is a dead end, we're done, as we must have already seen
                // and recorded a larger lastStart value that was the earliest valid starting position.
                if (TStateHandler.IsDeadend(ref state))
                {
                    Debug.Assert(lastStart != -1);
                    currentState = state;
                    i = pos;
                    return true;
                }

                // If the new state accepts the empty string, we found a valid starting position.  Record it and keep going,
                // since we're looking for the earliest one to occur within bounds.
                if (TStateHandler.IsNullable(ref state, GetCharKind(input, pos - 1)))
                {
                    lastStart = pos;
                }

                // Since we successfully transitioned, update our current index to match the fact that we consumed the previous character in the input.
                pos--;

                // If doing so now puts us below the start threshold, bail; we should have already found a valid starting location.
                if (pos < startThreshold)
                {
                    Debug.Assert(lastStart != -1);
                    currentState = state;
                    i = pos;
                    return true;
                }
            }

            // Unable to transition further.
            currentState = state;
            i = pos;
            return false;
        }

        /// <summary>Performs the initial Phase 1 match to find the first final state encountered.</summary>
        /// <param name="input">The input text.</param>
        /// <param name="i">The starting position in <paramref name="input"/>.</param>
        /// <param name="timeoutOccursAt">The time at which timeout occurs, if timeouts are being checked.</param>
        /// <param name="initialStateIndex">The last position the initial state of <see cref="_dotStarredPattern"/> was visited.</param>
        /// <param name="matchLength">Length of the match if there's a match; otherwise, -1.</param>
        /// <param name="perThreadData">Per thread data reused between calls.</param>
        /// <returns>The index into input that matches the final state, or NoMatchExists if no match exists. It returns -1 when i=0 and the initial state is nullable.</returns>
        private int FindFinalStatePosition(ReadOnlySpan<char> input, int i, int timeoutOccursAt, out int initialStateIndex, out int matchLength, PerThreadData perThreadData)
        {
            matchLength = -1;
            initialStateIndex = i;

            // Start with the start state of the dot-star pattern, which in general depends on the previous character kind in the input in order to handle anchors.
            // If the starting state is a dead end, then no match exists.
            var currentState = new CurrentState(_dotstarredInitialStates[GetCharKind(input, i - 1)]);
            if (currentState.DfaState!.IsNothing)
            {
                // This can happen, for example, when the original regex starts with a beginning anchor but the previous char kind is not Beginning.
                return NoMatchExists;
            }

            // If the starting state accepts the empty string in this context (factoring in anchors), we're done.
            if (currentState.DfaState.IsNullable(GetCharKind(input, i)))
            {
                // The initial state is nullable in this context so at least an empty match exists.
                // The last position of the match is i - 1 because the match is empty.
                // This value is -1 if i == 0.
                return i - 1;
            }

            // Otherwise, start searching from the current position until the end of the input.
            if ((uint)i < (uint)input.Length)
            {
                SymbolicRegexBuilder<TSetType> builder = currentState.DfaState.Node._builder;
                while (true)
                {
                    // If we're at an initial state, try to search ahead for the next possible match location
                    // using any find optimizations that may have previously been computed.
                    if (currentState.DfaState is { IsInitialState: true })
                    {
                        // i is the most recent position in the input when the dot-star pattern is in the initial state
                        initialStateIndex = i;

                        if (_findOpts is RegexFindOptimizations findOpts)
                        {
                            // Find the first position i that matches with some likely character.
                            if (!findOpts.TryFindNextStartingPosition(input, ref i, 0))
                            {
                                // no match was found
                                return NoMatchExists;
                            }

                            initialStateIndex = i;

                            // Update the starting state based on where TryFindNextStartingPosition moved us to.
                            // As with the initial starting state, if it's a dead end, no match exists.
                            currentState = new CurrentState(_dotstarredInitialStates[GetCharKind(input, i - 1)]);
                            if (currentState.DfaState!.IsNothing)
                            {
                                return NoMatchExists;
                            }
                        }
                    }

                    // Now run the DFA or NFA traversal from the current point using the current state. If timeouts are being checked,
                    // we need to pop out of the inner loop every now and then to do the timeout check in this outer loop.
                    const int CharsPerTimeoutCheck = 10_000;
                    ReadOnlySpan<char> inputForInnerLoop = _checkTimeout && input.Length - i > CharsPerTimeoutCheck ?
                        input.Slice(0, i + CharsPerTimeoutCheck) :
                        input;

                    int finalStatePosition;
                    int findResult = currentState.NfaState is not null ?
                        FindFinalStatePositionDeltas<NfaStateHandler>(builder, inputForInnerLoop, ref i, ref currentState, ref matchLength, out finalStatePosition) :
                        FindFinalStatePositionDeltas<DfaStateHandler>(builder, inputForInnerLoop, ref i, ref currentState, ref matchLength, out finalStatePosition);

                    // If we reached a final or deadend state, we're done.
                    if (findResult > 0)
                    {
                        return finalStatePosition;
                    }

                    // We're not at an end state, so we either ran out of input (in which case no match exists), hit an initial state (in which case
                    // we want to loop around to apply our initial state processing logic and optimizations), or failed to transition (which should
                    // only happen if we were in DFA mode and need to switch over to NFA mode).  If we exited because we hit an initial state,
                    // find result will be 0, otherwise negative.
                    if (findResult < 0)
                    {
                        if (i >= input.Length)
                        {
                            // We ran out of input. No match.
                            break;
                        }

                        if (i < inputForInnerLoop.Length)
                        {
                            // We failed to transition. Upgrade to DFA mode.
                            Debug.Assert(currentState.DfaState is not null);
                            NfaMatchingState nfaState = perThreadData.NfaState;
                            nfaState.InitializeFrom(currentState.DfaState);
                            currentState = new CurrentState(nfaState);
                        }
                    }

                    // Check for a timeout before continuing.
                    if (_checkTimeout)
                    {
                        DoCheckTimeout(timeoutOccursAt);
                    }
                }
            }

            // No match was found.
            return NoMatchExists;
        }

        /// <summary>
        /// Workhorse inner loop for <see cref="FindFinalStatePosition"/>.  Consumes the <paramref name="input"/> character by character,
        /// starting at <paramref name="i"/>, for each character transitioning from one state in the DFA or NFA graph to the next state,
        /// lazily building out the graph as needed.
        /// </summary>
        /// <remarks>
        /// The <typeparamref name="TStateHandler"/> supplies the actual transitioning logic, controlling whether processing is
        /// performed in DFA mode or in NFA mode.  However, it expects <paramref name="currentState"/> to be configured to match,
        /// so for example if <typeparamref name="TStateHandler"/> is a <see cref="DfaStateHandler"/>, it expects the <paramref name="currentState"/>'s
        /// <see cref="CurrentState.DfaState"/> to be non-null and its <see cref="CurrentState.NfaState"/> to be null; vice versa for
        /// <see cref="NfaStateHandler"/>.
        /// </remarks>
        /// <returns>
        /// A positive value if iteration completed because it reached a nullable or deadend state.
        /// 0 if iteration completed because we reached an initial state.
        /// A negative value if iteration completed because we ran out of input or we failed to transition.
        /// </returns>
        private int FindFinalStatePositionDeltas<TStateHandler>(SymbolicRegexBuilder<TSetType> builder, ReadOnlySpan<char> input, ref int i, ref CurrentState currentState, ref int matchLength, out int finalStatePosition)
            where TStateHandler : struct, IStateHandler
        {
            // To avoid frequent reads/writes to ref values, make and operate on local copies, which we then copy back once before returning.
            int pos = i;
            CurrentState state = currentState;

            // Loop through each character in the input, transitioning from state to state for each.
            while ((uint)pos < (uint)input.Length && TryTakeTransition<TStateHandler>(builder, input, pos, ref state))
            {
                // We successfully transitioned for the character at index i.  If the new state is nullable for
                // the next character, meaning it accepts the empty string, we found a final state and are done!
                if (TStateHandler.IsNullable(ref state, GetCharKind(input, pos + 1)))
                {
                    // Check whether there's a fixed-length marker for the current state.  If there is, we can
                    // use that length to optimize subsequent matching phases.
                    matchLength = TStateHandler.FixedLength(ref state);
                    currentState = state;
                    i = pos;
                    finalStatePosition = pos;
                    return 1;
                }

                // If the new state is a dead end, such that we didn't match and we can't transition anywhere
                // else, then no match exists.
                if (TStateHandler.IsDeadend(ref state))
                {
                    currentState = state;
                    i = pos;
                    finalStatePosition = NoMatchExists;
                    return 1;
                }

                // We successfully transitioned, so update our current input index to match.
                pos++;

                // Now that currentState and our position are coherent, check if currentState represents an initial state.
                // If it does, we exit out in order to allow our find optimizations to kick in to hopefully more quickly
                // find the next possible starting location.
                if (TStateHandler.IsInitialState(ref state))
                {
                    currentState = state;
                    i = pos;
                    finalStatePosition = 0;
                    return 0;
                }
            }

            currentState = state;
            i = pos;
            finalStatePosition = 0;
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint GetCharKind(ReadOnlySpan<char> input, int i)
        {
            return !_pattern._info.ContainsSomeAnchor ?
                CharKind.General : // The previous character kind is irrelevant when anchors are not used.
                GetCharKindWithAnchor(input, i);

            uint GetCharKindWithAnchor(ReadOnlySpan<char> input, int i)
            {
                Debug.Assert(_asciiCharKinds is not null);

                if ((uint)i >= (uint)input.Length)
                {
                    return CharKind.BeginningEnd;
                }

                char nextChar = input[i];
                if (nextChar == '\n')
                {
                    return
                        _builder._newLinePredicate.Equals(_builder._solver.False) ? 0 : // ignore \n
                        i == 0 || i == input.Length - 1 ? CharKind.NewLineS : // very first or very last \n. Detection of very first \n is needed for rev(\Z).
                        CharKind.Newline;
                }

                uint[] asciiCharKinds = _asciiCharKinds;
                return
                    nextChar < (uint)asciiCharKinds.Length ? asciiCharKinds[nextChar] :
                    _builder._solver.And(GetMinterm(nextChar), _builder._wordLetterPredicateForAnchors).Equals(_builder._solver.False) ? 0 : //apply the wordletter predicate to compute the kind of the next character
                    CharKind.WordLetter;
            }
        }

        /// <summary>Stores additional data for tracking capture start and end positions.</summary>
        /// <remarks>The NFA simulation based third phase has one of these for each current state in the current set of live states.</remarks>
        internal struct Registers
        {
            public Registers(int[] captureStarts, int[] captureEnds)
            {
                CaptureStarts = captureStarts;
                CaptureEnds = captureEnds;
            }

            public int[] CaptureStarts { get; set; }
            public int[] CaptureEnds { get; set; }

            /// <summary>
            /// Applies a list of effects in order to these registers at the provided input position. The order of effects
            /// should not matter though, as multiple effects to the same capture start or end do not arise.
            /// </summary>
            /// <param name="effects">list of effects to be applied</param>
            /// <param name="pos">the current input position to record</param>
            public void ApplyEffects(DerivativeEffect[] effects, int pos)
            {
                foreach (DerivativeEffect effect in effects)
                {
                    ApplyEffect(effect, pos);
                }
            }

            /// <summary>
            /// Apply a single effect to these registers at the provided input position.
            /// </summary>
            /// <param name="effect">the effecto to be applied</param>
            /// <param name="pos">the current input position to record</param>
            public void ApplyEffect(DerivativeEffect effect, int pos)
            {
                switch (effect.Kind)
                {
                    case DerivativeEffectKind.CaptureStart:
                        CaptureStarts[effect.CaptureNumber] = pos;
                        break;
                    case DerivativeEffectKind.CaptureEnd:
                        CaptureEnds[effect.CaptureNumber] = pos;
                        break;
                }
            }

            /// <summary>
            /// Make a copy of this set of registers.
            /// </summary>
            /// <returns>Registers pointing to copies of this set of registers</returns>
            public Registers Clone() => new Registers((int[])CaptureStarts.Clone(), (int[])CaptureEnds.Clone());

            /// <summary>
            /// Copy register values from another set of registers, possibly allocating new arrays if they were not yet allocated.
            /// </summary>
            /// <param name="other">the registers to copy from</param>
            public void Assign(Registers other)
            {
                if (CaptureStarts is not null && CaptureEnds is not null)
                {
                    Debug.Assert(CaptureStarts.Length == other.CaptureStarts.Length);
                    Debug.Assert(CaptureEnds.Length == other.CaptureEnds.Length);

                    Array.Copy(other.CaptureStarts, CaptureStarts, CaptureStarts.Length);
                    Array.Copy(other.CaptureEnds, CaptureEnds, CaptureEnds.Length);
                }
                else
                {
                    CaptureStarts = (int[])other.CaptureStarts.Clone();
                    CaptureEnds = (int[])other.CaptureEnds.Clone();
                }
            }
        }

        /// <summary>
        /// Per thread data to be held by the regex runner and passed into every call to FindMatch. This is used to
        /// avoid repeated memory allocation.
        /// </summary>
        internal sealed class PerThreadData
        {
            public readonly NfaMatchingState NfaState;
            /// <summary>Maps used for the capturing third phase.</summary>
            public readonly SparseIntMap<Registers>? Current, Next;
            /// <summary>Registers used for the capturing third phase.</summary>
            public readonly Registers InitialRegisters;

            public PerThreadData(SymbolicRegexBuilder<TSetType> builder, int capsize)
            {
                NfaState = new NfaMatchingState(builder);

                // Only create data used for capturing mode if there are subcaptures
                if (capsize > 1)
                {
                    Current = new();
                    Next = new();
                    InitialRegisters = new Registers(new int[capsize], new int[capsize]);
                }
            }
        }

        /// <summary>Stores the state that represents a current state in NFA mode.</summary>
        /// <remarks>The entire state is composed of a list of individual states.</remarks>
        internal sealed class NfaMatchingState
        {
            /// <summary>The associated builder used to lazily add new DFA or NFA nodes to the graph.</summary>
            public readonly SymbolicRegexBuilder<TSetType> Builder;

            /// <summary>Ordered set used to store the current NFA states.</summary>
            /// <remarks>The value is unused.  The type is used purely for its keys.</remarks>
            public SparseIntMap<int> NfaStateSet = new();
            /// <summary>Scratch set to swap with <see cref="NfaStateSet"/> on each transition.</summary>
            /// <remarks>
            /// On each transition, <see cref="NfaStateSetScratch"/> is cleared and filled with the next
            /// states computed from the current states in <see cref="NfaStateSet"/>, and then the sets
            /// are swapped so the scratch becomes the current and the current becomes the scratch.
            /// </remarks>
            public SparseIntMap<int> NfaStateSetScratch = new();

            /// <summary>Create the instance.</summary>
            /// <remarks>New instances should only be created once per runner.</remarks>
            public NfaMatchingState(SymbolicRegexBuilder<TSetType> builder) => Builder = builder;

            /// <summary>Resets this NFA state to represent the supplied DFA state.</summary>
            /// <param name="dfaMatchingState">The DFA state to use to initialize the NFA state.</param>
            public void InitializeFrom(DfaMatchingState<TSetType> dfaMatchingState)
            {
                NfaStateSet.Clear();

                // If the DFA state is a union of multiple DFA states, loop through all of them
                // adding an NFA state for each.
                if (dfaMatchingState.Node.Kind is SymbolicRegexNodeKind.Or)
                {
                    Debug.Assert(dfaMatchingState.Node._alts is not null);
                    foreach (SymbolicRegexNode<TSetType> node in dfaMatchingState.Node._alts)
                    {
                        // Create (possibly new) NFA states for all the members.
                        // Add their IDs to the current set of NFA states and into the list.
                        int nfaState = Builder.CreateNfaState(node, dfaMatchingState.PrevCharKind);
                        NfaStateSet.Add(nfaState, out _);
                    }
                }
                else
                {
                    // Otherwise, just add an NFA state for the singular DFA state.
                    SymbolicRegexNode<TSetType> node = dfaMatchingState.Node;
                    int nfaState = Builder.CreateNfaState(node, dfaMatchingState.PrevCharKind);
                    NfaStateSet.Add(nfaState, out _);
                }
            }
        }

        /// <summary>Represents a current state in a DFA or NFA graph walk while processing a regular expression.</summary>
        /// <remarks>This is a discriminated union between a DFA state and an NFA state. One and only one will be non-null.</remarks>
        private struct CurrentState
        {
            /// <summary>Initializes the state as a DFA state.</summary>
            public CurrentState(DfaMatchingState<TSetType> dfaState)
            {
                DfaState = dfaState;
                NfaState = null;
            }

            /// <summary>Initializes the state as an NFA state.</summary>
            public CurrentState(NfaMatchingState nfaState)
            {
                DfaState = null;
                NfaState = nfaState;
            }

            /// <summary>The DFA state.</summary>
            public DfaMatchingState<TSetType>? DfaState;
            /// <summary>The NFA state.</summary>
            public NfaMatchingState? NfaState;
        }

        /// <summary>Represents a set of routines for operating over a <see cref="CurrentState"/>.</summary>
        private interface IStateHandler
        {
#pragma warning disable CA2252 // This API requires opting into preview features
            public static abstract bool StartsWithLineAnchor(ref CurrentState state);
            public static abstract bool IsNullable(ref CurrentState state, uint nextCharKind);
            public static abstract bool IsDeadend(ref CurrentState state);
            public static abstract int FixedLength(ref CurrentState state);
            public static abstract bool IsInitialState(ref CurrentState state);
            public static abstract bool TakeTransition(SymbolicRegexBuilder<TSetType> builder, ref CurrentState state, int mintermId);
#pragma warning restore CA2252 // This API requires opting into preview features
        }

        /// <summary>An <see cref="IStateHandler"/> for operating over <see cref="CurrentState"/> instances configured as DFA states.</summary>
        private readonly struct DfaStateHandler : IStateHandler
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool StartsWithLineAnchor(ref CurrentState state) => state.DfaState!.StartsWithLineAnchor;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsNullable(ref CurrentState state, uint nextCharKind) => state.DfaState!.IsNullable(nextCharKind);

            /// <summary>Gets whether this is a dead-end state, meaning there are no transitions possible out of the state.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsDeadend(ref CurrentState state) => state.DfaState!.IsDeadend;

            /// <summary>Gets the length of any fixed-length marker that exists for this state, or -1 if there is none.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int FixedLength(ref CurrentState state) => state.DfaState!.FixedLength;

            /// <summary>Gets whether this is an initial state.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsInitialState(ref CurrentState state) => state.DfaState!.IsInitialState;

            /// <summary>Take the transition to the next DFA state.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool TakeTransition(SymbolicRegexBuilder<TSetType> builder, ref CurrentState state, int mintermId)
            {
                Debug.Assert(state.DfaState is not null, $"Expected non-null {nameof(state.DfaState)}.");
                Debug.Assert(state.NfaState is null, $"Expected null {nameof(state.NfaState)}.");
                Debug.Assert(builder._delta is not null);

                // Get the current state.
                DfaMatchingState<TSetType> dfaMatchingState = state.DfaState!;

                // Use the mintermId for the character being read to look up which state to transition to.
                // If that state has already been materialized, move to it, and we're done. If that state
                // hasn't been materialized, try to create it; if we can, move to it, and we're done.
                int dfaOffset = (dfaMatchingState.Id << builder._mintermsLog) | mintermId;
                DfaMatchingState<TSetType>? nextState = builder._delta[dfaOffset];
                if (nextState is not null || builder.TryCreateNewTransition(dfaMatchingState, mintermId, dfaOffset, checkThreshold: true, out nextState))
                {
                    // There was an existing state for this transition or we were able to create one.  Move to it and
                    // return that we're still operating as a DFA and can keep going.
                    state.DfaState = nextState;
                    return true;
                }

                return false;
            }
        }

        /// <summary>An <see cref="IStateHandler"/> for operating over <see cref="CurrentState"/> instances configured as NFA states.</summary>
        private readonly struct NfaStateHandler : IStateHandler
        {
            /// <summary>Check if any underlying core state starts with a line anchor.</summary>
            public static bool StartsWithLineAnchor(ref CurrentState state)
            {
                SymbolicRegexBuilder<TSetType> builder = state.NfaState!.Builder;
                foreach (ref KeyValuePair<int, int> nfaState in CollectionsMarshal.AsSpan(state.NfaState!.NfaStateSet.Values))
                {
                    if (builder.GetCoreState(nfaState.Key).StartsWithLineAnchor)
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>Check if any underlying core state is nullable.</summary>
            public static bool IsNullable(ref CurrentState state, uint nextCharKind)
            {
                SymbolicRegexBuilder<TSetType> builder = state.NfaState!.Builder;
                foreach (ref KeyValuePair<int, int> nfaState in CollectionsMarshal.AsSpan(state.NfaState!.NfaStateSet.Values))
                {
                    if (builder.GetCoreState(nfaState.Key).IsNullable(nextCharKind))
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>Gets whether this is a dead-end state, meaning there are no transitions possible out of the state.</summary>
            /// <remarks>In NFA mode, an empty set of states means that it is a dead end.</remarks>
            public static bool IsDeadend(ref CurrentState state) => state.NfaState!.NfaStateSet.Count == 0;

            /// <summary>Gets the length of any fixed-length marker that exists for this state, or -1 if there is none.</summary>
            /// <summary>In NFA mode, there are no fixed-length markers.</summary>
            public static int FixedLength(ref CurrentState state) => -1;

            /// <summary>Gets whether this is an initial state.</summary>
            /// <summary>In NFA mode, no set of states qualifies as an initial state.</summary>
            public static bool IsInitialState(ref CurrentState state) => false;

            /// <summary>Take the transition to the next NFA state.</summary>
            public static bool TakeTransition(SymbolicRegexBuilder<TSetType> builder, ref CurrentState state, int mintermId)
            {
                Debug.Assert(state.DfaState is null, $"Expected null {nameof(state.DfaState)}.");
                Debug.Assert(state.NfaState is not null, $"Expected non-null {nameof(state.NfaState)}.");

                NfaMatchingState nfaState = state.NfaState!;

                // Grab the sets, swapping the current active states set with the scratch set.
                SparseIntMap<int> nextStates = nfaState.NfaStateSetScratch;
                SparseIntMap<int> sourceStates = nfaState.NfaStateSet;
                nfaState.NfaStateSet = nextStates;
                nfaState.NfaStateSetScratch = sourceStates;

                // Compute the set of all unique next states from the current source states and the mintermId.
                nextStates.Clear();
                if (sourceStates.Count == 1)
                {
                    // We have a single source state.  We know its next states are already deduped,
                    // so we can just add them directly to the destination states list.
                    foreach (int nextState in GetNextStates(sourceStates.Values[0].Key, mintermId, builder))
                    {
                        nextStates.Add(nextState, out _);
                    }
                }
                else
                {
                    // We have multiple source states, so we need to potentially dedup across each of
                    // their next states.  For each source state, get its next states, adding each into
                    // our set (which exists purely for deduping purposes), and if we successfully added
                    // to the set, then add the known-unique state to the destination list.
                    foreach (ref KeyValuePair<int, int> sourceState in CollectionsMarshal.AsSpan(sourceStates.Values))
                    {
                        foreach (int nextState in GetNextStates(sourceState.Key, mintermId, builder))
                        {
                            nextStates.Add(nextState, out _);
                        }
                    }
                }

                return true;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static int[] GetNextStates(int sourceState, int mintermId, SymbolicRegexBuilder<TSetType> builder)
                {
                    // Calculate the offset into the NFA transition table.
                    int nfaOffset = (sourceState << builder._mintermsLog) | mintermId;

                    // Get the next NFA state.
                    return builder._nfaDelta[nfaOffset] ?? builder.CreateNewNfaTransition(sourceState, mintermId, nfaOffset);
                }
            }
        }

#if DEBUG
        public override void SaveDGML(TextWriter writer, bool nfa, bool addDotStar, bool reverse, int maxStates, int maxLabelLength) =>
            DgmlWriter<TSetType>.Write(writer, this, nfa, addDotStar, reverse, maxStates, maxLabelLength);

        public override IEnumerable<string> GenerateRandomMembers(int k, int randomseed, bool negative) =>
            new SymbolicRegexSampler<TSetType>(_pattern, randomseed, negative).GenerateRandomMembers(k);
#endif
    }
}
