// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Represents the set of captures made by a single capturing group. The collection is immutable
    /// (read-only) and has no public constructor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="CaptureCollection" /> object contains one or more <see cref="Capture" /> objects.
    /// Instances of the <see cref="CaptureCollection" /> class are returned by the following properties:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// The <see cref="Group.Captures" /> property. Each member of the collection represents a substring
    /// captured by a capturing group. If a quantifier is not applied to a capturing group, the
    /// <see cref="CaptureCollection" /> includes a single <see cref="Capture" /> object that represents
    /// the same captured substring as the <see cref="Group" /> object. If a quantifier is applied to a
    /// capturing group, the <see cref="CaptureCollection" /> includes one <see cref="Capture" /> object
    /// for each captured substring, and the <see cref="Group" /> object provides information only about
    /// the last captured substring.
    /// </item>
    /// <item>
    /// The <c>Match.Captures</c> property. In this case, the collection consists of a single
    /// <see cref="Capture" /> object that provides information about the match as a whole. That is, the
    /// <see cref="CaptureCollection" /> object provides the same information as the <see cref="Match" />
    /// object.
    /// </item>
    /// </list>
    /// <para>
    /// To iterate through the members of the collection, you should use the collection iteration construct
    /// provided by your language (such as <c>foreach</c> in C#) instead of retrieving the enumerator that
    /// is returned by the <see cref="GetEnumerator" /> method.
    /// </para>
    /// </remarks>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(CollectionDebuggerProxy<Capture>))]
    public class CaptureCollection : IList<Capture>, IReadOnlyList<Capture>, IList
    {
        private readonly Group _group;
        private readonly int _capcount;
        private Capture[]? _captures;

        internal CaptureCollection(Group group)
        {
            _group = group;
            _capcount = _group._capcount;
        }

        /// <summary>Gets a value that indicates whether the collection is read only.</summary>
        /// <value><see langword="true" /> in all cases.</value>
        public bool IsReadOnly => true;

        /// <summary>Gets the number of substrings captured by the group.</summary>
        /// <value>The number of items in the <see cref="CaptureCollection" />.</value>
        public int Count => _capcount;

        /// <summary>Gets an individual member of the collection.</summary>
        /// <param name="i">The index into the capture collection.</param>
        /// <value>The captured substring at position <paramref name="i" /> in the collection.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="i" /> is less than 0 or greater than or equal to
        /// <see cref="Count" />.
        /// </exception>
        public Capture this[int i] => GetCapture(i);

        /// <summary>Provides an enumerator that iterates through the collection.</summary>
        /// <returns>
        /// An object that contains all <see cref="Capture" /> objects within the
        /// <see cref="CaptureCollection" />.
        /// </returns>
        /// <remarks>
        /// Instead of calling the <see cref="GetEnumerator" /> method to retrieve an enumerator that lets
        /// you iterate through the <see cref="Capture" /> objects in the collection, you should use the
        /// collection iteration construct provided by your programming language (such as <c>foreach</c>
        /// in C#).
        /// </remarks>
        public IEnumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<Capture> IEnumerable<Capture>.GetEnumerator() => new Enumerator(this);

        /// <summary>Returns the set of captures for the group</summary>
        private Capture GetCapture(int i)
        {
            if ((uint)i == _capcount - 1)
            {
                return _group;
            }

            if (i >= _capcount || i < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.i);
            }

            // first time a capture is accessed, compute them all
            if (_captures is null)
            {
                ForceInitialized();
                Debug.Assert(_captures != null);
            }

            return _captures[i];
        }

        /// <summary>
        /// Compute all captures
        /// </summary>
        internal void ForceInitialized()
        {
            _captures = new Capture[_capcount];
            for (int j = 0; j < _capcount - 1; j++)
            {
                _captures[j] = new Capture(_group.Text, _group._caps[j * 2], _group._caps[j * 2 + 1]);
            }
        }

        /// <summary>
        /// Gets a value that indicates whether access to the collection is synchronized (thread-safe).
        /// </summary>
        /// <value><see langword="false" /> in all cases.</value>
        public bool IsSynchronized => false;

        /// <summary>Gets an object that can be used to synchronize access to the collection.</summary>
        /// <value>An object that can be used to synchronize access to the collection.</value>
        public object SyncRoot => _group;

        /// <summary>
        /// Copies all the elements of the collection to the given array beginning at the given index.
        /// </summary>
        /// <param name="array">The array the collection is to be copied into.</param>
        /// <param name="arrayIndex">The position in the destination array where copying is to begin.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="array" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="array" /> is multidimensional.
        /// </exception>
        /// <exception cref="IndexOutOfRangeException">
        /// <paramref name="arrayIndex" /> is outside the bounds of <paramref name="array" />.
        /// </exception>
        public void CopyTo(Array array, int arrayIndex)
        {
            if (array is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            for (int i = arrayIndex, j = 0; j < Count; i++, j++)
            {
                array.SetValue(this[j], i);
            }
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
        /// <exception cref="ArgumentNullException">
        /// <paramref name="array" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="arrayIndex" /> is less than 0.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The number of elements in the source collection is greater than the available space from
        /// <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />.
        /// </exception>
        public void CopyTo(Capture[] array, int arrayIndex)
        {
            if (array is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }
            if ((uint)arrayIndex > (uint)array.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.arrayIndex);
            }
            if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);
            }

            for (int i = arrayIndex, j = 0; j < Count; i++, j++)
            {
                array[i] = this[j];
            }
        }

        int IList<Capture>.IndexOf(Capture item)
        {
            for (int i = 0; i < Count; i++)
            {
                if (EqualityComparer<Capture>.Default.Equals(this[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

        void IList<Capture>.Insert(int index, Capture item) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        void IList<Capture>.RemoveAt(int index) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        Capture IList<Capture>.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        void ICollection<Capture>.Add(Capture item) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        void ICollection<Capture>.Clear() =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        bool ICollection<Capture>.Contains(Capture item) =>
            ((IList<Capture>)this).IndexOf(item) >= 0;

        bool ICollection<Capture>.Remove(Capture item) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        int IList.Add(object? value) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        void IList.Clear() =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        bool IList.Contains(object? value) =>
            value is Capture other && ((ICollection<Capture>)this).Contains(other);

        int IList.IndexOf(object? value) =>
            value is Capture other ? ((IList<Capture>)this).IndexOf(other) : -1;

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

        private sealed class Enumerator : IEnumerator<Capture>
        {
            private readonly CaptureCollection _collection;
            private int _index;

            internal Enumerator(CaptureCollection collection)
            {
                Debug.Assert(collection != null, "collection cannot be null.");

                _collection = collection;
                _index = -1;
            }

            public bool MoveNext()
            {
                int size = _collection.Count;

                if (_index >= size)
                {
                    return false;
                }

                _index++;

                return _index < size;
            }

            public Capture Current
            {
                get
                {
                    if (_index < 0 || _index >= _collection.Count)
                    {
                        throw new InvalidOperationException(SR.EnumNotStarted);
                    }

                    return _collection[_index];
                }
            }

            object IEnumerator.Current => Current;

            void IEnumerator.Reset() => _index = -1;

            void IDisposable.Dispose() { }
        }
    }
}
