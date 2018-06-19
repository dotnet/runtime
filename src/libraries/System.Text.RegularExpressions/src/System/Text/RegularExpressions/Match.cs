// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Match is the result class for a regex search.
// It returns the location, length, and substring for
// the entire match as well as every captured group.

// Match is also used during the search to keep track of each capture for each group.  This is
// done using the "_matches" array.  _matches[x] represents an array of the captures for group x.
// This array consists of start and length pairs, and may have empty entries at the end.  _matchcount[x]
// stores how many captures a group has.  Note that _matchcount[x]*2 is the length of all the valid
// values in _matches.  _matchcount[x]*2-2 is the Start of the last capture, and _matchcount[x]*2-1 is the
// Length of the last capture
//
// For example, if group 2 has one capture starting at position 4 with length 6,
// _matchcount[2] == 1
// _matches[2][0] == 4
// _matches[2][1] == 6
//
// Values in the _matches array can also be negative.  This happens when using the balanced match
// construct, "(?<start-end>...)".  When the "end" group matches, a capture is added for both the "start"
// and "end" groups.  The capture added for "start" receives the negative values, and these values point to
// the next capture to be balanced.  They do NOT point to the capture that "end" just balanced out.  The negative
// values are indices into the _matches array transformed by the formula -3-x.  This formula also untransforms.
//

