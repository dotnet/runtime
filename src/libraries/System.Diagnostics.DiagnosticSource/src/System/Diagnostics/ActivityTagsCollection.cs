// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics
{
    /// <summary>
    /// ActivityTagsCollection is collection class used to store the tracing tags.
    /// This collection will be used with classes like <see cref="ActivityEvent"/> and <see cref="ActivityLink"/>
    /// This collection behave as follow:
    ///     - The collection items will be ordered according to the precedence when the item stored.
    ///     - Don't allow a duplication of items with the same key.
    ///     - When using the indexer to store item in the collection,
    ///         - if the item has a key which previousely existed in the collection and the value is null, the collection item matching the key will get removed from the collection.
    ///         - if the item has a key which previousely existed in the collection and the value is not null, the item value will replace the old value stored in the collection.
    ///         - otherwise, the item will get added to the collection.
    ///     - Add method can add a new item to the collection if the collection didn't previously store item with same key. Otherwise, it'll throw exception.
    /// </summary>
    public class ActivityTagsCollection : IDictionary<string, object>
    {
        private List<KeyValuePair<string, object>> _list = new List<KeyValuePair<string, object>>();

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
        public ActivityTagsCollection(IEnumerable<KeyValuePair<string, object>> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            foreach (KeyValuePair<string, object> kvp in list)
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
        ///     - If the key previousely existed in the collection and the value is null, the collection item matching the key will get removed from the collection.
        ///     - If the key previousely existed in the collection and the value is not null, the value will replace the old value stored in the collection.
        ///     - Otherwise, a new item will get added to the collection.
        /// </summary>
        /// <value>Object mapped to the key</value>
        public object this[string key]
        {
            get
            {
                int index = _list.FindIndex(kvp => kvp.Key == key);
                return index < 0 ? null! : _list[index].Value;
            }

            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                int index = _list.FindIndex(kvp => kvp.Key == key);
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
                    _list[index] = new KeyValuePair<string, object>(key, value);
                }
                else
                {
                    _list.Add(new KeyValuePair<string, object>(key, value));
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
                foreach (KeyValuePair<string, object> kvp in _list)
                {
                    list.Add(kvp.Key);
                }
                return list;
            }
        }

        /// <summary>
        /// Get the list of the values of all stored tags.
        /// </summary>
        public ICollection<object> Values
        {
            get
            {
                List<object> list = new List<object>(_list.Count);
                foreach (KeyValuePair<string, object> kvp in _list)
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
        /// Adds an tag with the provided key and value to the collection.
        /// This collection doesn't allow adding two tags with the same key.
        /// </summary>
        /// <param name="key">The tag key.</param>
        /// <param name="value">The tag value.</param>
        public void Add(string key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            int index = _list.FindIndex(kvp => kvp.Key == key);
            if (index >= 0)
            {
                throw new InvalidOperationException(SR.Format(SR.KeyAlreadyExist, key));
            }

            _list.Add(new KeyValuePair<string, object>(key, value));
        }

        /// <summary>
        /// Adds an item to the collection
        /// </summary>
        /// <param name="item">Key and value pair of the tag to add to the collection.</param>
        public void Add(KeyValuePair<string, object> item)
        {
            if (item.Key == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            int index = _list.FindIndex(kvp => kvp.Key == item.Key);
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

        public bool Contains(KeyValuePair<string, object> item) => _list.Contains(item);

        /// <summary>
        /// Determines whether the collection contains an element with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>True if the collection contains tag with that key. False otherwise.</returns>
        public bool ContainsKey(string key) => _list.FindIndex(kvp => kvp.Key == key) >= 0;

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The array that is the destination of the elements copied from collection.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _list.GetEnumerator();

        /// <summary>
        /// Removes the tag with the specified key from the collection.
        /// </summary>
        /// <param name="key">The tag key</param>
        /// <returns>True if the item existed and removed. False otherwise.</returns>
        public bool Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            int index = _list.FindIndex(kvp => kvp.Key == key);
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
        public bool Remove(KeyValuePair<string, object> item) => _list.Remove(item);

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The tag key.</param>
        /// <param name="value">The tag value.</param>
        /// <returns>When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</returns>
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
        {
            int index = _list.FindIndex(kvp => kvp.Key == key);
            if (index >= 0)
            {
                value = _list[index].Value;
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}