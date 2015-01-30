﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//

using System;
using System.Runtime;
using System.Security;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // This is a set of stub methods implementing the support for the ICollection interface on WinRT
    // objects that support IBindableVector. Used by the interop mashaling infrastructure.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not BindableVectorToCollectionAdapter objects. Rather, they are
    // of type IBindableVector. No actual BindableVectorToCollectionAdapter object is ever instantiated.
    // Thus, you will see a lot of expressions that cast "this" to "IBindableVector".
    internal sealed class BindableVectorToCollectionAdapter
    {
        private BindableVectorToCollectionAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        // int Count { get }
        [Pure]
        [SecurityCritical]
        internal int Count()
        {
            IBindableVector _this = JitHelpers.UnsafeCast<IBindableVector>(this);
            uint size = _this.Size;
            if (((uint)Int32.MaxValue) < size)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
            }

            return (int)size;
        }

        // bool IsSynchronized { get }
        [Pure]
        [SecurityCritical]
        internal bool IsSynchronized()
        {
            return false;
        }

        // object SyncRoot { get }
        [Pure]
        [SecurityCritical]
        internal object SyncRoot()
        {
            return this;
        }

        // void CopyTo(Array array, int index)
        [Pure]
        [SecurityCritical]
        internal void CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            // ICollection expects the destination array to be single-dimensional.
            if (array.Rank != 1)
                throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));

            int destLB = array.GetLowerBound(0);

            int srcLen = Count();
            int destLen = array.GetLength(0);

            if (arrayIndex < destLB)
                throw new ArgumentOutOfRangeException("arrayIndex");

            // Does the dimension in question have sufficient space to copy the expected number of entries?
            // We perform this check before valid index check to ensure the exception message is in sync with
            // the following snippet that uses regular framework code:
            //
            // ArrayList list = new ArrayList();
            // list.Add(1);
            // Array items = Array.CreateInstance(typeof(object), new int[] { 1 }, new int[] { -1 });
            // list.CopyTo(items, 0);

            if(srcLen > (destLen - (arrayIndex - destLB)))
                throw new ArgumentException(Environment.GetResourceString("Argument_InsufficientSpaceToCopyCollection"));

            if(arrayIndex - destLB > destLen)
                throw new ArgumentException(Environment.GetResourceString("Argument_IndexOutOfArrayBounds"));

            // We need to verify the index as we;
            IBindableVector _this = JitHelpers.UnsafeCast<IBindableVector>(this);

            for (uint i = 0; i < srcLen; i++)
            {
                array.SetValue(_this.GetAt(i), i + arrayIndex);
            }
        }
    }
}
