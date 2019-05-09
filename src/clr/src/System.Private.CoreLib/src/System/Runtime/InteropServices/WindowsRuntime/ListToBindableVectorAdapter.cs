// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // This is a set of stub methods implementing the support for the IBindableVector interface on managed
    // objects that implement IList. Used by the interop mashaling infrastructure.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not ListToBindableVectorAdapter objects. Rather, they are of type
    // IList. No actual ListToVectorBindableAdapter object is ever instantiated. Thus, you will
    // see a lot of expressions that cast "this" to "IList". 
    internal sealed class ListToBindableVectorAdapter
    {
        private ListToBindableVectorAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        // object GetAt(uint index)
        internal object? GetAt(uint index)
        {
            IList _this = Unsafe.As<IList>(this);
            EnsureIndexInt32(index, _this.Count);

            try
            {
                return _this[(int)index];
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw WindowsRuntimeMarshal.GetExceptionForHR(HResults.E_BOUNDS, ex, "ArgumentOutOfRange_IndexOutOfRange");
            }
        }

        // uint Size { get }
        internal uint Size()
        {
            IList _this = Unsafe.As<IList>(this);
            return (uint)_this.Count;
        }

        // IBindableVectorView GetView()
        internal IBindableVectorView GetView()
        {
            IList _this = Unsafe.As<IList>(this);
            return new ListToBindableVectorViewAdapter(_this);
        }

        // bool IndexOf(object value, out uint index)
        internal bool IndexOf(object? value, out uint index)
        {
            IList _this = Unsafe.As<IList>(this);
            int ind = _this.IndexOf(value);

            if (-1 == ind)
            {
                index = 0;
                return false;
            }

            index = (uint)ind;
            return true;
        }

        // void SetAt(uint index, object value)
        internal void SetAt(uint index, object? value)
        {
            IList _this = Unsafe.As<IList>(this);
            EnsureIndexInt32(index, _this.Count);

            try
            {
                _this[(int)index] = value;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw WindowsRuntimeMarshal.GetExceptionForHR(HResults.E_BOUNDS, ex, "ArgumentOutOfRange_IndexOutOfRange");
            }
        }

        // void InsertAt(uint index, object value)
        internal void InsertAt(uint index, object? value)
        {
            IList _this = Unsafe.As<IList>(this);

            // Inserting at an index one past the end of the list is equivalent to appending
            // so we need to ensure that we're within (0, count + 1).
            EnsureIndexInt32(index, _this.Count + 1);

            try
            {
                _this.Insert((int)index, value);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // Change error code to match what WinRT expects
                ex.HResult = HResults.E_BOUNDS;
                throw;
            }
        }

        // void RemoveAt(uint index)
        internal void RemoveAt(uint index)
        {
            IList _this = Unsafe.As<IList>(this);
            EnsureIndexInt32(index, _this.Count);

            try
            {
                _this.RemoveAt((int)index);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // Change error code to match what WinRT expects
                ex.HResult = HResults.E_BOUNDS;
                throw;
            }
        }

        // void Append(object value)
        internal void Append(object? value)
        {
            IList _this = Unsafe.As<IList>(this);
            _this.Add(value);
        }

        // void RemoveAtEnd()
        internal void RemoveAtEnd()
        {
            IList _this = Unsafe.As<IList>(this);
            if (_this.Count == 0)
            {
                Exception e = new InvalidOperationException(SR.InvalidOperation_CannotRemoveLastFromEmptyCollection);
                e.HResult = HResults.E_BOUNDS;
                throw e;
            }

            uint size = (uint)_this.Count;
            RemoveAt(size - 1);
        }

        // void Clear()
        internal void Clear()
        {
            IList _this = Unsafe.As<IList>(this);
            _this.Clear();
        }

        // Helpers:

        private static void EnsureIndexInt32(uint index, int listCapacity)
        {
            // We use '<=' and not '<' becasue int.MaxValue == index would imply
            // that Size > int.MaxValue:
            if (((uint)int.MaxValue) <= index || index >= (uint)listCapacity)
            {
                Exception e = new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexLargerThanMaxValue);
                e.HResult = HResults.E_BOUNDS;
                throw e;
            }
        }
    }
}
