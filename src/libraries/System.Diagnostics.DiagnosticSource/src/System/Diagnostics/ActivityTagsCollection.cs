// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics
{
    /// <summary>
    /// ActivityTagsCollection is a collection class used to store tracing tags.
    /// This collection will be used with classes like <see cref="ActivityEvent"/> and <see cref="ActivityLink"/>.
    /// This collection behaves as follows:
    ///     - The collection items will be ordered according to how they are added.
    ///     - Don't allow duplication of items with the same key.
    ///     - When using the indexer to store an item in the collection:
    ///         - If the item has a key that previously existed in the collection and the value is null, the collection item matching the key will be removed from the collection.
    ///         - If the item has a key that previously existed in the collection and the value is not null, the new item value will replace the old value stored in the collection.
    ///         - Otherwise, the item will be added to the collection.
    ///     - Add method will add a new item to the collection if an item doesn't already exist with the same key. Otherwise, it will throw an exception.
    /// </summary>
    public class ActivityTagsCollection : IDictionary<string, object?>
    {
        private readonly List<KeyValuePair<string, object?>> _list = new List<KeyValuePair<string, object?>>();

        /// <summary>
        /// Create a new instance of the collection.
        /// </summary>
        public ActivityTagsCollection()
        {
        }

        /// <summary>
        /// Create a new instance of the collection and store the input list items in the collection.
        /// </summary>
        /// <param name="list">Initial list to store in the collection.</param>
        public ActivityTagsCollection(IEnumerable<KeyValuePair<string, object?>> list)
        {
            if (list is null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            foreach (KeyValuePair<string, object?> kvp in list)
            {
                if (kvp.Key != null)
                {
                    this[kvp.Key] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Get or set collection item
        /// When setting a value to this indexer property, the following behavior will be observed:
        ///     - If the key previously existed in the collection and the value is null, the collection item matching the key will get removed from the collection.
        ///     - If the key previously existed in the collection and the value is not null, the value will replace the old value stored in the collection.
        ///     - Otherwise, a new item will get added to the collection.
        /// </summary>
        /// <value>Object mapped to the key</value>
        public object? this[string key]
        {
            get
            {
                int index = FindIndex(key);
                return index < 0 ? null : _list[index].Value;
            }

            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                int index = FindIndex(key);
                if (value == null)
                {
                    if (index >= 0)
                    {
                        _list.RemoveAt(index);
                    }
                    return;
                }

                if (index >= 0)
                {
                    _list[index] = new KeyValuePair<string, object?>(key, value);
                }
                else
                {
                    _list.Add(new KeyValuePair<string, object?>(key, value));
                }
            }
        }

        /// <summary>
        /// Get the list of the keys of all stored tags.
        /// </summary>
        public ICollection<string> Keys
        {
            get
            {
                List<string> list = new List<string>(_list.Count);
                foreach (KeyValuePair<string, object?> kvp in _list)
                {
                    list.Add(kvp.Key);
                }
                return list;
            }
        }

        /// <summary>
        /// Get the list of the values of all stored tags.
        /// </summary>
        public ICollection<object?> Values
        {
            get
            {
                List<object?> list = new List<object?>(_list.Count);
                foreach (KeyValuePair<string, object?> kvp in _list)
                {
                    list.Add(kvp.Value);
                }
                return list;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets the number of elements contained in the collection.
        /// </summary>
        public int Count => _list.Count;

        /// <summary>
        /// Adds a tag with the provided key and value to the collection.
        /// This collection doesn't allow adding two tags with the same key.
        /// </summary>
        /// <param name="key">The tag key.</param>
        /// <param name="value">The tag value.</param>
        public void Add(string key, object? value)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            int index = FindIndex(key);
            if (index >= 0)
            {
                throw new InvalidOperationException(SR.Format(SR.KeyAlreadyExist, key));
            }

            _list.Add(new KeyValuePair<string, object?>(key, value));
        }

        /// <summary>
        /// Adds an item to the collection
        /// </summary>
        /// <param name="item">Key and value pair of the tag to add to the collection.</param>
        public void Add(KeyValuePair<string, object?> item)
        {
            if (item.Key == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            int index = FindIndex(item.Key);
            if (index >= 0)
            {
                throw new InvalidOperationException(SR.Format(SR.KeyAlreadyExist, item.Key));
            }

            _list.Add(item);
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear() => _list.Clear();

        public bool Contains(KeyValuePair<string, object?> item) => _list.Contains(item);

        /// <summary>
        /// Determines whether the collection contains an element with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>True if the collection contains tag with that key. False otherwise.</returns>
        public bool ContainsKey(string key) => FindIndex(key) >= 0;

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The array that is the destination of the elements copied from collection.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => new Enumerator(_list);

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public Enumerator GetEnumerator() => new Enumerator(_list);

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_list);

        /// <summary>
        /// Removes the tag with the specified key from the collection.
        /// </summary>
        /// <param name="key">The tag key</param>
        /// <returns>True if the item existed and removed. False otherwise.</returns>
        public bool Remove(string key)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            int index = FindIndex(key);
            if (index >= 0)
            {
                _list.RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes the first occurrence of a specific item from the collection.
        /// </summary>
        /// <param name="item">The tag key value pair to remove.</param>
        /// <returns>True if item was successfully removed from the collection; otherwise, false. This method also returns false if item is not found in the original collection.</returns>
        public bool Remove(KeyValuePair<string, object?> item) => _list.Remove(item);

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The tag key.</param>
        /// <param name="value">The tag value.</param>
        /// <returns>When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</returns>
        public bool TryGetValue(string key, out object? value)
        {
            int index = FindIndex(key);
            if (index >= 0)
            {
                value = _list[index].Value;
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// FindIndex finds the index of item in the list having a key matching the input key.
        /// We didn't use List.FindIndex to avoid the extra allocation caused by the closure when calling the Predicate delegate.
        /// </summary>
        /// <param name="key">The key to search the item in the list</param>
        /// <returns>The index of the found item, or -1 if the item not found.</returns>
        private int FindIndex(string key)
        {
            for (int i = 0; i < _list.Count; i++)
            {
                if (_list[i].Key == key)
                {
                    return i;
                }
            }

            return -1;
        }

        public struct Enumerator : IEnumerator<KeyValuePair<string, object?>>, IEnumerator
        {
            private List<KeyValuePair<string, object?>>.Enumerator _enumerator;
            internal Enumerator(List<KeyValuePair<string, object?>> list) => _enumerator = list.GetEnumerator();

            public KeyValuePair<string, object?> Current => _enumerator.Current;
            object IEnumerator.Current => ((IEnumerator)_enumerator).Current;
            public void Dispose() => _enumerator.Dispose();
            public bool MoveNext() => _enumerator.MoveNext();
            void IEnumerator.Reset() => ((IEnumerator)_enumerator).Reset();
        }
    }
}
