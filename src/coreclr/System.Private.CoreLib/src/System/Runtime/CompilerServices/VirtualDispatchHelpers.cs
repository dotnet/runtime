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
        public int _hashCode;
        public MethodTable* _objectMethodTable;
        public IntPtr _classHandle;
        public IntPtr _methodHandle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VirtualResolutionData(MethodTable* objectMethodTable, IntPtr classHandle, IntPtr methodHandle)
        {
            _hashCode = (int) ((uint)objectMethodTable + (BitOperations.RotateLeft((uint)classHandle, 5)) + (BitOperations.RotateRight((uint)methodHandle, 5)));
            _objectMethodTable = objectMethodTable;
            _classHandle = classHandle;
            _methodHandle = methodHandle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(VirtualResolutionData other) =>
            _hashCode == other._hashCode &&
            (((nint)_objectMethodTable - (nint)other._objectMethodTable) |
            (_classHandle - other._classHandle) |
            (_methodHandle - other._methodHandle)) == 0;

        public override bool Equals(object? obj) => obj is VirtualResolutionData other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => _hashCode;
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

    [LibraryImport(RuntimeHelpers.QCall)]
    private static unsafe partial IntPtr ResolveVirtualFunctionPointer(ObjectHandleOnStack obj, IntPtr classHandle, IntPtr methodHandle);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DebuggerHidden]
    private static unsafe IntPtr VirtualFunctionPointerSlow(object obj, IntPtr classHandle, IntPtr methodHandle)
    {
        IntPtr result = ResolveVirtualFunctionPointer(ObjectHandleOnStack.Create(ref obj), classHandle, methodHandle);
        s_virtualFunctionPointerCache.TrySet(new VirtualResolutionData(RuntimeHelpers.GetMethodTable(obj), classHandle, methodHandle), result);
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
        return VirtualFunctionPointerSlow(obj, classHandle, methodHandle);
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
        return VirtualFunctionPointerSlow(obj, classHandle, methodHandle);
    }
}
