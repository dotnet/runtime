// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Legacy;

namespace Microsoft.Diagnostics.DataContractReader;

internal static class Entrypoints
{
    private const string CDAC = "cdac_reader_";

    [UnmanagedCallersOnly(EntryPoint = $"{CDAC}init")]
    private static unsafe int Init(
        ulong descriptor,
        delegate* unmanaged<ulong, byte*, uint, void*, int> readFromTarget,
        delegate* unmanaged<uint, uint, uint, byte*, void*, int> readThreadContext,
        delegate* unmanaged<int*, void*, int> getPlatform,
        void* readContext,
        IntPtr* handle)
    {
        // TODO: [cdac] Better error code/details
        if (!ContractDescriptorTarget.TryCreate(
            descriptor,
            (address, buffer) =>
            {
                fixed (byte* bufferPtr = buffer)
                {
                    return readFromTarget(address, bufferPtr, (uint)buffer.Length, readContext);
                }
            },
            (threadId, contextFlags, contextSize, buffer) =>
            {
                fixed (byte* bufferPtr = buffer)
                {
                    return readThreadContext(threadId, contextFlags, contextSize, bufferPtr, readContext);
                }
            },
            (out int platform) =>
            {
                fixed (int* platformPtr = &platform)
                {
                    return getPlatform(platformPtr, readContext);
                }
            },
            out ContractDescriptorTarget? target))
            return -1;

        GCHandle gcHandle = GCHandle.Alloc(target);
        *handle = GCHandle.ToIntPtr(gcHandle);
        return 0;
    }

    [UnmanagedCallersOnly(EntryPoint = $"{CDAC}free")]
    private static unsafe int Free(IntPtr handle)
    {
        GCHandle h = GCHandle.FromIntPtr(handle);
        h.Free();
        return 0;
    }

    /// <summary>
    /// Create the SOS-DAC interface implementation.
    /// </summary>
    /// <param name="handle">Handle crated via cdac initialization</param>
    /// <param name="legacyImplPtr">Optional. Pointer to legacy implementation of ISOSDacInterface*</param>
    /// <param name="obj"><c>IUnknown</c> pointer that can be queried for ISOSDacInterface*</param>
    /// <returns></returns>
    [UnmanagedCallersOnly(EntryPoint = $"{CDAC}create_sos_interface")]
    private static unsafe int CreateSosInterface(IntPtr handle, IntPtr legacyImplPtr, nint* obj)
    {
        ComWrappers cw = new StrategyBasedComWrappers();
        Target? target = GCHandle.FromIntPtr(handle).Target as Target;
        if (target == null)
            return -1;

        object? legacyImpl = legacyImplPtr != IntPtr.Zero
            ? cw.GetOrCreateObjectForComInstance(legacyImplPtr, CreateObjectFlags.None)
            : null;
        Legacy.SOSDacImpl impl = new(target, legacyImpl);
        nint ptr = cw.GetOrCreateComInterfaceForObject(impl, CreateComInterfaceFlags.None);
        *obj = ptr;
        return 0;
    }

    [UnmanagedCallersOnly(EntryPoint = "CLRDataCreateInstance")]
    private static unsafe int CLRDataCreateInstance(Guid* pIID, IntPtr /*ICLRDataTarget*/ pLegacyTarget, void** iface)
    {
        if (pLegacyTarget == IntPtr.Zero || iface == null)
            return HResults.E_INVALIDARG;

        *iface = null;

        ComWrappers cw = new StrategyBasedComWrappers();
        object obj = cw.GetOrCreateObjectForComInstance(pLegacyTarget, CreateObjectFlags.None);
        ICLRDataTarget dataTarget = obj as ICLRDataTarget ?? throw new ArgumentException($"pLegacyTarget does not implement ${nameof(ICLRDataTarget)}", nameof(pLegacyTarget));
        ICLRContractLocator contractLocator = obj as ICLRContractLocator ?? throw new ArgumentException($"pLegacyTarget does not implement ${nameof(ICLRContractLocator)}", nameof(pLegacyTarget));

        ulong contractAddress;
        if (contractLocator.GetContractDescriptor(&contractAddress) != 0)
        {
            throw new InvalidOperationException("Unable to retrieve contract address from ICLRContractLocator");
        }

        if (!ContractDescriptorTarget.TryCreate(
            contractAddress,
            (address, buffer) =>
            {
                fixed (byte* bufferPtr = buffer)
                {
                    uint bytesRead;
                    return dataTarget.ReadVirtual(address, bufferPtr, (uint)buffer.Length, &bytesRead);
                }
            },
            (threadId, contextFlags, contextSize, bufferToFill) =>
            {
                fixed (byte* bufferPtr = bufferToFill)
                {
                    return dataTarget.GetThreadContext(threadId, contextFlags, contextSize, bufferPtr);
                }
            },
            (out platform) =>
            {
                platform = 0;
                uint machineType;
                int hr = dataTarget.GetMachineType(&machineType);
                switch (machineType)
                {
                    // ICLRDataTarget can not be used to find OS. For now labeling all platforms as Windows
                    //
                    case 0x014c: // IMAGE_FILE_MACHINE_I386
                        platform = (int)Target.CorDebugPlatform.CORDB_PLATFORM_WINDOWS_X86;
                        break;
                    case 0x8664: // IMAGE_FILE_MACHINE_AMD64
                        platform = (int)Target.CorDebugPlatform.CORDB_PLATFORM_WINDOWS_AMD64;
                        break;
                    case 0x01c4: // IMAGE_FILE_MACHINE_ARMNT
                        platform = (int)Target.CorDebugPlatform.CORDB_PLATFORM_WINDOWS_ARM;
                        break;
                    case 0xAA64: // IMAGE_FILE_MACHINE_ARM64
                        platform = (int)Target.CorDebugPlatform.CORDB_PLATFORM_WINDOWS_ARM64;
                        break;
                }
                return hr;
            },
            out ContractDescriptorTarget? target))
        {
            return -1;
        }

        Legacy.SOSDacImpl impl = new(target, null);
        nint ccw = cw.GetOrCreateComInterfaceForObject(impl, CreateComInterfaceFlags.None);
        Marshal.QueryInterface(ccw, *pIID, out nint ptrToIface);
        *iface = (void*)ptrToIface;

        // Decrement reference count on ccw because QI increments it
        Marshal.Release(ccw);

        return 0;
    }
}