using System.Collections;
using System.Globalization;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Represents the results from a single regular expression match.
    /// </summary>
    public class Match : Group
    {
        private const int ReplaceBufferSize = 256;
        internal GroupCollection _groupcoll;

        // input to the match
        internal Regex _regex;
        internal int _textbeg;
        internal int _textpos;
        internal int _textend;
        internal int _textstart;

        // output from the match
        internal int[][] _matches;
        internal int[] _matchcount;
        internal bool _balancing;        // whether we've done any balancing with this match.  If we
                                         // have done balancing, we'll need to do extra work in Tidy().

        internal Match(Regex regex, int capcount, string text, int begpos, int len, int startpos)
            : base(text, new int[2], 0, "0")
        {
            _regex = regex;
            _matchcount = new int[capcount];
            _matches = new int[capcount][];
            _matches[0] = _caps;
            _textbeg = begpos;
            _textend = begpos + len;
            _textstart = startpos;
            _balancing = false;

            // No need for an exception here.  This is only called internally, so we'll use an Assert instead
            System.Diagnostics.Debug.Assert(!(_textbeg < 0 || _textstart < _textbeg || _textend < _textstart || Text.Length < _textend),
                                            "The parameters are out of range.");
        }

        /// <summary>
        /// Returns an empty Match object.
        /// </summary>
        public static Match Empty { get; } = new Match(null, 1, string.Empty, 0, 0, 0);

        internal virtual void Reset(Regex regex, string text, int textbeg, int textend, int textstart)
        {
            _regex = regex;
            Text = text;
            _textbeg = textbeg;
            _textend = textend;
            _textstart = textstart;

            for (int i = 0; i < _matchcount.Length; i++)
            {
                _matchcount[i] = 0;
            }

            _balancing = false;
        }

        public virtual GroupCollection Groups
        {
            get
            {
                if (_groupcoll == null)
                    _groupcoll = new GroupCollection(this, null);

                return _groupcoll;
            }
        }

        /// <summary>
        /// Returns a new Match with the results for the next match, starting
        /// at the position at which the last match ended (at the character beyond the last
        /// matched character).
        /// </summary>
        public Match NextMatch()
        {
            if (_regex == null)
                return this;

            return _regex.Run(false, Length, Text, _textbeg, _textend - _textbeg, _textpos);
        }

        /// <summary>
        /// Returns the expansion of the passed replacement pattern. For
        /// example, if the replacement pattern is ?$1$2?, Result returns the concatenation
        /// of Group(1).ToString() and Group(2).ToString().
        /// </summary>
        public virtual string Result(string replacement)
        {
            if (replacement == null)
                throw new ArgumentNullException(nameof(replacement));

            if (_regex == null)
                throw new NotSupportedException(SR.NoResultOnFailed);

            // Gets the weakly cached replacement helper or creates one if there isn't one already.
            RegexReplacement repl = RegexReplacement.GetOrCreate(_regex._replref, replacement, _regex.caps, _regex.capsize,
                _regex.capnames, _regex.roptions);
            Span<char> charInitSpan = stackalloc char[ReplaceBufferSize];
            var vsb = new ValueStringBuilder(charInitSpan);

            repl.ReplacementImpl(ref vsb, this);

            return vsb.ToString();
        }

        internal virtual ReadOnlySpan<char> GroupToStringImpl(int groupnum)
        {
            int c = _matchcount[groupnum];
            if (c == 0)
                return string.Empty;

            int[] matches = _matches[groupnum];

            return Text.AsSpan(matches[(c - 1) * 2], matches[(c * 2) - 1]);
        }

        internal ReadOnlySpan<char> LastGroupToStringImpl()
        {
            return GroupToStringImpl(_matchcount.Length - 1);
        }

        /// <summary>
        /// Returns a Match instance equivalent to the one supplied that is safe to share
        /// between multiple threads.
        /// </summary>
        public static Match Synchronized(Match inner)
        {
            if (inner == null)
                throw new ArgumentNullException(nameof(inner));

            int numgroups = inner._matchcount.Length;

            // Populate all groups by looking at each one
            for (int i = 0; i < numgroups; i++)
            {
                Group group = inner.Groups[i];

                // Depends on the fact that Group.Synchronized just
                // operates on and returns the same instance
                Group.Synchronized(group);
            }

            return inner;
        }

        /// <summary>
        /// Adds a capture to the group specified by "cap"
        /// </summary>
        internal virtual void AddMatch(int cap, int start, int len)
        {
            int capcount;

            if (_matches[cap] == null)
                _matches[cap] = new int[2];

            capcount = _matchcount[cap];

            if (capcount * 2 + 2 > _matches[cap].Length)
            {
                int[] oldmatches = _matches[cap];
                int[] newmatches = new int[capcount * 8];
                for (int j = 0; j < capcount * 2; j++)
                    newmatches[j] = oldmatches[j];
                _matches[cap] = newmatches;
            }

            _matches[cap][capcount * 2] = start;
            _matches[cap][capcount * 2 + 1] = len;
            _matchcount[cap] = capcount + 1;
        }

        /*
         * Nonpublic builder: Add a capture to balance the specified group.  This is used by the
                              balanced match construct. (?<foo-foo2>...)

           If there were no such thing as backtracking, this would be as simple as calling RemoveMatch(cap).
           However, since we have backtracking, we need to keep track of everything.
         */
        internal virtual void BalanceMatch(int cap)
        {
            _balancing = true;

            // we'll look at the last capture first
            int capcount = _matchcount[cap];
            int target = capcount * 2 - 2;

            // first see if it is negative, and therefore is a reference to the next available
            // capture group for balancing.  If it is, we'll reset target to point to that capture.
            if (_matches[cap][target] < 0)
                target = -3 - _matches[cap][target];

            // move back to the previous capture
            target -= 2;

            // if the previous capture is a reference, just copy that reference to the end.  Otherwise, point to it.
            if (target >= 0 && _matches[cap][target] < 0)
                AddMatch(cap, _matches[cap][target], _matches[cap][target + 1]);
            else
                AddMatch(cap, -3 - target, -4 - target /* == -3 - (target + 1) */ );
        }

        /// <summary>
        /// Removes a group match by capnum
        /// </summary>
        internal virtual void RemoveMatch(int cap)
        {
            _matchcount[cap]--;
        }

        /// <summary>
        /// Tells if a group was matched by capnum
        /// </summary>
        internal virtual bool IsMatched(int cap)
        {
            return cap < _matchcount.Length && _matchcount[cap] > 0 && _matches[cap][_matchcount[cap] * 2 - 1] != (-3 + 1);
        }

        /// <summary>
        /// Returns the index of the last specified matched group by capnum
        /// </summary>
        internal virtual int MatchIndex(int cap)
        {
            int i = _matches[cap][_matchcount[cap] * 2 - 2];
            if (i >= 0)
                return i;

            return _matches[cap][-3 - i];
        }

        /// <summary>
        /// Returns the length of the last specified matched group by capnum
        /// </summary>
        internal virtual int MatchLength(int cap)
        {
            int i = _matches[cap][_matchcount[cap] * 2 - 1];
            if (i >= 0)
                return i;

            return _matches[cap][-3 - i];
        }

        /// <summary>
        /// Tidy the match so that it can be used as an immutable result
        /// </summary>
        internal virtual void Tidy(int textpos)
        {
            int[] interval = _matches[0];
            Index = interval[0];
            Length = interval[1];
            _textpos = textpos;
            _capcount = _matchcount[0];

            if (_balancing)
            {
                // The idea here is that we want to compact all of our unbalanced captures.  To do that we
                // use j basically as a count of how many unbalanced captures we have at any given time
                // (really j is an index, but j/2 is the count).  First we skip past all of the real captures
                // until we find a balance captures.  Then we check each subsequent entry.  If it's a balance
                // capture (it's negative), we decrement j.  If it's a real capture, we increment j and copy
                // it down to the last free position.
                for (int cap = 0; cap < _matchcount.Length; cap++)
                {
                    int limit;
                    int[] matcharray;

                    limit = _matchcount[cap] * 2;
                    matcharray = _matches[cap];

                    int i = 0;
                    int j;

                    for (i = 0; i < limit; i++)
                    {
                        if (matcharray[i] < 0)
                            break;
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
                                matcharray[j] = matcharray[i];
                            j++;
                        }
                    }

                    _matchcount[cap] = j / 2;
                }

                _balancing = false;
            }
        }

