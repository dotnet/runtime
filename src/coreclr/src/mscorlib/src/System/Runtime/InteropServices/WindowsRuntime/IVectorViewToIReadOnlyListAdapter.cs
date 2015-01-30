﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            Contract.Assert(false, "This class is never instantiated");
        }

        // T this[int index] { get }
        [SecurityCritical]
        internal T Indexer_Get<T>(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");

            IVectorView<T> _this = JitHelpers.UnsafeCast<IVectorView<T>>(this);

            try
            {
                return _this.GetAt((uint) index);

                // We delegate bounds checking to the underlying collection and if it detected a fault,
                // we translate it to the right exception:
            }
            catch (Exception ex)
            {
                if (__HResults.E_BOUNDS == ex._HResult)
                    throw new ArgumentOutOfRangeException("index");

                throw;
            }
        }

        // T this[int index] { get }
        [SecurityCritical]
        internal T Indexer_Get_Variance<T>(int index) where T : class
        {
            bool fUseString;
            Delegate target = System.StubHelpers.StubHelpers.GetTargetForAmbiguousVariantCall(
                this, 
                typeof(IReadOnlyList<T>).TypeHandle.Value, 
                out fUseString);

            if (target != null)
            {
                return (JitHelpers.UnsafeCast<Indexer_Get_Delegate<T>>(target))(index);
            }
            
            if (fUseString)
            {
                return JitHelpers.UnsafeCast<T>(Indexer_Get<string>(index));
            }

            return Indexer_Get<T>(index);
        }
    }
}
