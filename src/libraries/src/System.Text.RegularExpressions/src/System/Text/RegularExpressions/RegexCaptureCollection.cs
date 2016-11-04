// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The CaptureCollection lists the captured Capture numbers
// contained in a compiled Regex.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions
{
    /*
     * This collection returns the Captures for a group
     * in the order in which they were matched (left to right
     * or right to left). It is created by Group.Captures
     */
    /// <summary>
    /// Represents a sequence of capture substrings. The object is used
    /// to return the set of captures done by a single capturing group.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(RegexCollectionDebuggerProxy<Capture>))]
    [Serializable]
    public class CaptureCollection : IList<Capture>, IReadOnlyList<Capture>, IList
    {
        private readonly Group _group;
        private readonly int _capcount;
        private Capture[] _captures;

        internal CaptureCollection(Group group)
        {
            _group = group;
            _capcount = _group._capcount;
        }

        public bool IsReadOnly => true;

        /// <summary>
        /// Returns the number of captures.
        /// </summary>
        public int Count => _capcount;

        /// <summary>
        /// Returns a specific capture, by index, in this collection.
        /// </summary>
        public Capture this[int i] => GetCapture(i);

        /// <summary>
        /// Provides an enumerator in the same order as Item[].
        /// </summary>
        public IEnumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<Capture> IEnumerable<Capture>.GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Returns the set of captures for the group
        /// </summary>
        private Capture GetCapture(int i)
        {
            if (i == _capcount - 1 && i >= 0)
                return _group;

            if (i >= _capcount || i < 0)
                throw new ArgumentOutOfRangeException(nameof(i));

            // first time a capture is accessed, compute them all
            if (_captures == null)
            {
                _captures = new Capture[_capcount];
                for (int j = 0; j < _capcount - 1; j++)
                {
                    _captures[j] = new Capture(_group._text, _group._caps[j * 2], _group._caps[j * 2 + 1]);
                }
            }

            return _captures[i];
        }

        public bool IsSynchronized => false;

        public object SyncRoot => _group;

        public void CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            for (int i = arrayIndex, j = 0; j < Count; i++, j++)
            {
                array.SetValue(this[j], i);
            }
        }

        public void CopyTo(Capture[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);

            for (int i = arrayIndex, j = 0; j < Count; i++, j++)
            {
                array[i] = this[j];
            }
        }

        int IList<Capture>.IndexOf(Capture item)
        {
            var comparer = EqualityComparer<Capture>.Default;
            for (int i = 0; i < Count; i++)
            {
                if (comparer.Equals(this[i], item))
                    return i;
            }
            return -1;
        }

        void IList<Capture>.Insert(int index, Capture item)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        void IList<Capture>.RemoveAt(int index)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        Capture IList<Capture>.this[int index]
        {
            get { return this[index]; }
            set { throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection); }
        }

        void ICollection<Capture>.Add(Capture item)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        void ICollection<Capture>.Clear()
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        bool ICollection<Capture>.Contains(Capture item) =>
            ((IList<Capture>)this).IndexOf(item) >= 0;

        bool ICollection<Capture>.Remove(Capture item)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        int IList.Add(object value)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        void IList.Clear()
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        bool IList.Contains(object value) =>
            value is Capture && ((ICollection<Capture>)this).Contains((Capture)value);

        int IList.IndexOf(object value) =>
            value is Capture ? ((IList<Capture>)this).IndexOf((Capture)value) : -1;

        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        bool IList.IsFixedSize => true;

        void IList.Remove(object value)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        object IList.this[int index]
        {
            get { return this[index]; }
            set { throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection); }
        }

        [Serializable]
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
                    return false;

                _index++;

                return _index < size;
            }

            public Capture Current
            {
                get
                {
                    if (_index < 0 || _index >= _collection.Count)
                        throw new InvalidOperationException(SR.EnumNotStarted);

                    return _collection[_index];
                }
            }

            object IEnumerator.Current => Current;

            void IEnumerator.Reset()
            {
                _index = -1;
            }

            void IDisposable.Dispose() { }
        }
    }
}
