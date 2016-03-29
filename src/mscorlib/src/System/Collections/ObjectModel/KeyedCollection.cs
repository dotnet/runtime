// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Collections.ObjectModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
       
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(false)]
    [DebuggerTypeProxy(typeof(Mscorlib_KeyedCollectionDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]        
    public abstract class KeyedCollection<TKey,TItem>: Collection<TItem>
    {
        const int defaultThreshold = 0;

        IEqualityComparer<TKey> comparer;
        Dictionary<TKey,TItem> dict;
        int keyCount;
        int threshold;

        protected KeyedCollection(): this(null, defaultThreshold) {}

        protected KeyedCollection(IEqualityComparer<TKey> comparer): this(comparer, defaultThreshold) {}


        protected KeyedCollection(IEqualityComparer<TKey> comparer, int dictionaryCreationThreshold)
            : base(new List<TItem>()) { // Be explicit about the use of List<T> so we can foreach over
                                        // Items internally without enumerator allocations.
            if (comparer == null) { 
                comparer = EqualityComparer<TKey>.Default;
            }

            if (dictionaryCreationThreshold == -1) {
                dictionaryCreationThreshold = int.MaxValue;
            }

            if( dictionaryCreationThreshold < -1) {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.dictionaryCreationThreshold, ExceptionResource.ArgumentOutOfRange_InvalidThreshold);
            }

            this.comparer = comparer;
            this.threshold = dictionaryCreationThreshold;
        }

        /// <summary>
        /// Enables the use of foreach internally without allocations using <see cref="List{T}"/>'s struct enumerator.
        /// </summary>
        new private List<TItem> Items {
            get {
                Contract.Assert(base.Items is List<TItem>);

                return (List<TItem>)base.Items;
            }
        }

        public IEqualityComparer<TKey> Comparer {
            get {
                return comparer;                
            }               
        }

        public TItem this[TKey key] {
            get {
                if( key == null) {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
                }

                if (dict != null) {
                    return dict[key];
                }

                foreach (TItem item in Items) {
                    if (comparer.Equals(GetKeyForItem(item), key)) return item;
                }

                ThrowHelper.ThrowKeyNotFoundException();
                return default(TItem);
            }
        }

        public bool Contains(TKey key) {
            if( key == null) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }
            
            if (dict != null) {
                return dict.ContainsKey(key);
            }

            foreach (TItem item in Items) {
                if (comparer.Equals(GetKeyForItem(item), key)) return true;
            }
            return false;
        }

        private bool ContainsItem(TItem item) {                        
            TKey key;
            if( (dict == null) || ((key = GetKeyForItem(item)) == null)) {
                return Items.Contains(item);
            }

            TItem itemInDict;
            bool exist = dict.TryGetValue(key, out itemInDict);
            if( exist) {
                return EqualityComparer<TItem>.Default.Equals(itemInDict, item);
            }
            return false;
        }

        public bool Remove(TKey key) {
            if( key == null) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }
            
            if (dict != null) {
                if (dict.ContainsKey(key)) {
                    return Remove(dict[key]);
                }

                return false;
            }

            for (int i = 0; i < Items.Count; i++) {
                if (comparer.Equals(GetKeyForItem(Items[i]), key)) {
                    RemoveItem(i);
                    return true;
                }
            }
            return false;
        }

        protected IDictionary<TKey,TItem> Dictionary {
            get { return dict; }
        }

        protected void ChangeItemKey(TItem item, TKey newKey) {
            // check if the item exists in the collection
            if( !ContainsItem(item)) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_ItemNotExist);
            }

            TKey oldKey = GetKeyForItem(item);            
            if (!comparer.Equals(oldKey, newKey)) {
                if (newKey != null) {
                    AddKey(newKey, item);
                }

                if (oldKey != null) {
                    RemoveKey(oldKey);
                }
            }
        }

        protected override void ClearItems() {
            base.ClearItems();
            if (dict != null) {
                dict.Clear();
            }

            keyCount = 0;
        }

        protected abstract TKey GetKeyForItem(TItem item);

        protected override void InsertItem(int index, TItem item) {
            TKey key = GetKeyForItem(item);
            if (key != null) {
                AddKey(key, item);
            }
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index) {
            TKey key = GetKeyForItem(Items[index]);
            if (key != null) {
                RemoveKey(key);
            }
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, TItem item) {
            TKey newKey = GetKeyForItem(item);
            TKey oldKey = GetKeyForItem(Items[index]);

            if (comparer.Equals(oldKey, newKey)) {
                if (newKey != null && dict != null) {
                    dict[newKey] = item;
                }
            }
            else {
                if (newKey != null) {
                    AddKey(newKey, item);
                }

                if (oldKey != null) {
                    RemoveKey(oldKey);
                }
            }
            base.SetItem(index, item);
        }

        private void AddKey(TKey key, TItem item) {
            if (dict != null) {
                dict.Add(key, item);
            }
            else if (keyCount == threshold) {
                CreateDictionary();
                dict.Add(key, item);
            }
            else {
                if (Contains(key)) {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_AddingDuplicate);
                }

                keyCount++;
            }
        }

        private void CreateDictionary() {
            dict = new Dictionary<TKey,TItem>(comparer);
            foreach (TItem item in Items) {
                TKey key = GetKeyForItem(item);
                if (key != null) {
                    dict.Add(key, item);
                }
            }
        }

        private void RemoveKey(TKey key) {
            Contract.Assert(key != null, "key shouldn't be null!");
            if (dict != null) {
                dict.Remove(key);
            }
            else {
                keyCount--;
            }
        }
    }
}
