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
    // These stubs will be used when a call via IReadOnlyCollection<KeyValuePair<K, V>> is made in managed code.
    // This can mean two things - either the underlying unmanaged object implements IMapView<K, V> or it
    // implements IVectorView<IKeyValuePair<K, V>> and we cannot determine this statically in the general
    // case so we have to cast at run-time. Used by the interop mashaling infrastructure.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not MapViewToReadOnlyCollectionAdapter objects. Rather, they are of type
    // IVectorView<KeyValuePair<K, V>> or IMapView<K, V>. No actual MapViewToReadOnlyCollectionAdapter object is ever
    // instantiated. Thus, you will see a lot of expressions that cast "this" to "IVectorView<KeyValuePair<K, V>>"
    // or "IMapView<K, V>".
    internal sealed class MapViewToReadOnlyCollectionAdapter
    {
        private MapViewToReadOnlyCollectionAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        // int Count { get }
        [Pure]
        [SecurityCritical]
        internal int Count<K, V>()
        {
            object _this = JitHelpers.UnsafeCast<object>(this);

            IMapView<K, V> _this_map = _this as IMapView<K, V>;
            if (_this_map != null)
            {
                uint size = _this_map.Size;

                if (((uint)Int32.MaxValue) < size)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingDictionaryTooLarge"));
                }

                return (int)size;
            }
            else
            {
                IVectorView<KeyValuePair<K, V>> _this_vector = JitHelpers.UnsafeCast<IVectorView<KeyValuePair<K, V>>>(this);
                uint size = _this_vector.Size;

                if (((uint)Int32.MaxValue) < size)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
                }

                return (int)size;
            }
        }
    }
}
