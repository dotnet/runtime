// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Internal.Runtime
{
    /// <summary>
    /// TypeManagerHandle represents an AOT module in MRT based runtimes.
    /// These handles are a pointer to a TypeManager
    /// </summary>
    public unsafe partial struct TypeManagerHandle
    {
        private TypeManager* _handleValue;

        // This is a partial definition of the TypeManager struct which is defined in TypeManager.h
        [StructLayout(LayoutKind.Sequential)]
        private struct TypeManager
        {
            public IntPtr OsHandle;
            public IntPtr ReadyToRunHeader;
        }

        public TypeManagerHandle(IntPtr handleValue)
        {
            _handleValue = (TypeManager*)handleValue;
        }

        public IntPtr GetIntPtrUNSAFE()
        {
            return (IntPtr)_handleValue;
        }

        public bool IsNull
        {
            get
            {
                return _handleValue == null;
            }
        }

        public unsafe IntPtr OsModuleBase
        {
            get
            {
                return _handleValue->OsHandle;
            }
        }
    }
}
