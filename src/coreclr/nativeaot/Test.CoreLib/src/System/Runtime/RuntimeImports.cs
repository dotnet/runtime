// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Internal.Runtime;

namespace System.Runtime
{
    // CONTRACT with Runtime
    // This class lists all the static methods that the redhawk runtime exports to a class library
    // These are not expected to change much but are needed by the class library to implement its functionality
    //
    //      The contents of this file can be modified if needed by the class library
    //      E.g., the class and methods are marked internal assuming that only the base class library needs them
    //            but if a class library wants to factor differently (such as putting the GCHandle methods in an
    //            optional library, those methods can be moved to a different file/namespace/dll

    public static partial class RuntimeImports
    {
        private const string RuntimeLibrary = "*";

        //
        // calls for GCHandle.
        // These methods are needed to implement GCHandle class like functionality (optional)
        //

        // Allocate handle.
        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpHandleAlloc")]
        private static extern IntPtr RhpHandleAlloc(object value, GCHandleType type);

        internal static IntPtr RhHandleAlloc(object value, GCHandleType type)
        {
            IntPtr h = RhpHandleAlloc(value, type);
            if (h == IntPtr.Zero)
                throw new OutOfMemoryException();
            return h;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpRegisterFrozenSegment")]
        internal static extern IntPtr RhpRegisterFrozenSegment(IntPtr pSegmentStart, IntPtr length);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpUnregisterFrozenSegment")]
        internal static extern void RhpUnregisterFrozenSegment(IntPtr pSegmentHandle);

        [RuntimeImport(RuntimeLibrary, "RhpGetModuleSection")]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr RhGetModuleSection(ref TypeManagerHandle module, ReadyToRunSectionType section, out int length);

        internal static IntPtr RhGetModuleSection(TypeManagerHandle module, ReadyToRunSectionType section, out int length)
        {
            return RhGetModuleSection(ref module, section, out length);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpCreateTypeManager")]
        internal static extern unsafe TypeManagerHandle RhpCreateTypeManager(IntPtr osModule, IntPtr moduleHeader, IntPtr* pClasslibFunctions, int nClasslibFunctions);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpRegisterOsModule")]
        internal static extern unsafe IntPtr RhpRegisterOsModule(IntPtr osModule);

        //
        // calls to runtime for allocation
        // These calls are needed in types which cannot use "new" to allocate and need to do it manually
        //
        // calls to runtime for allocation
        //
        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhNewObject")]
        private static extern unsafe object RhNewObject(MethodTable* pEEType);

        internal static unsafe object RhNewObject(EETypePtr pEEType)
            => RhNewObject(pEEType.ToPointer());

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhNewArray")]
        private static extern unsafe Array RhNewArray(MethodTable* pEEType, int length);

        internal static unsafe Array RhNewArray(EETypePtr pEEType, int length)
            => RhNewArray(pEEType.ToPointer(), length);

        [DllImport(RuntimeLibrary)]
        internal static extern unsafe void RhAllocateNewObject(IntPtr pEEType, uint flags, void* pResult);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpFallbackFailFast")]
        internal static extern unsafe void RhpFallbackFailFast();

        //
        // Interlocked helpers
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpLockCmpXchg32")]
        internal static extern int InterlockedCompareExchange(ref int location1, int value, int comparand);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpLockCmpXchg64")]
        internal static extern long InterlockedCompareExchange(ref long location1, long value, long comparand);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpMemoryBarrier")]
        internal static extern void MemoryBarrier();

        // Moves memory from smem to dmem. Size must be a positive value.
        // This copy uses an intrinsic to be safe for copying arbitrary bits of
        // heap memory
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhBulkMoveWithWriteBarrier")]
        internal static extern unsafe void RhBulkMoveWithWriteBarrier(ref byte dmem, ref byte smem, nuint size);
    }
}
