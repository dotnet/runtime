// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.Runtime;

namespace System
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RuntimeTypeHandle
    {
        private IntPtr _value;

        internal RuntimeTypeHandle(IntPtr value)
        {
            _value = value;
        }

        unsafe internal RuntimeTypeHandle(MethodTable* value)
            :this((IntPtr)value)
        {
        }

        [Intrinsic]
        internal static unsafe IntPtr ToIntPtr(RuntimeTypeHandle handle)
        {
            return handle._value;
        }

        // Implementation of CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE
        internal static unsafe RuntimeTypeHandle GetRuntimeTypeHandleFromMethodTable(MethodTable* pMT)
        {
            return new RuntimeTypeHandle(pMT);
        }
    }
}
