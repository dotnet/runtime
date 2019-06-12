// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Internal.Runtime.CompilerServices;

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
            Debug.Fail("This class is never instantiated");
        }

        // V Lookup(K key)
        internal V Lookup<K, V>(K key) where K : notnull
        {
            IDictionary<K, V> _this = Unsafe.As<IDictionary<K, V>>(this);
            V value;
            bool keyFound = _this.TryGetValue(key, out value);

            if (!keyFound)
            {
                Debug.Assert(key != null);
                Exception e = new KeyNotFoundException(SR.Format(SR.Arg_KeyNotFoundWithKey, key.ToString()));
                e.HResult = HResults.E_BOUNDS;
                throw e;
            }

            return value;
        }

        // uint Size { get }
        internal uint Size<K, V>() where K : notnull
        {
            IDictionary<K, V> _this = Unsafe.As<IDictionary<K, V>>(this);
            return (uint)_this.Count;
        }

        // bool HasKey(K key)
        internal bool HasKey<K, V>(K key) where K : notnull
        {
            IDictionary<K, V> _this = Unsafe.As<IDictionary<K, V>>(this);
            return _this.ContainsKey(key);
        }

        // IMapView<K, V> GetView()
        internal IReadOnlyDictionary<K, V> GetView<K, V>() where K : notnull
        {
            IDictionary<K, V> _this = Unsafe.As<IDictionary<K, V>>(this);
            Debug.Assert(_this != null);

            // Note: This dictionary is not really read-only - you could QI for a modifiable
            // dictionary.  We gain some perf by doing this.  We believe this is acceptable.
            if (!(_this is IReadOnlyDictionary<K, V> roDictionary))
            {
                roDictionary = new ReadOnlyDictionary<K, V>(_this);
            }
            return roDictionary;
        }

        // bool Insert(K key, V value)
        internal bool Insert<K, V>(K key, V value) where K : notnull
        {
            IDictionary<K, V> _this = Unsafe.As<IDictionary<K, V>>(this);
            bool replacing = _this.ContainsKey(key);
            _this[key] = value;
            return replacing;
        }

        // void Remove(K key)
        internal void Remove<K, V>(K key) where K : notnull
        {
            IDictionary<K, V> _this = Unsafe.As<IDictionary<K, V>>(this);
            bool removed = _this.Remove(key);

            if (!removed)
            {
                Debug.Assert(key != null);
                Exception e = new KeyNotFoundException(SR.Format(SR.Arg_KeyNotFoundWithKey, key.ToString()));
                e.HResult = HResults.E_BOUNDS;
                throw e;
            }
        }

        // void Clear()
        internal void Clear<K, V>() where K : notnull
        {
            IDictionary<K, V> _this = Unsafe.As<IDictionary<K, V>>(this);
            _this.Clear();
        }
    }
}
