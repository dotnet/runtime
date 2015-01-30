// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    // This is a set of stub methods implementing the support for the IReadOnlyCollection<T> interface on WinRT
    // objects that support IVectorView<T>. Used by the interop mashaling infrastructure.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not VectorViewToReadOnlyCollectionAdapter objects. Rather, they are of type
    // IVectorView<T>. No actual VectorViewToReadOnlyCollectionAdapter object is ever instantiated. Thus, you will see
    // a lot of expressions that cast "this" to "IVectorView<T>".
    internal sealed class VectorViewToReadOnlyCollectionAdapter
    {
        private VectorViewToReadOnlyCollectionAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        // int Count { get }
        [Pure]
        [SecurityCritical]
        internal int Count<T>()
        {
            IVectorView<T> _this = JitHelpers.UnsafeCast<IVectorView<T>>(this);
            uint size = _this.Size;
            if (((uint)Int32.MaxValue) < size)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
            }

            return (int)size;
        }
    }
}
