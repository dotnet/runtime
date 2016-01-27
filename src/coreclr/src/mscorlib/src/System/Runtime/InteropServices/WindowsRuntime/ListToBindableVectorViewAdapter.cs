// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Security;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

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
                throw new ArgumentNullException("list");

            Contract.EndContractBlock();

            this.list = list;
        }

        private static void EnsureIndexInt32(uint index, int listCapacity)
        {
            // We use '<=' and not '<' becasue Int32.MaxValue == index would imply
            // that Size > Int32.MaxValue:
            if (((uint)Int32.MaxValue) <= index || index >= (uint)listCapacity)
            {
                Exception e = new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_IndexLargerThanMaxValue"));
                e.SetErrorCode(__HResults.E_BOUNDS);
                throw e;
            }
        }

        // IBindableIterable implementation:

        public IBindableIterator First()
        {
            IEnumerator enumerator = list.GetEnumerator();
            return new EnumeratorToIteratorAdapter<object>(new EnumerableToBindableIterableAdapter.NonGenericToGenericEnumerator(enumerator));
        }

        // IBindableVectorView implementation:

        public object GetAt(uint index)
        {
            EnsureIndexInt32(index, list.Count);

            try
            {
                return list[(int)index];

            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw WindowsRuntimeMarshal.GetExceptionForHR(__HResults.E_BOUNDS, ex, "ArgumentOutOfRange_IndexOutOfRange");
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
