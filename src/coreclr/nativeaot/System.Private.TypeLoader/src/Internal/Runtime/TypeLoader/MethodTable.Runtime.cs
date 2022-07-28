// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Internal.Runtime.TypeLoader;

namespace Internal.Runtime
{
    // Supplies type loader specific extensions to MethodTable
    internal partial struct MethodTable
    {
        private static unsafe MethodTable* GetArrayEEType()
        {
            return typeof(Array).TypeHandle.ToEETypePtr();
        }

        internal unsafe RuntimeTypeHandle ToRuntimeTypeHandle()
        {
            fixed (MethodTable* pThis = &this)
            {
                IntPtr result = (IntPtr)pThis;
                return *(RuntimeTypeHandle*)&result;
            }
        }
    }
}
