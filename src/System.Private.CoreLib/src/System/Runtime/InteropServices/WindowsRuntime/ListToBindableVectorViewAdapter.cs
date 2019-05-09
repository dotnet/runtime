// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    /// A Windows Runtime IBindableVectorView implementation that wraps around a managed IList exposing
    /// it to Windows runtime interop.
    internal sealed class ListToBindableVectorViewAdapter : IBindableVectorView
    {
        private readonly IList list;

        internal ListToBindableVectorViewAdapter(IList list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));


            this.list = list;
        }

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

        // IBindableIterable implementation:

        public IBindableIterator First()
        {
            IEnumerator enumerator = list.GetEnumerator();
            return new EnumeratorToIteratorAdapter<object?>(new EnumerableToBindableIterableAdapter.NonGenericToGenericEnumerator(enumerator));
        }

        // IBindableVectorView implementation:

        public object? GetAt(uint index)
        {
            EnsureIndexInt32(index, list.Count);

            try
            {
                return list[(int)index];
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw WindowsRuntimeMarshal.GetExceptionForHR(HResults.E_BOUNDS, ex, "ArgumentOutOfRange_IndexOutOfRange");
            }
        }

        public uint Size
        {
            get
            {
                return (uint)list.Count;
            }
        }

        public bool IndexOf(object value, out uint index)
        {
            int ind = list.IndexOf(value);

            if (-1 == ind)
            {
                index = 0;
                return false;
            }

            index = (uint)ind;
            return true;
        }
    }
}
