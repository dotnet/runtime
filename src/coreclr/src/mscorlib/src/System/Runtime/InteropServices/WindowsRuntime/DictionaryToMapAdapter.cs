// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Security;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // This is a set of stub methods implementing the support for the IMap`2 interface on managed
    // objects that implement IDictionary`2. Used by the interop mashaling infrastructure.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not DictionaryToMapAdapter objects. Rather, they are of type
    // IDictionary<K, V>. No actual DictionaryToMapAdapter object is ever instantiated. Thus, you will
    // see a lot of expressions that cast "this" to "IDictionary<K, V>". 
    internal sealed class DictionaryToMapAdapter
    {
        private DictionaryToMapAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        // V Lookup(K key)
        [SecurityCritical]
        internal V Lookup<K, V>(K key)
        {
            IDictionary<K, V> _this = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
            V value;
            bool keyFound = _this.TryGetValue(key, out value);

            if (!keyFound)
            {
                Exception e = new KeyNotFoundException(Environment.GetResourceString("Arg_KeyNotFound"));
                e.SetErrorCode(__HResults.E_BOUNDS);
                throw e;
            }

            return value;
        }

        // uint Size { get }
        [SecurityCritical]
        internal uint Size<K, V>()
        {
            IDictionary<K, V> _this = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
            return (uint)_this.Count;
        }
        
        // bool HasKey(K key)
        [SecurityCritical]
        internal bool HasKey<K, V>(K key)
        {
            IDictionary<K, V> _this = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
            return _this.ContainsKey(key);
        }

        // IMapView<K, V> GetView()
        [SecurityCritical]
        internal IReadOnlyDictionary<K, V> GetView<K, V>()
        {
            IDictionary<K, V> _this = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
            Contract.Assert(_this != null);

            // Note: This dictionary is not really read-only - you could QI for a modifiable
            // dictionary.  We gain some perf by doing this.  We believe this is acceptable.
            IReadOnlyDictionary<K, V> roDictionary = _this as IReadOnlyDictionary<K, V>;
            if (roDictionary == null)
            {
                roDictionary = new ReadOnlyDictionary<K, V>(_this);
            }
            return roDictionary;
        }

        // bool Insert(K key, V value)
        [SecurityCritical]
        internal bool Insert<K, V>(K key, V value)
        {
            IDictionary<K, V> _this = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
            bool replacing = _this.ContainsKey(key);
            _this[key] = value;
            return replacing;
        }

        // void Remove(K key)
        [SecurityCritical]
        internal void Remove<K, V>(K key)
        {
            IDictionary<K, V> _this = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
            bool removed = _this.Remove(key);

            if (!removed)
            {
                Exception e = new KeyNotFoundException(Environment.GetResourceString("Arg_KeyNotFound"));
                e.SetErrorCode(__HResults.E_BOUNDS);
                throw e;
            }
        }

        // void Clear()
        [SecurityCritical]
        internal void Clear<K, V>()
        {
            IDictionary<K, V> _this = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
            _this.Clear();
        }
    }
}
