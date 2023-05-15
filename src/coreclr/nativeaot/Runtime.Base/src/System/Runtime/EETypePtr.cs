// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
/*============================================================
**
** Class:  EETypePtr
**
**
** Purpose: Pointer Type to a MethodTable in the runtime.
**
**
===========================================================*/

namespace System
{
    // This type does not implement GetHashCode but implements Equals
#pragma warning disable 0659

    [StructLayout(LayoutKind.Sequential)]
    public struct EETypePtr
    {
        private IntPtr _value;

        internal EETypePtr(IntPtr value)
        {
            _value = value;
        }

        internal bool Equals(EETypePtr p)
        {
            return (_value == p._value);
        }

        internal unsafe Internal.Runtime.MethodTable* ToPointer()
        {
            return (Internal.Runtime.MethodTable*)(void*)_value;
        }

        [Intrinsic]
        internal static EETypePtr EETypePtrOf<T>()
        {
            throw new NotImplementedException();
        }
    }
}
