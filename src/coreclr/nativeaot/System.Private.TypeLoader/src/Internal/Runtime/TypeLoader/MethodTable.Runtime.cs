// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.Runtime.TypeLoader;

namespace Internal.Runtime
{
    // Supplies type loader specific extensions to MethodTable
    internal partial struct MethodTable
    {
        private static unsafe MethodTable* GetArrayEEType()
        {
            return MethodTable.Of<Array>();
        }

        internal unsafe RuntimeTypeHandle ToRuntimeTypeHandle()
        {
            IntPtr result = (IntPtr)Unsafe.AsPointer(ref this);
            return *(RuntimeTypeHandle*)&result;
        }
    }
}
