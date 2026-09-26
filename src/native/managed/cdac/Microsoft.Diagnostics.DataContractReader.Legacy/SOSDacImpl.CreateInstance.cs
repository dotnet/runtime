// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

public sealed unsafe partial class SOSDacImpl
{
    public static int CreateInstance(
        Guid* pIID,
        object legacyTarget,
        ulong contractAddress,
        IntPtr legacyImplPtr,
        void** iface)
    {
        ICLRDataTarget dataTarget = legacyTarget as ICLRDataTarget ?? throw new ArgumentException(
            $"Data target does not implement {nameof(ICLRDataTarget)}", nameof(legacyTarget));
        ICLRDataTarget2? dataTarget2 = legacyTarget as ICLRDataTarget2;

        ContractDescriptorTarget.AllocVirtualDelegate allocVirtual = (ulong size, out ulong allocatedAddress) =>
        {
            allocatedAddress = 0;
            return HResults.E_NOTIMPL;
        };

        if (dataTarget2 is not null)
        {
            const uint MemCommit = 0x1000;
            const uint PageReadWrite = 0x04;

            allocVirtual = (ulong size, out ulong allocatedAddress) =>
            {
                ClrDataAddress address;
                int result = dataTarget2.AllocVirtual(0, (uint)size, MemCommit, PageReadWrite, &address);
                allocatedAddress = address.Value;
                return result;
            };
        }

        ContractDescriptorTarget target = ContractDescriptorTarget.Create(
            contractAddress,
            (address, buffer) =>
            {
                fixed (byte* bufferPointer = buffer)
                {
                    uint bytesRead;
                    return dataTarget.ReadVirtual(address, bufferPointer, (uint)buffer.Length, &bytesRead);
                }
            },
            (address, buffer) =>
            {
                fixed (byte* bufferPointer = buffer)
                {
                    uint bytesWritten;
                    return dataTarget.WriteVirtual(address, bufferPointer, (uint)buffer.Length, &bytesWritten);
                }
            },
            (threadId, contextFlags, buffer) =>
            {
                fixed (byte* bufferPointer = buffer)
                {
                    return dataTarget.GetThreadContext(threadId, contextFlags, (uint)buffer.Length, bufferPointer);
                }
            },
            (threadId, context) =>
            {
                const nuint ContextAlignment = 16;

                fixed (byte* contextPointer = context)
                {
                    if (((nuint)contextPointer & (ContextAlignment - 1)) == 0)
                        return dataTarget.SetThreadContext(threadId, (uint)context.Length, contextPointer);

                    byte* alignedBuffer = (byte*)NativeMemory.AlignedAlloc((nuint)context.Length, ContextAlignment);
                    try
                    {
                        context.CopyTo(new Span<byte>(alignedBuffer, context.Length));
                        return dataTarget.SetThreadContext(threadId, (uint)context.Length, alignedBuffer);
                    }
                    finally
                    {
                        NativeMemory.AlignedFree(alignedBuffer);
                    }
                }
            },
            allocVirtual,
            [CoreCLRContracts.Register]);

        Lock apiLock = new();
        CoreCLRContracts.ValidateForDataAccess(target, apiLock);

        object? legacyImpl = legacyImplPtr != IntPtr.Zero
            ? ComInterfaceMarshaller<ISOSDacInterface>.ConvertToManaged((void*)legacyImplPtr)
            : null;

        SOSDacImpl impl = new(target, legacyImpl, apiLock);
        void* ccw = ComInterfaceMarshaller<IXCLRDataProcess>.ConvertToUnmanaged(impl);
        int hr = Marshal.QueryInterface((nint)ccw, *pIID, out nint requestedInterface);
        ComInterfaceMarshaller<IXCLRDataProcess>.Free(ccw);
        if (hr < 0)
            return hr;

        *iface = (void*)requestedInterface;
        return HResults.S_OK;
    }
}
