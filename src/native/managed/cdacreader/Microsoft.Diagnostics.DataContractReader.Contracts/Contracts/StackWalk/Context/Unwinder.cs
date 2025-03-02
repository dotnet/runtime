// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;
internal static unsafe partial class Unwinder
{
    [LibraryImport("unwinder_cdac_arm64", EntryPoint = "arm64Unwind")]
    private static partial int ARM64Unwind(
        ref ARM64Context context,
        delegate* unmanaged<ulong, void*, int, void*, int> readFromTarget,
        delegate* unmanaged<int, void**, void*, int> getAllocatedBuffer,
        delegate* unmanaged<ulong, void*, void*, void*, void> getStackWalkInfo,
        delegate* unmanaged<void> unwinderFail,
        void* callbackContext);

    public static int ARM64Unwind(
        ref ARM64Context context,
        Target target)
    {
        using CallbackContext callbackContext = new(target);

        GCHandle handle = GCHandle.Alloc(callbackContext);
        int ret = ARM64Unwind(
            ref context,
            &ReadFromTarget,
            &GetAllocatedBuffer,
            &GetStackWalkInfo,
            &UnwinderFail,
            GCHandle.ToIntPtr(handle).ToPointer());
        handle.Free();

        return ret;
    }

    [LibraryImport("unwinder_cdac_amd64", EntryPoint = "amd64Unwind")]
    private static partial int AMD64Unwind(
        ref AMD64Context context,
        delegate* unmanaged<ulong, void*, int, void*, int> readFromTarget,
        delegate* unmanaged<int, void**, void*, int> getAllocatedBuffer,
        delegate* unmanaged<ulong, void*, void*, void*, void> getStackWalkInfo,
        delegate* unmanaged<void> unwinderFail,
        void* callbackContext);

    public static int AMD64Unwind(
        ref AMD64Context context,
        Target target)
    {
        using CallbackContext callbackContext = new(target);

        GCHandle handle = GCHandle.Alloc(callbackContext);
        int ret = AMD64Unwind(
            ref context,
            &ReadFromTarget,
            &GetAllocatedBuffer,
            &GetStackWalkInfo,
            &UnwinderFail,
            GCHandle.ToIntPtr(handle).ToPointer());
        handle.Free();

        return ret;
    }

    /// <summary>
    /// Used to inject target into unwinder callbacks and track memory allocated for native unwinder.
    /// </summary>
    private sealed class CallbackContext(Target target) : IDisposable
    {
        private bool disposed;
        public Target Target { get; } = target;
        public List<IntPtr> AllocatedRegions { get; } = [];

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                foreach (IntPtr ptr in AllocatedRegions)
                {
                    NativeMemory.Free(ptr.ToPointer());
                }
            }
            disposed = true;
        }
    }

    // ReadFromTarget allows the unwinder to read memory from the target process
    // into an allocated buffer. This buffer is either allocated by the unwinder
    // with its lifetime managed by the unwinder or allocated through GetAllocatedBuffer.
    // In the latter case, the unwinder can only use the buffer for the duration of the
    // unwind call. Once the call is over the cDAC will free all allocated buffers.
    [UnmanagedCallersOnly]
    private static unsafe int ReadFromTarget(ulong address, void* pBuffer, int bufferSize, void* context)
    {
        if (GCHandle.FromIntPtr((IntPtr)context).Target is not CallbackContext callbackContext)
        {
            return -1;
        }
        Span<byte> span = new Span<byte>(pBuffer, bufferSize);
        callbackContext.Target.ReadBuffer(address, span);
        return 0;
    }

    // GetAllocatedBuffer allows the unwinder to allocate a buffer that will be freed
    // once the unwinder call is complete.
    // Freeing is handeled in the Dispose method of CallbackContext.
    [UnmanagedCallersOnly]
    private static unsafe int GetAllocatedBuffer(int bufferSize, void** ppBuffer, void* context)
    {
        if (GCHandle.FromIntPtr((IntPtr)context).Target is not CallbackContext callbackContext)
        {
            return -1;
        }
        *ppBuffer = NativeMemory.Alloc((nuint)bufferSize);
        callbackContext.AllocatedRegions.Add((IntPtr)(*ppBuffer));
        return 0;
    }

    // cDAC version of GetRuntimeStackWalkInfo defined in codeman.cpp
    // To maintain the same signature as the original function, this returns void.
    // If the unwindInfoBase or funcEntry can not be found, both will be 0.
    [UnmanagedCallersOnly]
    private static unsafe void GetStackWalkInfo(ulong controlPC, void* pUnwindInfoBase, void* pFuncEntry, void* context)
    {
        if ((nuint)pUnwindInfoBase != 0) *(nuint*)pUnwindInfoBase = 0;
        if ((nuint)pFuncEntry != 0) *(nuint*)pFuncEntry = 0;

        if (GCHandle.FromIntPtr((IntPtr)context).Target is not CallbackContext callbackContext)
        {
            return;
        }

        IExecutionManager eman = callbackContext.Target.Contracts.ExecutionManager;
        try
        {
            if (eman.GetCodeBlockHandle(controlPC) is CodeBlockHandle cbh)
            {
                TargetPointer methodDesc = eman.GetMethodDesc(cbh);
                TargetPointer unwindInfoBase = eman.GetUnwindInfoBaseAddress(cbh);
                TargetPointer unwindInfo = eman.GetUnwindInfo(cbh, controlPC);
                if ((nuint)pUnwindInfoBase != 0) *(nuint*)pUnwindInfoBase = (nuint)unwindInfoBase.Value;
                if ((nuint)pFuncEntry != 0) *(nuint*)pFuncEntry = (nuint)unwindInfo.Value;
            }
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"GetStackWalkInfo failed: {ex}");
        }
    }

    [UnmanagedCallersOnly]
    private static void UnwinderFail()
    {
        Debug.Fail("Native unwinder assertion failure.");
    }
}
