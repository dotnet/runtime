// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // This is a set of stub methods implementing the support for the IVectorView`1 interface on managed
    // objects that implement IReadOnlyList`1. Used by the interop mashaling infrastructure.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not IReadOnlyListToIVectorViewAdapter objects. Rather, they are of type
    // IReadOnlyList<T>. No actual IReadOnlyListToIVectorViewAdapter object is ever instantiated. Thus, you will
    // see a lot of expressions that cast "this" to "IReadOnlyList<T>". 
    [DebuggerDisplay("Size = {Size}")]
    internal sealed class IReadOnlyListToIVectorViewAdapter
    {
        private IReadOnlyListToIVectorViewAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        // T GetAt(uint index)
        internal T GetAt<T>(uint index)
        {
            IReadOnlyList<T> _this = Unsafe.As<IReadOnlyList<T>>(this);
            EnsureIndexInt32(index, _this.Count);

            try
            {
                return _this[(int)index];
            }
            catch (ArgumentOutOfRangeException ex)
            {
                ex.HResult = HResults.E_BOUNDS;
                throw;
            }
        }

        // uint Size { get }
        internal uint Size<T>()
        {
            IReadOnlyList<T> _this = Unsafe.As<IReadOnlyList<T>>(this);
            return (uint)_this.Count;
        }

        // bool IndexOf(T value, out uint index)
        internal bool IndexOf<T>(T value, out uint index)
        {
            IReadOnlyList<T> _this = Unsafe.As<IReadOnlyList<T>>(this);

            int ind = -1;
            int max = _this.Count;
            for (int i = 0; i < max; i++)
            {
                if (EqualityComparer<T>.Default.Equals(value, _this[i]))
                {
                    ind = i;
                    break;
                }
            }

            if (-1 == ind)
            {
                index = 0;
                return false;
            }

            index = (uint)ind;
            return true;
        }

        // uint GetMany(uint startIndex, T[] items)
        internal uint GetMany<T>(uint startIndex, T[] items)
        {
            IReadOnlyList<T> _this = Unsafe.As<IReadOnlyList<T>>(this);

            // REX spec says "calling GetMany with startIndex equal to the length of the vector 
            // (last valid index + 1) and any specified capacity will succeed and return zero actual
            // elements".
            if (startIndex == _this.Count)
                return 0;

            EnsureIndexInt32(startIndex, _this.Count);

            if (items == null)
            {
                return 0;
            }

            uint itemCount = Math.Min((uint)items.Length, (uint)_this.Count - startIndex);

            for (uint i = 0; i < itemCount; ++i)
            {
                items[i] = _this[(int)(i + startIndex)];
            }

            if (typeof(T) == typeof(string))
            {
                string[] stringItems = (items as string[])!;

                // Fill in the rest of the array with string.Empty to avoid marshaling failure
                for (uint i = itemCount; i < items.Length; ++i)
                    stringItems[i] = string.Empty;
            }

            return itemCount;
        }

        #region Helpers

        private static void EnsureIndexInt32(uint index, int listCapacity)
        {
            // We use '<=' and not '<' because int.MaxValue == index would imply
            // that Size > int.MaxValue:
            if (((uint)int.MaxValue) <= index || index >= (uint)listCapacity)
            {
                Exception e = new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexLargerThanMaxValue);
                e.HResult = HResults.E_BOUNDS;
                throw e;
            }
        }

        #endregion Helpers
    }
}
