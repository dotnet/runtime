// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.ObjectModel
{
    [Serializable]
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public abstract class KeyedCollection<TKey, TItem> : Collection<TItem>
    {
        private const int defaultThreshold = 0;

        private readonly IEqualityComparer<TKey> _comparer;
        private Dictionary<TKey, TItem> _dict;
        private int _keyCount;
        private readonly int _threshold;

        protected KeyedCollection() : this(null, defaultThreshold) { }

        protected KeyedCollection(IEqualityComparer<TKey> comparer) : this(comparer, defaultThreshold) { }


        protected KeyedCollection(IEqualityComparer<TKey> comparer, int dictionaryCreationThreshold)
            : base(new List<TItem>()) // Be explicit about the use of List<T> so we can foreach over
                                      // Items internally without enumerator allocations.
        {
            if (comparer == null)
            {
                comparer = EqualityComparer<TKey>.Default;
            }

            if (dictionaryCreationThreshold == -1)
            {
                dictionaryCreationThreshold = int.MaxValue;
            }

            if (dictionaryCreationThreshold < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(dictionaryCreationThreshold), SR.ArgumentOutOfRange_InvalidThreshold);
            }

            _comparer = comparer;
            _threshold = dictionaryCreationThreshold;
        }

        /// <summary>
        /// Enables the use of foreach internally without allocations using <see cref="List{T}"/>'s struct enumerator.
        /// </summary>
        new private List<TItem> Items
        {
            get
            {
                Debug.Assert(base.Items is List<TItem>);

                return (List<TItem>)base.Items;
            }
        }

        public IEqualityComparer<TKey> Comparer
        {
            get
            {
                return _comparer;
            }
        }

        public TItem this[TKey key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                if (_dict != null)
                {
                    return _dict[key];
                }

                foreach (TItem item in Items)
                {
                    if (_comparer.Equals(GetKeyForItem(item), key)) return item;
                }

                throw new KeyNotFoundException();
            }
        }

        public bool Contains(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (_dict != null)
            {
                return _dict.ContainsKey(key);
            }

            foreach (TItem item in Items)
            {
                if (_comparer.Equals(GetKeyForItem(item), key)) return true;
            }
            return false;
        }

        private bool ContainsItem(TItem item)
        {
            TKey key;
            if ((_dict == null) || ((key = GetKeyForItem(item)) == null))
            {
                return Items.Contains(item);
            }

            TItem itemInDict;
            bool exist = _dict.TryGetValue(key, out itemInDict);
            if (exist)
            {
                return EqualityComparer<TItem>.Default.Equals(itemInDict, item);
            }
            return false;
        }

        public bool Remove(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (_dict != null)
            {
                TItem item;
                return _dict.TryGetValue(key, out item) && Remove(item);
            }

            for (int i = 0; i < Items.Count; i++)
            {
                if (_comparer.Equals(GetKeyForItem(Items[i]), key))
                {
                    RemoveItem(i);
                    return true;
                }
            }
            return false;
        }

        protected IDictionary<TKey, TItem> Dictionary
        {
            get { return _dict; }
        }

        protected void ChangeItemKey(TItem item, TKey newKey)
        {
            // Check if the item exists in the collection
            if (!ContainsItem(item))
            {
                throw new ArgumentException(SR.Argument_ItemNotExist);
            }

            TKey oldKey = GetKeyForItem(item);
            if (!_comparer.Equals(oldKey, newKey))
            {
                if (newKey != null)
                {
                    AddKey(newKey, item);
                }

                if (oldKey != null)
                {
                    RemoveKey(oldKey);
                }
            }
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            if (_dict != null)
            {
                _dict.Clear();
            }

            _keyCount = 0;
        }

        protected abstract TKey GetKeyForItem(TItem item);

        protected override void InsertItem(int index, TItem item)
        {
            TKey key = GetKeyForItem(item);
            if (key != null)
            {
                AddKey(key, item);
            }
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            TKey key = GetKeyForItem(Items[index]);
            if (key != null)
            {
                RemoveKey(key);
            }
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, TItem item)
        {
            TKey newKey = GetKeyForItem(item);
            TKey oldKey = GetKeyForItem(Items[index]);

            if (_comparer.Equals(oldKey, newKey))
            {
                if (newKey != null && _dict != null)
                {
                    _dict[newKey] = item;
                }
            }
            else
            {
                if (newKey != null)
                {
                    AddKey(newKey, item);
                }

                if (oldKey != null)
                {
                    RemoveKey(oldKey);
                }
            }
            base.SetItem(index, item);
        }

        private void AddKey(TKey key, TItem item)
        {
            if (_dict != null)
            {
                _dict.Add(key, item);
            }
            else if (_keyCount == _threshold)
            {
                CreateDictionary();
                _dict.Add(key, item);
            }
            else
            {
                if (Contains(key))
                {
                    throw new ArgumentException(SR.Format(SR.Argument_AddingDuplicate, key));
                }

                _keyCount++;
            }
        }

        private void CreateDictionary()
        {
            _dict = new Dictionary<TKey, TItem>(_comparer);
            foreach (TItem item in Items)
            {
                TKey key = GetKeyForItem(item);
                if (key != null)
                {
                    _dict.Add(key, item);
                }
            }
        }

        private void RemoveKey(TKey key)
        {
            Debug.Assert(key != null, "key shouldn't be null!");
            if (_dict != null)
            {
                _dict.Remove(key);
            }
            else
            {
                _keyCount--;
            }
        }
    }
}
