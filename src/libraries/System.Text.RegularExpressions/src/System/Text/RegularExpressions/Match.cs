// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;

namespace System.Text.RegularExpressions
{
    /// <summary>Represents the results from a single regular expression match.</summary>
    /// <remarks>
    /// <para>
    /// The <see cref="Match"/> class is immutable and has no public constructor. An instance of the
    /// <see cref="Match"/> class is returned by the <see cref="Regex.Match(string)"/> method and
    /// represents the first pattern match in a string. Subsequent matches are represented by
    /// <see cref="Match"/> objects returned by the <see cref="Match.NextMatch"/> method. In addition, a
    /// <see cref="MatchCollection"/> object that consists of zero, one, or more <see cref="Match"/>
    /// objects is returned by the <see cref="Regex.Matches(string)"/> method.
    /// </para>
    /// <para>
    /// If the <see cref="Regex.Matches(string)"/> method fails to match a regular expression pattern in
    /// an input string, it returns an empty <see cref="MatchCollection"/> object. You can then use a
    /// <see langword="foreach"/> construct to iterate over the collection.
    /// </para>
    /// <para>
    /// If the <see cref="Regex.Match(string)"/> method fails to match the regular expression pattern, it
    /// returns a <see cref="Match"/> object that is equal to <see cref="Match.Empty"/>. You can use the
    /// <see cref="Group.Success"/> property to determine whether the match was successful.
    /// </para>
    /// <para>
    /// If a pattern match is successful, the <see cref="Capture.Value"/> property contains the matched
    /// substring, the <see cref="Capture.Index"/> property indicates the zero-based starting position of
    /// the matched substring in the input string, and the <see cref="Capture.Length"/> property indicates
    /// the length of the matched substring in the input string.
    /// </para>
    /// <para>
    /// Because a single match can involve multiple capturing groups, <see cref="Match"/> has a
    /// <see cref="Groups"/> property that returns the <see cref="GroupCollection"/>. The
    /// <see cref="Match"/> instance itself is equivalent to the first object in the collection, at
    /// <c>Match.Groups[0]</c>, which represents the entire match.
    /// </para>
    /// </remarks>
    public class Match : Group
    {
        internal GroupCollection? _groupcoll;

        // input to the match
        internal readonly Regex? _regex;
        internal int _textbeg; // 0 while the match is being generated, then bumped in Tidy to the actual beginning
        internal int _textpos; // position offset from beginning
        internal int _textend; // input length while the match is being generated, them bumped in Tidy to be the end

        // output from the match
        internal readonly int[][] _matches;
        internal readonly int[] _matchcount;
        internal bool _balancing;        // whether we've done any balancing with this match.  If we
                                         // have done balancing, we'll need to do extra work in Tidy().

        internal Match(Regex? regex, int capcount, string? text, int textLength) :
            base(text, new int[2], 0, "0")
        {
            _regex = regex;
            _matchcount = new int[capcount];
            _matches = new int[capcount][];
            _matches[0] = _caps;
            _textend = textLength;
            _balancing = false;
        }

        /// <summary>Gets the empty match. All failed matches return this empty match.</summary>
        /// <value>An empty match.</value>
        /// <remarks>
        /// This property should not be used to determine if a match is successful. Instead, use the
        /// <see cref="Group.Success"/> property.
        /// </remarks>
        public static Match Empty { get; } = new Match(null, 1, string.Empty, 0);

        internal void Reset(string? text, int textLength)
        {
            Text = text;
            _textbeg = 0;
            _textend = textLength;

            int[] matchcount = _matchcount;
            for (int i = 0; i < matchcount.Length; i++)
            {
                matchcount[i] = 0;
            }

            _balancing = false;
            _groupcoll?.Reset();
        }

        /// <summary>
        /// Returns <see langword="true"/> if this object represents a successful match, and <see langword="false"/> otherwise.
        /// </summary>
        /// <remarks>
        /// The main difference between the public <see cref="Group.Success"/> property and this one, is that <see cref="Group.Success"/> requires
        /// for a <see cref="Match"/> to call <see cref="Tidy"/> first, in order to report the correct value, while this API will return
        /// the correct value right after a Match gets calculated, meaning that it will return <see langword="true"/> right after <see cref="RegexRunner.Capture(int, int, int)"/>
        /// </remarks>
        internal bool FoundMatch => _matchcount[0] > 0;

