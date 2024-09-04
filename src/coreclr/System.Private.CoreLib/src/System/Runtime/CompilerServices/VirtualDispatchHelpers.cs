// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices;

[StackTraceHidden]
[DebuggerStepThrough]
internal static unsafe partial class VirtualDispatchHelpers
{
    private struct VirtualResolutionData : IEquatable<VirtualResolutionData>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VirtualResolutionData(MethodTable* methodTable, IntPtr classHandleTargetMethod, IntPtr methodHandle)
        {
            HashCode = (int) ((uint)methodTable + (BitOperations.RotateLeft((uint)classHandleTargetMethod, 5)) + (BitOperations.RotateRight((uint)methodHandle, 5)));
            MethodTable = methodTable;
            ClassHandleTargetMethod = classHandleTargetMethod;
            MethodHandle = methodHandle;
        }
        public int HashCode;
        public MethodTable* MethodTable;
        public IntPtr ClassHandleTargetMethod;
        public IntPtr MethodHandle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(VirtualResolutionData other) =>
            HashCode == other.HashCode &&
            MethodTable == other.MethodTable &&
            ClassHandleTargetMethod == other.ClassHandleTargetMethod &&
            MethodHandle == other.MethodHandle;

        public override bool Equals(object? obj) => obj is VirtualResolutionData other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode;
    }

    private struct VirtualResolutionData2 : IEquatable<VirtualResolutionData2>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VirtualResolutionData2(MethodTable* methodTable, IntPtr methodHandle)
        {
            MethodTable = methodTable;
            MethodHandle = methodHandle;
        }
        public MethodTable* MethodTable;
        public IntPtr MethodHandle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(VirtualResolutionData2 other) =>
            MethodTable == other.MethodTable &&
            MethodHandle == other.MethodHandle;

        public override bool Equals(object? obj) => obj is VirtualResolutionData2 other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => (int) ((uint)MethodTable + (BitOperations.RotateLeft((uint)MethodHandle, 5)));
    }

    private struct VirtualFunctionPointerArgs
    {
        public IntPtr classHnd;
        public IntPtr methodHnd;
    };

#if DEBUG
        // use smaller numbers to hit resizing/preempting logic in debug
        private const int InitialCacheSize = 8; // MUST BE A POWER OF TWO
        private const int MaximumCacheSize = 512;
#else
        private const int InitialCacheSize = 128; // MUST BE A POWER OF TWO
        private const int MaximumCacheSize = 8 * 1024;
#endif // DEBUG

    private static GenericCache<VirtualResolutionData, IntPtr> s_virtualFunctionPointerCache = new GenericCache<VirtualResolutionData, IntPtr>(InitialCacheSize, MaximumCacheSize);
    private static GenericCache<VirtualResolutionData2, IntPtr> s_virtualFunctionPointerCache2 = new GenericCache<VirtualResolutionData2, IntPtr>(InitialCacheSize, MaximumCacheSize);

    [LibraryImport(RuntimeHelpers.QCall)]
    private static unsafe partial IntPtr JIT_ResolveVirtualFunctionPointer(ObjectHandleOnStack obj, IntPtr classHandle, IntPtr methodHandle);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DebuggerHidden]
    private static unsafe IntPtr VirtualFunctionPointerSlowpath(object obj, IntPtr classHandle, IntPtr methodHandle)
    {
        IntPtr result = JIT_ResolveVirtualFunctionPointer(ObjectHandleOnStack.Create(ref obj), classHandle, methodHandle);
        s_virtualFunctionPointerCache.TrySet(new VirtualResolutionData(RuntimeHelpers.GetMethodTable(obj), classHandle, methodHandle), result);
        GC.KeepAlive(obj);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DebuggerHidden]
    private static unsafe IntPtr VirtualFunctionPointer2Slowpath(object obj, IntPtr methodHandle)
    {
        IntPtr result = JIT_ResolveVirtualFunctionPointer(ObjectHandleOnStack.Create(ref obj), IntPtr.Zero, methodHandle);
        s_virtualFunctionPointerCache2.TrySet(new VirtualResolutionData2(RuntimeHelpers.GetMethodTable(obj), methodHandle), result);
        GC.KeepAlive(obj);
        return result;
    }

    [DebuggerHidden]
    private static unsafe IntPtr VirtualFunctionPointer(object obj, IntPtr classHandle, IntPtr methodHandle)
    {
        if (s_virtualFunctionPointerCache.TryGet(new VirtualResolutionData(RuntimeHelpers.GetMethodTable(obj), classHandle, methodHandle), out IntPtr result))
        {
            return result;
        }
        return VirtualFunctionPointerSlowpath(obj, classHandle, methodHandle);
    }

    [DebuggerHidden]
    private static unsafe IntPtr VirtualFunctionPointer2(object obj, IntPtr methodHandle)
    {
        if (s_virtualFunctionPointerCache2.TryGet(new VirtualResolutionData2(RuntimeHelpers.GetMethodTable(obj), methodHandle), out IntPtr result))
        {
            return result;
        }
        return VirtualFunctionPointer2Slowpath(obj, methodHandle);
    }

    [DebuggerHidden]
    private static unsafe IntPtr VirtualFunctionPointer_Dynamic(object obj, ref VirtualFunctionPointerArgs virtualFunctionPointerArgs)
    {
        IntPtr classHandle = virtualFunctionPointerArgs.classHnd;
        IntPtr methodHandle = virtualFunctionPointerArgs.methodHnd;

        if (s_virtualFunctionPointerCache.TryGet(new VirtualResolutionData(RuntimeHelpers.GetMethodTable(obj), classHandle, methodHandle), out IntPtr result))
        {
            return result;
        }
        return VirtualFunctionPointerSlowpath(obj, classHandle, methodHandle);
    }
}
