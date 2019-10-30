// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Internal.Runtime.CompilerServices;

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
            Debug.Fail("This class is never instantiated");
        }

        // int Count { get }
        internal int Count()
        {
            IBindableVector _this = Unsafe.As<IBindableVector>(this);
            uint size = _this.Size;
            if (((uint)int.MaxValue) < size)
            {
                throw new InvalidOperationException(SR.InvalidOperation_CollectionBackingListTooLarge);
            }

            return (int)size;
        }

        // bool IsSynchronized { get }
        internal bool IsSynchronized()
        {
            return false;
        }

        // object SyncRoot { get }
        internal object SyncRoot()
        {
            return this;
        }

        // void CopyTo(Array array, int index)
        internal void CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            // ICollection expects the destination array to be single-dimensional.
            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);

            int destLB = array.GetLowerBound(0);

            int srcLen = Count();
            int destLen = array.GetLength(0);

            if (arrayIndex < destLB)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            // Does the dimension in question have sufficient space to copy the expected number of entries?
            // We perform this check before valid index check to ensure the exception message is in sync with
            // the following snippet that uses regular framework code:
            //
            // ArrayList list = new ArrayList();
            // list.Add(1);
            // Array items = Array.CreateInstance(typeof(object), new int[] { 1 }, new int[] { -1 });
            // list.CopyTo(items, 0);

            if (srcLen > (destLen - (arrayIndex - destLB)))
                throw new ArgumentException(SR.Argument_InsufficientSpaceToCopyCollection);

            if (arrayIndex - destLB > destLen)
                throw new ArgumentException(SR.Argument_IndexOutOfArrayBounds);

            // We need to verify the index as we;
            IBindableVector _this = Unsafe.As<IBindableVector>(this);

            for (uint i = 0; i < srcLen; i++)
            {
                array.SetValue(_this.GetAt(i), i + arrayIndex);
            }
        }
    }
}
