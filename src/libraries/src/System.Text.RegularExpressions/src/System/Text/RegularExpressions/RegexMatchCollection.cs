// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The MatchCollection lists the successful matches that
// result when searching a string for a regular expression.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.RegularExpressions
{
    /*
     * This collection returns a sequence of successful match results, either
     * from GetMatchCollection() or GetExecuteCollection(). It stops when the
     * first failure is encountered (it does not return the failed match).
     */
    /// <summary>
    /// Represents the set of names appearing as capturing group
    /// names in a regular expression.
    /// </summary>
    public class MatchCollection : ICollection
    {
        internal Regex _regex;
        internal List<Match> _matches;
        internal bool _done;
        internal String _input;
        internal int _beginning;
        internal int _length;
        internal int _startat;
        internal int _prevlen;

        private static int s_infinite = 0x7FFFFFFF;

        internal MatchCollection(Regex regex, String input, int beginning, int length, int startat)
        {
            if (startat < 0 || startat > input.Length)
                throw new ArgumentOutOfRangeException("startat", SR.BeginIndexNotNegative);

            _regex = regex;
            _input = input;
            _beginning = beginning;
            _length = length;
            _startat = startat;
            _prevlen = -1;
            _matches = new List<Match>();
            _done = false;
        }

        internal Match GetMatch(int i)
        {
            if (i < 0)
                return null;

            if (_matches.Count > i)
                return (Match)_matches[i];

            if (_done)
                return null;

            Match match;

            do
            {
                match = _regex.Run(false, _prevlen, _input, _beginning, _length, _startat);

                if (!match.Success)
                {
                    _done = true;
                    return null;
                }

                _matches.Add(match);

                _prevlen = match._length;
                _startat = match._textpos;
            } while (_matches.Count <= i);

            return match;
        }

        /// <summary>
        /// Returns the number of captures.
        /// </summary>
        public int Count
        {
            get
            {
                if (_done)
                    return _matches.Count;

                GetMatch(s_infinite);

                return _matches.Count;
            }
        }

        Object ICollection.SyncRoot
        {
            get
            {
                return this;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the ith Match in the collection.
        /// </summary>
        public virtual Match this[int i]
        {
            get
            {
                Match match;

                match = GetMatch(i);

                if (match == null)
                    throw new ArgumentOutOfRangeException("i");

                return match;
            }
        }

        /// <summary>
        /// Copies all the elements of the collection to the given array
        /// starting at the given index.
        /// </summary>
        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if (!_done)
                GetMatch(s_infinite);

            ((ICollection)_matches).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Provides an enumerator in the same order as Item[i].
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return new MatchEnumerator(this);
        }
    }

    /*
     * This non-public enumerator lists all the group matches.
     * Should it be public?
     */
    internal class MatchEnumerator : IEnumerator
    {
        internal MatchCollection _matchcoll;
        internal Match _match = null;
        internal int _curindex;
        internal bool _done;

        /*
         * Nonpublic constructor
         */
        internal MatchEnumerator(MatchCollection matchcoll)
        {
            _matchcoll = matchcoll;
        }

        /*
         * Advance to the next match
         */
        public bool MoveNext()
        {
            if (_done)
                return false;

            _match = _matchcoll.GetMatch(_curindex);
            _curindex++;

            if (_match == null)
            {
                _done = true;
                return false;
            }

            return true;
        }

        /*
         * The current match
         */
        public Object Current
        {
            get
            {
                if (_match == null)
                    throw new InvalidOperationException(SR.EnumNotStarted);
                return _match;
            }
        }

        /*
         * Position before the first item
         */
        public void Reset()
        {
            _curindex = 0;
            _done = false;
            _match = null;
        }
    }
}
