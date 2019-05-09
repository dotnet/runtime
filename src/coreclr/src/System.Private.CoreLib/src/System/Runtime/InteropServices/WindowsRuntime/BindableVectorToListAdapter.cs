// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // This is a set of stub methods implementing the support for the IList interface on WinRT
    // objects that support IBindableVector. Used by the interop mashaling infrastructure.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not BindableVectorToListAdapter objects. Rather, they are
    // of type IBindableVector. No actual BindableVectorToListAdapter object is ever instantiated.
    // Thus, you will see a lot of expressions that cast "this" to "IBindableVector".
    internal sealed class BindableVectorToListAdapter
    {
        private BindableVectorToListAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        // object this[int index] { get }
        internal object? Indexer_Get(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            IBindableVector _this = Unsafe.As<IBindableVector>(this);
            return GetAt(_this, (uint)index);
        }

        // object this[int index] { set }
        internal void Indexer_Set(int index, object value)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            IBindableVector _this = Unsafe.As<IBindableVector>(this);
            SetAt(_this, (uint)index, value);
        }

        // int Add(object value)
        internal int Add(object value)
        {
            IBindableVector _this = Unsafe.As<IBindableVector>(this);
            _this.Append(value);

            uint size = _this.Size;
            if (((uint)int.MaxValue) < size)
            {
                throw new InvalidOperationException(SR.InvalidOperation_CollectionBackingListTooLarge);
            }

            return (int)(size - 1);
        }

        // bool Contains(object item)
        internal bool Contains(object item)
        {
            IBindableVector _this = Unsafe.As<IBindableVector>(this);

            uint index;
            return _this.IndexOf(item, out index);
        }

        // void Clear()
        internal void Clear()
        {
            IBindableVector _this = Unsafe.As<IBindableVector>(this);
            _this.Clear();
        }

        // bool IsFixedSize { get }
        internal bool IsFixedSize()
        {
            return false;
        }

        // bool IsReadOnly { get }
        internal bool IsReadOnly()
        {
            return false;
        }

        // int IndexOf(object item)
        internal int IndexOf(object item)
        {
            IBindableVector _this = Unsafe.As<IBindableVector>(this);

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

        // void Insert(int index, object item)
        internal void Insert(int index, object item)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            IBindableVector _this = Unsafe.As<IBindableVector>(this);
            InsertAtHelper(_this, (uint)index, item);
        }

        // bool Remove(object item)
        internal void Remove(object item)
        {
            IBindableVector _this = Unsafe.As<IBindableVector>(this);

            uint index;
            bool exists = _this.IndexOf(item, out index);

            if (exists)
            {
                if (((uint)int.MaxValue) < index)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_CollectionBackingListTooLarge);
                }

                RemoveAtHelper(_this, index);
            }
        }

        // void RemoveAt(int index)
        internal void RemoveAt(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            IBindableVector _this = Unsafe.As<IBindableVector>(this);
            RemoveAtHelper(_this, (uint)index);
        }

        // Helpers:

        private static object? GetAt(IBindableVector _this, uint index)
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

        private static void SetAt(IBindableVector _this, uint index, object value)
        {
            try
            {
                _this.SetAt(index, value);

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

        private static void InsertAtHelper(IBindableVector _this, uint index, object item)
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

        private static void RemoveAtHelper(IBindableVector _this, uint index)
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