#if DEBUG
        internal bool Debug
        {
            get
            {
                if (_regex == null)
                    return false;

                return _regex.Debug;
            }
        }

        internal virtual void Dump()
        {
            int i, j;

            for (i = 0; i < _matchcount.Length; i++)
            {
                System.Diagnostics.Debug.WriteLine("Capnum " + i.ToString(CultureInfo.InvariantCulture) + ":");

                for (j = 0; j < _matchcount[i]; j++)
                {
                    string text = "";

                    if (_matches[i][j * 2] >= 0)
                        text = Text.Substring(_matches[i][j * 2], _matches[i][j * 2 + 1]);

                    System.Diagnostics.Debug.WriteLine("  (" + _matches[i][j * 2].ToString(CultureInfo.InvariantCulture) + "," + _matches[i][j * 2 + 1].ToString(CultureInfo.InvariantCulture) + ") " + text);
                }
            }
        }
#endif
    }

    /// <summary>
    /// MatchSparse is for handling the case where slots are sparsely arranged (e.g., if somebody says use slot 100000)
    /// </summary>
    internal class MatchSparse : Match
    {
        // the lookup hashtable
        new internal readonly Hashtable _caps;

        internal MatchSparse(Regex regex, Hashtable caps, int capcount, string text, int begpos, int len, int startpos)
            : base(regex, capcount, text, begpos, len, startpos)
        {
            _caps = caps;
        }

        public override GroupCollection Groups
        {
            get
            {
                if (_groupcoll == null)
                    _groupcoll = new GroupCollection(this, _caps);

                return _groupcoll;
            }
        }

#if DEBUG
        internal override void Dump()
        {
            if (_caps != null)
            {
                foreach (DictionaryEntry kvp in _caps)
                {
                    System.Diagnostics.Debug.WriteLine("Slot " + kvp.Key.ToString() + " -> " + kvp.Value.ToString());
                }
            }

            base.Dump();
        }
#endif
    }
}
