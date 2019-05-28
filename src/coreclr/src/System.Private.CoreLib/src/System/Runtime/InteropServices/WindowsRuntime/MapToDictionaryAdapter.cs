// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // This is a set of stub methods implementing the support for the IDictionary`2 interface on WinRT
    // objects that support IMap`2. Used by the interop mashaling infrastructure.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not MapToDictionaryAdapter objects. Rather, they are of type
    // IMap<K, V>. No actual MapToDictionaryAdapter object is ever instantiated. Thus, you will see
    // a lot of expressions that cast "this" to "IMap<K, V>".
    internal sealed class MapToDictionaryAdapter
    {
        private MapToDictionaryAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        // V this[K key] { get }
        internal V Indexer_Get<K, V>(K key) where K : object
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));


            IMap<K, V> _this = Unsafe.As<IMap<K, V>>(this);
            return Lookup(_this, key);
        }

        // V this[K key] { set }
        internal void Indexer_Set<K, V>(K key, V value) where K : object
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));


            IMap<K, V> _this = Unsafe.As<IMap<K, V>>(this);
            Insert(_this, key, value);
        }

        // ICollection<K> Keys { get }
        internal ICollection<K> Keys<K, V>() where K : object
        {
            IMap<K, V> _this = Unsafe.As<IMap<K, V>>(this);
            IDictionary<K, V> dictionary = (IDictionary<K, V>)_this;
            return new DictionaryKeyCollection<K, V>(dictionary);
        }

        // ICollection<V> Values { get }
        internal ICollection<V> Values<K, V>() where K : object
        {
            IMap<K, V> _this = Unsafe.As<IMap<K, V>>(this);
            IDictionary<K, V> dictionary = (IDictionary<K, V>)_this;
            return new DictionaryValueCollection<K, V>(dictionary);
        }

        // bool ContainsKey(K key)
        internal bool ContainsKey<K, V>(K key) where K : object
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            IMap<K, V> _this = Unsafe.As<IMap<K, V>>(this);
            return _this.HasKey(key);
        }

        // void Add(K key, V value)
        internal void Add<K, V>(K key, V value) where K : object
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (ContainsKey<K, V>(key))
                throw new ArgumentException(SR.Argument_AddingDuplicate);


            IMap<K, V> _this = Unsafe.As<IMap<K, V>>(this);
            Insert(_this, key, value);
        }

        // bool Remove(TKey key)
        internal bool Remove<K, V>(K key) where K : object
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            IMap<K, V> _this = Unsafe.As<IMap<K, V>>(this);
            if (!_this.HasKey(key))
                return false;

            try
            {
                _this.Remove(key);
                return true;
            }
            catch (Exception ex)
            {
                if (HResults.E_BOUNDS == ex.HResult)
                    return false;

                throw;
            }
        }

        // bool TryGetValue(TKey key, out TValue value)
        internal bool TryGetValue<K, V>(K key, [MaybeNullWhen(false)] out V value) where K : object
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            IMap<K, V> _this = Unsafe.As<IMap<K, V>>(this);
            if (!_this.HasKey(key))
            {
                value = default!;
                return false;
            }

            try
            {
                value = Lookup(_this, key);
                return true;
            }
            catch (KeyNotFoundException)
            {
                value = default!;
                return false;
            }
        }

        // Helpers:

        private static V Lookup<K, V>(IMap<K, V> _this, K key) where K : object
        {
            Debug.Assert(null != key);

            try
            {
                return _this.Lookup(key);
            }
            catch (Exception ex)
            {
                if (HResults.E_BOUNDS == ex.HResult)
                    throw new KeyNotFoundException(SR.Arg_KeyNotFound);
                throw;
            }
        }

        private static bool Insert<K, V>(IMap<K, V> _this, K key, V value) where K : object
        {
            Debug.Assert(null != key);

            bool replaced = _this.Insert(key, value);
            return replaced;
        }
    }
}
