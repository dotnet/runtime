// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal delegate T Indexer_Get_Delegate<out T>(int index);

    // This is a set of stub methods implementing the support for the IReadOnlyList`1 interface on WinRT
    // objects that support IVectorView`1. Used by the interop mashaling infrastructure.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not IVectorViewToIReadOnlyListAdapter objects. Rather, they are of type
    // IVectorView<T>. No actual IVectorViewToIReadOnlyListAdapter object is ever instantiated. Thus, you will see
    // a lot of expressions that cast "this" to "IVectorView<T>".
    [DebuggerDisplay("Count = {Count}")]
    internal sealed class IVectorViewToIReadOnlyListAdapter
    {
        private IVectorViewToIReadOnlyListAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        // T this[int index] { get }
        internal T Indexer_Get<T>(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            IVectorView<T> _this = Unsafe.As<IVectorView<T>>(this);

            try
            {
                return _this.GetAt((uint)index);

                // We delegate bounds checking to the underlying collection and if it detected a fault,
                // we translate it to the right exception:
            }
            catch (Exception ex)
            {
                if (HResults.E_BOUNDS == ex.HResult)
                    throw new ArgumentOutOfRangeException(nameof(index));

                throw;
            }
        }

        // T this[int index] { get }
        internal T Indexer_Get_Variance<T>(int index) where T : class
        {
            bool fUseString;
            Delegate target = System.StubHelpers.StubHelpers.GetTargetForAmbiguousVariantCall(
                this,
                typeof(IReadOnlyList<T>).TypeHandle.Value,
                out fUseString);

            if (target != null)
            {
                return (Unsafe.As<Indexer_Get_Delegate<T>>(target))(index);
            }

            if (fUseString)
            {
                return Unsafe.As<T>(Indexer_Get<string>(index));
            }

            return Indexer_Get<T>(index);
        }
    }
}
