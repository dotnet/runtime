// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Specialized
{
    /// <devdoc>
    ///  <para>
    ///    This data structure implements IDictionary first using a linked list
    ///    (ListDictionary) and then switching over to use Hashtable when large. This is recommended
    ///    for cases where the number of elements in a dictionary is unknown and might be small.
    ///
    ///    It also has a single boolean parameter to allow case-sensitivity that is not affected by
    ///    ambient culture and has been optimized for looking up case-insensitive symbols
    ///  </para>
    /// </devdoc>
    public class HybridDictionary : IDictionary
    {
        // These numbers have been carefully tested to be optimal. Please don't change them
        // without doing thorough performance testing.
        private const int CutoverPoint = 9;
        private const int InitialHashtableSize = 13;
        private const int FixedSizeCutoverPoint = 6;

        // Instance variables. This keeps the HybridDictionary very light-weight when empty
        private ListDictionary _list;
        private Hashtable _hashtable;
        private readonly bool _caseInsensitive;

        public HybridDictionary()
        {
        }

        public HybridDictionary(int initialSize) : this(initialSize, false)
        {
        }

        public HybridDictionary(bool caseInsensitive)
        {
            _caseInsensitive = caseInsensitive;
        }

        public HybridDictionary(int initialSize, bool caseInsensitive)
        {
            _caseInsensitive = caseInsensitive;
            if (initialSize >= FixedSizeCutoverPoint)
            {
                if (caseInsensitive)
                {
                    _hashtable = new Hashtable(initialSize, StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    _hashtable = new Hashtable(initialSize);
                }
            }
        }

        public object this[object key]
        {
            get
            {
                // Hashtable supports multiple read, one writer thread safety.
                // Although we never made the same guarantee for HybridDictionary,
                // it is still nice to do the same thing here since we have recommended
                // HybridDictionary as replacement for Hashtable.
                ListDictionary cachedList = _list;
                if (_hashtable != null)
                {
                    return _hashtable[key];
                }
                else if (cachedList != null)
                {
                    return cachedList[key];
                }
                // cachedList can be null in too cases:
                //   (1) The dictionary is empty, we will return null in this case
                //   (2) There is writer which is doing ChangeOver. However in that case
                //       we should see the change to hashtable as well.
                //       So it should work just fine.
                else if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }
                return null;
            }
            set
            {
                if (_hashtable != null)
                {
                    _hashtable[key] = value;
                }
                else if (_list != null)
                {
                    if (_list.Count >= CutoverPoint - 1)
                    {
                        ChangeOver();
                        _hashtable[key] = value;
                    }
                    else
                    {
                        _list[key] = value;
                    }
                }
                else
                {
                    _list = new ListDictionary(_caseInsensitive ? StringComparer.OrdinalIgnoreCase : null);
                    _list[key] = value;
                }
            }
        }

        private ListDictionary List
        {
            get
            {
                if (_list == null)
                {
                    _list = new ListDictionary(_caseInsensitive ? StringComparer.OrdinalIgnoreCase : null);
                }
                return _list;
            }
        }

        private void ChangeOver()
        {
            IDictionaryEnumerator en = _list.GetEnumerator();
            Hashtable newTable;
            if (_caseInsensitive)
            {
                newTable = new Hashtable(InitialHashtableSize, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                newTable = new Hashtable(InitialHashtableSize);
            }
            while (en.MoveNext())
            {
                newTable.Add(en.Key, en.Value);
            }

            // Keep the order of writing to hashtable and list.
            // We assume we will see the change in hashtable if list is set to null in
            // this method in another reader thread.
            _hashtable = newTable;
            _list = null;
        }

        public int Count
        {
            get
            {
                ListDictionary cachedList = _list;
                if (_hashtable != null)
                {
                    return _hashtable.Count;
                }
                else if (cachedList != null)
                {
                    return cachedList.Count;
                }
                else
                {
                    return 0;
                }
            }
        }

        public ICollection Keys
        {
            get
            {
                if (_hashtable != null)
                {
                    return _hashtable.Keys;
                }
                else
                {
                    return List.Keys;
                }
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public object SyncRoot
        {
            get
            {
                return this;
            }
        }

        public ICollection Values
        {
            get
            {
                if (_hashtable != null)
                {
                    return _hashtable.Values;
                }
                else
                {
                    return List.Values;
                }
            }
        }

        public void Add(object key, object value)
        {
            if (_hashtable != null)
            {
                _hashtable.Add(key, value);
            }
            else
            {
                if (_list == null)
                {
                    _list = new ListDictionary(_caseInsensitive ? StringComparer.OrdinalIgnoreCase : null);
                    _list.Add(key, value);
                }
                else if (_list.Count + 1 >= CutoverPoint)
                {
                    ChangeOver();
                    _hashtable.Add(key, value);
                }
                else
                {
                    _list.Add(key, value);
                }
            }
        }

        public void Clear()
        {
            if (_hashtable != null)
            {
                Hashtable cachedHashtable = _hashtable;
                _hashtable = null;
                cachedHashtable.Clear();
            }

            if (_list != null)
            {
                ListDictionary cachedList = _list;
                _list = null;
                cachedList.Clear();
            }
        }

        public bool Contains(object key)
        {
            ListDictionary cachedList = _list;
            if (_hashtable != null)
            {
                return _hashtable.Contains(key);
            }
            else if (cachedList != null)
            {
                return cachedList.Contains(key);
            }
            else if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            return false;
        }

        public void CopyTo(Array array, int index)
        {
            if (_hashtable != null)
            {
                _hashtable.CopyTo(array, index);
            }
            else
            {
                List.CopyTo(array, index);
            }
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            if (_hashtable != null)
            {
                return _hashtable.GetEnumerator();
            }
            if (_list == null)
            {
                _list = new ListDictionary(_caseInsensitive ? StringComparer.OrdinalIgnoreCase : null);
            }
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_hashtable != null)
            {
                return _hashtable.GetEnumerator();
            }
            if (_list == null)
            {
                _list = new ListDictionary(_caseInsensitive ? StringComparer.OrdinalIgnoreCase : null);
            }
            return _list.GetEnumerator();
        }

        public void Remove(object key)
        {
            if (_hashtable != null)
            {
                _hashtable.Remove(key);
            }
            else if (_list != null)
            {
                _list.Remove(key);
            }
            else if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
        }
    }
}
