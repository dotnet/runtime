// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The CaptureCollection lists the captured Capture numbers
// contained in a compiled Regex.

using System.Collections;
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
    public class CaptureCollection : ICollection
    {
        private readonly Group _group;
        private readonly int _capcount;
        private Capture[] _captures;

        internal CaptureCollection(Group group)
        {
            _group = group;
            _capcount = _group._capcount;
        }

        /// <summary>
        /// Returns the number of captures.
        /// </summary>
        public int Count
        {
            get { return _capcount; }
        }

        /// <summary>
        /// Returns a specific capture, by index, in this collection.
        /// </summary>
        public Capture this[int i]
        {
            get { return GetCapture(i); }
        }

        /// <summary>
        /// Provides an enumerator in the same order as Item[].
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

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

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return _group; }
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            for (int i = arrayIndex, j = 0; j < Count; i++, j++)
            {
                array.SetValue(this[j], i);
            }
        }

        private class Enumerator : IEnumerator
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
