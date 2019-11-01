// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // This is a set of stub methods implementing the support for the IList`1 interface on WinRT
    // objects that support IVector`1. Used by the interop mashaling infrastructure.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not VectorToListAdapter objects. Rather, they are of type
    // IVector<T>. No actual VectorToListAdapter object is ever instantiated. Thus, you will see
    // a lot of expressions that cast "this" to "IVector<T>".
    internal sealed class VectorToListAdapter
    {
        private VectorToListAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        // T this[int index] { get }
        internal T Indexer_Get<T>(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            IVector<T> _this = Unsafe.As<IVector<T>>(this);
            return GetAt(_this, (uint)index);
        }

        // T this[int index] { set }
        internal void Indexer_Set<T>(int index, T value)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            IVector<T> _this = Unsafe.As<IVector<T>>(this);
            SetAt(_this, (uint)index, value);
        }

        // int IndexOf(T item)
        internal int IndexOf<T>(T item)
        {
            IVector<T> _this = Unsafe.As<IVector<T>>(this);

            uint index;
            bool exists = _this.IndexOf(item, out index);

            if (!exists)
                return -1;

            if (((uint)int.MaxValue) < index)
            {
                throw new InvalidOperationException(SR.InvalidOperation_CollectionBackingListTooLarge);
            }

            return (int)index;
        }

        // void Insert(int index, T item)
        internal void Insert<T>(int index, T item)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            IVector<T> _this = Unsafe.As<IVector<T>>(this);
            InsertAtHelper<T>(_this, (uint)index, item);
        }

        // void RemoveAt(int index)
        internal void RemoveAt<T>(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            IVector<T> _this = Unsafe.As<IVector<T>>(this);
            RemoveAtHelper<T>(_this, (uint)index);
        }

        // Helpers:

        internal static T GetAt<T>(IVector<T> _this, uint index)
        {
            try
            {
                return _this.GetAt(index);

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

        private static void SetAt<T>(IVector<T> _this, uint index, T value)
        {
            try
            {
                _this.SetAt(index, value);

                // We deligate bounds checking to the underlying collection and if it detected a fault,
                // we translate it to the right exception:
            }
            catch (Exception ex)
            {
                if (HResults.E_BOUNDS == ex.HResult)
                    throw new ArgumentOutOfRangeException(nameof(index));

                throw;
            }
        }

        private static void InsertAtHelper<T>(IVector<T> _this, uint index, T item)
        {
            try
            {
                _this.InsertAt(index, item);

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

        internal static void RemoveAtHelper<T>(IVector<T> _this, uint index)
        {
            try
            {
                _this.RemoveAt(index);

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
    }
}
