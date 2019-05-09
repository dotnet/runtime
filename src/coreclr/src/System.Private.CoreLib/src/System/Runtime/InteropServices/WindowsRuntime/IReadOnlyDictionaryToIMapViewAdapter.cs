// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // This is a set of stub methods implementing the support for the IMapView`2 interface on managed
    // objects that implement IReadOnlyDictionary`2. Used by the interop mashaling infrastructure.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not IReadOnlyDictionaryToIMapViewAdapter objects. Rather, they are of type
    // IReadOnlyDictionary<K, V>. No actual IReadOnlyDictionaryToIMapViewAdapter object is ever instantiated. Thus, you will
    // see a lot of expressions that cast "this" to "IReadOnlyDictionary<K, V>". 
    [DebuggerDisplay("Size = {Size}")]
    internal sealed class IReadOnlyDictionaryToIMapViewAdapter
    {
        private IReadOnlyDictionaryToIMapViewAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        // V Lookup(K key)
        internal V Lookup<K, V>(K key) where K : object
        {
            IReadOnlyDictionary<K, V> _this = Unsafe.As<IReadOnlyDictionary<K, V>>(this);
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
        internal uint Size<K, V>() where K : object
        {
            IReadOnlyDictionary<K, V> _this = Unsafe.As<IReadOnlyDictionary<K, V>>(this);
            return (uint)_this.Count;
        }

        // bool HasKey(K key)
        internal bool HasKey<K, V>(K key) where K : object
        {
            IReadOnlyDictionary<K, V> _this = Unsafe.As<IReadOnlyDictionary<K, V>>(this);
            return _this.ContainsKey(key);
        }

        // void Split(out IMapView<K, V> first, out IMapView<K, V> second)
        internal void Split<K, V>(out IMapView<K, V>? first, out IMapView<K, V>? second) where K : object
        {
            IReadOnlyDictionary<K, V> _this = Unsafe.As<IReadOnlyDictionary<K, V>>(this);

            if (_this.Count < 2)
            {
                first = null;
                second = null;
                return;
            }

            if (!(_this is ConstantSplittableMap<K, V> splittableMap))
                splittableMap = new ConstantSplittableMap<K, V>(_this);

            splittableMap.Split(out first, out second);
        }
    }
}
