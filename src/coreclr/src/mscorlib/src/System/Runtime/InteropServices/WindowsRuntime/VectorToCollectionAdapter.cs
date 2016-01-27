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
    // This is a set of stub methods implementing the support for the ICollection`1 interface on WinRT
    // objects that support IVector`1. Used by the interop mashaling infrastructure.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not VectorToCollectionAdapter objects. Rather, they are of type
    // IVector<T>. No actual VectorToCollectionAdapter object is ever instantiated. Thus, you will see
    // a lot of expressions that cast "this" to "IVector<T>".
    internal sealed class VectorToCollectionAdapter
    {
        private VectorToCollectionAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        // int Count { get }
        [Pure]
        [SecurityCritical]
        internal int Count<T>()
        {
            IVector<T> _this = JitHelpers.UnsafeCast<IVector<T>>(this);
            uint size = _this.Size;
            if (((uint)Int32.MaxValue) < size)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
            }

            return (int)size;
        }

        // bool IsReadOnly { get }
        [SecurityCritical]
        internal bool IsReadOnly<T>()
        {
            return false;
        }

        // void Add(T item)
        [SecurityCritical]
        internal void Add<T>(T item)
        {
            IVector<T> _this = JitHelpers.UnsafeCast<IVector<T>>(this);
            _this.Append(item);
        }

        // void Clear()
        [SecurityCritical]
        internal void Clear<T>()
        {
            IVector<T> _this = JitHelpers.UnsafeCast<IVector<T>>(this);
            _this.Clear();
        }

        // bool Contains(T item)
        [SecurityCritical]
        internal bool Contains<T>(T item)
        {
            IVector<T> _this = JitHelpers.UnsafeCast<IVector<T>>(this);

            uint index;
            return _this.IndexOf(item, out index);
        }

        // void CopyTo(T[] array, int arrayIndex)
        [SecurityCritical]
        internal void CopyTo<T>(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex");

            if (array.Length <= arrayIndex && Count<T>() > 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_IndexOutOfArrayBounds"));

            if (array.Length - arrayIndex < Count<T>())
                throw new ArgumentException(Environment.GetResourceString("Argument_InsufficientSpaceToCopyCollection"));

            Contract.EndContractBlock();

            IVector<T> _this = JitHelpers.UnsafeCast<IVector<T>>(this);
            int count = Count<T>();
            for (int i = 0; i < count; i++)
            {
                array[i + arrayIndex] = VectorToListAdapter.GetAt<T>(_this, (uint)i);
            }
        }

        // bool Remove(T item)
        [SecurityCritical]
        internal bool Remove<T>(T item)
        {
            IVector<T> _this = JitHelpers.UnsafeCast<IVector<T>>(this);

            uint index;
            bool exists = _this.IndexOf(item, out index);

            if (!exists)
                return false;

            if (((uint)Int32.MaxValue) < index)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
            }

            VectorToListAdapter.RemoveAtHelper<T>(_this, index);
            return true;
        }
    }
}
