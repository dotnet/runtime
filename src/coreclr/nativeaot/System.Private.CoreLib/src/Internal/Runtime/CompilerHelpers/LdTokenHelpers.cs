// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Reflection.Core.NonPortable;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// These methods are used to implement ldtoken instruction.
    /// </summary>
    internal static class LdTokenHelpers
    {
        private static RuntimeTypeHandle GetRuntimeTypeHandle(IntPtr pEEType)
        {
            return new RuntimeTypeHandle(new EETypePtr(pEEType));
        }

        private static unsafe RuntimeMethodHandle GetRuntimeMethodHandle(IntPtr pHandleSignature)
        {
            RuntimeMethodHandle returnValue;
            *(IntPtr*)&returnValue = pHandleSignature;
            return returnValue;
        }

        private static unsafe RuntimeFieldHandle GetRuntimeFieldHandle(IntPtr pHandleSignature)
        {
            RuntimeFieldHandle returnValue;
            *(IntPtr*)&returnValue = pHandleSignature;
            return returnValue;
        }

        private static Type GetRuntimeType(IntPtr pEEType)
        {
            return Type.GetTypeFromEETypePtr(new EETypePtr(pEEType));
        }
    }
}
