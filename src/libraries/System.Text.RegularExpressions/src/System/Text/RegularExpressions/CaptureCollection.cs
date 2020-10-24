// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Represents a sequence of capture substrings. The object is used
    /// to return the set of captures done by a single capturing group.
    /// </summary>
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

        public bool IsReadOnly => true;

        /// <summary>Returns the number of captures.</summary>
        public int Count => _capcount;

        /// <summary>Returns a specific capture, by index, in this collection.</summary>
        public Capture this[int i] => GetCapture(i);

        /// <summary>Provides an enumerator in the same order as Item[].</summary>
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

        public bool IsSynchronized => false;

        public object SyncRoot => _group;

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
