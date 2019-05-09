// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Internal.Runtime.CompilerServices;

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
            Debug.Fail("This class is never instantiated");
        }

        // int Count { get }
        internal int Count<T>()
        {
            IVector<T> _this = Unsafe.As<IVector<T>>(this);
            uint size = _this.Size;
            if (((uint)int.MaxValue) < size)
            {
                throw new InvalidOperationException(SR.InvalidOperation_CollectionBackingListTooLarge);
            }

            return (int)size;
        }

        // bool IsReadOnly { get }
        internal bool IsReadOnly<T>()
        {
            return false;
        }

        // void Add(T item)
        internal void Add<T>(T item)
        {
            IVector<T> _this = Unsafe.As<IVector<T>>(this);
            _this.Append(item);
        }

        // void Clear()
        internal void Clear<T>()
        {
            IVector<T> _this = Unsafe.As<IVector<T>>(this);
            _this.Clear();
        }

        // bool Contains(T item)
        internal bool Contains<T>(T item)
        {
            IVector<T> _this = Unsafe.As<IVector<T>>(this);

            uint index;
            return _this.IndexOf(item, out index);
        }

        // void CopyTo(T[] array, int arrayIndex)
        internal void CopyTo<T>(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (array.Length <= arrayIndex && Count<T>() > 0)
                throw new ArgumentException(SR.Argument_IndexOutOfArrayBounds);

            if (array.Length - arrayIndex < Count<T>())
                throw new ArgumentException(SR.Argument_InsufficientSpaceToCopyCollection);


            IVector<T> _this = Unsafe.As<IVector<T>>(this);
            int count = Count<T>();
            for (int i = 0; i < count; i++)
            {
                array[i + arrayIndex] = VectorToListAdapter.GetAt<T>(_this, (uint)i);
            }
        }

        // bool Remove(T item)
        internal bool Remove<T>(T item)
        {
            IVector<T> _this = Unsafe.As<IVector<T>>(this);

            uint index;
            bool exists = _this.IndexOf(item, out index);

            if (!exists)
                return false;

            if (((uint)int.MaxValue) < index)
            {
                throw new InvalidOperationException(SR.InvalidOperation_CollectionBackingListTooLarge);
            }

            VectorToListAdapter.RemoveAtHelper<T>(_this, index);
            return true;
        }
    }
}
