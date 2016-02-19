// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The MatchCollection lists the successful matches that
// result when searching a string for a regular expression.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

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
        private readonly Regex _regex;
        private readonly List<Match> _matches;
        private bool _done;
        private readonly string _input;
        private readonly int _beginning;
        private readonly int _length;
        private int _startat;
        private int _prevlen;

        internal MatchCollection(Regex regex, string input, int beginning, int length, int startat)
        {
            if (startat < 0 || startat > input.Length)
                throw new ArgumentOutOfRangeException(nameof(startat), SR.BeginIndexNotNegative);

            _regex = regex;
            _input = input;
            _beginning = beginning;
            _length = length;
            _startat = startat;
            _prevlen = -1;
            _matches = new List<Match>();
            _done = false;
        }

        /// <summary>
        /// Returns the number of captures.
        /// </summary>
        public int Count
        {
            get
            {
                EnsureInitialized();
                return _matches.Count;
            }
        }

        /// <summary>
        /// Returns the ith Match in the collection.
        /// </summary>
        public virtual Match this[int i]
        {
            get
            {
                if (i < 0)
                    throw new ArgumentOutOfRangeException(nameof(i));

                Match match = GetMatch(i);

                if (match == null)
                    throw new ArgumentOutOfRangeException(nameof(i));

                return match;
            }
        }

        /// <summary>
        /// Provides an enumerator in the same order as Item[i].
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        private Match GetMatch(int i)
        {
            Debug.Assert(i >= 0, "i cannot be negative.");

            if (_matches.Count > i)
                return _matches[i];

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

        private void EnsureInitialized()
        {
            if (!_done)
            {
                GetMatch(int.MaxValue);
            }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return this; }
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            EnsureInitialized();
            ((ICollection)_matches).CopyTo(array, arrayIndex);
        }

        private class Enumerator : IEnumerator
        {
            private readonly MatchCollection _collection;
            private int _index;

            internal Enumerator(MatchCollection collection)
            {
                Debug.Assert(collection != null, "collection cannot be null.");

                _collection = collection;
                _index = -1;
            }

            public bool MoveNext()
            {
                if (_index == -2)
                    return false;

                _index++;
                Match match = _collection.GetMatch(_index);

                if (match == null)
                {
                    _index = -2;
                    return false;
                }

                return true;
            }

            public Match Current
            {
                get
                {
                    if (_index < 0)
                        throw new InvalidOperationException(SR.EnumNotStarted);

                    return _collection.GetMatch(_index);
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                _index = -1;
            }
        }
    }
}
