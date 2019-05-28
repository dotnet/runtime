// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // This is a set of stub methods implementing the support for the IReadOnlyDictionary`2 interface on WinRT
    // objects that support IMapView`2. Used by the interop mashaling infrastructure.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not IMapViewToIReadOnlyDictionaryAdapter objects. Rather, they are of type
    // IMapView<K, V>. No actual IMapViewToIReadOnlyDictionaryAdapter object is ever instantiated. Thus, you will see
    // a lot of expressions that cast "this" to "IMapView<K, V>".
    [DebuggerDisplay("Count = {Count}")]
    internal sealed class IMapViewToIReadOnlyDictionaryAdapter
    {
        private IMapViewToIReadOnlyDictionaryAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        // V this[K key] { get }
        internal V Indexer_Get<K, V>(K key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            IMapView<K, V> _this = Unsafe.As<IMapView<K, V>>(this);
            return Lookup(_this, key);
        }

        // IEnumerable<K> Keys { get }
        internal IEnumerable<K> Keys<K, V>() where K : object
        {
            IMapView<K, V> _this = Unsafe.As<IMapView<K, V>>(this);
            IReadOnlyDictionary<K, V> roDictionary = (IReadOnlyDictionary<K, V>)_this;
            return new ReadOnlyDictionaryKeyCollection<K, V>(roDictionary);
        }

        // IEnumerable<V> Values { get }
        internal IEnumerable<V> Values<K, V>() where K : object
        {
            IMapView<K, V> _this = Unsafe.As<IMapView<K, V>>(this);
            IReadOnlyDictionary<K, V> roDictionary = (IReadOnlyDictionary<K, V>)_this;
            return new ReadOnlyDictionaryValueCollection<K, V>(roDictionary);
        }

        // bool ContainsKey(K key)
        internal bool ContainsKey<K, V>(K key) where K : object
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            IMapView<K, V> _this = Unsafe.As<IMapView<K, V>>(this);
            return _this.HasKey(key);
        }

        // bool TryGetValue(TKey key, out TValue value)
        internal bool TryGetValue<K, V>(K key, [MaybeNullWhen(false)] out V value) where K : object
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            IMapView<K, V> _this = Unsafe.As<IMapView<K, V>>(this);

            // It may be faster to call HasKey then Lookup.  On failure, we would otherwise
            // throw an exception from Lookup.
            if (!_this.HasKey(key))
            {
                value = default!;
                return false;
            }

            try
            {
                value = _this.Lookup(key);
                return true;
            }
            catch (Exception ex)  // Still may hit this case due to a race condition
            {
                if (HResults.E_BOUNDS == ex.HResult)
                {
                    value = default!;
                    return false;
                }
                throw;
            }
        }

        #region Helpers

        private static V Lookup<K, V>(IMapView<K, V> _this, K key)
        {
            Debug.Assert(null != key);

            try
            {
                return _this.Lookup(key);
            }
            catch (Exception ex)
            {
                if (HResults.E_BOUNDS == ex.HResult)
                    throw new KeyNotFoundException(SR.Format(SR.Arg_KeyNotFoundWithKey, key.ToString()));
                throw;
            }
        }

        #endregion Helpers
    }

    // Note: One day we may make these return IReadOnlyCollection<T>
    [DebuggerDisplay("Count = {Count}")]
    internal sealed class ReadOnlyDictionaryKeyCollection<TKey, TValue> : IEnumerable<TKey> where TKey : object
    {
        private readonly IReadOnlyDictionary<TKey, TValue> dictionary;

        public ReadOnlyDictionaryKeyCollection(IReadOnlyDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            this.dictionary = dictionary;
        }

        /*
        public void CopyTo(TKey[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (array.Length <= index && this.Count > 0)
                throw new ArgumentException(SR.Arg_IndexOutOfRangeException);
            if (array.Length - index < dictionary.Count)
                throw new ArgumentException(SR.Argument_InsufficientSpaceToCopyCollection);

            int i = index;
            foreach (KeyValuePair<TKey, TValue> mapping in dictionary)
            {
                array[i++] = mapping.Key;
            }
        }
        
        public int Count {
            get { return dictionary.Count; }
        }

        public bool Contains(TKey item)
        {
            return dictionary.ContainsKey(item);
        }
        */

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TKey>)this).GetEnumerator();
        }

        public IEnumerator<TKey> GetEnumerator()
        {
            return new ReadOnlyDictionaryKeyEnumerator<TKey, TValue>(dictionary);
        }
    }  // public class ReadOnlyDictionaryKeyCollection<TKey, TValue>


    internal sealed class ReadOnlyDictionaryKeyEnumerator<TKey, TValue> : IEnumerator<TKey> where TKey : object
    {
        private readonly IReadOnlyDictionary<TKey, TValue> dictionary;
        private IEnumerator<KeyValuePair<TKey, TValue>> enumeration;

        public ReadOnlyDictionaryKeyEnumerator(IReadOnlyDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            this.dictionary = dictionary;
            enumeration = dictionary.GetEnumerator();
        }

        void IDisposable.Dispose()
        {
            enumeration.Dispose();
        }

        public bool MoveNext()
        {
            return enumeration.MoveNext();
        }

        object? IEnumerator.Current
        {
            get { return ((IEnumerator<TKey>)this).Current; }
        }

        public TKey Current
        {
            get { return enumeration.Current.Key; }
        }

        public void Reset()
        {
            enumeration = dictionary.GetEnumerator();
        }
    }  // class ReadOnlyDictionaryKeyEnumerator<TKey, TValue>


    [DebuggerDisplay("Count = {Count}")]
    internal sealed class ReadOnlyDictionaryValueCollection<TKey, TValue> : IEnumerable<TValue> where TKey : object
    {
        private readonly IReadOnlyDictionary<TKey, TValue> dictionary;

        public ReadOnlyDictionaryValueCollection(IReadOnlyDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            this.dictionary = dictionary;
        }

        /*
        public void CopyTo(TValue[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (array.Length <= index && this.Count > 0)
                throw new ArgumentException(SR.Arg_IndexOutOfRangeException);
            if (array.Length - index < dictionary.Count)
                throw new ArgumentException(SR.Argument_InsufficientSpaceToCopyCollection);

            int i = index;
            foreach (KeyValuePair<TKey, TValue> mapping in dictionary)
            {
                array[i++] = mapping.Value;
            }
        }

        public int Count {
            get { return dictionary.Count; }
        }

        public bool Contains(TValue item)
        {
            EqualityComparer<TValue> comparer = EqualityComparer<TValue>.Default;
            foreach (TValue value in this)
                if (comparer.Equals(item, value))
                    return true;
            return false;
        }
        */

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TValue>)this).GetEnumerator();
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return new ReadOnlyDictionaryValueEnumerator<TKey, TValue>(dictionary);
        }
    }  // public class ReadOnlyDictionaryValueCollection<TKey, TValue>


    internal sealed class ReadOnlyDictionaryValueEnumerator<TKey, TValue> : IEnumerator<TValue> where TKey : object
    {
        private readonly IReadOnlyDictionary<TKey, TValue> dictionary;
        private IEnumerator<KeyValuePair<TKey, TValue>> enumeration;

        public ReadOnlyDictionaryValueEnumerator(IReadOnlyDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            this.dictionary = dictionary;
            enumeration = dictionary.GetEnumerator();
        }

        void IDisposable.Dispose()
        {
            enumeration.Dispose();
        }

        public bool MoveNext()
        {
            return enumeration.MoveNext();
        }

        object? IEnumerator.Current
        {
            get { return ((IEnumerator<TValue>)this).Current; }
        }

        public TValue Current
        {
            get { return enumeration.Current.Value; }
        }

        public void Reset()
        {
            enumeration = dictionary.GetEnumerator();
        }
    }  // class ReadOnlyDictionaryValueEnumerator<TKey, TValue>
}
