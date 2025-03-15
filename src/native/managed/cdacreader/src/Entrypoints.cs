// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Decoder;
using Microsoft.Diagnostics.DataContractReader.Legacy;

namespace Microsoft.Diagnostics.DataContractReader;

internal static class Entrypoints
{
    private const string CDAC = "cdac_reader_";

    static Entrypoints()
    {
        StreamWriter logFileWriter = new StreamWriter("C:\\Users\\maxcharlamb\\OneDrive - Microsoft\\Desktop\\out.txt", append: true);
        Console.SetOut(logFileWriter);
        Console.WriteLine("Creating cDAC entrypoints");
        logFileWriter.AutoFlush = true;
        logFileWriter.Flush();
    }

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

    [UnmanagedCallersOnly(EntryPoint = $"{CDAC}create_instance")]
    private static unsafe int CLRDataCreateInstance(Guid* pIID, IntPtr /*ICLRDataTarget*/ pLegacyTarget, void** iface)
    {
        if (pLegacyTarget == IntPtr.Zero || iface == null)
            return HResults.E_INVALIDARG;

        *iface = null;

        ComWrappers cw = new StrategyBasedComWrappers();
        object obj = cw.GetOrCreateObjectForComInstance(pLegacyTarget, CreateObjectFlags.None);
        ICLRDataTarget dataTarget = obj as ICLRDataTarget ?? throw new ArgumentException("Invalid ICLRDataTarget");
        ICLRRuntimeLocator? locator = obj as ICLRRuntimeLocator;

        ulong baseAddress;
        if (locator is ICLRRuntimeLocator loc)
        {
            locator.GetRuntimeBase(&baseAddress);
        }
        else
        {
            dataTarget.GetImageBase("coreclr.dll", &baseAddress);
        }


        using PEDecoder peDecoder = new(new DataTargetStream(dataTarget, baseAddress));
        using ELFDecoder elfDecoder = new(new DataTargetStream(dataTarget, baseAddress), baseAddress);
        using MachODecoder machODecoder = new(new DataTargetStream(dataTarget, baseAddress), baseAddress);

        Console.WriteLine($"PE: {peDecoder.IsValid} ELF: {elfDecoder.IsValid} MachO: {machODecoder.IsValid}");

        Target.CorDebugPlatform targetPlatform;
        ulong contractDescriptor;
        if (peDecoder.IsValid)
        {
            targetPlatform = Target.CorDebugPlatform.CORDB_PLATFORM_WINDOWS_AMD64;
            if (!peDecoder.TryGetRelativeSymbolAddress("DotNetRuntimeContractDescriptor", out contractDescriptor))
            {
                return -1;
            }
        }
        else if (elfDecoder.IsValid)
        {
            targetPlatform = Target.CorDebugPlatform.CORDB_PLATFORM_POSIX_ARM64;
            if (!elfDecoder.TryGetRelativeSymbolAddress("DotNetRuntimeContractDescriptor", out contractDescriptor))
            {
                return -1;
            }
        }
        else if (machODecoder.IsValid)
        {
            targetPlatform = Target.CorDebugPlatform.CORDB_PLATFORM_POSIX_ARM64;
            if (!machODecoder.TryGetRelativeSymbolAddress("DotNetRuntimeContractDescriptor", out contractDescriptor))
            {
                return -1;
            }
        }
        else
        {
            return -1;
        }

        if (!ContractDescriptorTarget.TryCreate(
            baseAddress + contractDescriptor,
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
                platform = (int)targetPlatform;
                return 0;
            },
            out ContractDescriptorTarget? target))
        {
            return -1;
        }

        Legacy.SOSDacImpl impl = new(target, null);
        nint ccw = cw.GetOrCreateComInterfaceForObject(impl, CreateComInterfaceFlags.None);
        Marshal.QueryInterface(ccw, *pIID, out nint ptrToIface);
        *iface = (void*)ptrToIface;

        // Decrement reference count on ccw because QI increments it?
        Marshal.Release(ccw);

        return 0;
    }
}
