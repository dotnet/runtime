// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;
internal static unsafe partial class Unwinder
{
    // ReadCallback allows the unwinder to read memory from the target process
    // into an allocated buffer. This buffer is either allocated by the unwinder
    // with its lifetime managed by the unwinder or allocated through GetAllocatedBuffer.
    // In the latter case, the unwinder can only use the buffer for the duration of the
    // unwind call. Once the call is over the cDAC will free all allocated buffers.
    public delegate int ReadCallback(ulong address, void* buffer, int bufferSize);
    public delegate int GetAllocatedBuffer(int bufferSize, void** buffer);

    // cDAC version of GetRuntimeStackWalkInfo defined in codeman.cpp
    // To maintain the same signature as the original function, we return void.
    // If the moduleBase or funcEntry can not be found, both will be 0.
    public delegate void GetStackWalkInfo(ulong controlPC, void* pModuleBase, void* pFuncEntry);

    [LibraryImport("unwinder_cdac_arm64", EntryPoint = "arm64Unwind")]
    private static partial int ARM64Unwind(
        ref ARM64Context context,
        [MarshalAs(UnmanagedType.FunctionPtr)] ReadCallback readCallback,
        [MarshalAs(UnmanagedType.FunctionPtr)] GetAllocatedBuffer getAllocatedBuffer,
        [MarshalAs(UnmanagedType.FunctionPtr)] GetStackWalkInfo getStackWalkInfo);

    [LibraryImport("unwinder_cdac_amd64", EntryPoint = "amd64Unwind")]
    private static partial int AMD64Unwind(
        ref AMD64Context context,
        [MarshalAs(UnmanagedType.FunctionPtr)] ReadCallback readCallback,
        [MarshalAs(UnmanagedType.FunctionPtr)] GetAllocatedBuffer getAllocatedBuffer,
        [MarshalAs(UnmanagedType.FunctionPtr)] GetStackWalkInfo getStackWalkInfo);

    public static int AMD64Unwind(
        ref AMD64Context context,
        Target target)
    {
        ReadCallback readCallback;
        GetAllocatedBuffer getAllocatedBuffer;
        GetStackWalkInfo getStackWalkInfo;

        // Move to IDisposable for freeing
        List<IntPtr> allocatedRegions = [];

        readCallback = (address, pBuffer, bufferSize) =>
        {
            Span<byte> span = new Span<byte>(pBuffer, bufferSize);
            target.ReadBuffer(address, span);
            return 0;
        };
        getAllocatedBuffer = (bufferSize, ppBuffer) =>
        {
            *ppBuffer = NativeMemory.Alloc((nuint)bufferSize);
            IntPtr pBuffer = new(*ppBuffer);
            //Console.WriteLine($"Allocating buffer at {pBuffer:x16}");
            allocatedRegions.Add(pBuffer);
            return 0;
        };
        getStackWalkInfo = (controlPC, pModuleBase, pFuncEntry) =>
        {
            IExecutionManager eman = target.Contracts.ExecutionManager;

            if ((nuint)pModuleBase != 0) *(nuint*)pModuleBase = 0;
            if ((nuint)pFuncEntry != 0) *(nuint*)pFuncEntry = 0;

            try
            {
                if (eman.GetCodeBlockHandle(controlPC) is CodeBlockHandle cbh)
                {
                    TargetPointer methodDesc = eman.GetMethodDesc(cbh);
                    TargetPointer moduleBase = eman.GetModuleBaseAddress(cbh);
                    TargetPointer unwindInfo = eman.GetUnwindInfo(cbh, controlPC);
                    if ((nuint)pModuleBase != 0) *(nuint*)pModuleBase = (nuint)moduleBase.Value;
                    if ((nuint)pFuncEntry != 0) *(nuint*)pFuncEntry = (nuint)unwindInfo.Value;
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"GetStackWalkInfo failed: {ex}");
            }
        };

        int ret = AMD64Unwind(ref context, readCallback, getAllocatedBuffer, getStackWalkInfo);

        foreach (IntPtr ptr in allocatedRegions)
        {
            //Console.WriteLine($"Freeing buffer at {ptr:x16}");
            NativeMemory.Free(ptr.ToPointer());
        }

        return ret;
    }

    public static int ARM64Unwind(
        ref ARM64Context context,
        Target target)
    {
        ReadCallback readCallback;
        GetAllocatedBuffer getAllocatedBuffer;
        GetStackWalkInfo getStackWalkInfo;

        // Move to IDisposable for freeing
        List<IntPtr> allocatedRegions = [];

        readCallback = (address, pBuffer, bufferSize) =>
        {
            Span<byte> span = new Span<byte>(pBuffer, bufferSize);
            target.ReadBuffer(address, span);
            return 0;
        };
        getAllocatedBuffer = (bufferSize, ppBuffer) =>
        {
            *ppBuffer = NativeMemory.Alloc((nuint)bufferSize);
            IntPtr pBuffer = new(*ppBuffer);
            //Console.WriteLine($"Allocating buffer at {pBuffer:x16}");
            allocatedRegions.Add(pBuffer);
            return 0;
        };
        getStackWalkInfo = (controlPC, pModuleBase, pFuncEntry) =>
        {
            IExecutionManager eman = target.Contracts.ExecutionManager;

            if ((nuint)pModuleBase != 0) *(nuint*)pModuleBase = 0;
            if ((nuint)pFuncEntry != 0) *(nuint*)pFuncEntry = 0;

            try
            {
                if (eman.GetCodeBlockHandle(controlPC) is CodeBlockHandle cbh)
                {
                    TargetPointer methodDesc = eman.GetMethodDesc(cbh);
                    TargetPointer moduleBase = eman.GetModuleBaseAddress(cbh);
                    TargetPointer unwindInfo = eman.GetUnwindInfo(cbh, controlPC);
                    if ((nuint)pModuleBase != 0) *(nuint*)pModuleBase = (nuint)moduleBase.Value;
                    if ((nuint)pFuncEntry != 0) *(nuint*)pFuncEntry = (nuint)unwindInfo.Value;
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"GetStackWalkInfo failed: {ex}");
            }
        };

        int ret = ARM64Unwind(ref context, readCallback, getAllocatedBuffer, getStackWalkInfo);

        foreach (IntPtr ptr in allocatedRegions)
        {
            //Console.WriteLine($"Freeing buffer at {ptr:x16}");
            NativeMemory.Free(ptr.ToPointer());
        }

        return ret;
    }
}
