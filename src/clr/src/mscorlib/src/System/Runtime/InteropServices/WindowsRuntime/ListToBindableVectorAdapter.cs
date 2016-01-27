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
            Contract.Assert(false, "This class is never instantiated");
        }

        // object GetAt(uint index)
        [SecurityCritical]
        internal object GetAt(uint index)
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
            EnsureIndexInt32(index, _this.Count);        

            try
            {
                return _this[(Int32)index];
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw WindowsRuntimeMarshal.GetExceptionForHR(__HResults.E_BOUNDS, ex, "ArgumentOutOfRange_IndexOutOfRange");
            }
        }

        // uint Size { get }
        [SecurityCritical]
        internal uint Size()
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
            return (uint)_this.Count;
        }

        // IBindableVectorView GetView()
        [SecurityCritical]
        internal IBindableVectorView GetView()
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
            return new ListToBindableVectorViewAdapter(_this);
        }

        // bool IndexOf(object value, out uint index)
        [SecurityCritical]
        internal bool IndexOf(object value, out uint index)
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
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
        [SecurityCritical]
        internal void SetAt(uint index, object value)
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
            EnsureIndexInt32(index, _this.Count);

            try
            {
                _this[(int)index] = value;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw WindowsRuntimeMarshal.GetExceptionForHR(__HResults.E_BOUNDS, ex, "ArgumentOutOfRange_IndexOutOfRange");
            }
        }

        // void InsertAt(uint index, object value)
        [SecurityCritical]
        internal void InsertAt(uint index, object value)
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);

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
                ex.SetErrorCode(__HResults.E_BOUNDS);
                throw;
            }
        }

        // void RemoveAt(uint index)
        [SecurityCritical]
        internal void RemoveAt(uint index)
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
            EnsureIndexInt32(index, _this.Count); 

            try
            {
                _this.RemoveAt((Int32)index);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // Change error code to match what WinRT expects
                ex.SetErrorCode(__HResults.E_BOUNDS);
                throw;
            }
        }

        // void Append(object value)
        [SecurityCritical]
        internal void Append(object value)
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
            _this.Add(value);
        }

        // void RemoveAtEnd()
        [SecurityCritical]
        internal void RemoveAtEnd()
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
            if (_this.Count == 0)
            {
                Exception e = new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotRemoveLastFromEmptyCollection"));
                e.SetErrorCode(__HResults.E_BOUNDS);
                throw e;
            }

            uint size = (uint)_this.Count;
            RemoveAt(size - 1);
        }

        // void Clear()
        [SecurityCritical]
        internal void Clear()
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
            _this.Clear();
        }

        // Helpers:

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
    }
}
