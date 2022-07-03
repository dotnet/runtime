// Licensed to the .NET Foundation under one or more agreements.
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
        /// <inheritdoc cref="Regex.SaveDGML(TextWriter, int)"/>
        public abstract void SaveDGML(TextWriter writer, int maxLabelLength);

        /// <inheritdoc cref="Regex.SampleMatches(int, int)"/>
        public abstract IEnumerable<string> SampleMatches(int k, int randomseed);

        /// <inheritdoc cref="Regex.Explore(bool, bool, bool, bool, bool)"/>
        public abstract void Explore(bool includeDotStarred, bool includeReverse, bool includeOriginal, bool exploreDfa, bool exploreNfa);
#endif
    }

    /// <summary>Represents a regex matching engine that performs regex matching using symbolic derivatives.</summary>
    /// <typeparam name="TSet">Character set type.</typeparam>
    internal sealed partial class SymbolicRegexMatcher<TSet> : SymbolicRegexMatcher where TSet : IComparable<TSet>, IEquatable<TSet>
    {
        /// <summary>Sentinel value used internally by the matcher to indicate no match exists.</summary>
        private const int NoMatchExists = -2;

        /// <summary>Builder used to create <see cref="SymbolicRegexNode{S}"/>s while matching.</summary>
        /// <remarks>
        /// The builder is used to build up the DFA state space lazily, which means we need to be able to
        /// produce new <see cref="SymbolicRegexNode{S}"/>s as we match.  Once in NFA mode, we also use
        /// the builder to produce new NFA states.  The builder maintains a cache of all DFA and NFA states.
        /// </remarks>
        internal readonly SymbolicRegexBuilder<TSet> _builder;

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
        internal readonly SymbolicRegexNode<TSet> _dotStarredPattern;

        /// <summary>The original regex pattern.</summary>
        internal readonly SymbolicRegexNode<TSet> _pattern;

        /// <summary>The reverse of <see cref="_pattern"/>.</summary>
        /// <remarks>
        /// Determining that there is a match and where the match ends requires only <see cref="_pattern"/>.
        /// But from there determining where the match began requires reversing the pattern and running
        /// the matcher again, starting from the ending position. This <see cref="_reversePattern"/> caches
        /// that reversed pattern used for extracting match start.
        /// </remarks>
        internal readonly SymbolicRegexNode<TSet> _reversePattern;

        /// <summary>true iff timeout checking is enabled.</summary>
        private readonly bool _checkTimeout;

        /// <summary>Timeout in milliseconds. This is only used if <see cref="_checkTimeout"/> is true.</summary>
        private readonly int _timeout;

        /// <summary>Data and routines for skipping ahead to the next place a match could potentially start.</summary>
        private readonly RegexFindOptimizations? _findOpts;

        /// <summary>The initial states for the original pattern, keyed off of the previous character kind.</summary>
        /// <remarks>If the pattern doesn't contain any anchors, there will only be a single initial state.</remarks>
        private readonly DfaMatchingState<TSet>[] _initialStates;

        /// <summary>The initial states for the dot-star pattern, keyed off of the previous character kind.</summary>
        /// <remarks>If the pattern doesn't contain any anchors, there will only be a single initial state.</remarks>
        private readonly DfaMatchingState<TSet>[] _dotstarredInitialStates;

        /// <summary>The initial states for the reverse pattern, keyed off of the previous character kind.</summary>
        /// <remarks>If the pattern doesn't contain any anchors, there will only be a single initial state.</remarks>
        private readonly DfaMatchingState<TSet>[] _reverseInitialStates;

        /// <summary>Lookup table to quickly determine the character kind for ASCII characters.</summary>
        /// <remarks>Non-null iff the pattern contains anchors; otherwise, it's unused.</remarks>
        private readonly uint[]? _asciiCharKinds;

        /// <summary>Number of capture groups.</summary>
        private readonly int _capsize;

        /// <summary>Gets whether the regular expression contains captures (beyond the implicit root-level capture).</summary>
        /// <remarks>This determines whether the matcher uses the special capturing NFA simulation mode.</remarks>
        internal bool HasSubcaptures => _capsize > 1;

        /// <summary>Get the minterm of <paramref name="c"/>.</summary>
        /// <param name="c">character code</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TSet GetMinterm(int c)
        {
            Debug.Assert(_builder._minterms is not null);
            return _builder._minterms[_mintermClassifier.GetMintermID(c)];
        }

        /// <summary>Creates a new <see cref="SymbolicRegexMatcher{TSetType}"/>.</summary>
        /// <param name="captureCount">The number of captures in the regular expression.</param>
        /// <param name="findOptimizations">The find optimizations computed from the expression.</param>
        /// <param name="bddBuilder">The <see cref="BDD"/>-based builder.</param>
        /// <param name="rootBddNode">The root <see cref="BDD"/>-based node from the pattern.</param>
        /// <param name="solver">The solver to use.</param>
        /// <param name="matchTimeout">The match timeout to use.</param>
        public static SymbolicRegexMatcher<TSet> Create(
            int captureCount, RegexFindOptimizations findOptimizations,
            SymbolicRegexBuilder<BDD> bddBuilder, SymbolicRegexNode<BDD> rootBddNode, ISolver<TSet> solver,
            TimeSpan matchTimeout)
        {
            CharSetSolver charSetSolver = (CharSetSolver)bddBuilder._solver;

            var builder = new SymbolicRegexBuilder<TSet>(solver, charSetSolver)
            {
                // The default constructor sets the following sets to empty; they're lazily-initialized when needed.
                // Only if anchors are in the regex will these be set to non-empty.
                _wordLetterForBoundariesSet = solver.ConvertFromBDD(bddBuilder._wordLetterForBoundariesSet, charSetSolver),
                _newLineSet = solver.ConvertFromBDD(bddBuilder._newLineSet, charSetSolver)
            };

            // Convert the BDD-based AST to TSetType-based AST
            SymbolicRegexNode<TSet> rootNode = bddBuilder.Transform(rootBddNode, builder, (builder, bdd) => builder._solver.ConvertFromBDD(bdd, charSetSolver));
            return new SymbolicRegexMatcher<TSet>(rootNode, captureCount, findOptimizations, matchTimeout);
        }

        /// <summary>Constructs matcher for given symbolic regex.</summary>
        private SymbolicRegexMatcher(SymbolicRegexNode<TSet> rootNode, int captureCount, RegexFindOptimizations findOptimizations, TimeSpan matchTimeout)
        {
            Debug.Assert(rootNode._builder._solver is UInt64Solver or BitVectorSolver, $"Unsupported solver: {rootNode._builder._solver}");

            _pattern = rootNode;
            _builder = rootNode._builder;
            _checkTimeout = Regex.InfiniteMatchTimeout != matchTimeout;
            _timeout = (int)(matchTimeout.TotalMilliseconds + 0.5); // Round up, so it will be at least 1ms
            _mintermClassifier = _builder._solver is UInt64Solver bv64 ?
                bv64._classifier :
                ((BitVectorSolver)(object)_builder._solver)._classifier;
            _capsize = captureCount;

            // Store the find optimizations that can be used to jump ahead to the next possible starting location.
            // If there's a leading beginning anchor, the find optimizations are unnecessary on top of the DFA's
            // handling for beginning anchors.
            if (findOptimizations.IsUseful &&
                findOptimizations.LeadingAnchor is not RegexNodeKind.Beginning)
            {
                _findOpts = findOptimizations;
            }

            // Determine the number of initial states. If there's no anchor, only the default previous
            // character kind 0 is ever going to be used for all initial states.
            int statesCount = _pattern._info.ContainsSomeAnchor ? CharKind.CharKindCount : 1;

            // Create the initial states for the original pattern.
            var initialStates = new DfaMatchingState<TSet>[statesCount];
            for (uint i = 0; i < initialStates.Length; i++)
            {
                initialStates[i] = _builder.CreateState(_pattern, i, capturing: HasSubcaptures);
            }
            _initialStates = initialStates;

            // Create the dot-star pattern (a concatenation of any* with the original pattern)
            // and all of its initial states.
            _dotStarredPattern = _builder.CreateConcat(_builder._anyStarLazy, _pattern);
            var dotstarredInitialStates = new DfaMatchingState<TSet>[statesCount];
            for (uint i = 0; i < dotstarredInitialStates.Length; i++)
            {
                // Used to detect if initial state was reentered,
                // but observe that the behavior from the state may ultimately depend on the previous
                // input char e.g. possibly causing nullability of \b or \B or of a start-of-line anchor,
                // in that sense there can be several "versions" (not more than StateCount) of the initial state.
                DfaMatchingState<TSet> state = _builder.CreateState(_dotStarredPattern, i, capturing: false, isInitialState: true);
                dotstarredInitialStates[i] = state;
            }
            _dotstarredInitialStates = dotstarredInitialStates;

            // Create the reverse pattern (the original pattern in reverse order) and all of its
            // initial states. Also disable backtracking simulation to ensure the reverse path from
            // the final state that was found is followed. Not doing so might cause the earliest
            // starting point to not be found.
            _reversePattern = _builder.CreateDisableBacktrackingSimulation(_pattern.Reverse());
            var reverseInitialStates = new DfaMatchingState<TSet>[statesCount];
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
                    TSet set;
                    uint charKind;

                    if (i == '\n')
                    {
                        set = _builder._newLineSet;
                        charKind = CharKind.Newline;
                    }
                    else
                    {
                        set = _builder._wordLetterForBoundariesSet;
                        charKind = CharKind.WordLetter;
                    }

                    asciiCharKinds[i] = _builder._solver.And(GetMinterm(i), set).Equals(_builder._solver.Empty) ? 0 : charKind;
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
        private bool TryTakeTransition<TStateHandler>(SymbolicRegexBuilder<TSet> builder, ReadOnlySpan<char> input, int i, ref CurrentState state)
            where TStateHandler : struct, IStateHandler
        {
            int c = input[i];

            // Find the minterm, handling the special case for the last \n for states that start with a relevant anchor
            int mintermId = c == '\n' && i == input.Length - 1 && TStateHandler.StartsWithLineAnchor(builder, ref state) ?
                builder._minterms!.Length : // mintermId = minterms.Length represents an \n at the very end of input
                _mintermClassifier.GetMintermID(c);

            return TStateHandler.TakeTransition(builder, ref state, mintermId);
        }

        private List<(DfaMatchingState<TSet>, DerivativeEffect[])> CreateNewCapturingTransitions(DfaMatchingState<TSet> state, TSet minterm, int offset)
        {
            Debug.Assert(_builder._capturingDelta is not null);
            lock (this)
            {
                // Get the next state if it exists.  The caller should have already tried and found it null (not yet created),
                // but in the interim another thread could have created it.
                List<(DfaMatchingState<TSet>, DerivativeEffect[])>? p = _builder._capturingDelta[offset];
                if (p is null)
                {
                    // Build the new state and store it into the array.
                    p = state.NfaNextWithEffects(minterm);
                    Volatile.Write(ref _builder._capturingDelta[offset], p);
                }

                return p;
            }
        }

        private void CheckTimeout(long timeoutOccursAt)
        {
            Debug.Assert(_checkTimeout);
            if (Environment.TickCount64 >= timeoutOccursAt)
            {
                throw new RegexMatchTimeoutException(string.Empty, string.Empty, TimeSpan.FromMilliseconds(_timeout));
            }
        }

        /// <summary>Find a match.</summary>
        /// <param name="mode">The mode of execution based on the regex operation being performed.</param>
        /// <param name="input">The input span</param>
        /// <param name="startat">The position to start search in the input span.</param>
        /// <param name="perThreadData">Per thread data reused between calls.</param>
        public SymbolicMatch FindMatch(RegexRunnerMode mode, ReadOnlySpan<char> input, int startat, PerThreadData perThreadData)
        {
            Debug.Assert(startat >= 0 && startat <= input.Length, $"{nameof(startat)} == {startat}, {nameof(input.Length)} == {input.Length}");
            Debug.Assert(perThreadData is not null);

            // If we need to perform timeout checks, store the absolute timeout value.
            long timeoutOccursAt = 0;
            if (_checkTimeout)
            {
                // Using Environment.TickCount for efficiency instead of Stopwatch -- as in the non-DFA case.
                timeoutOccursAt = Environment.TickCount64 + _timeout;
            }

            // Phase 1:
            // Determine the end point of the match.  The returned index is one-past-the-end index for the characters
            // in the match.  Note that -1 is a valid end point for an empty match at the beginning of the input.
            // It returns NoMatchExists (-2) when there is no match.
            // As an example, consider the pattern a{1,3}(b*) run against an input of aacaaaabbbc: phase 1 will find
            // the position of the last b: aacaaaabbbc.  It additionally records the position of the first a after
            // the c as the low boundary for the starting position.
            int matchStartLowBoundary, matchStartLengthMarker;
            int matchEnd = (_findOpts is not null, _pattern._info.ContainsSomeAnchor) switch
            {
                (true, true) => FindEndPosition<InitialStateFindOptimizationsHandler, FullNullabilityHandler>(input, startat, timeoutOccursAt, mode, out matchStartLowBoundary, out matchStartLengthMarker, perThreadData),
                (true, false) => FindEndPosition<InitialStateFindOptimizationsHandler, NoAnchorsNullabilityHandler>(input, startat, timeoutOccursAt, mode, out matchStartLowBoundary, out matchStartLengthMarker, perThreadData),
                (false, true) => FindEndPosition<NoOptimizationsInitialStateHandler, FullNullabilityHandler>(input, startat, timeoutOccursAt, mode, out matchStartLowBoundary, out matchStartLengthMarker, perThreadData),
                (false, false) => FindEndPosition<NoOptimizationsInitialStateHandler, NoAnchorsNullabilityHandler>(input, startat, timeoutOccursAt, mode, out matchStartLowBoundary, out matchStartLengthMarker, perThreadData),
            };

            // If there wasn't a match, we're done.
            if (matchEnd == NoMatchExists)
            {
                return SymbolicMatch.NoMatch;
            }

            // A match exists. If we don't need further details, e.g. because IsMatch was used (and thus we don't
            // need the exact bounds of the match, captures, etc.), we're done.
            if (mode == RegexRunnerMode.ExistenceRequired)
            {
                return SymbolicMatch.MatchExists;
            }

            // Phase 2:
            // Match backwards through the input matching against the reverse of the pattern, looking for the earliest
            // start position.  That tells us the actual starting position of the match.  We can skip this phase if we
            // recorded a fixed-length marker for the portion of the pattern that matched, as we can then jump that
            // exact number of positions backwards.  Continuing the previous example, phase 2 will walk backwards from
            // that last b until it finds the 4th a: aaabbbc.
            int matchStart;
            if (matchStartLengthMarker >= 0)
            {
                matchStart = matchEnd - matchStartLengthMarker;
            }
            else
            {
                Debug.Assert(matchEnd >= startat - 1);
                matchStart = matchEnd < startat ?
                    startat : _pattern._info.ContainsSomeAnchor ?
                        FindStartPosition<FullNullabilityHandler>(input, matchEnd, matchStartLowBoundary, perThreadData) :
                        FindStartPosition<NoAnchorsNullabilityHandler>(input, matchEnd, matchStartLowBoundary, perThreadData);
            }

            // Phase 3:
            // If there are no subcaptures (or if they're not needed), the matching process is done.  For patterns with subcaptures
            // (captures other than the top-level capture for the whole match), we need to do an additional pass to find their bounds.
            // Continuing for the previous example, phase 3 will be executed for the characters inside the match, aaabbbc,
            // and will find associate the one capture (b*) with it's match: bbb.
            if (!HasSubcaptures || mode < RegexRunnerMode.FullMatchRequired)
            {
                return new SymbolicMatch(matchStart, matchEnd - matchStart);
            }
            else
            {
                Registers endRegisters = FindSubcaptures(input, matchStart, matchEnd, perThreadData);
                return new SymbolicMatch(matchStart, matchEnd - matchStart, endRegisters.CaptureStarts, endRegisters.CaptureEnds);
            }
        }

        /// <summary>Performs the initial Phase 1 match to find the end position of the match, or first final state if this is an isMatch call.</summary>
        /// <param name="input">The input text.</param>
        /// <param name="pos">The starting position in <paramref name="input"/>.</param>
        /// <param name="timeoutOccursAt">The time at which timeout occurs, if timeouts are being checked.</param>
        /// <param name="mode">The mode of execution based on the regex operation being performed.</param>
        /// <param name="initialStatePos">The last position the initial state of <see cref="_dotStarredPattern"/> was visited before the end position was found.</param>
        /// <param name="matchLength">Length of the match if there's a match; otherwise, -1.</param>
        /// <param name="perThreadData">Per thread data reused between calls.</param>
        /// <returns>
        /// A one-past-the-end index into input for the preferred match, or first final state position if isMatch is true, or NoMatchExists if no match exists.
        /// </returns>
        private int FindEndPosition<TFindOptimizationsHandler, TNullabilityHandler>(ReadOnlySpan<char> input, int pos, long timeoutOccursAt, RegexRunnerMode mode, out int initialStatePos, out int matchLength, PerThreadData perThreadData)
            where TFindOptimizationsHandler : struct, IInitialStateHandler
            where TNullabilityHandler : struct, INullabilityHandler
        {
            initialStatePos = pos;
            int initialStatePosCandidate = pos;

            var currentState = new CurrentState(_dotstarredInitialStates[GetCharKind(input, pos - 1)]);
            SymbolicRegexBuilder<TSet> builder = _builder;

            int endPos = NoMatchExists;
            int endStateId = -1;

            while (true)
            {
                // Now run the DFA or NFA traversal from the current point using the current state. If timeouts are being checked,
                // we need to pop out of the inner loop every now and then to do the timeout check in this outer loop. Note that
                // the timeout exists not to provide perfect guarantees around execution time but rather as a mitigation against
                // catastrophic backtracking.  Catastrophic backtracking is not an issue for the NonBacktracking engine, but we
                // still check the timeout now and again to provide some semblance of the behavior a developer experiences with
                // the backtracking engines.  We can, however, choose a large number here, since it's not actually needed for security.
                const int CharsPerTimeoutCheck = 1_000;
                ReadOnlySpan<char> inputForInnerLoop = _checkTimeout && input.Length - pos > CharsPerTimeoutCheck ?
                    input.Slice(0, pos + CharsPerTimeoutCheck) :
                    input;

                bool done = currentState.NfaState is not null ?
                    FindEndPositionDeltas<NfaStateHandler, TFindOptimizationsHandler, TNullabilityHandler>(builder, input, mode, ref pos, ref currentState, ref endPos, ref endStateId, ref initialStatePos, ref initialStatePosCandidate) :
                    FindEndPositionDeltas<DfaStateHandler, TFindOptimizationsHandler, TNullabilityHandler>(builder, input, mode, ref pos, ref currentState, ref endPos, ref endStateId, ref initialStatePos, ref initialStatePosCandidate);

                // If the inner loop indicates that the search finished (for example due to reaching a deadend state) or
                // there is no more input available, then the whole search is done.
                if (done || pos >= input.Length)
                {
                    break;
                }

                // The search did not finish, so we either failed to transition (which should only happen if we were in DFA mode and
                // need to switch over to NFA mode) or ran out of input in the inner loop. Check if the inner loop still had more
                // input available.
                if (pos < inputForInnerLoop.Length)
                {
                    // Because there was still more input available, a failure to transition in DFA mode must be the cause
                    // of the early exit. Upgrade to NFA mode.
                    DfaMatchingState<TSet>? dfaState = currentState.DfaState(_builder);
                    Debug.Assert(dfaState is not null);
                    NfaMatchingState nfaState = perThreadData.NfaState;
                    nfaState.InitializeFrom(dfaState);
                    currentState = new CurrentState(nfaState);
                }

                // Check for a timeout before continuing.
                if (_checkTimeout)
                {
                    CheckTimeout(timeoutOccursAt);
                }
            }

            // Check whether there's a fixed-length marker for the current state.  If there is, we can
            // use that length to optimize subsequent matching phases.
            matchLength = endStateId > 0 ? _builder._stateArray![endStateId].FixedLength(GetCharKind(input, endPos)) : -1;
            return endPos;
        }

        /// <summary>
        /// Workhorse inner loop for <see cref="FindEndPosition"/>.  Consumes the <paramref name="input"/> character by character,
        /// starting at <paramref name="posRef"/>, for each character transitioning from one state in the DFA or NFA graph to the next state,
        /// lazily building out the graph as needed.
        /// </summary>
        /// <remarks>
        /// The <typeparamref name="TStateHandler"/> supplies the actual transitioning logic, controlling whether processing is
        /// performed in DFA mode or in NFA mode.  However, it expects <paramref name="stateRef"/> to be configured to match,
        /// so for example if <typeparamref name="TStateHandler"/> is a <see cref="DfaStateHandler"/>, it expects the <paramref name="stateRef"/>'s
        /// <see cref="CurrentState.DfaStateId"/> to be non-negative and its <see cref="CurrentState.NfaState"/> to be null; vice versa for
        /// <see cref="NfaStateHandler"/>.
        /// </remarks>
        /// <returns>
        /// A positive value if iteration completed because it reached a deadend state or nullable state and the call is an isMatch.
        /// 0 if iteration completed because we reached an initial state.
        /// A negative value if iteration completed because we ran out of input or we failed to transition.
        /// </returns>
        private bool FindEndPositionDeltas<TStateHandler, TFindOptimizationsHandler, TNullabilityHandler>(SymbolicRegexBuilder<TSet> builder, ReadOnlySpan<char> input, RegexRunnerMode mode,
                ref int posRef, ref CurrentState stateRef, ref int endPosRef, ref int endStateIdRef, ref int initialStatePosRef, ref int initialStatePosCandidateRef)
            where TStateHandler : struct, IStateHandler
            where TFindOptimizationsHandler : struct, IInitialStateHandler
            where TNullabilityHandler : struct, INullabilityHandler
        {
            // To avoid frequent reads/writes to ref and out values, make and operate on local copies, which we then copy back once before returning.
            int pos = posRef;
            CurrentState state = stateRef;
            int endPos = endPosRef;
            int endStateId = endStateIdRef;
            int initialStatePos = initialStatePosRef;
            int initialStatePosCandidate = initialStatePosCandidateRef;
            try
            {
                // Loop through each character in the input, transitioning from state to state for each.
                while (true)
                {
                    (bool isInitial, bool isDeadend, bool isNullable, bool canBeNullable) = TStateHandler.GetStateInfo(builder, ref state);

                    // Check if currentState represents an initial state. If it does, call into any possible find optimizations
                    // to hopefully more quickly find the next possible starting location.
                    if (isInitial)
                    {
                        if (!TFindOptimizationsHandler.TryFindNextStartingPosition(this, input, ref state, ref pos))
                        {
                            return true;
                        }

                        initialStatePosCandidate = pos;
                    }

                    // If the state is a dead end, such that we can't transition anywhere else, end the search.
                    if (isDeadend)
                    {
                        return true;
                    }

                    // If the state is nullable for the next character, meaning it accepts the empty string,
                    // we found a potential end state.
                    if (TNullabilityHandler.IsNullableAt<TStateHandler>(this, ref state, input, pos, isNullable, canBeNullable))
                    {
                        endPos = pos;
                        endStateId = TStateHandler.ExtractNullableCoreStateId(this, ref state, input, pos);
                        initialStatePos = initialStatePosCandidate;

                        // A match is known to exist.  If that's all we need to know, we're done.
                        if (mode == RegexRunnerMode.ExistenceRequired)
                        {
                            return true;
                        }
                    }

                    // If there is more input available try to transition with the next character.
                    if ((uint)pos >= (uint)input.Length || !TryTakeTransition<TStateHandler>(builder, input, pos, ref state))
                    {
                        return false;
                    }

                    // We successfully transitioned, so update our current input index to match.
                    pos++;
                }
            }
            finally
            {
                // Write back the local copies of the ref values.
                posRef = pos;
                stateRef = state;
                endPosRef = endPos;
                endStateIdRef = endStateId;
                initialStatePosRef = initialStatePos;
                initialStatePosCandidateRef = initialStatePosCandidate;
            }
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
        /// <param name="i">The ending position to walk backwards from. <paramref name="i"/> points one past the last character of the match.</param>
        /// <param name="matchStartBoundary">The initial starting location discovered in phase 1, a point we must not walk earlier than.</param>
        /// <param name="perThreadData">Per thread data reused between calls.</param>
        /// <returns>The found starting position for the match.</returns>
        private int FindStartPosition<TNullabilityHandler>(ReadOnlySpan<char> input, int i, int matchStartBoundary, PerThreadData perThreadData)
            where TNullabilityHandler : struct, INullabilityHandler
        {
            Debug.Assert(i >= 0, $"{nameof(i)} == {i}");
            Debug.Assert(matchStartBoundary >= 0 && matchStartBoundary <= input.Length, $"{nameof(matchStartBoundary)} == {matchStartBoundary}");
            Debug.Assert(i >= matchStartBoundary, $"Expected {i} >= {matchStartBoundary}.");

            // Get the starting state for the reverse pattern. This depends on previous character (which, because we're
            // going backwards, is character number i).
            var currentState = new CurrentState(_reverseInitialStates[GetCharKind(input, i)]);

            int lastStart = -1; // invalid sentinel value

            // Walk backwards to the furthest accepting state of the reverse pattern but no earlier than matchStartBoundary.
            SymbolicRegexBuilder<TSet> builder = _builder;
            while (true)
            {
                // Run the DFA or NFA traversal backwards from the current point using the current state.
                bool done = currentState.NfaState is not null ?
                    FindStartPositionDeltas<NfaStateHandler, TNullabilityHandler>(builder, input, ref i, matchStartBoundary, ref currentState, ref lastStart) :
                    FindStartPositionDeltas<DfaStateHandler, TNullabilityHandler>(builder, input, ref i, matchStartBoundary, ref currentState, ref lastStart);

                // If we found the starting position, we're done.
                if (done)
                {
                    break;
                }

                // We didn't find the starting position but we did exit out of the backwards traversal.  That should only happen
                // if we were unable to transition, which should only happen if we were in DFA mode and exceeded our graph size.
                // Upgrade to NFA mode and continue.
                Debug.Assert(i >= matchStartBoundary);
                DfaMatchingState<TSet>? dfaState = currentState.DfaState(_builder);
                Debug.Assert(dfaState is not null);
                NfaMatchingState nfaState = perThreadData.NfaState;
                nfaState.InitializeFrom(dfaState);
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
        private bool FindStartPositionDeltas<TStateHandler, TNullabilityHandler>(SymbolicRegexBuilder<TSet> builder, ReadOnlySpan<char> input, ref int i, int startThreshold, ref CurrentState currentState, ref int lastStart)
            where TStateHandler : struct, IStateHandler
            where TNullabilityHandler : struct, INullabilityHandler
        {
            // To avoid frequent reads/writes to ref values, make and operate on local copies, which we then copy back once before returning.
            int pos = i;
            CurrentState state = currentState;
            try
            {
                // Loop backwards through each character in the input, transitioning from state to state for each.
                while (true)
                {
                    (bool isInitial, bool isDeadend, bool isNullable, bool canBeNullable) = TStateHandler.GetStateInfo(builder, ref state);

                    // If the state accepts the empty string, we found a valid starting position.  Record it and keep going,
                    // since we're looking for the earliest one to occur within bounds.
                    if (TNullabilityHandler.IsNullableAt<TStateHandler>(this, ref state, input, pos - 1, isNullable, canBeNullable))
                    {
                        lastStart = pos;
                    }

                    // If we are past the start threshold or if the state is a dead end, bail; we should have already
                    // found a valid starting location.
                    if (pos <= startThreshold || isDeadend)
                    {
                        Debug.Assert(lastStart != -1);
                        return true;
                    }

                    // Try to transition with the next character, the one before the current position.
                    if (!TryTakeTransition<TStateHandler>(builder, input, pos - 1, ref state))
                    {
                        // Return false to indicate the search didn't finish.
                        return false;
                    }

                    // Since we successfully transitioned, update our current index to match the fact that we consumed the previous character in the input.
                    pos--;
                }
            }
            finally
            {
                // Write back the local copies of the ref values.
                currentState = state;
                i = pos;
            }
        }


        /// <summary>Run the pattern on a match to record the capture starts and ends.</summary>
        /// <param name="input">input span</param>
        /// <param name="i">inclusive start position</param>
        /// <param name="iEnd">exclusive end position</param>
        /// <param name="perThreadData">Per thread data reused between calls.</param>
        /// <returns>the final register values, which indicate capture starts and ends</returns>
        private Registers FindSubcaptures(ReadOnlySpan<char> input, int i, int iEnd, PerThreadData perThreadData)
        {
            // Pick the correct start state based on previous character kind.
            DfaMatchingState<TSet> initialState = _initialStates[GetCharKind(input, i - 1)];

            Registers initialRegisters = perThreadData.InitialRegisters;

            // Initialize registers with -1, which means "not seen yet"
            Array.Fill(initialRegisters.CaptureStarts, -1);
            Array.Fill(initialRegisters.CaptureEnds, -1);

            // Use two maps from state IDs to register values for the current and next set of states.
            // Note that these maps use insertion order, which is used to maintain priorities between states in a way
            // that matches the order the backtracking engines visit paths.
            Debug.Assert(perThreadData.Current is not null && perThreadData.Next is not null);
            SparseIntMap<Registers> current = perThreadData.Current, next = perThreadData.Next;
            current.Clear();
            next.Clear();
            current.Add(initialState.Id, initialRegisters);

            SymbolicRegexBuilder<TSet> builder = _builder;

            while ((uint)i < (uint)iEnd)
            {
                Debug.Assert(next.Count == 0);

                // Read the next character and find its minterm
                int c = input[i];
                int normalMintermId = _mintermClassifier.GetMintermID(c);

                foreach ((int sourceId, Registers sourceRegisters) in current.Values)
                {
                    Debug.Assert(builder._capturingStateArray is not null);
                    DfaMatchingState<TSet> sourceState = builder._capturingStateArray[sourceId];

                    // Handle the special case for the last \n for states that start with a relevant anchor
                    int mintermId = c == '\n' && i == input.Length - 1 && sourceState.StartsWithLineAnchor ?
                        builder._minterms!.Length : // mintermId = minterms.Length represents an \n at the very end of input
                        normalMintermId;
                    TSet minterm = builder.GetMinterm(mintermId);

                    // Get or create the transitions
                    int offset = (sourceId << builder._mintermsLog) | mintermId;
                    Debug.Assert(builder._capturingDelta is not null);
                    List<(DfaMatchingState<TSet>, DerivativeEffect[])>? transitions =
                        builder._capturingDelta[offset] ??
                        CreateNewCapturingTransitions(sourceState, minterm, offset);

                    // Take the transitions in their prioritized order
                    for (int j = 0; j < transitions.Count; ++j)
                    {
                        (DfaMatchingState<TSet> targetState, DerivativeEffect[] effects) = transitions[j];
                        Debug.Assert(!targetState.IsDeadend, "Transitions should not include dead ends.");

                        // Try to add the state and handle the case where it didn't exist before. If the state already
                        // exists, then the transition can be safely ignored, as the existing state was generated by a
                        // higher priority transition.
                        if (next.Add(targetState.Id, out int index))
                        {
                            // Avoid copying the registers on the last transition from this state, reusing the registers instead
                            Registers newRegisters = j != transitions.Count - 1 ? sourceRegisters.Clone() : sourceRegisters;
                            newRegisters.ApplyEffects(effects, i);
                            next.Update(index, targetState.Id, newRegisters);
                            if (targetState.IsNullableFor(GetCharKind(input, i + 1)))
                            {
                                // No lower priority transitions from this or other source states are taken because the
                                // backtracking engines would return the match ending here.
                                goto BreakNullable;
                            }
                        }
                    }
                }

            BreakNullable:
                // Swap the state sets and prepare for the next character
                SparseIntMap<Registers> tmp = current;
                current = next;
                next = tmp;
                next.Clear();
                i++;
            }

            Debug.Assert(current.Count > 0);
            Debug.Assert(_builder._capturingStateArray is not null);
            foreach (var (endStateId, endRegisters) in current.Values)
            {
                DfaMatchingState<TSet> endState = _builder._capturingStateArray[endStateId];
                if (endState.IsNullableFor(GetCharKind(input, iEnd)))
                {
                    // Apply effects for finishing at the stored end state
                    endState.Node.ApplyEffects((effect, args) => args.Registers.ApplyEffect(effect, args.Pos),
                        CharKind.Context(endState.PrevCharKind, GetCharKind(input, iEnd)), (Registers: endRegisters, Pos: iEnd));
                    return endRegisters;
                }
            }

            Debug.Fail("No nullable state found in the set of end states");
            return default;
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
                        _builder._newLineSet.Equals(_builder._solver.Empty) ? 0 : // ignore \n
                        i == 0 || i == input.Length - 1 ? CharKind.NewLineS : // very first or very last \n. Detection of very first \n is needed for rev(\Z).
                        CharKind.Newline;
                }

                uint[] asciiCharKinds = _asciiCharKinds;
                return
                    nextChar < (uint)asciiCharKinds.Length ? asciiCharKinds[nextChar] :
                    _builder._solver.And(GetMinterm(nextChar), _builder._wordLetterForBoundariesSet).Equals(_builder._solver.Empty) ? 0 : // intersect with the wordletter set to compute the kind of the next character
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

            public PerThreadData(SymbolicRegexBuilder<TSet> builder, int capsize)
            {
                NfaState = new NfaMatchingState(builder);

                // Only create data used for capturing mode if there are subcaptures
                if (capsize > 1)
                {
                    Current = new SparseIntMap<Registers>();
                    Next = new SparseIntMap<Registers>();
                    InitialRegisters = new Registers(new int[capsize], new int[capsize]);
                }
            }
        }

        /// <summary>Stores the state that represents a current state in NFA mode.</summary>
        /// <remarks>The entire state is composed of a list of individual states.</remarks>
        internal sealed class NfaMatchingState
        {
            /// <summary>The associated builder used to lazily add new DFA or NFA nodes to the graph.</summary>
            public readonly SymbolicRegexBuilder<TSet> Builder;

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
            public NfaMatchingState(SymbolicRegexBuilder<TSet> builder) => Builder = builder;

            /// <summary>Resets this NFA state to represent the supplied DFA state.</summary>
            /// <param name="dfaMatchingState">The DFA state to use to initialize the NFA state.</param>
            public void InitializeFrom(DfaMatchingState<TSet> dfaMatchingState)
            {
                NfaStateSet.Clear();

                // If the DFA state is a union of multiple DFA states, loop through all of them
                // adding an NFA state for each.
                foreach (SymbolicRegexNode<TSet> element in dfaMatchingState.Node.EnumerateAlternationBranches())
                {
                    // Create (possibly new) NFA states for all the members.
                    // Add their IDs to the current set of NFA states and into the list.
                    NfaStateSet.Add(Builder.CreateNfaState(element, dfaMatchingState.PrevCharKind), out _);
                }
            }
        }

        /// <summary>Represents a current state in a DFA or NFA graph walk while processing a regular expression.</summary>
        /// <remarks>This is a discriminated union between a DFA state and an NFA state. One and only one will be non-null.</remarks>
        private struct CurrentState
        {
            /// <summary>Initializes the state as a DFA state.</summary>
            public CurrentState(DfaMatchingState<TSet> dfaState)
            {
                DfaStateId = dfaState.Id;
                NfaState = null;
            }

            /// <summary>Initializes the state as an NFA state.</summary>
            public CurrentState(NfaMatchingState nfaState)
            {
                DfaStateId = -1;
                NfaState = nfaState;
            }

            /// <summary>The DFA state.</summary>
            public int DfaStateId;
            /// <summary>The NFA state.</summary>
            public NfaMatchingState? NfaState;

            public DfaMatchingState<TSet>? DfaState(SymbolicRegexBuilder<TSet> builder) => DfaStateId > 0 ? builder._stateArray![DfaStateId] : null;
        }

        /// <summary>Represents a set of routines for operating over a <see cref="CurrentState"/>.</summary>
        private interface IStateHandler
        {
            public static abstract bool StartsWithLineAnchor(SymbolicRegexBuilder<TSet> builder, ref CurrentState state);
            public static abstract bool IsNullableFor(SymbolicRegexBuilder<TSet> builder, ref CurrentState state, uint nextCharKind);
            public static abstract int ExtractNullableCoreStateId(SymbolicRegexMatcher<TSet> matcher, ref CurrentState state, ReadOnlySpan<char> input, int pos);
            public static abstract int FixedLength(SymbolicRegexBuilder<TSet> builder, ref CurrentState state, uint nextCharKind);
            public static abstract bool TakeTransition(SymbolicRegexBuilder<TSet> builder, ref CurrentState state, int mintermId);
            public static abstract (bool IsInitial, bool IsDeadend, bool IsNullable, bool CanBeNullable) GetStateInfo(SymbolicRegexBuilder<TSet> builder, ref CurrentState state);
        }

        /// <summary>An <see cref="IStateHandler"/> for operating over <see cref="CurrentState"/> instances configured as DFA states.</summary>
        private readonly struct DfaStateHandler : IStateHandler
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool StartsWithLineAnchor(SymbolicRegexBuilder<TSet> builder, ref CurrentState state) => state.DfaState(builder)!.StartsWithLineAnchor;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsNullableFor(SymbolicRegexBuilder<TSet> builder, ref CurrentState state, uint nextCharKind) => state.DfaState(builder)!.IsNullableFor(nextCharKind);

            /// <summary>Gets the preferred DFA state for nullability. In DFA mode this is just the state itself.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int ExtractNullableCoreStateId(SymbolicRegexMatcher<TSet> matcher, ref CurrentState state, ReadOnlySpan<char> input, int pos) => state.DfaStateId;

            /// <summary>Gets the length of any fixed-length marker that exists for this state, or -1 if there is none.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int FixedLength(SymbolicRegexBuilder<TSet> builder, ref CurrentState state, uint nextCharKind) => state.DfaState(builder)!.FixedLength(nextCharKind);

            /// <summary>Take the transition to the next DFA state.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool TakeTransition(SymbolicRegexBuilder<TSet> builder, ref CurrentState state, int mintermId)
            {
                Debug.Assert(state.DfaStateId > 0, $"Expected non-zero {nameof(state.DfaStateId)}.");
                Debug.Assert(state.NfaState is null, $"Expected null {nameof(state.NfaState)}.");
                Debug.Assert(builder._delta is not null);

                // Use the mintermId for the character being read to look up which state to transition to.
                // If that state has already been materialized, move to it, and we're done. If that state
                // hasn't been materialized, try to create it; if we can, move to it, and we're done.
                int dfaOffset = (state.DfaStateId << builder._mintermsLog) | mintermId;
                int nextStateId = builder._delta[dfaOffset];
                if (nextStateId > 0)
                {
                    // There was an existing DFA transition to some state. Move to it and
                    // return that we're still operating as a DFA and can keep going.
                    state.DfaStateId = nextStateId;
                    return true;
                }

                if (builder.TryCreateNewTransition(state.DfaState(builder)!, mintermId, dfaOffset, checkThreshold: true, out DfaMatchingState<TSet>? nextState))
                {
                    // We were able to create a new DFA transition to some state. Move to it and
                    // return that we're still operating as a DFA and can keep going.
                    state.DfaStateId = nextState.Id;
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Gets context independent state information:
            /// - whether this is an initial state
            /// - whether this is a dead-end state, meaning there are no transitions possible out of the state
            /// - whether this state is unconditionally nullable
            /// - whether this state may be contextually nullable
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static (bool IsInitial, bool IsDeadend, bool IsNullable, bool CanBeNullable) GetStateInfo(SymbolicRegexBuilder<TSet> builder, ref CurrentState state)
            {
                Debug.Assert(state.DfaStateId > 0);
                return builder.GetStateInfo(state.DfaStateId);
            }
        }

        /// <summary>An <see cref="IStateHandler"/> for operating over <see cref="CurrentState"/> instances configured as NFA states.</summary>
        private readonly struct NfaStateHandler : IStateHandler
        {
            /// <summary>Check if any underlying core state starts with a line anchor.</summary>
            public static bool StartsWithLineAnchor(SymbolicRegexBuilder<TSet> builder, ref CurrentState state)
            {
                foreach (ref KeyValuePair<int, int> nfaState in CollectionsMarshal.AsSpan(state.NfaState!.NfaStateSet.Values))
                {
                    if (builder.GetCoreState(nfaState.Key).StartsWithLineAnchor)
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>Check if any underlying core state is nullable in the context of the next character kind.</summary>
            public static bool IsNullableFor(SymbolicRegexBuilder<TSet> builder, ref CurrentState state, uint nextCharKind)
            {
                foreach (ref KeyValuePair<int, int> nfaState in CollectionsMarshal.AsSpan(state.NfaState!.NfaStateSet.Values))
                {
                    if (builder.GetCoreState(nfaState.Key).IsNullableFor(nextCharKind))
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>Gets the preferred DFA state for nullability. In DFA mode this is just the state itself.</summary>
            public static int ExtractNullableCoreStateId(SymbolicRegexMatcher<TSet> matcher, ref CurrentState state, ReadOnlySpan<char> input, int pos)
            {
                uint nextCharKind = matcher.GetCharKind(input, pos);
                foreach (ref KeyValuePair<int, int> nfaState in CollectionsMarshal.AsSpan(state.NfaState!.NfaStateSet.Values))
                {
                    DfaMatchingState<TSet> coreState = matcher._builder.GetCoreState(nfaState.Key);
                    if (coreState.IsNullableFor(nextCharKind))
                    {
                        return coreState.Id;
                    }
                }

                Debug.Fail("ExtractNullableCoreStateId should only be called in nullable state/context.");
                return -1;
            }

            /// <summary>Gets the length of any fixed-length marker that exists for this state, or -1 if there is none.</summary>
            public static int FixedLength(SymbolicRegexBuilder<TSet> builder, ref CurrentState state, uint nextCharKind)
            {
                foreach (ref KeyValuePair<int, int> nfaState in CollectionsMarshal.AsSpan(state.NfaState!.NfaStateSet.Values))
                {
                    DfaMatchingState<TSet> coreState = builder.GetCoreState(nfaState.Key);
                    if (coreState.IsNullableFor(nextCharKind))
                    {
                        return coreState.FixedLength(nextCharKind);
                    }
                }

                Debug.Fail("FixedLength should only be called in nullable state/context.");
                return -1;
            }

            /// <summary>Take the transition to the next NFA state.</summary>
            public static bool TakeTransition(SymbolicRegexBuilder<TSet> builder, ref CurrentState state, int mintermId)
            {
                Debug.Assert(state.DfaStateId < 0, $"Expected negative {nameof(state.DfaStateId)}.");
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
                static int[] GetNextStates(int sourceState, int mintermId, SymbolicRegexBuilder<TSet> builder)
                {
                    // Calculate the offset into the NFA transition table.
                    int nfaOffset = (sourceState << builder._mintermsLog) | mintermId;

                    // Get the next NFA state.
                    return builder._nfaDelta[nfaOffset] ?? builder.CreateNewNfaTransition(sourceState, mintermId, nfaOffset);
                }
            }

            /// <summary>
            /// Gets context independent state information:
            /// - whether this is an initial state
            /// - whether this is a dead-end state, meaning there are no transitions possible out of the state
            /// - whether this state is unconditionally nullable
            /// - whether this state may be contextually nullable
            /// </summary>
            /// <remarks>
            /// In NFA mode:
            /// - an empty set of states means that it is a dead end
            /// - no set of states qualifies as an initial state. This could be made more accurate, but with that the
            ///   matching logic would need to be updated to handle the fact that <see cref="InitialStateFindOptimizationsHandler"/>
            ///   can transition back to a DFA state.
            /// </remarks>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static (bool IsInitial, bool IsDeadend, bool IsNullable, bool CanBeNullable) GetStateInfo(SymbolicRegexBuilder<TSet> builder, ref CurrentState state) =>
                (false, state.NfaState!.NfaStateSet.Count == 0, IsNullable(builder, ref state), CanBeNullable(builder, ref state));

            /// <summary>Check if any underlying core state is unconditionally nullable.</summary>
            private static bool IsNullable(SymbolicRegexBuilder<TSet> builder, ref CurrentState state)
            {
                foreach (ref KeyValuePair<int, int> nfaState in CollectionsMarshal.AsSpan(state.NfaState!.NfaStateSet.Values))
                {
                    if (builder.GetStateInfo(builder.GetCoreStateId(nfaState.Key)).IsNullable)
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>Check if any underlying core state can be nullable in some context.</summary>
            private static bool CanBeNullable(SymbolicRegexBuilder<TSet> builder, ref CurrentState state)
            {
                foreach (ref KeyValuePair<int, int> nfaState in CollectionsMarshal.AsSpan(state.NfaState!.NfaStateSet.Values))
                {
                    if (builder.GetStateInfo(builder.GetCoreStateId(nfaState.Key)).CanBeNullable)
                    {
                        return true;
                    }
                }

                return false;
            }

#if DEBUG
            /// <summary>Undo a previous call to <see cref="TakeTransition"/>.</summary>
            public static void UndoTransition(ref CurrentState state)
            {
                Debug.Assert(state.DfaStateId < 0, $"Expected negative {nameof(state.DfaState)}.");
                Debug.Assert(state.NfaState is not null, $"Expected non-null {nameof(state.NfaState)}.");

                NfaMatchingState nfaState = state.NfaState!;

                // Swap the current active states set with the scratch set to undo a previous transition.
                SparseIntMap<int> nextStates = nfaState.NfaStateSet;
                SparseIntMap<int> sourceStates = nfaState.NfaStateSetScratch;
                nfaState.NfaStateSet = sourceStates;
                nfaState.NfaStateSetScratch = nextStates;

                // Sanity check: if there are any next states, then there must have been some source states.
                Debug.Assert(nextStates.Count == 0 || sourceStates.Count > 0);
            }

            /// <summary>Check if any underlying core state is unconditionally nullable.</summary>
            public static bool IsNullable(ref CurrentState state)
            {
                SymbolicRegexBuilder<TSet> builder = state.NfaState!.Builder;
                foreach (ref KeyValuePair<int, int> nfaState in CollectionsMarshal.AsSpan(state.NfaState!.NfaStateSet.Values))
                {
                    if (builder.GetCoreState(nfaState.Key).Node.IsNullable)
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>Check if any underlying core state can be nullable.</summary>
            public static bool CanBeNullable(ref CurrentState state)
            {
                SymbolicRegexBuilder<TSet> builder = state.NfaState!.Builder;
                foreach (ref KeyValuePair<int, int> nfaState in CollectionsMarshal.AsSpan(state.NfaState!.NfaStateSet.Values))
                {
                    if (builder.GetCoreState(nfaState.Key).Node.CanBeNullable)
                    {
                        return true;
                    }
                }

                return false;
            }
#endif
        }

        /// <summary>
        /// Interface for optimizations to accelerate search from initial states.
        /// </summary>
        private interface IInitialStateHandler
        {
            public static abstract bool TryFindNextStartingPosition(SymbolicRegexMatcher<TSet> matcher, ReadOnlySpan<char> input, ref CurrentState state, ref int pos);
        }

        /// <summary>
        /// No-op handler for when there are no initial state optimizations to apply.
        /// </summary>
        private readonly struct NoOptimizationsInitialStateHandler : IInitialStateHandler
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool TryFindNextStartingPosition(SymbolicRegexMatcher<TSet> matcher, ReadOnlySpan<char> input, ref CurrentState state, ref int pos)
            {
                // return true to indicate that the current position is a possible starting position
                return true;
            }
        }

        /// <summary>
        /// Handler for when a <see cref="RegexFindOptimizations"/> instance is available.
        /// </summary>
        private readonly struct InitialStateFindOptimizationsHandler : IInitialStateHandler
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool TryFindNextStartingPosition(SymbolicRegexMatcher<TSet> matcher, ReadOnlySpan<char> input, ref CurrentState state, ref int pos)
            {
                // Find the first position that matches with some likely character.
                if (!matcher._findOpts!.TryFindNextStartingPosition(input, ref pos, 0))
                {
                    // No match exists
                    return false;
                }

                // Update the starting state based on where TryFindNextStartingPosition moved us to.
                // As with the initial starting state, if it's a dead end, no match exists.
                state = new CurrentState(matcher._dotstarredInitialStates[matcher.GetCharKind(input, pos - 1)]);
                return true;
            }
        }

        /// <summary>
        /// Interface for evaluating nullability of states.
        /// </summary>
        private interface INullabilityHandler
        {
            public static abstract bool IsNullableAt<TStateHandler>(SymbolicRegexMatcher<TSet> matcher, ref CurrentState state, ReadOnlySpan<char> input, int pos, bool isNullable, bool canBeNullable)
                    where TStateHandler : struct, IStateHandler;
        }

        /// <summary>
        /// Specialized nullability handler for patterns without any anchors.
        /// </summary>
        private readonly struct NoAnchorsNullabilityHandler : INullabilityHandler
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsNullableAt<TStateHandler>(SymbolicRegexMatcher<TSet> matcher, ref CurrentState state, ReadOnlySpan<char> input, int pos, bool isNullable, bool canBeNullable)
                where TStateHandler : struct, IStateHandler
            {
                Debug.Assert(!matcher._pattern._info.ContainsSomeAnchor);
                return isNullable;
            }
        }

        /// <summary>
        /// Nullability handler that will work for any pattern.
        /// </summary>
        private readonly struct FullNullabilityHandler : INullabilityHandler
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsNullableAt<TStateHandler>(SymbolicRegexMatcher<TSet> matcher, ref CurrentState state, ReadOnlySpan<char> input, int pos, bool isNullable, bool canBeNullable)
                where TStateHandler : struct, IStateHandler
            {
                return isNullable || (canBeNullable && TStateHandler.IsNullableFor(matcher._builder, ref state, matcher.GetCharKind(input, pos)));
            }
        }
    }
}
