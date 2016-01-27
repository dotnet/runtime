// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Security;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

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
            Contract.Assert(false, "This class is never instantiated");
        }

        // V Lookup(K key)
        [SecurityCritical]
        internal V Lookup<K, V>(K key)
        {
            IReadOnlyDictionary<K, V> _this = JitHelpers.UnsafeCast<IReadOnlyDictionary<K, V>>(this);
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
            IReadOnlyDictionary<K, V> _this = JitHelpers.UnsafeCast<IReadOnlyDictionary<K, V>>(this);
            return (uint)_this.Count;
        }
        
        // bool HasKey(K key)
        [SecurityCritical]
        internal bool HasKey<K, V>(K key)
        {
            IReadOnlyDictionary<K, V> _this = JitHelpers.UnsafeCast<IReadOnlyDictionary<K, V>>(this);
            return _this.ContainsKey(key);
        }

        // void Split(out IMapView<K, V> first, out IMapView<K, V> second)
        [SecurityCritical]
        internal void Split<K, V>(out IMapView<K, V> first, out IMapView<K, V> second)
        {
            IReadOnlyDictionary<K, V> _this = JitHelpers.UnsafeCast<IReadOnlyDictionary<K, V>>(this);

            if (_this.Count < 2) {
                first = null;
                second = null;
                return;
            }

            ConstantSplittableMap<K, V> splittableMap = _this as ConstantSplittableMap<K, V>;

            if (splittableMap == null)
                splittableMap = new ConstantSplittableMap<K, V>(_this);

            splittableMap.Split(out first, out second);
        }
    }
}
