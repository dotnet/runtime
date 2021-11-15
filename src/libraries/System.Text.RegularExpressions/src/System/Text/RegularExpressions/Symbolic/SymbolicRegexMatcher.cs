// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Represents a regex matching engine that performs regex matching using symbolic derivatives.</summary>
    internal abstract class SymbolicRegexMatcher
    {
        /// <summary>Returns the next match index and length in the input string.</summary>
        /// <param name="isMatch">Whether to return once we know there's a match without determining where exactly it matched.</param>
        /// <param name="input">The input string.</param>
        /// <param name="startat">The start position in the input.</param>
        /// <param name="endat">The end position in the input.</param>
        public abstract SymbolicMatch FindMatch(bool isMatch, string input, int startat, int endat);

#if DEBUG
        /// <summary>Unwind the regex of the matcher and save the resulting state graph in DGML</summary>
        /// <param name="bound">roughly the maximum number of states, 0 means no bound</param>
        /// <param name="hideStateInfo">if true then hide state info</param>
        /// <param name="addDotStar">if true then pretend that there is a .* at the beginning</param>
        /// <param name="inReverse">if true then unwind the regex backwards (addDotStar is then ignored)</param>
        /// <param name="onlyDFAinfo">if true then compute and save only genral DFA info</param>
        /// <param name="writer">dgml output is written here</param>
        /// <param name="maxLabelLength">maximum length of labels in nodes anything over that length is indicated with .. </param>
        /// <param name="asNFA">if true creates NFA instead of DFA</param>
        public abstract void SaveDGML(TextWriter writer, int bound, bool hideStateInfo, bool addDotStar, bool inReverse, bool onlyDFAinfo, int maxLabelLength, bool asNFA);


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
        /// <summary>Maximum number of states before switching over to Antimirov mode.</summary>
        /// <remarks>
        /// "Brzozowski" is by default used for state graphs that represent the DFA nodes for the regex.
        /// In this mode, for the singular state we're currently in, we can evaluate the next character and determine
        /// the singular next state to be in. Some regular expressions, however, can result in really, really large DFA
        /// state graphs.  Instead of falling over with large representations, after this (somewhat arbitrary) threshold,
        /// the implementation switches to "Antimirov" mode.  In this mode, which can be thought of as NFA-based instead
        /// of DFA-based, we can be in any number of states at the same time, represented as a <see cref="SymbolicRegexNode{S}"/>
        /// that's the union of all such states; transitioning based on the next character is then handled by finding
        /// all possible states we might transition to from each of the states in the current set, and producing a new set
        /// that's the union of all of those.  The matching engine switches dynamically from Brzozowski to Antimirov once
        /// it trips over this threshold in the size of the state graph, which may be produced lazily.
        /// </remarks>
        private const int AntimirovThreshold = 10_000;

        /// <summary>Wiggle room around the exact size of the state graph before we switch to Antimirov.</summary>
        /// <remarks>
        /// The inner loop of the matching engine wants to be as streamlined as possible, and in Brzozowski mode
        /// having to check at every iteration whether we need to switch to Antimirov is a perf bottleneck.  As such,
        /// the inner loop is allowed to run for up to this many transitions without regard for the size of the
        /// graph, which could cause <see cref="AntimirovThreshold"/> to be exceeded by this limit.  Once
        /// we've hit <see cref="AntimirovThresholdLeeway"/> number of transitions in the inner loop, we
        /// force ourselves back out to the outerloop, where we can check the graph size and other things like timeouts.
        /// </remarks>
        private const int AntimirovThresholdLeeway = 1_000;

        /// <summary>Sentinel value used internally by the matcher to indicate no match exists.</summary>
        private const int NoMatchExists = -2;

        /// <summary>Builder used to create <see cref="SymbolicRegexNode{S}"/>s while matching.</summary>
        /// <remarks>
        /// The builder servers two purposes:
        /// 1. For Brzozowski, we build up the DFA state space lazily, which means we need to be able to
        ///    produce new <see cref="SymbolicRegexNode{S}"/>s as we match.
        /// 2. For Antimirov, currently the list of states we're currently in is represented as a <see cref="SymbolicRegexNode{S}"/>
        ///    that's a union of all current states.  Augmenting that list requires building new nodes.
        /// The builder maintains a cache of nodes, and requests for it to make new ones might return existing ones from the cache.
        /// The separation of a matcher and a builder is somewhat arbitrary; they could potentially be combined.
        /// </remarks>
        internal readonly SymbolicRegexBuilder<TSetType> _builder;

        /// <summary>Maps each character into a partition id in the range 0..K-1.</summary>
        private readonly MintermClassifier _partitions;

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

        /// <summary>Classifier used to say whether a particular character can start a match for <see cref="_pattern"/>.</summary>
        internal readonly BooleanClassifier _startSetClassifier;

        /// <summary>Predicate over characters that make some progress</summary>
        private readonly TSetType _startSet;

        /// <summary>Maximum allowed size of <see cref="_startSetArray"/>.</summary>
        private const int StartSetArrayMaxSize = 5;

        /// <summary>String of at most <see cref="StartSetArrayMaxSize"/> many characters</summary>
        private readonly char[] _startSetArray;

        /// <summary>Number of elements in <see cref="_startSetClassifier"/></summary>
        private readonly int _startSetSize;

        /// <summary>If nonempty then <see cref="_pattern"/> has that fixed prefix</summary>
        private readonly string _prefix;

        /// <summary>Non-null when <see cref="_prefix"/> is nonempty</summary>
        private readonly RegexBoyerMoore? _prefixBoyerMoore;

        /// <summary>If true then the fixed prefix of <see cref="_pattern"/> is idependent of case</summary>
        private readonly bool _isPrefixCaseInsensitive;

        /// <summary>Cached skip states from the initial state of <see cref="_dotStarredPattern"/> for the 5 possible previous character kinds.</summary>
        private readonly DfaMatchingState<TSetType>?[] _prefixSkipStates = new DfaMatchingState<TSetType>[CharKind.CharKindCount];
        /// <summary>Cached skip states from the initial state of Ar for the 5 possible previous character kinds.</summary>
        private readonly DfaMatchingState<TSetType>?[] _reversePrefixSkipStates = new DfaMatchingState<TSetType>[CharKind.CharKindCount];

        private readonly string _reversePrefix;

        private readonly DfaMatchingState<TSetType>[] _initialStates = new DfaMatchingState<TSetType>[CharKind.CharKindCount];
        private readonly DfaMatchingState<TSetType>[] _dotstarredInitialStates = new DfaMatchingState<TSetType>[CharKind.CharKindCount];
        private readonly DfaMatchingState<TSetType>[] _reverseInitialStates = new DfaMatchingState<TSetType>[CharKind.CharKindCount];

        private readonly uint[] _asciiCharKinds = new uint[128];

        internal readonly CultureInfo _culture;

        private DfaMatchingState<TSetType> GetSkipState(uint prevCharKind) =>
            Volatile.Read(ref _prefixSkipStates[prevCharKind]) ??
            Interlocked.CompareExchange(ref _prefixSkipStates[prevCharKind], DeltaPlus<BrzozowskiTransition>(_prefix, _dotstarredInitialStates[prevCharKind]), null) ??
            _prefixSkipStates[prevCharKind]!;

        private DfaMatchingState<TSetType> GetReverseSkipState(uint prevCharKind) =>
            Volatile.Read(ref _reversePrefixSkipStates[prevCharKind]) ??
            Interlocked.CompareExchange(ref _reversePrefixSkipStates[prevCharKind], DeltaPlus<BrzozowskiTransition>(_reversePrefix, _reverseInitialStates[prevCharKind]), null) ??
            _reversePrefixSkipStates[prevCharKind]!;

        /// <summary>Get the minterm of <paramref name="c"/>.</summary>
        /// <param name="c">character code</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TSetType GetMinterm(int c)
        {
            Debug.Assert(_builder._minterms is not null);
            return _builder._minterms[_partitions.GetMintermID(c)];
        }

        /// <summary>Constructs matcher for given symbolic regex.</summary>
        internal SymbolicRegexMatcher(SymbolicRegexNode<TSetType> sr, CharSetSolver css, BDD[] minterms, TimeSpan matchTimeout, CultureInfo culture)
        {
            _pattern = sr;
            _builder = sr._builder;

            _checkTimeout = Regex.InfiniteMatchTimeout != matchTimeout;
            _timeout = (int)(matchTimeout.TotalMilliseconds + 0.5); // Round up, so it will be at least 1ms
            _culture = culture;

            Debug.Assert(_builder._solver is BV64Algebra or BVAlgebra or CharSetSolver, $"Unsupported algebra: {_builder._solver}");
            _partitions = _builder._solver switch
            {
                BV64Algebra bv64 => bv64._classifier,
                BVAlgebra bv => bv._classifier,
                _ => new MintermClassifier((CharSetSolver)(object)_builder._solver, minterms),
            };

            _dotStarredPattern = _builder.MkConcat(_builder._anyStar, _pattern);
            _reversePattern = _pattern.Reverse();
            ConfigureRegexes();

            _startSet = _pattern.GetStartSet();
            if (!_builder._solver.IsSatisfiable(_startSet) || _pattern.CanBeNullable)
            {
                // If the startset is empty make it full instead by including all characters
                // this is to ensure that startset is nonempty -- as an invariant assumed by operations using it
                //
                // Also, if A can be nullable then effectively disable use of startset by making it true
                // because it may force search of next character in startset and fail to recognize an empty match
                // because (by definition) an empty match has no start character.
                //
                // For example (this is also a unit test):
                // for pattern "\B\W*?" or "\B\W*" or "\B\W?" and input "e.g:abc" there is an empty match in position 5
                // but startset \W will force search beyond position 5 and fails to find that match
                _startSet = _builder._solver.True;
            }

            _startSetSize = (int)_builder._solver.ComputeDomainSize(_startSet);

            BDD startbdd = _builder._solver.ConvertToCharSet(css, _startSet);
            _startSetClassifier = new BooleanClassifier(css, startbdd);

            //store the start characters in the A_startset_array if there are not too many characters
            _startSetArray = _startSetSize <= StartSetArrayMaxSize ?
                new List<char>(css.GenerateAllCharacters(startbdd)).ToArray() :
                Array.Empty<char>();

            _prefix = _pattern.GetFixedPrefix(css, culture.Name, out _isPrefixCaseInsensitive);
            _reversePrefix = _reversePattern.GetFixedPrefix(css, culture.Name, out _);

            _prefixBoyerMoore = InitializePrefixBoyerMoore();

            if (_pattern._info.ContainsSomeAnchor)
            {
                for (int i = 0; i < 128; i++)
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

                    _asciiCharKinds[i] = _builder._solver.And(GetMinterm(i), predicate2).Equals(_builder._solver.False) ? 0 : charKind;
                }
            }
        }

        private RegexBoyerMoore? InitializePrefixBoyerMoore()
        {
            if (_prefix != string.Empty && _prefix.Length <= RegexBoyerMoore.MaxLimit && _prefix.Length > 1)
            {
                // RegexBoyerMoore expects the prefix to be lower case when case is ignored.
                // Use the culture of the matcher.
                string prefix = _isPrefixCaseInsensitive ? _prefix.ToLower(_culture) : _prefix;
                return new RegexBoyerMoore(prefix, _isPrefixCaseInsensitive, rightToLeft: false, _culture);
            }

            return null;
        }

        private void ConfigureRegexes()
        {
            void Configure(uint i)
            {
                _initialStates[i] = _builder.MkState(_pattern, i);

                // Used to detect if initial state was reentered, then startset can be triggered
                // but observe that the behavior from the state may ultimately depend on the previous
                // input char e.g. possibly causing nullability of \b or \B or of a start-of-line anchor,
                // in that sense there can be several "versions" (not more than StateCount) of the initial state.
                _dotstarredInitialStates[i] = _builder.MkState(_dotStarredPattern, i);
                _dotstarredInitialStates[i].IsInitialState = true;

                _reverseInitialStates[i] = _builder.MkState(_reversePattern, i);
            }

            // Create initial states for A, A1 and Ar.
            if (!_pattern._info.ContainsSomeAnchor)
            {
                // Only the default previous character kind 0 is ever going to be used for all initial states.
                // _A1q0[0] is recognized as special initial state.
                // This information is used for search optimization based on start set and prefix of A.
                Configure(0);
            }
            else
            {
                for (uint i = 0; i < CharKind.CharKindCount; i++)
                {
                    Configure(i);
                }
            }
        }

        /// <summary>Return the state after the given <paramref name="pattern"/> string from the given state <paramref name="state"/>.</summary>
        private DfaMatchingState<TSetType> DeltaPlus<TTransition>(string pattern, DfaMatchingState<TSetType> state) where TTransition : struct, ITransition
        {
            for (int i = 0; i < pattern.Length; i++)
            {
                state = Delta<TTransition>(pattern, i, state);
            }

            return state;
        }

        /// <summary>Interface for transitions used by the <see cref="Delta"/> method.</summary>
        private interface ITransition
        {
            /// <summary>Find the next state given the current state and next character.</summary>
            DfaMatchingState<TSetType> TakeTransition(SymbolicRegexMatcher<TSetType> matcher, DfaMatchingState<TSetType> currentState, int mintermId, TSetType minterm);
        }

        /// <summary>Compute the target state for the source state and input[i] character.</summary>
        /// <param name="input">input string</param>
        /// <param name="i">The index into <paramref name="input"/> at which the target character lives.</param>
        /// <param name="sourceState">The source state</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DfaMatchingState<TSetType> Delta<TTransition>(string input, int i, DfaMatchingState<TSetType> sourceState) where TTransition : struct, ITransition
        {
            TSetType[]? minterms = _builder._minterms;
            Debug.Assert(minterms is not null);

            int c = input[i];

            int mintermId = c == '\n' && i == input.Length - 1 && sourceState.StartsWithLineAnchor ?
                minterms.Length : // mintermId = minterms.Length represents \Z (last \n)
                _partitions.GetMintermID(c);

            TSetType minterm = (uint)mintermId < minterms.Length ?
                minterms[mintermId] :
                _builder._solver.False; // minterm=False represents \Z

            return default(TTransition).TakeTransition(this, sourceState, mintermId, minterm);
        }

        /// <summary>Transition for Brzozowski-style derivatives (i.e. a DFA).</summary>
        private readonly struct BrzozowskiTransition : ITransition
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DfaMatchingState<TSetType> TakeTransition(
                SymbolicRegexMatcher<TSetType> matcher, DfaMatchingState<TSetType> currentState, int mintermId, TSetType minterm)
            {
                SymbolicRegexBuilder<TSetType> builder = matcher._builder;
                Debug.Assert(builder._delta is not null);

                int offset = (currentState.Id << builder._mintermsCount) | mintermId;
                return Volatile.Read(ref builder._delta[offset]) ?? matcher.CreateNewTransition(currentState, minterm, offset);
            }
        }

        /// <summary>Transition for Antimirov-style derivatives (i.e. an NFA).</summary>
        private readonly struct AntimirovTransition : ITransition
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DfaMatchingState<TSetType> TakeTransition(
                SymbolicRegexMatcher<TSetType> matcher, DfaMatchingState<TSetType> currentStates, int mintermId, TSetType minterm)
            {
                if (currentStates.Node.Kind != SymbolicRegexKind.Or)
                {
                    // Fall back to Brzozowski when the state is not a disjunction.
                    return default(BrzozowskiTransition).TakeTransition(matcher, currentStates, mintermId, minterm);
                }

                SymbolicRegexBuilder<TSetType> builder = matcher._builder;
                Debug.Assert(builder._delta is not null);

                SymbolicRegexNode<TSetType> union = builder._nothing;
                uint kind = 0;

                // Produce the new list of states from the current list, considering transitions from members one at a time.
                Debug.Assert(currentStates.Node._alts is not null);
                foreach (SymbolicRegexNode<TSetType> oneState in currentStates.Node._alts)
                {
                    DfaMatchingState<TSetType> nextStates = builder.MkState(oneState, currentStates.PrevCharKind);

                    int offset = (nextStates.Id << builder._mintermsCount) | mintermId;
                    DfaMatchingState<TSetType> p = Volatile.Read(ref builder._delta[offset]) ?? matcher.CreateNewTransition(nextStates, minterm, offset);

                    // Observe that if p.Node is an Or it will be flattened.
                    union = builder.MkOr2(union, p.Node);

                    // kind is just the kind of the partition.
                    kind = p.PrevCharKind;
                }

                return builder.MkState(union, kind, true);
            }
        }

        /// <summary>Critical region for defining a new transition</summary>
        private DfaMatchingState<TSetType> CreateNewTransition(DfaMatchingState<TSetType> state, TSetType minterm, int offset)
        {
            Debug.Assert(_builder._delta is not null);
            lock (this)
            {
                // check if meanwhile delta[offset] has become defined possibly by another thread
                DfaMatchingState<TSetType> p = _builder._delta[offset];
                if (p is null)
                {
                    // this is the only place in code where the Next method is called in the matcher
                    _builder._delta[offset] = p = state.Next(minterm);

                    // switch to antimirov mode if the maximum bound has been reached
                    if (p.Id == AntimirovThreshold)
                    {
                        _builder._antimirov = true;
                    }
                }

                return p;
            }
        }

        private void DoCheckTimeout(int timeoutOccursAt)
        {
            // This code is identical to RegexRunner.DoCheckTimeout(),
            // with the exception of check skipping. RegexRunner calls
            // DoCheckTimeout potentially on every iteration of a loop,
            // whereas this calls it only once per transition.

            int currentMillis = Environment.TickCount;

            if (currentMillis < timeoutOccursAt)
                return;

            if (0 > timeoutOccursAt && 0 < currentMillis)
                return;

            //regex pattern is in general not available in srm and
            //the input is not available here but could be passed as argument to DoCheckTimeout
            throw new RegexMatchTimeoutException(string.Empty, string.Empty, TimeSpan.FromMilliseconds(_timeout));
        }

        /// <summary>Find a match.</summary>
        /// <param name="isMatch">Whether to return once we know there's a match without determining where exactly it matched.</param>
        /// <param name="input">input string</param>
        /// <param name="startat">the position to start search in the input string</param>
        /// <param name="k">the next position after the end position in the input</param>
        public override SymbolicMatch FindMatch(bool isMatch, string input, int startat, int k)
        {
            int timeoutOccursAt = 0;
            if (_checkTimeout)
            {
                // Using Environment.TickCount for efficiency instead of Stopwatch -- as in the non-DFA case.
                timeoutOccursAt = Environment.TickCount + (int)(_timeout + 0.5);
            }

            if (startat == k)
            {
                //covers the special case when the remaining input suffix
                //where a match is sought is empty (for example when the input is empty)
                //in this case the only possible match is an empty match
                uint prevKind = GetCharKind(input, startat - 1);
                uint nextKind = GetCharKind(input, startat);

                bool emptyMatchExists = _pattern.IsNullableFor(CharKind.Context(prevKind, nextKind));
                return
                    !emptyMatchExists ? SymbolicMatch.NoMatch :
                    new SymbolicMatch(startat, 0);
            }

            // Find the first accepting state. Initial start position in the input is i == 0.
            int i = startat;

            // May return -1 as a legitimate value when the initial state is nullable and startat == 0.
            // Returns NoMatchExists when there is no match.
            i = FindFinalStatePosition(input, k, i, timeoutOccursAt, out int i_q0_A1, out int watchdog);

            if (i == NoMatchExists)
            {
                return SymbolicMatch.NoMatch;
            }

            if (isMatch)
            {
                // this means success -- the original call was IsMatch
                return SymbolicMatch.QuickMatch;
            }

            int i_start;
            int i_end;

            if (watchdog >= 0)
            {
                i_start = i - watchdog + 1;
                i_end = i;
            }
            else
            {
                if (i < startat)
                {
                    Debug.Assert(i == startat - 1);
                    i_start = startat;
                }
                else
                {
                    // Walk in reverse to locate the start position of the match
                    i_start = FindStartPosition(input, i, i_q0_A1);
                }

                i_end = FindEndPosition(input, k, i_start);
            }

            return new SymbolicMatch(i_start, i_end + 1 - i_start);
        }

        /// <summary>Find match end position using A, end position is known to exist.</summary>
        /// <param name="input">input array</param>
        /// <param name="i">inclusive start position</param>
        /// <param name="exclusiveEnd">exclusive end position</param>
        /// <returns></returns>
        private int FindEndPosition(string input, int exclusiveEnd, int i)
        {
            int i_end = exclusiveEnd;

            // Pick the correct start state based on previous character kind.
            uint prevCharKind = GetCharKind(input, i - 1);
            DfaMatchingState<TSetType> state = _initialStates[prevCharKind];

            if (state.IsNullable(GetCharKind(input, i)))
            {
                // Empty match exists because the initial state is accepting.
                i_end = i - 1;

                // Stop here if q is lazy.
                if (state.IsLazy)
                {
                    return i_end;
                }
            }

            while (i < exclusiveEnd)
            {
                int j = Math.Min(exclusiveEnd, i + AntimirovThresholdLeeway);
                bool done = _builder._antimirov ?
                    FindEndPositionDeltas<AntimirovTransition>(input, ref i, j, ref state, ref i_end) :
                    FindEndPositionDeltas<BrzozowskiTransition>(input, ref i, j, ref state, ref i_end);

                if (done)
                {
                    break;
                }
            }

            Debug.Assert(i_end != exclusiveEnd);
            return i_end;
        }

        // Inner loop for FindEndPosition parameterized by an ITransition type.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool FindEndPositionDeltas<TTransition>(string input, ref int i, int j, ref DfaMatchingState<TSetType> q, ref int i_end) where TTransition : struct, ITransition
        {
            do
            {
                q = Delta<TTransition>(input, i, q);

                if (q.IsNullable(GetCharKind(input, i + 1)))
                {
                    // Accepting state has been reached. Record the position.
                    i_end = i;

                    // Stop here if q is lazy.
                    if (q.IsLazy)
                    {
                        return true;
                    }
                }
                else if (q.IsDeadend)
                {
                    // Nonaccepting sink state (deadend) has been reached in A.
                    // So the match ended when the last i_end was updated.
                    return true;
                }

                i++;
            }
            while (i < j);

            return false;
        }

        /// <summary>Walk back in reverse using Ar to find the start position of match, start position is known to exist.</summary>
        /// <param name="input">the input string</param>
        /// <param name="i">position to start walking back from, i points at the last character of the match</param>
        /// <param name="match_start_boundary">do not pass this boundary when walking back</param>
        /// <returns></returns>
        private int FindStartPosition(string input, int i, int match_start_boundary)
        {
            // Fetch the correct start state for Ar.
            // This depends on previous character --- which, because going backwards, is character number i+1.
            uint prevKind = GetCharKind(input, i + 1);
            DfaMatchingState<TSetType> q = _reverseInitialStates[prevKind];

            // Ar may have a fixed prefix sequence
            if (_reversePrefix.Length > 0)
            {
                //skip past the prefix portion of Ar
                q = GetReverseSkipState(prevKind);
                i -= _reversePrefix.Length;
            }

            if (i == -1)
            {
                Debug.Assert(q.IsNullable(GetCharKind(input, i)), "we reached the beginning of the input, thus the state q must be accepting");
                return 0;
            }

            int last_start = -1;
            if (q.IsNullable(GetCharKind(input, i)))
            {
                // The whole prefix of Ar was in reverse a prefix of A,
                // for example when the pattern of A is concrete word such as "abc"
                last_start = i + 1;
            }

            //walk back to the accepting state of Ar
            while (i >= match_start_boundary)
            {
                int j = Math.Max(match_start_boundary, i - AntimirovThresholdLeeway);
                bool done = _builder._antimirov ?
                    FindStartPositionDeltas<AntimirovTransition>(input, ref i, j, ref q, ref last_start) :
                    FindStartPositionDeltas<BrzozowskiTransition>(input, ref i, j, ref q, ref last_start);

                if (done)
                {
                    break;
                }
            }

            Debug.Assert(last_start != -1);
            return last_start;
        }

        // Inner loop for FindStartPosition parameterized by an ITransition type.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool FindStartPositionDeltas<TTransition>(string input, ref int i, int j, ref DfaMatchingState<TSetType> q, ref int last_start) where TTransition : struct, ITransition
        {
            do
            {
                q = Delta<TTransition>(input, i, q);

                // Reached a deadend state, thus the earliest match start point must have occurred already.
                if (q.IsNothing)
                {
                    return true;
                }

                if (q.IsNullable(GetCharKind(input, i - 1)))
                {
                    // Earliest start point so far. This must happen at some point
                    // or else A1 would not have reached a final state after match_start_boundary.
                    last_start = i;
                }

                i -= 1;
            }
            while (i > j);

            return false;
        }

        /// <summary>Returns NoMatchExists if no match exists. Returns -1 when i=0 and the initial state is nullable.</summary>
        /// <param name="input">given input string</param>
        /// <param name="k">input length or bounded input length</param>
        /// <param name="i">start position</param>
        /// <param name="timeoutOccursAt">The time at which timeout occurs, if timeouts are being checked.</param>
        /// <param name="initialStateIndex">last position the initial state of <see cref="_dotStarredPattern"/> was visited</param>
        /// <param name="watchdog">length of match when positive</param>
        private int FindFinalStatePosition(string input, int k, int i, int timeoutOccursAt, out int initialStateIndex, out int watchdog)
        {
            // Get the correct start state of A1, which in general depends on the previous character kind in the input.
            uint prevCharKindId = GetCharKind(input, i - 1);
            DfaMatchingState<TSetType> q = _dotstarredInitialStates[prevCharKindId];
            initialStateIndex = i;

            if (q.IsNothing)
            {
                // If q is nothing then it is a deadend from the beginning this happens for example when the original
                // regex started with start anchor and prevCharKindId is not Start
                watchdog = -1;
                return NoMatchExists;
            }

            if (q.IsNullable(GetCharKind(input, i)))
            {
                // The initial state is nullable in this context so at least an empty match exists.
                // The last position of the match is i-1 because the match is empty.
                // This value is -1 if i == 0.
                watchdog = -1;
                return i - 1;
            }

            watchdog = -1;

            // Search for a match end position within input[i..k-1]
            while (i < k)
            {
                if (q.IsInitialState)
                {
                    // i_q0_A1 is the most recent position in the input when A1 is in the initial state
                    initialStateIndex = i;

                    if (_prefixBoyerMoore != null)
                    {
                        // Stay in the initial state if the prefix does not match.
                        // Thus advance the current position to the first position where the prefix does match.
                        i = _prefixBoyerMoore.Scan(input, i, 0, input.Length);

                        if (i == -1) // Scan returns -1 when a matching position does not exist
                        {
                            watchdog = -1;
                            return -2;
                        }

                        // Compute the end state for the A prefix.
                        // Skip directly to the resulting state
                        //  --- i.e. do the loop ---
                        // for (int j = 0; j < prefix.Length; j++)
                        //     q = Delta(prefix[j], q, out regex);
                        //  ---
                        q = GetSkipState(q.PrevCharKind);

                        // skip the prefix
                        i += _prefix.Length;

                        // here i points at the next character (the character immediately following the prefix)
                        if (q.IsNullable(GetCharKind(input, i)))
                        {
                            // Return the last position of the match
                            watchdog = q.WatchDog;
                            return i - 1;
                        }

                        if (i == k)
                        {
                            // no match was found
                            return -2;
                        }
                    }
                    else
                    {
                        // we are still in the initial state, when the prefix is empty
                        // find the first position i that matches with some character in the start set
                        i = IndexOfStartSet(input, i);

                        if (i == -1)
                        {
                            // no match was found
                            return NoMatchExists;
                        }

                        initialStateIndex = i;

                        // the start state must be updated
                        // to reflect the kind of the previous character
                        // when anchors are not used, q will remain the same state
                        q = _dotstarredInitialStates[GetCharKind(input, i - 1)];
                        if (q.IsNothing)
                        {
                            return NoMatchExists;
                        }
                    }
                }

                int result;
                int j = Math.Min(k, i + AntimirovThresholdLeeway);
                bool done = _builder._antimirov ?
                    FindFinalStatePositionDeltas<AntimirovTransition>(input, j, ref i, ref q, ref watchdog, out result) :
                    FindFinalStatePositionDeltas<BrzozowskiTransition>(input, j, ref i, ref q, ref watchdog, out result);

                if (done)
                {
                    return result;
                }

                if (_checkTimeout)
                {
                    DoCheckTimeout(timeoutOccursAt);
                }
            }

            //no match was found
            return NoMatchExists;
        }

        /// <summary>Inner loop for FindFinalStatePosition parameterized by an ITransition type.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool FindFinalStatePositionDeltas<TTransition>(string input, int j, ref int i, ref DfaMatchingState<TSetType> q, ref int watchdog, out int result) where TTransition : struct, ITransition
        {
            do
            {
                // Make the transition based on input[i].
                q = Delta<TTransition>(input, i, q);

                if (q.IsNullable(GetCharKind(input, i + 1)))
                {
                    watchdog = q.WatchDog;
                    result = i;
                    return true;
                }

                if (q.IsNothing)
                {
                    // q is a deadend state so any further search is meaningless
                    result = NoMatchExists;
                    return true;
                }

                // continue from the next character
                i++;
            }
            while (i < j && !q.IsInitialState);

            result = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint GetCharKind(string input, int i)
        {
            if (!_pattern._info.ContainsSomeAnchor)
            {
                // The previous character kind is irrelevant when anchors are not used.
                return CharKind.General;
            }

            if (i == -1 || i == input.Length)
            {
                return CharKind.StartStop;
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
                nextChar < asciiCharKinds.Length ? asciiCharKinds[nextChar] :
                _builder._solver.And(GetMinterm(nextChar), _builder._wordLetterPredicateForAnchors).Equals(_builder._solver.False) ? 0 : //apply the wordletter predicate to compute the kind of the next character
                CharKind.WordLetter;
        }

        /// <summary>
        /// Find first occurrence of startset element in input starting from index i.
        /// Startset here is assumed to consist of a few characters.
        /// </summary>
        /// <param name="input">input string to search in</param>
        /// <param name="i">the start index in input to search from</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int IndexOfStartSet(string input, int i)
        {
            if (_startSetSize <= StartSetArrayMaxSize)
            {
                return input.IndexOfAny(_startSetArray, i);
            }

            for (int j = i; j < input.Length; j++)
            {
                if (_startSetClassifier.IsTrue(input[j]))
                {
                    return j;
                }
            }

            return -1;
        }

#if DEBUG
        public override void SaveDGML(TextWriter writer, int bound, bool hideStateInfo, bool addDotStar, bool inReverse, bool onlyDFAinfo, int maxLabelLength, bool asNFA)
        {
            var graph = new DGML.RegexAutomaton<TSetType>(this, bound, addDotStar, inReverse, asNFA);
            var dgml = new DGML.DgmlWriter(writer, hideStateInfo, maxLabelLength, onlyDFAinfo);
            dgml.Write(graph);
        }

        public override IEnumerable<string> GenerateRandomMembers(int k, int randomseed, bool negative) =>
            new SymbolicRegexSampler<TSetType>(_pattern, randomseed, negative).GenerateRandomMembers(k);
#endif
    }
}