        /// <summary>
        /// Gets a collection of groups matched by the regular expression.
        /// </summary>
        /// <value>The character groups matched by the pattern.</value>
        /// <remarks>
        /// <para>
        /// A regular expression pattern can include subexpressions, which are defined by enclosing a
        /// portion of the regular expression pattern in parentheses. Every such subexpression forms a
        /// group. The <see cref="Groups"/> property provides access to information about those
        /// subexpression matches. For example, the regular expression pattern <c>(\d{3})-(\d{3}-\d{4})</c>,
        /// which matches North American telephone numbers, has two subexpressions. The first consists of
        /// the area code, which composes the first three digits of the telephone number. This group is
        /// captured by the first set of parentheses, <c>(\d{3})</c>. The second consists of the
        /// individual telephone number, which composes the last seven digits of the telephone number.
        /// This group is captured by the second set of parentheses, <c>(\d{3}-\d{4})</c>.
        /// </para>
        /// <para>
        /// The <see cref="GroupCollection"/> object returned by the <see cref="Groups"/> property always
        /// has at least one member. If the regular expression engine cannot find any matches in a
        /// particular input string, the single <see cref="Group"/> object in the collection has its
        /// <c>Group.Success</c> property set to <see langword="false"/> and its <c>Group.Value</c>
        /// property set to <see cref="string.Empty"/>.
        /// </para>
        /// <para>
        /// If the collection has more than one member, the first item in the collection is the same as
        /// the <see cref="Match"/> object (the entire match). Each subsequent member represents a
        /// captured group, if the regular expression includes capturing groups. Groups can be accessed by
        /// their integer index or, for named groups, by name.
        /// </para>
        /// </remarks>
        public virtual GroupCollection Groups => _groupcoll ??= new GroupCollection(this, null);

        /// <summary>
        /// Returns a new <see cref="Match"/> object with the results for the next match, starting at the
        /// position at which the last match ended (at the character after the last matched character).
        /// </summary>
        /// <returns>The next regular expression match.</returns>
        /// <remarks>
        /// <para>
        /// This method is similar to calling <see cref="Regex.Match(string, int)"/> again and passing
        /// <c>Index + Length</c> as the new starting position.
        /// </para>
        /// <para>
        /// This method does not modify the current instance. Instead, it returns a new
        /// <see cref="Match"/> object that contains information about the next match.
        /// </para>
        /// <para>
        /// Attempting to retrieve the next match may throw a <see cref="RegexMatchTimeoutException"/> if
        /// a time-out value for matching operations is in effect and the attempt to find the next match
        /// exceeds that time-out interval.
        /// </para>
        /// <para>
        /// When attempting to match a regular expression pattern against an empty match, the regular
        /// expression engine will advance one character in the input string before retrying the match, to
        /// avoid an infinite loop. Callers should not assume that each match will advance by at least one
        /// position, since zero-width matches are possible.
        /// </para>
        /// </remarks>
        public Match NextMatch()
        {
            Regex? r = _regex;
            Debug.Assert(Text != null);
            return r != null ?
                r.RunSingleMatch(RegexRunnerMode.FullMatchRequired, Length, Text, _textbeg, _textend - _textbeg, _textpos)! :
                this;
        }

        /// <summary>
        /// Returns the expansion of the passed replacement pattern. For example, if the replacement
        /// pattern is <c>$1$2</c>, <see cref="Result"/> returns the concatenation of
        /// <c>Groups[1].Value</c> and <c>Groups[2].Value</c>.
        /// </summary>
        /// <param name="replacement">The replacement pattern to use.</param>
        /// <returns>The expanded version of the <paramref name="replacement"/> parameter.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="replacement"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">Expansion is not allowed for this pattern.</exception>
        /// <remarks>
        /// <para>
        /// Whereas the <see cref="Regex.Replace(string, string)"/> method replaces all matches in an
        /// input string with a specified replacement pattern, the <see cref="Result"/> method replaces a
        /// single match with a specified replacement pattern. Because it operates on an individual match,
        /// it is also possible to perform processing on the matched string before you call the
        /// <see cref="Result"/> method.
        /// </para>
        /// <para>
        /// The <paramref name="replacement"/> parameter is a standard regular expression replacement
        /// pattern. It can consist of literal characters and regular expression substitutions. For more
        /// information, see
        /// <see href="https://github.com/dotnet/docs/blob/main/docs/standard/base-types/substitutions-in-regular-expressions.md">Substitutions</see>.
        /// </para>
        /// </remarks>
        public virtual string Result(string replacement)
        {
            if (replacement is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.replacement);
            }

