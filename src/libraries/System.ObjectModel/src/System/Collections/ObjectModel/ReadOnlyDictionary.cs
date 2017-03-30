// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace System.Collections.ObjectModel
{
    [Serializable]
    [DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _dictionary;
        [NonSerialized]
        private Object _syncRoot;
        [NonSerialized]
        private KeyCollection _keys;
        [NonSerialized]
        private ValueCollection _values;

        public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }
            Contract.EndContractBlock();
            _dictionary = dictionary;
        }

        protected IDictionary<TKey, TValue> Dictionary
        {
            get { return _dictionary; }
        }

        public KeyCollection Keys
        {
            get
            {
                Contract.Ensures(Contract.Result<KeyCollection>() != null);
                if (_keys == null)
                {
                    _keys = new KeyCollection(_dictionary.Keys);
                }
                return _keys;
            }
        }

        public ValueCollection Values
        {
            get
            {
                Contract.Ensures(Contract.Result<ValueCollection>() != null);
                if (_values == null)
                {
                    _values = new ValueCollection(_dictionary.Values);
                }
                return _values;
            }
        }

        #region IDictionary<TKey, TValue> Members

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get
            {
                return Keys;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get
            {
                return Values;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                return _dictionary[key];
            }
        }

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
            get
            {
                return _dictionary[key];
            }
            set
            {
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }
        }

        #endregion

        #region ICollection<KeyValuePair<TKey, TValue>> Members

        public int Count
        {
            get { return _dictionary.Count; }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Contains(item);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _dictionary.CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return true; }
        }

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

        #endregion

        #region IEnumerable<KeyValuePair<TKey, TValue>> Members

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_dictionary).GetEnumerator();
        }

        #endregion

        #region IDictionary Members

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
            IDictionary d = _dictionary as IDictionary;
            if (d != null)
            {
                return d.GetEnumerator();
            }
            return new DictionaryEnumerator(_dictionary);
        }

        bool IDictionary.IsFixedSize
        {
            get { return true; }
        }

        bool IDictionary.IsReadOnly
        {
            get { return true; }
        }

        ICollection IDictionary.Keys
        {
            get
            {
                return Keys;
            }
        }

        void IDictionary.Remove(object key)
        {
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        ICollection IDictionary.Values
        {
            get
            {
                return Values;
            }
        }

        object IDictionary.this[object key]
        {
            get
            {
                if (IsCompatibleKey(key))
                {
                    return this[(TKey)key];
                }
                return null;
            }
            set
            {
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(SR.Arg_NonZeroLowerBound);
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
                _dictionary.CopyTo(pairs, index);
            }
            else
            {
                DictionaryEntry[] dictEntryArray = array as DictionaryEntry[];
                if (dictEntryArray != null)
                {
                    foreach (var item in _dictionary)
                    {
                        dictEntryArray[index++] = new DictionaryEntry(item.Key, item.Value);
                    }
                }
                else
                {
                    object[] objects = array as object[];
                    if (objects == null)
                    {
                        throw new ArgumentException(SR.Argument_InvalidArrayType);
                    }

                    try
                    {
                        foreach (var item in _dictionary)
                        {
                            objects[index++] = new KeyValuePair<TKey, TValue>(item.Key, item.Value);
                        }
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw new ArgumentException(SR.Argument_InvalidArrayType);
                    }
                }
            }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    ICollection c = _dictionary as ICollection;
                    if (c != null)
                    {
                        _syncRoot = c.SyncRoot;
                    }
                    else
                    {
                        System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                    }
                }
                return _syncRoot;
            }
        }

        [Serializable]
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
                get { return new DictionaryEntry(_enumerator.Current.Key, _enumerator.Current.Value); }
            }

            public object Key
            {
                get { return _enumerator.Current.Key; }
            }

            public object Value
            {
                get { return _enumerator.Current.Value; }
            }

            public object Current
            {
                get { return Entry; }
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator.Reset();
            }
        }

        #endregion

        #region IReadOnlyDictionary members

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        {
            get
            {
                return Keys;
            }
        }

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        {
            get
            {
                return Values;
            }
        }

        #endregion IReadOnlyDictionary members

        [Serializable]
        [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
        [DebuggerDisplay("Count = {Count}")]
        public sealed class KeyCollection : ICollection<TKey>, ICollection, IReadOnlyCollection<TKey>
        {
            private readonly ICollection<TKey> _collection;
            [NonSerialized]
            private Object _syncRoot;

            internal KeyCollection(ICollection<TKey> collection)
            {
                if (collection == null)
                {
                    throw new ArgumentNullException(nameof(collection));
                }
                _collection = collection;
            }

            #region ICollection<T> Members

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

            public int Count
            {
                get { return _collection.Count; }
            }

            bool ICollection<TKey>.IsReadOnly
            {
                get { return true; }
            }

            bool ICollection<TKey>.Remove(TKey item)
            {
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }

            #endregion

            #region IEnumerable<T> Members

            public IEnumerator<TKey> GetEnumerator()
            {
                return _collection.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_collection).GetEnumerator();
            }

            #endregion

            #region ICollection Members

            void ICollection.CopyTo(Array array, int index)
            {
                ReadOnlyDictionaryHelpers.CopyToNonGenericICollectionHelper<TKey>(_collection, array, index);
            }

            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            object ICollection.SyncRoot
            {
                get
                {
                    if (_syncRoot == null)
                    {
                        ICollection c = _collection as ICollection;
                        if (c != null)
                        {
                            _syncRoot = c.SyncRoot;
                        }
                        else
                        {
                            System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                        }
                    }
                    return _syncRoot;
                }
            }
            #endregion
        }

        [Serializable]
        [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
        [DebuggerDisplay("Count = {Count}")]
        public sealed class ValueCollection : ICollection<TValue>, ICollection, IReadOnlyCollection<TValue>
        {
            private readonly ICollection<TValue> _collection;
            [NonSerialized]
            private Object _syncRoot;

            internal ValueCollection(ICollection<TValue> collection)
            {
                if (collection == null)
                {
                    throw new ArgumentNullException(nameof(collection));
                }
                _collection = collection;
            }

            #region ICollection<T> Members

            void ICollection<TValue>.Add(TValue item)
            {
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }

            void ICollection<TValue>.Clear()
            {
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }

            bool ICollection<TValue>.Contains(TValue item)
            {
                return _collection.Contains(item);
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                _collection.CopyTo(array, arrayIndex);
            }

            public int Count
            {
                get { return _collection.Count; }
            }

            bool ICollection<TValue>.IsReadOnly
            {
                get { return true; }
            }

            bool ICollection<TValue>.Remove(TValue item)
            {
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }

            #endregion

            #region IEnumerable<T> Members

            public IEnumerator<TValue> GetEnumerator()
            {
                return _collection.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_collection).GetEnumerator();
            }

            #endregion

            #region ICollection Members

            void ICollection.CopyTo(Array array, int index)
            {
                ReadOnlyDictionaryHelpers.CopyToNonGenericICollectionHelper<TValue>(_collection, array, index);
            }

            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            object ICollection.SyncRoot
            {
                get
                {
                    if (_syncRoot == null)
                    {
                        ICollection c = _collection as ICollection;
                        if (c != null)
                        {
                            _syncRoot = c.SyncRoot;
                        }
                        else
                        {
                            System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                        }
                    }
                    return _syncRoot;
                }
            }
            #endregion ICollection Members
        }
    }

    // To share code when possible, use a non-generic class to get rid of irrelevant type parameters.
    internal static class ReadOnlyDictionaryHelpers
    {
        #region Helper method for our KeyCollection and ValueCollection

        // Abstracted away to avoid redundant implementations.
        internal static void CopyToNonGenericICollectionHelper<T>(ICollection<T> collection, Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(SR.Arg_NonZeroLowerBound);
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
                /*
                    FxOverRh: Type.IsAssignableNot() not an api on that platform.

                //
                // Catch the obvious case assignment will fail.
                // We can found all possible problems by doing the check though.
                // For example, if the element type of the Array is derived from T,
                // we can't figure out if we can successfully copy the element beforehand.
                //
                Type targetType = array.GetType().GetElementType();
                Type sourceType = typeof(T);
                if (!(targetType.IsAssignableFrom(sourceType) || sourceType.IsAssignableFrom(targetType))) {
                    throw new ArgumentException(SR.Argument_InvalidArrayType);
                }
                */

                //
                // We can't cast array of value type to object[], so we don't support 
                // widening of primitive types here.
                //
                object[] objects = array as object[];
                if (objects == null)
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType);
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
                    throw new ArgumentException(SR.Argument_InvalidArrayType);
                }
            }
        }
        #endregion Helper method for our KeyCollection and ValueCollection
    }
}

