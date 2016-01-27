// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Security;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

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
            Contract.Assert(false, "This class is never instantiated");
        }

        // V this[K key] { get }
        [SecurityCritical]
        internal V Indexer_Get<K, V>(K key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            Contract.EndContractBlock();

            IMap<K, V> _this = JitHelpers.UnsafeCast<IMap<K, V>>(this);
            return Lookup(_this, key);
        }

        // V this[K key] { set }
        [SecurityCritical]
        internal void Indexer_Set<K, V>(K key, V value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            Contract.EndContractBlock();

            IMap<K, V> _this = JitHelpers.UnsafeCast<IMap<K, V>>(this);
            Insert(_this, key, value);
        }

        // ICollection<K> Keys { get }
        [SecurityCritical]
        internal ICollection<K> Keys<K, V>()
        {
            IMap<K, V> _this = JitHelpers.UnsafeCast<IMap<K, V>>(this);
            IDictionary<K, V> dictionary = (IDictionary<K, V>)_this;
            return new DictionaryKeyCollection<K, V>(dictionary);
        }

        // ICollection<V> Values { get }
        [SecurityCritical]
        internal ICollection<V> Values<K, V>()
        {
            IMap<K, V> _this = JitHelpers.UnsafeCast<IMap<K, V>>(this);
            IDictionary<K, V> dictionary = (IDictionary<K, V>)_this;
            return new DictionaryValueCollection<K, V>(dictionary);
        }

        // bool ContainsKey(K key)
        [Pure]
        [SecurityCritical]
        internal bool ContainsKey<K, V>(K key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            IMap<K, V> _this = JitHelpers.UnsafeCast<IMap<K, V>>(this);
            return _this.HasKey(key);
        }

        // void Add(K key, V value)
        [SecurityCritical]
        internal void Add<K, V>(K key, V value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            if (ContainsKey<K, V>(key))
                throw new ArgumentException(Environment.GetResourceString("Argument_AddingDuplicate"));

            Contract.EndContractBlock();

            IMap<K, V> _this = JitHelpers.UnsafeCast<IMap<K, V>>(this);
            Insert(_this, key, value);
        }

        // bool Remove(TKey key)
        [SecurityCritical]
        internal bool Remove<K, V>(K key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            IMap<K, V> _this = JitHelpers.UnsafeCast<IMap<K, V>>(this);
            if (!_this.HasKey(key))
                return false;

            try
            {
                _this.Remove(key);
                return true;

            }
            catch (Exception ex)
            {
                if (__HResults.E_BOUNDS == ex._HResult)
                    return false;

                throw;
            }
        }

        // bool TryGetValue(TKey key, out TValue value)
        [SecurityCritical]
        internal bool TryGetValue<K, V>(K key, out V value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            IMap<K, V> _this = JitHelpers.UnsafeCast<IMap<K, V>>(this);
            if (!_this.HasKey(key))
            {
                value = default(V);
                return false;
            }

            try
            {
                value = Lookup(_this, key);
                return true;
            }
            catch (KeyNotFoundException)
            {
                value = default(V);
                return false;
            }
        }

        // Helpers:

        private static V Lookup<K, V>(IMap<K, V> _this, K key)
        {
            Contract.Requires(null != key);

            try
            {
                return _this.Lookup(key);
            }
            catch (Exception ex)
            {

                if (__HResults.E_BOUNDS == ex._HResult)
                    throw new KeyNotFoundException(Environment.GetResourceString("Arg_KeyNotFound"));
                throw;
            }
        }

        private static bool Insert<K, V>(IMap<K, V> _this, K key, V value)
        {
            Contract.Requires(null != key);

            bool replaced = _this.Insert(key, value);
            return replaced;
        }
    }
}