            Regex? regex = _regex ?? throw new NotSupportedException(SR.NoResultOnFailed);

            // Gets the weakly cached replacement helper or creates one if there isn't one already.
            RegexReplacement repl = RegexReplacement.GetOrCreate(regex.RegexReplacementWeakReference, replacement, regex.caps!, regex.capsize, regex.capnames!, regex.roptions);
            var segments = new StructListBuilder<ReadOnlyMemory<char>>();
            repl.ReplacementImpl(ref segments, this);
            return Regex.SegmentsToStringAndDispose(ref segments);
        }

        internal ReadOnlyMemory<char> GroupToStringImpl(int groupnum)
        {
            int c = _matchcount[groupnum];
            if (c == 0)
            {
                return default;
            }

            int[] matches = _matches[groupnum];
            return Text.AsMemory(matches[(c - 1) * 2], matches[(c * 2) - 1]);
        }

        internal ReadOnlyMemory<char> LastGroupToStringImpl() =>
            GroupToStringImpl(_matchcount.Length - 1);

        /// <summary>
        /// Returns a <see cref="Match"/> instance equivalent to the one supplied that is safe to share
        /// between multiple threads.
        /// </summary>
        /// <param name="inner">The input <see cref="Match"/> object.</param>
        /// <returns>A regular expression <see cref="Match"/> object.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inner"/> is <see langword="null"/>.
        /// </exception>
        public static Match Synchronized(Match inner)
        {
            if (inner is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.inner);
            }

            int numgroups = inner._matchcount.Length;

            // Populate all groups by looking at each one
            for (int i = 0; i < numgroups; i++)
            {
                // Depends on the fact that Group.Synchronized just
                // operates on and returns the same instance
                Synchronized(inner.Groups[i]);
            }

