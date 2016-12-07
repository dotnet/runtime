// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System
{
    // ByReference<T> is meant to be used to represent "ref T" fields. It is working
    // around lack of first class support for byref fields in C# and IL. The JIT and 
    // type loader has special handling for it that turns it into a thin wrapper around ref T.
    internal struct ByReference<T>
    {
        private IntPtr _value;

        public ByReference(ref T value)
        {
            // TODO-SPAN: This has GC hole. It needs to be JIT intrinsic instead
            unsafe { _value = (IntPtr)Unsafe.AsPointer(ref value); }
        }

        public ref T Value
        {
            get
            {
                // TODO-SPAN: This has GC hole. It needs to be JIT intrinsic instead
                unsafe { return ref Unsafe.As<IntPtr, T>(ref *(IntPtr*)_value); }
            }
        }
    }
}
