// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // This is a set of stub methods implementing the support for the IVector`1 interface on managed
    // objects that implement IList`1. Used by the interop mashaling infrastructure.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not ListToVectorAdapter objects. Rather, they are of type
    // IList<T>. No actual ListToVectorAdapter object is ever instantiated. Thus, you will
    // see a lot of expressions that cast "this" to "IList<T>". 
    internal sealed class ListToVectorAdapter
    {
        private ListToVectorAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        // T GetAt(uint index)
        internal T GetAt<T>(uint index)
        {
            IList<T> _this = Unsafe.As<IList<T>>(this);
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
        internal uint Size<T>()
        {
            IList<T> _this = Unsafe.As<IList<T>>(this);
            return (uint)_this.Count;
        }

        // IVectorView<T> GetView()
        internal IReadOnlyList<T> GetView<T>()
        {
            IList<T> _this = Unsafe.As<IList<T>>(this);
            Debug.Assert(_this != null);

            // Note: This list is not really read-only - you could QI for a modifiable
            // list.  We gain some perf by doing this.  We believe this is acceptable.
            if (!(_this is IReadOnlyList<T> roList))
            {
                roList = new ReadOnlyCollection<T>(_this);
            }
            return roList;
        }

        // bool IndexOf(T value, out uint index)
        internal bool IndexOf<T>(T value, out uint index)
        {
            IList<T> _this = Unsafe.As<IList<T>>(this);
            int ind = _this.IndexOf(value);

            if (-1 == ind)
            {
                index = 0;
                return false;
            }

            index = (uint)ind;
            return true;
        }

        // void SetAt(uint index, T value)
        internal void SetAt<T>(uint index, T value)
        {
            IList<T> _this = Unsafe.As<IList<T>>(this);
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

        // void InsertAt(uint index, T value)
        internal void InsertAt<T>(uint index, T value)
        {
            IList<T> _this = Unsafe.As<IList<T>>(this);

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
        internal void RemoveAt<T>(uint index)
        {
            IList<T> _this = Unsafe.As<IList<T>>(this);
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

        // void Append(T value)
        internal void Append<T>(T value)
        {
            IList<T> _this = Unsafe.As<IList<T>>(this);
            _this.Add(value);
        }

        // void RemoveAtEnd()
        internal void RemoveAtEnd<T>()
        {
            IList<T> _this = Unsafe.As<IList<T>>(this);
            if (_this.Count == 0)
            {
                Exception e = new InvalidOperationException(SR.InvalidOperation_CannotRemoveLastFromEmptyCollection);
                e.HResult = HResults.E_BOUNDS;
                throw e;
            }

            uint size = (uint)_this.Count;
            RemoveAt<T>(size - 1);
        }

        // void Clear()
        internal void Clear<T>()
        {
            IList<T> _this = Unsafe.As<IList<T>>(this);
            _this.Clear();
        }

        // uint GetMany(uint startIndex, T[] items)
        internal uint GetMany<T>(uint startIndex, T[] items)
        {
            IList<T> _this = Unsafe.As<IList<T>>(this);
            return GetManyHelper<T>(_this, startIndex, items);
        }

        // void ReplaceAll(T[] items)
        internal void ReplaceAll<T>(T[] items)
        {
            IList<T> _this = Unsafe.As<IList<T>>(this);
            _this.Clear();

            if (items != null)
            {
                foreach (T item in items)
                {
                    _this.Add(item);
                }
            }
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

        private static uint GetManyHelper<T>(IList<T> sourceList, uint startIndex, T[] items)
        {
            // Calling GetMany with a start index equal to the size of the list should always
            // return 0 elements, regardless of the input item size
            if (startIndex == sourceList.Count)
            {
                return 0;
            }

            EnsureIndexInt32(startIndex, sourceList.Count);

            if (items == null)
            {
                return 0;
            }

            uint itemCount = Math.Min((uint)items.Length, (uint)sourceList.Count - startIndex);
            for (uint i = 0; i < itemCount; ++i)
            {
                items[i] = sourceList[(int)(i + startIndex)];
            }

            if (typeof(T) == typeof(string))
            {
                string[] stringItems = (items as string[])!;

                // Fill in rest of the array with string.Empty to avoid marshaling failure
                for (uint i = itemCount; i < items.Length; ++i)
                    stringItems[i] = string.Empty;
            }

            return itemCount;
        }
    }
}
