// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

        [Intrinsic]
        internal static unsafe IntPtr ToIntPtr(RuntimeTypeHandle handle)
        {
            return handle._value;
        }
    }
}

namespace Internal.Runtime.CompilerHelpers
{
    // Needed by the compiler to lower LDTOKEN
    internal static class LdTokenHelpers
    {
        private static RuntimeTypeHandle GetRuntimeTypeHandle(IntPtr pEEType)
        {
            return new RuntimeTypeHandle(pEEType);
        }

        private static Type GetRuntimeType(IntPtr pEEType)
        {
            return Type.GetTypeFromHandle(new RuntimeTypeHandle(pEEType));
        }
    }
}
