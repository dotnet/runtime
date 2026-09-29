// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if SYSTEM_PRIVATE_CORELIB
using Internal.Metadata.NativeFormat;
#endif

namespace System.Runtime
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DispatchCell
    {
        public nint MethodTable;
        public nint Code;
    }

#if SYSTEM_PRIVATE_CORELIB
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public unsafe struct DynamicDispatchCell
    {
        private const nint GvmTypeFlag = 1;

        public DispatchCell Cell;
        private nint _associatedTypeAndFlag;

        public bool IsGvmDispatchCell => ((nint)_associatedTypeAndFlag & GvmTypeFlag) != 0;

        public DynamicInterfaceDispatchCell* AsInterfaceDispatchCell()
        {
            return (DynamicInterfaceDispatchCell*)Unsafe.AsPointer(ref this);
        }

        public DynamicGvmDispatchCell* AsGvmDispatchCell()
        {
            return (DynamicGvmDispatchCell*)Unsafe.AsPointer(ref this);
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct DynamicInterfaceDispatchCell
        {
            public DynamicDispatchCell DispatchCell;
            public nint Slot;

            public nint InterfaceType
            {
                get
                {
                    Debug.Assert(!DispatchCell.IsGvmDispatchCell);
                    return DispatchCell._associatedTypeAndFlag;
                }
                set
                {
                    Debug.Assert((value & GvmTypeFlag) == 0);
                    DispatchCell._associatedTypeAndFlag = value;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct DynamicGvmDispatchCell
        {
            public DynamicDispatchCell DispatchCell;
            private MethodHandle _handle;
            private bool _isAsyncVariant;
            public void* Instantiation;

            public nint OwningType
            {
                get
                {
                    Debug.Assert(DispatchCell.IsGvmDispatchCell);
                    return DispatchCell._associatedTypeAndFlag & ~GvmTypeFlag;
                }
                set
                {
                    Debug.Assert((value & GvmTypeFlag) == 0);
                    DispatchCell._associatedTypeAndFlag = value | GvmTypeFlag;
                }
            }

            public MethodHandle Handle
            {
                get => _handle;
                set => _handle = value;
            }

            public bool IsAsyncVariant
            {
                get => _isAsyncVariant;
                set => _isAsyncVariant = value;
            }
        }
    }
#endif
}
