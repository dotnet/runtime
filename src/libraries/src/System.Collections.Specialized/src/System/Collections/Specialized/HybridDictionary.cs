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
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class HybridDictionary : IDictionary
    {
        // These numbers have been carefully tested to be optimal. Please don't change them
        // without doing thorough performance testing.
        private const int CutoverPoint = 9;
        private const int InitialHashtableSize = 13;
        private const int FixedSizeCutoverPoint = 6;

        // Instance variables. This keeps the HybridDictionary very light-weight when empty
        private ListDictionary list; // Do not rename (binary serialization)
        private Hashtable hashtable; // Do not rename (binary serialization)
        private readonly bool caseInsensitive; // Do not rename (binary serialization)

        public HybridDictionary()
        {
        }

        public HybridDictionary(int initialSize) : this(initialSize, false)
        {
        }

        public HybridDictionary(bool caseInsensitive)
        {
            this.caseInsensitive = caseInsensitive;
        }

        public HybridDictionary(int initialSize, bool caseInsensitive)
        {
            this.caseInsensitive = caseInsensitive;
            if (initialSize >= FixedSizeCutoverPoint)
            {
                if (caseInsensitive)
                {
                    hashtable = new Hashtable(initialSize, StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    hashtable = new Hashtable(initialSize);
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
                ListDictionary cachedList = list;
                if (hashtable != null)
                {
                    return hashtable[key];
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
                if (hashtable != null)
                {
                    hashtable[key] = value;
                }
                else if (list != null)
                {
                    if (list.Count >= CutoverPoint - 1)
                    {
                        ChangeOver();
                        hashtable[key] = value;
                    }
                    else
                    {
                        list[key] = value;
                    }
                }
                else
                {
                    list = new ListDictionary(caseInsensitive ? StringComparer.OrdinalIgnoreCase : null);
                    list[key] = value;
                }
            }
        }

        private ListDictionary List
        {
            get
            {
                if (list == null)
                {
                    list = new ListDictionary(caseInsensitive ? StringComparer.OrdinalIgnoreCase : null);
                }
                return list;
            }
        }

        private void ChangeOver()
        {
            IDictionaryEnumerator en = list.GetEnumerator();
            Hashtable newTable;
            if (caseInsensitive)
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
            hashtable = newTable;
            list = null;
        }

        public int Count
        {
            get
            {
                ListDictionary cachedList = list;
                if (hashtable != null)
                {
                    return hashtable.Count;
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
                if (hashtable != null)
                {
                    return hashtable.Keys;
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
                if (hashtable != null)
                {
                    return hashtable.Values;
                }
                else
                {
                    return List.Values;
                }
            }
        }

        public void Add(object key, object value)
        {
            if (hashtable != null)
            {
                hashtable.Add(key, value);
            }
            else
            {
                if (list == null)
                {
                    list = new ListDictionary(caseInsensitive ? StringComparer.OrdinalIgnoreCase : null);
                    list.Add(key, value);
                }
                else if (list.Count + 1 >= CutoverPoint)
                {
                    ChangeOver();
                    hashtable.Add(key, value);
                }
                else
                {
                    list.Add(key, value);
                }
            }
        }

        public void Clear()
        {
            if (hashtable != null)
            {
                Hashtable cachedHashtable = hashtable;
                hashtable = null;
                cachedHashtable.Clear();
            }

            if (list != null)
            {
                ListDictionary cachedList = list;
                list = null;
                cachedList.Clear();
            }
        }

        public bool Contains(object key)
        {
            ListDictionary cachedList = list;
            if (hashtable != null)
            {
                return hashtable.Contains(key);
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
            if (hashtable != null)
            {
                hashtable.CopyTo(array, index);
            }
            else
            {
                List.CopyTo(array, index);
            }
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            if (hashtable != null)
            {
                return hashtable.GetEnumerator();
            }
            if (list == null)
            {
                list = new ListDictionary(caseInsensitive ? StringComparer.OrdinalIgnoreCase : null);
            }
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (hashtable != null)
            {
                return hashtable.GetEnumerator();
            }
            if (list == null)
            {
                list = new ListDictionary(caseInsensitive ? StringComparer.OrdinalIgnoreCase : null);
            }
            return list.GetEnumerator();
        }

        public void Remove(object key)
        {
            if (hashtable != null)
            {
                hashtable.Remove(key);
            }
            else if (list != null)
            {
                list.Remove(key);
            }
            else if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
        }
    }
}
