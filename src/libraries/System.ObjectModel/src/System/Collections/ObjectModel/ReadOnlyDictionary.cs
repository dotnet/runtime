// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.ObjectModel
{
    [Serializable]
    [DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> m_dictionary; // Do not rename (binary serialization)

        [NonSerialized]
        private KeyCollection _keys;
        [NonSerialized]
        private ValueCollection _values;

        public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
        {
            m_dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        protected IDictionary<TKey, TValue> Dictionary => m_dictionary;

        public KeyCollection Keys
        {
            get => _keys ?? (_keys = new KeyCollection(m_dictionary.Keys));
        }

        public ValueCollection Values
        {
            get => _values ?? (_values = new ValueCollection(m_dictionary.Values));
        }

        public bool ContainsKey(TKey key) => m_dictionary.ContainsKey(key);

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

        public bool TryGetValue(TKey key, out TValue value)
        {
            return m_dictionary.TryGetValue(key, out value);
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

        public TValue this[TKey key] => m_dictionary[key];

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get => m_dictionary[key];
            set => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        public int Count => m_dictionary.Count;

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return m_dictionary.Contains(item);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            m_dictionary.CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => true;

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return m_dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)m_dictionary).GetEnumerator();
        }

        private static bool IsCompatibleKey(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return key is TKey;
        }

        void IDictionary.Add(object key, object value)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        void IDictionary.Clear()
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        bool IDictionary.Contains(object key)
        {
            return IsCompatibleKey(key) && ContainsKey((TKey)key);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            IDictionary d = m_dictionary as IDictionary;
            if (d != null)
            {
                return d.GetEnumerator();
            }
            return new DictionaryEnumerator(m_dictionary);
        }

        bool IDictionary.IsFixedSize => true;

        bool IDictionary.IsReadOnly => true;

        ICollection IDictionary.Keys => Keys;

        void IDictionary.Remove(object key)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        ICollection IDictionary.Values => Values;

        object IDictionary.this[object key]
        {
            get
            {
                if (!IsCompatibleKey(key))
                {
                    return null;
                }

                return this[(TKey)key];
            }
            set => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            }
            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(SR.Arg_NonZeroLowerBound, nameof(array));
            }
            if (index < 0 || index > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (array.Length - index < Count)
            {
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);
            }

            KeyValuePair<TKey, TValue>[] pairs = array as KeyValuePair<TKey, TValue>[];
            if (pairs != null)
            {
                m_dictionary.CopyTo(pairs, index);
            }
            else
            {
                DictionaryEntry[] dictEntryArray = array as DictionaryEntry[];
                if (dictEntryArray != null)
                {
                    foreach (var item in m_dictionary)
                    {
                        dictEntryArray[index++] = new DictionaryEntry(item.Key, item.Value);
                    }
                }
                else
                {
                    object[] objects = array as object[];
                    if (objects == null)
                    {
                        throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                    }

                    try
                    {
                        foreach (var item in m_dictionary)
                        {
                            objects[index++] = new KeyValuePair<TKey, TValue>(item.Key, item.Value);
                        }
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                    }
                }
            }
        }

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => (m_dictionary is ICollection coll) ? coll.SyncRoot : this;

        private struct DictionaryEnumerator : IDictionaryEnumerator
        {
            private readonly IDictionary<TKey, TValue> _dictionary;
            private IEnumerator<KeyValuePair<TKey, TValue>> _enumerator;

            public DictionaryEnumerator(IDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _enumerator = _dictionary.GetEnumerator();
            }

            public DictionaryEntry Entry
            {
                get => new DictionaryEntry(_enumerator.Current.Key, _enumerator.Current.Value);
            }

            public object Key => _enumerator.Current.Key;

            public object Value => _enumerator.Current.Value;

            public object Current => Entry;

            public bool MoveNext() => _enumerator.MoveNext();

            public void Reset() => _enumerator.Reset();
        }

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
        [DebuggerDisplay("Count = {Count}")]
        public sealed class KeyCollection : ICollection<TKey>, ICollection, IReadOnlyCollection<TKey>
        {
            private readonly ICollection<TKey> _collection;

            internal KeyCollection(ICollection<TKey> collection)
            {
                _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            }

            void ICollection<TKey>.Add(TKey item)
            {
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }

            void ICollection<TKey>.Clear()
            {
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }

            bool ICollection<TKey>.Contains(TKey item)
            {
                return _collection.Contains(item);
            }

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                _collection.CopyTo(array, arrayIndex);
            }

            public int Count => _collection.Count;

            bool ICollection<TKey>.IsReadOnly => true;

            bool ICollection<TKey>.Remove(TKey item)
            {
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }

            public IEnumerator<TKey> GetEnumerator() => _collection.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_collection).GetEnumerator();

            void ICollection.CopyTo(Array array, int index)
            {
                ReadOnlyDictionaryHelpers.CopyToNonGenericICollectionHelper<TKey>(_collection, array, index);
            }

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => (_collection is ICollection coll) ? coll.SyncRoot : this;
        }

        [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
        [DebuggerDisplay("Count = {Count}")]
        public sealed class ValueCollection : ICollection<TValue>, ICollection, IReadOnlyCollection<TValue>
        {
            private readonly ICollection<TValue> _collection;

            internal ValueCollection(ICollection<TValue> collection)
            {
                _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            }

            void ICollection<TValue>.Add(TValue item)
            {
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }

            void ICollection<TValue>.Clear()
            {
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }

            bool ICollection<TValue>.Contains(TValue item) => _collection.Contains(item);

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                _collection.CopyTo(array, arrayIndex);
            }

            public int Count => _collection.Count;

            bool ICollection<TValue>.IsReadOnly => true;

            bool ICollection<TValue>.Remove(TValue item)
            {
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }
            public IEnumerator<TValue> GetEnumerator() => _collection.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_collection).GetEnumerator();

            void ICollection.CopyTo(Array array, int index)
            {
                ReadOnlyDictionaryHelpers.CopyToNonGenericICollectionHelper<TValue>(_collection, array, index);
            }

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => (_collection is ICollection coll) ? coll.SyncRoot : this;
        }
    }

    // To share code when possible, use a non-generic class to get rid of irrelevant type parameters.
    internal static class ReadOnlyDictionaryHelpers
    {
        // Abstracted away to avoid redundant implementations.
        internal static void CopyToNonGenericICollectionHelper<T>(ICollection<T> collection, Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            }
            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(SR.Arg_NonZeroLowerBound, nameof(array));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (array.Length - index < collection.Count)
            {
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);
            }

            // Easy out if the ICollection<T> implements the non-generic ICollection
            ICollection nonGenericCollection = collection as ICollection;
            if (nonGenericCollection != null)
            {
                nonGenericCollection.CopyTo(array, index);
                return;
            }

            T[] items = array as T[];
            if (items != null)
            {
                collection.CopyTo(items, index);
            }
            else
            {
                // We can't cast array of value type to object[], so we don't support 
                // widening of primitive types here.
                object[] objects = array as object[];
                if (objects == null)
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }

                try
                {
                    foreach (var item in collection)
                    {
                        objects[index++] = item;
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }
            }
        }
    }
}
