// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Represents the set of successful matches found by iteratively applying a regular expression
    /// pattern to the input string. The collection is immutable (read-only) and has no public
    /// constructor. The <see cref="Regex.Matches(string)" /> method returns a
    /// <see cref="MatchCollection" /> object.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The collection contains zero or more <see cref="Match" /> objects. If the match is successful,
    /// the collection is populated with one <see cref="Match" /> object for each match found in the
    /// input string. If the match is unsuccessful, the collection contains no <see cref="Match" />
    /// objects, and its <see cref="Count" /> property equals zero.
    /// </para>
    /// <para>
    /// When applying a regular expression pattern to a particular input string, the regular expression
    /// engine uses either of two techniques to build the <see cref="MatchCollection" /> object:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term>Direct evaluation</term>
    /// <description>
    /// The <see cref="MatchCollection" /> object is populated all at once,
    /// with all matches resulting from a particular call to the <see cref="Regex.Matches(string)" />
    /// method. This technique is used when the collection's <see cref="Count" /> property is accessed.
    /// It typically is the more expensive method of populating the collection and entails a greater
    /// performance hit.
    /// </description>
    /// </item>
    /// <item>
    /// <term>Lazy evaluation</term>
    /// <description>
    /// The <see cref="MatchCollection" /> object is populated as needed on a
    /// match-by-match basis. It is equivalent to the regular expression engine calling the
    /// <see cref="Regex.Match(string)" /> method repeatedly and adding each match to the collection.
    /// This technique is used when the collection is accessed through its <see cref="GetEnumerator" />
    /// method, or when it is accessed using the <c>foreach</c> statement.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// To iterate through the members of the collection, you should use the collection iteration
    /// construct provided by your language (such as <c>foreach</c> in C#) instead of retrieving the
    /// enumerator that is returned by the <see cref="GetEnumerator" /> method.
    /// </para>
    /// </remarks>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(CollectionDebuggerProxy<Match>))]
    public class MatchCollection : IList<Match>, IReadOnlyList<Match>, IList
    {
        private readonly Regex _regex;
        private readonly List<Match> _matches;
        private readonly string _input;
        private int _startat;
        private int _prevlen;
        private bool _done;

        internal MatchCollection(Regex regex, string input, int startat)
        {
            if ((uint)startat > (uint)input.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startat, ExceptionResource.BeginIndexNotNegative);
            }

            _regex = regex;
            _input = input;
            _startat = startat;
            _prevlen = -1;
            _matches = new List<Match>();
            _done = false;
        }

        /// <summary>Gets a value that indicates whether the collection is read only.</summary>
        /// <value><see langword="true" /> in all cases.</value>
        public bool IsReadOnly => true;

        /// <summary>Gets the number of matches.</summary>
        /// <value>The number of matches.</value>
        /// <remarks>
        /// <para>
        /// Accessing the <see cref="Count" /> property causes the regular expression engine to populate
        /// the collection using direct evaluation. In contrast, calling the <see cref="GetEnumerator" />
        /// method (or using the <c>foreach</c> statement) causes the regular expression engine to
        /// populate the collection on an as-needed basis using lazy evaluation. Direct evaluation can be
        /// a much more expensive method of building the collection than lazy evaluation.
        /// </para>
        /// <para>
        /// Because the <see cref="MatchCollection" /> object is generally populated by using lazy
        /// evaluation, trying to determine the number of elements in the collection before it has been
        /// fully populated may throw a <see cref="RegexMatchTimeoutException" /> exception. This
        /// exception can be thrown if a time-out value for matching operations is in effect, and the
        /// attempt to find a single match exceeds that time-out interval.
        /// </para>
        /// </remarks>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        public int Count
        {
            get
            {
                EnsureInitialized();
                return _matches.Count;
            }
        }

        /// <summary>Gets an individual member of the collection.</summary>
        /// <param name="i">Index into the <see cref="Match" /> collection.</param>
        /// <value>The captured substring at position <paramref name="i" /> in the collection.</value>
        /// <remarks>
        /// <para>
        /// In C#, the <see cref="this[int]" /> property is an indexer; it is not explicitly referenced
        /// in code, but instead allows the <see cref="MatchCollection" /> to be accessed as if it were
        /// an array.
        /// </para>
        /// <para>
        /// Typically, individual items in the <see cref="MatchCollection" /> are accessed by their index
        /// only after the total number of items in the collection has been determined from the
        /// <see cref="Count" /> property. However, accessing the <see cref="Count" /> property causes
        /// the regular expression engine to use direct evaluation to build the collection all at once.
        /// This is typically more expensive than iterating the collection using the
        /// <see cref="GetEnumerator" /> method or the <c>foreach</c> statement.
        /// </para>
        /// <para>
        /// Because the <see cref="MatchCollection" /> object is generally populated by using lazy
        /// evaluation, trying to navigate to a specific match may throw a
        /// <see cref="RegexMatchTimeoutException" /> exception. This exception can be thrown if a
        /// time-out value for matching operations is in effect, and the attempt to find a specific
        /// match exceeds that time-out interval.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="i" /> is less than 0 or greater than or equal to <see cref="Count" />.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        public virtual Match this[int i]
        {
            get
            {
                Match? match = null;
                if (i < 0 || (match = GetMatch(i)) is null)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.i);
                }
                return match;
            }
        }

        /// <summary>Provides an enumerator that iterates through the collection.</summary>
        /// <returns>
        /// An object that contains all <see cref="Match" /> objects within the
        /// <see cref="MatchCollection" />.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Instead of calling the <see cref="GetEnumerator" /> method to retrieve an enumerator that
        /// lets you iterate through the <see cref="Match" /> objects in the collection, you should use
        /// the collection iteration construct provided by your programming language (such as
        /// <c>foreach</c> in C#).
        /// </para>
        /// <para>
        /// Iterating the members of the <see cref="MatchCollection" /> using the
        /// <see cref="GetEnumerator" /> method (or the <c>foreach</c> statement) causes the regular
        /// expression engine to populate the collection on an as-needed basis using lazy evaluation.
        /// In contrast, the regular expression engine uses direct evaluation to populate the collection
        /// all at once when the <see cref="Count" /> property is accessed. This can be a much more
        /// expensive method of building the collection than lazy evaluation.
        /// </para>
        /// <para>
        /// Because the <see cref="MatchCollection" /> object is generally populated by using lazy
        /// evaluation, trying to navigate to the next member of the collection may throw a
        /// <see cref="RegexMatchTimeoutException" /> exception. This exception can be thrown if a
        /// time-out value for matching operations is in effect, and the attempt to find the next match
        /// exceeds that time-out interval.
        /// </para>
        /// </remarks>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        public IEnumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<Match> IEnumerable<Match>.GetEnumerator() => new Enumerator(this);

        private Match? GetMatch(int i)
        {
            Debug.Assert(i >= 0, "i cannot be negative.");

            if (_matches.Count > i)
            {
                return _matches[i];
            }

            if (_done)
            {
                return null;
            }

            Match match;
            do
            {
                match = _regex.RunSingleMatch(RegexRunnerMode.FullMatchRequired, _prevlen, _input, 0, _input.Length, _startat)!;
                if (!match.Success)
                {
                    _done = true;
                    return null;
                }

                _matches.Add(match);
                _prevlen = match.Length;
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

        /// <summary>
        /// Gets a value indicating whether access to the collection is synchronized (thread-safe).
        /// </summary>
        /// <value><see langword="false" /> in all cases.</value>
        public bool IsSynchronized => false;

        /// <summary>Gets an object that can be used to synchronize access to the collection.</summary>
        /// <value>
        /// An object that can be used to synchronize access to the collection. This property always
        /// returns the object itself.
        /// </value>
        public object SyncRoot => this;

        /// <summary>
        /// Copies all the elements of the collection to the given array starting at the given index.
        /// </summary>
        /// <param name="array">The array the collection is to be copied into.</param>
        /// <param name="arrayIndex">The position in the array where copying is to begin.</param>
        /// <remarks>
        /// Because the <see cref="MatchCollection" /> object is generally populated by using lazy
        /// evaluation, trying to copy the collection before it has been fully populated may throw a
        /// <see cref="RegexMatchTimeoutException" /> exception. This exception can be thrown if a
        /// time-out value for matching operations is in effect, and the attempt to find a single match
        /// exceeds that time-out interval.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// <paramref name="array" /> is multi-dimensional.
        /// -or-
        /// The number of elements in the source <see cref="MatchCollection" /> is greater than the
        /// available space from <paramref name="arrayIndex" /> to the end of <paramref name="array" />.
        /// -or-
        /// The type of the source <see cref="MatchCollection" /> cannot be cast automatically to the
        /// type of the destination <paramref name="array" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="arrayIndex" /> is less than the lower bound of <paramref name="array" />.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        public void CopyTo(Array array, int arrayIndex)
        {
            EnsureInitialized();
            ((ICollection)_matches).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Copies the elements of the collection to an <see cref="Array" />, starting at a particular
        /// <see cref="Array" /> index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional <see cref="Array" /> that is the destination of the elements copied from
        /// the collection. The <see cref="Array" /> must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">
        /// The zero-based index in <paramref name="array" /> at which copying begins.
        /// </param>
        public void CopyTo(Match[] array, int arrayIndex)
        {
            EnsureInitialized();
            _matches.CopyTo(array, arrayIndex);
        }

        int IList<Match>.IndexOf(Match item)
        {
            EnsureInitialized();
            return _matches.IndexOf(item);
        }

        void IList<Match>.Insert(int index, Match item) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        void IList<Match>.RemoveAt(int index) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        Match IList<Match>.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        void ICollection<Match>.Add(Match item) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        void ICollection<Match>.Clear() =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        bool ICollection<Match>.Contains(Match item)
        {
            EnsureInitialized();
            return _matches.Contains(item);
        }

        bool ICollection<Match>.Remove(Match item) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        int IList.Add(object? value) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        void IList.Clear() =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        bool IList.Contains(object? value) =>
            value is Match match && ((ICollection<Match>)this).Contains(match);

        int IList.IndexOf(object? value) =>
            value is Match other ? ((IList<Match>)this).IndexOf(other) : -1;

        void IList.Insert(int index, object? value) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        bool IList.IsFixedSize => true;

        void IList.Remove(object? value) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        void IList.RemoveAt(int index) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        object? IList.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        private sealed class Enumerator : IEnumerator<Match>
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
                {
                    return false;
                }

                _index++;
                Match? match = _collection.GetMatch(_index);

                if (match is null)
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
                    {
                        throw new InvalidOperationException(SR.EnumNotStarted);
                    }

                    return _collection.GetMatch(_index)!;
                }
            }

            object IEnumerator.Current => Current;

            void IEnumerator.Reset() => _index = -1;

            void IDisposable.Dispose() { }
        }
    }
}
