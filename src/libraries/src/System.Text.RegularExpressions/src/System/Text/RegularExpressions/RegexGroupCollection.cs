// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The GroupCollection lists the captured Capture numbers
// contained in a compiled Regex.

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
    [DebuggerTypeProxy(typeof(RegexCollectionDebuggerProxy<Group>))]
    [Serializable]
    public class GroupCollection : IList<Group>, IReadOnlyList<Group>, IList
    {
        private readonly Match _match;
        private readonly Hashtable _captureMap;

        // cache of Group objects fed to the user
        private Group[] _groups;

        internal GroupCollection(Match match, Hashtable caps)
        {
            _match = match;
            _captureMap = caps;
        }

        public bool IsReadOnly => true;

        /// <summary>
        /// Returns the number of groups.
        /// </summary>
        public int Count => _match._matchcount.Length;

        public Group this[int groupnum] => GetGroup(groupnum);

        public Group this[string groupname] => _match._regex == null ?
            Group.s_emptyGroup :
            GetGroup(_match._regex.GroupNumberFromName(groupname));

        /// <summary>
        /// Provides an enumerator in the same order as Item[].
        /// </summary>
        public IEnumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<Group> IEnumerable<Group>.GetEnumerator() => new Enumerator(this);

        private Group GetGroup(int groupnum)
        {
            if (_captureMap != null)
            {
                int groupNumImpl;
                if (_captureMap.TryGetValue(groupnum, out groupNumImpl))
                {
                    return GetGroupImpl(groupNumImpl);
                }
            }
            else if (groupnum < _match._matchcount.Length && groupnum >= 0)
            {
                return GetGroupImpl(groupnum);
            }

            return Group.s_emptyGroup;
        }

        /// <summary>
        /// Caches the group objects
        /// </summary>
        private Group GetGroupImpl(int groupnum)
        {
            if (groupnum == 0)
                return _match;

            // Construct all the Group objects the first time GetGroup is called

            if (_groups == null)
            {
                _groups = new Group[_match._matchcount.Length - 1];
                for (int i = 0; i < _groups.Length; i++)
                {
                    string groupname = _match._regex.GroupNameFromNumber(i + 1);
                    _groups[i] = new Group(_match._text, _match._matches[i + 1], _match._matchcount[i + 1], groupname);
                }
            }

            return _groups[groupnum - 1];
        }

        public bool IsSynchronized => false;

        public object SyncRoot => _match;

        public void CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            for (int i = arrayIndex, j = 0; j < Count; i++, j++)
            {
                array.SetValue(this[j], i);
            }
        }

        public void CopyTo(Group[] array, int arrayIndex)
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

        int IList<Group>.IndexOf(Group item)
        {
            var comparer = EqualityComparer<Group>.Default;
            for (int i = 0; i < Count; i++)
            {
                if (comparer.Equals(this[i], item))
                    return i;
            }
            return -1;
        }

        void IList<Group>.Insert(int index, Group item)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        void IList<Group>.RemoveAt(int index)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        Group IList<Group>.this[int index]
        {
            get { return this[index]; }
            set { throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection); }
        }

        void ICollection<Group>.Add(Group item)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        void ICollection<Group>.Clear()
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        bool ICollection<Group>.Contains(Group item) =>
            ((IList<Group>)this).IndexOf(item) >= 0;

        bool ICollection<Group>.Remove(Group item)
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
            value is Group && ((ICollection<Group>)this).Contains((Group)value);

        int IList.IndexOf(object value) =>
            value is Group ? ((IList<Group>)this).IndexOf((Group)value) : -1;

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

        private sealed class Enumerator : IEnumerator<Group>
        {
            private readonly GroupCollection _collection;
            private int _index;

            internal Enumerator(GroupCollection collection)
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

            public Group Current
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
