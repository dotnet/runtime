// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Represents the results from a single regular expression match.
    /// </summary>
    /// <remarks>
    /// Match is the result class for a regex search.
    /// It returns the location, length, and substring for
    /// the entire match as well as every captured group.
    ///
    /// Match is also used during the search to keep track of each capture for each group.  This is
    /// done using the "_matches" array.  _matches[x] represents an array of the captures for group x.
    /// This array consists of start and length pairs, and may have empty entries at the end.  _matchcount[x]
    /// stores how many captures a group has.  Note that _matchcount[x]*2 is the length of all the valid
    /// values in _matches.  _matchcount[x]*2-2 is the Start of the last capture, and _matchcount[x]*2-1 is the
    /// Length of the last capture
    ///
    /// For example, if group 2 has one capture starting at position 4 with length 6,
    /// _matchcount[2] == 1
    /// _matches[2][0] == 4
    /// _matches[2][1] == 6
    ///
    /// Values in the _matches array can also be negative.  This happens when using the balanced match
    /// construct, "(?&lt;start-end&gt;...)".  When the "end" group matches, a capture is added for both the "start"
    /// and "end" groups.  The capture added for "start" receives the negative values, and these values point to
    /// the next capture to be balanced.  They do NOT point to the capture that "end" just balanced out.  The negative
    /// values are indices into the _matches array transformed by the formula -3-x.  This formula also untransforms.
    /// </remarks>
    public class Match : Group
    {
        internal GroupCollection? _groupcoll;

        // input to the match
        internal Regex? _regex;
        internal int _textbeg;
        internal int _textpos;
        internal int _textend;
        internal int _textstart;

        // output from the match
        internal readonly int[][] _matches;
        internal readonly int[] _matchcount;
        internal bool _balancing;        // whether we've done any balancing with this match.  If we
                                         // have done balancing, we'll need to do extra work in Tidy().

        internal Match(Regex? regex, int capcount, string? text, int begpos, int len, int startpos) :
            base(text, new int[2], 0, "0")
        {
            _regex = regex;
            _matchcount = new int[capcount];
            _matches = new int[capcount][];
            _matches[0] = _caps;
            _textbeg = begpos;
            _textend = begpos + len;
            _textstart = startpos;
            _balancing = false;
        }

        /// <summary>Returns an empty Match object.</summary>
        public static Match Empty { get; } = new Match(null, 1, string.Empty, 0, 0, 0);

        internal void Reset(Regex regex, string? text, int textbeg, int textend, int textstart)
        {
            _regex = regex;
            Text = text;
            _textbeg = textbeg;
            _textend = textend;
            _textstart = textstart;

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

        public virtual GroupCollection Groups => _groupcoll ??= new GroupCollection(this, null);

        /// <summary>
        /// Returns a new Match with the results for the next match, starting
        /// at the position at which the last match ended (at the character beyond the last
        /// matched character).
        /// </summary>
        public Match NextMatch()
        {
            Regex? r = _regex;
            Debug.Assert(Text != null);
            return r != null ?
                r.RunSingleMatch(RegexRunnerMode.FullMatchRequired, Length, Text, _textbeg, _textend - _textbeg, _textpos)! :
                this;
        }

        /// <summary>
        /// Returns the expansion of the passed replacement pattern. For
        /// example, if the replacement pattern is ?$1$2?, Result returns the concatenation
        /// of Group(1).ToString() and Group(2).ToString().
        /// </summary>
        public virtual string Result(string replacement)
        {
            if (replacement is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.replacement);
            }

            Regex? regex = _regex;
            if (regex is null)
            {
                throw new NotSupportedException(SR.NoResultOnFailed);
            }

            // Gets the weakly cached replacement helper or creates one if there isn't one already.
            RegexReplacement repl = RegexReplacement.GetOrCreate(regex.RegexReplacementWeakReference, replacement, regex.caps!, regex.capsize, regex.capnames!, regex.roptions);
            SegmentStringBuilder segments = SegmentStringBuilder.Create();
            repl.ReplacementImpl(ref segments, this);
            return segments.ToString();
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
        /// Returns a Match instance equivalent to the one supplied that is safe to share
        /// between multiple threads.
        /// </summary>
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
                for (int j = 0; j < capcount * 2; j++)
                {
                    newmatches[j] = oldmatches[j];
                }

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
            _textpos = textpos; // used to determine where to perform next match

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

        internal MatchSparse(Regex regex, Hashtable caps, int capcount, string? text, int begpos, int len, int startpos) :
            base(regex, capcount, text, begpos, len, startpos)
        {
            _caps = caps;
        }

        public override GroupCollection Groups => _groupcoll ??= new GroupCollection(this, _caps);
    }
}
