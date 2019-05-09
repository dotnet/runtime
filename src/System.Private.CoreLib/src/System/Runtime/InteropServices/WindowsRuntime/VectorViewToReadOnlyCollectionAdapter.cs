// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Internal.Runtime.CompilerServices;

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
            Debug.Fail("This class is never instantiated");
        }

        // int Count { get }
        internal int Count<T>()
        {
            IVectorView<T> _this = Unsafe.As<IVectorView<T>>(this);
            uint size = _this.Size;
            if (((uint)int.MaxValue) < size)
            {
                throw new InvalidOperationException(SR.InvalidOperation_CollectionBackingListTooLarge);
            }

            return (int)size;
        }
    }
}