            return inner;
        }

        /// <summary>Adds a capture to the group specified by "cap"</summary>
        internal void AddMatch(int cap, int start, int len)
        {
            _matches[cap] ??= new int[2];
            int[][] matches = _matches;

            int[] matchcount = _matchcount;
            int capcount = matchcount[cap];

            if (capcount * 2 + 2 > matches[cap].Length)
            {
                int[] oldmatches = matches[cap];
                int[] newmatches = new int[capcount * 8];
                Array.Copy(oldmatches, newmatches, capcount * 2);

                matches[cap] = newmatches;
            }

            matches[cap][capcount * 2] = start;
            matches[cap][capcount * 2 + 1] = len;
            matchcount[cap] = capcount + 1;
        }

        /// <summary>
        /// Nonpublic builder: Add a capture to balance the specified group.  This is used by the
        /// balanced match construct. (?&lt;foo-foo2&gt;...)
        /// If there were no such thing as backtracking, this would be as simple as calling RemoveMatch(cap).
        /// However, since we have backtracking, we need to keep track of everything.
        /// </summary>
        internal void BalanceMatch(int cap)
        {
            _balancing = true;

            // we'll look at the last capture first
            int capcount = _matchcount[cap];
            int target = capcount * 2 - 2;

            // first see if it is negative, and therefore is a reference to the next available
            // capture group for balancing.  If it is, we'll reset target to point to that capture.
            int[][] matches = _matches;
            if (matches[cap][target] < 0)
            {
                target = -3 - matches[cap][target];
            }

            // move back to the previous capture
            target -= 2;

            // if the previous capture is a reference, just copy that reference to the end.  Otherwise, point to it.
            if (target >= 0 && matches[cap][target] < 0)
            {
                AddMatch(cap, matches[cap][target], matches[cap][target + 1]);
            }
            else
            {
                AddMatch(cap, -3 - target, -4 - target /* == -3 - (target + 1) */ );
            }
        }

        /// <summary>Removes a group match by capnum</summary>
        internal void RemoveMatch(int cap) => _matchcount[cap]--;

        /// <summary>Tells if a group was matched by capnum</summary>
        internal bool IsMatched(int cap)
        {
            int[] matchcount = _matchcount;
            return
                (uint)cap < (uint)matchcount.Length &&
                matchcount[cap] > 0 &&
                _matches[cap][matchcount[cap] * 2 - 1] != (-3 + 1);
        }

        /// <summary>
        /// Returns the index of the last specified matched group by capnum
        /// </summary>
        internal int MatchIndex(int cap)
        {
            int[][] matches = _matches;

            int i = matches[cap][_matchcount[cap] * 2 - 2];
            return i >= 0 ? i : matches[cap][-3 - i];
        }

        /// <summary>
        /// Returns the length of the last specified matched group by capnum
        /// </summary>
        internal int MatchLength(int cap)
        {
            int[][] matches = _matches;

            int i = matches[cap][_matchcount[cap] * 2 - 1];
            return i >= 0 ? i : matches[cap][-3 - i];
        }

        /// <summary>Tidy the match so that it can be used as an immutable result</summary>
        internal void Tidy(int textpos, int beginningOfSpanSlice, RegexRunnerMode mode)
        {
            Debug.Assert(mode != RegexRunnerMode.ExistenceRequired);

            int[] matchcount = _matchcount;
            _capcount = matchcount[0]; // used to indicate Success

            // Used for NextMatch. During the matching process these stored the span-based offsets.
            // If there was actually a beginning supplied, all of these need to be shifted by that
            // beginning value.
            Debug.Assert(_textbeg == 0);
            _textbeg = beginningOfSpanSlice;
            _textpos = beginningOfSpanSlice + textpos;
            _textend += beginningOfSpanSlice;

            int[][] matches = _matches;
            int[] interval = matches[0];
            Length = interval[1]; // the length of the match
            Index = interval[0] + beginningOfSpanSlice; // the index of the match, adjusted for input slicing

            // At this point the Match is consistent for handing back to a caller, with regards to the span that was processed.
            // However, the caller may have actually provided a string, and may have done so with a non-zero beginning.
            // In such a case, all offsets need to be shifted by beginning, e.g. if beginning is 5 and a capture occurred at
            // offset 17, that 17 offset needs to be increased to 22 to account for the fact that it was actually 17 from the
            // beginning, which the implementation saw as 0 but which from the caller's perspective was 5.
            if (mode == RegexRunnerMode.FullMatchRequired)
            {
                if (_balancing)
                {
                    TidyBalancing();
                }
                Debug.Assert(!_balancing);

                if (beginningOfSpanSlice != 0)
                {
                    for (int groupNumber = 0; groupNumber < matches.Length; groupNumber++)
                    {
                        int[] captures = matches[groupNumber];
                        if (captures is not null)
                        {
                            int capturesLength = matchcount[groupNumber] * 2; // each capture has an offset and a length
                            for (int c = 0; c < capturesLength; c += 2)
                            {
                                captures[c] += beginningOfSpanSlice;
                            }
                        }
                    }
                }
            }
        }

        private void TidyBalancing()
        {
            int[] matchcount = _matchcount;
            int[][] matches = _matches;

            // The idea here is that we want to compact all of our unbalanced captures.  To do that we
            // use j basically as a count of how many unbalanced captures we have at any given time
            // (really j is an index, but j/2 is the count).  First we skip past all of the real captures
            // until we find a balance captures.  Then we check each subsequent entry.  If it's a balance
            // capture (it's negative), we decrement j.  If it's a real capture, we increment j and copy
            // it down to the last free position.
            for (int cap = 0; cap < matchcount.Length; cap++)
            {
                int limit;
                int[] matcharray;

                limit = matchcount[cap] * 2;
                matcharray = matches[cap];

                int i;
                int j;

                for (i = 0; i < limit; i++)
                {
                    if (matcharray[i] < 0)
                    {
                        break;
                    }
                }

                for (j = i; i < limit; i++)
                {
                    if (matcharray[i] < 0)
                    {
                        // skip negative values
                        j--;
                    }
                    else
                    {
                        // but if we find something positive (an actual capture), copy it back to the last
                        // unbalanced position.
                        if (i != j)
                        {
                            matcharray[j] = matcharray[i];
                        }

                        j++;
                    }
                }

                matchcount[cap] = j / 2;
            }

            _balancing = false;
        }
    }

    /// <summary>
    /// MatchSparse is for handling the case where slots are sparsely arranged (e.g., if somebody says use slot 100000)
    /// </summary>
    internal sealed class MatchSparse : Match
    {
        private new readonly Hashtable _caps;

        internal MatchSparse(Regex regex, Hashtable caps, int capcount, string? text, int textLength) :
            base(regex, capcount, text, textLength)
        {
            _caps = caps;
        }

        public override GroupCollection Groups => _groupcoll ??= new GroupCollection(this, _caps);
    }
}
