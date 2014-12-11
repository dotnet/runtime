// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The CaptureCollection lists the captured Capture numbers
// contained in a compiled Regex.

using System.Collections;

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
        internal Group _group;
        internal int _capcount;
        internal Capture[] _captures;

        internal CaptureCollection(Group group)
        {
            _group = group;
            _capcount = _group._capcount;
        }

        /// <summary>
        /// The object on which to synchronize.
        /// </summary>
        Object ICollection.SyncRoot
        {
            get
            {
                return _group;
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
        /// Returns the number of captures.
        /// </summary>
        public int Count
        {
            get
            {
                return _capcount;
            }
        }

        /// <summary>
        /// Returns a specific capture, by index, in this collection.
        /// </summary>
        public Capture this[int i]
        {
            get
            {
                return GetCapture(i);
            }
        }

        /// <summary>
        /// Copies all the elements of the collection to the given array
        /// beginning at the given index.
        /// </summary>
        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            for (int i = arrayIndex, j = 0; j < Count; i++, j++)
            {
                array.SetValue(this[j], i);
            }
        }

        /// <summary>
        /// Provides an enumerator in the same order as Item[].
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return new CaptureEnumerator(this);
        }

        /*
         * Nonpublic code to return set of captures for the group
         */
        internal Capture GetCapture(int i)
        {
            if (i == _capcount - 1 && i >= 0)
                return _group;

            if (i >= _capcount || i < 0)
                throw new ArgumentOutOfRangeException("i");

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
    }


    /*
     * This non-public enumerator lists all the captures
     * Should it be public?
     */

    internal class CaptureEnumerator : IEnumerator
    {
        internal CaptureCollection _rcc;
        internal int _curindex;

        /*
         * Nonpublic constructor
         */
        internal CaptureEnumerator(CaptureCollection rcc)
        {
            _curindex = -1;
            _rcc = rcc;
        }

        /*
         * As required by IEnumerator
         */
        public bool MoveNext()
        {
            int size = _rcc.Count;

            if (_curindex >= size)
                return false;

            _curindex++;

            return (_curindex < size);
        }

        /*
         * As required by IEnumerator
         */
        public Object Current
        {
            get { return Capture; }
        }

        /*
         * Returns the current capture
         */
        public Capture Capture
        {
            get
            {
                if (_curindex < 0 || _curindex >= _rcc.Count)
                    throw new InvalidOperationException(SR.EnumNotStarted);

                return _rcc[_curindex];
            }
        }

        /*
         * Reset to before the first item
         */
        public void Reset()
        {
            _curindex = -1;
        }
    }
}
