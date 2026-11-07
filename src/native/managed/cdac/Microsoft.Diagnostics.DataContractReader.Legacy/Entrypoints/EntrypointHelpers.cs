// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Provides shared activation helpers for native cDAC entrypoints.
/// </summary>
public static unsafe class EntrypointHelpers
{
    /// <summary>
    /// Creates a cDAC instance exposing the requested COM interface.
    /// </summary>
    /// <param name="pIID">A pointer to the requested interface identifier.</param>
    /// <param name="pDataTarget">An <see cref="ICLRDataTarget"/> interface pointer.</param>
    /// <param name="legacyImplPtr">An optional legacy <see cref="ISOSDacInterface"/> pointer, or zero.</param>
    /// <param name="iface">When this method returns, contains the requested interface pointer on success, or null on failure. The caller must release a successful result.</param>
    /// <param name="contractAddress">The nonzero target contract descriptor address.</param>
    /// <param name="runtimeImageBase">The runtime image base address, or <see langword="default"/> when unavailable.</param>
    /// <returns>An HRESULT indicating whether activation succeeded.</returns>
    public static int CreateInstance(Guid* pIID, IntPtr pDataTarget, IntPtr legacyImplPtr, void** iface, ulong contractAddress, TargetPointer runtimeImageBase = default)
    {
        if (iface == null)
            return HResults.E_INVALIDARG;

        *iface = null;
        if (pIID == null || pDataTarget == IntPtr.Zero || contractAddress == 0)
            return HResults.E_INVALIDARG;

        try
        {
            ICLRDataTarget dataTarget = ComInterfaceMarshaller<ICLRDataTarget>.ConvertToManaged((void*)pDataTarget)!;
            ContractDescriptorTarget target = CreateTarget(dataTarget, contractAddress, runtimeImageBase);
            return CreateSosInterface(target, legacyImplPtr, *pIID, iface);
        }
        catch (System.Exception ex)
        {
            int hr = ex.HResult;
            return hr < 0 ? hr : HResults.E_FAIL;
        }
    }

    /// <summary>
    /// Locates the runtime image through the runtime locator, falling back to the data target's module lookup.
    /// </summary>
    /// <param name="dataTarget">The data target used to locate the runtime module.</param>
    /// <param name="address">When this method returns successfully, contains the runtime image base; otherwise, contains zero.</param>
    /// <returns><see langword="true"/> if a nonzero runtime image base was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetRuntimeImageBase(ICLRDataTarget dataTarget, out TargetPointer address)
    {
        const string RuntimeModuleName = "coreclr.dll";

        address = TargetPointer.Null;
        ulong imageBase = 0;
        int hr = HResults.S_OK;
        if (dataTarget is not ICLRRuntimeLocator runtimeLocator || runtimeLocator.GetRuntimeBase(&imageBase) != HResults.S_OK)
        {
            imageBase = 0;
            hr = dataTarget.GetImageBase(RuntimeModuleName, &imageBase);
        }
        if (hr != HResults.S_OK || imageBase == 0)
            return false;

        address = new(imageBase);
        return true;
    }

    /// <summary>
    /// Finds the contract descriptor through the data target's contract locator.
    /// </summary>
    /// <param name="dataTarget">The data target queried for <see cref="ICLRContractLocator"/>.</param>
    /// <param name="address">When this method returns, contains the descriptor address on success, or zero on failure.</param>
    /// <returns><see langword="true"/> if the locator returns a nonzero descriptor address; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetContractDescriptorAddress(ICLRDataTarget dataTarget, out TargetPointer address)
    {
        address = TargetPointer.Null;
        ulong contractAddress = 0;
        if (dataTarget is not ICLRContractLocator contractLocator
            || contractLocator.GetContractDescriptor(&contractAddress) != HResults.S_OK
            || contractAddress == 0)
        {
            return false;
        }

        address = new(contractAddress);
        return true;
    }

    private static ContractDescriptorTarget CreateTarget(ICLRDataTarget dataTarget, ulong contractAddress, TargetPointer runtimeImageBase)
    {
        ContractDescriptorTarget.AllocVirtualDelegate allocVirtual = (ulong size, out ulong allocatedAddress) =>
        {
            allocatedAddress = 0;
            return HResults.E_NOTIMPL;
        };

        if (dataTarget is ICLRDataTarget2 dataTarget2)
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
                    int hr = dataTarget.ReadVirtual(address, bufferPointer, (uint)buffer.Length, &bytesRead);
                    return hr < 0 || bytesRead == (uint)buffer.Length ? hr : HResults.E_FAIL;
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
            dataTarget.GetThreadContext,
            (threadId, context) => SetThreadContext(dataTarget, threadId, context),
            allocVirtual,
            [CoreCLRContracts.Register],
            runtimeImageBase);

        EnableMetadataRecovery(target, dataTarget);
        return target;
    }

    internal static void EnableMetadataRecovery(ContractDescriptorTarget target, object dataTarget)
    {
        MetadataMemoryCache.GetMetadataDelegate? callback = CreateMetadataCallback(dataTarget);
        if (callback is not null)
            target.EnableMetadataRecovery(callback);
    }

    internal static MetadataMemoryCache.GetMetadataDelegate? CreateMetadataCallback(object dataTarget)
    {
        ICLRMetadataLocator? clrLocator = dataTarget as ICLRMetadataLocator;
        IXCLRDataTarget3? ixclrLocator = dataTarget as IXCLRDataTarget3;
        ICorDebugMetaDataLocator? corDebugLocator = dataTarget as ICorDebugMetaDataLocator;
        if (clrLocator is null && ixclrLocator is null && corDebugLocator is null)
            return null;

        return (string path, uint timestamp, uint imageSize, uint rva, Span<byte> buffer, out uint dataSize) =>
        {
            dataSize = 0;
            int hr = HResults.E_NOTIMPL;
            fixed (char* pathPointer = path)
            fixed (byte* bufferPointer = buffer)
            fixed (uint* sizePointer = &dataSize)
            {
                if (clrLocator is not null)
                    hr = clrLocator.GetMetadata(pathPointer, timestamp, imageSize, null, rva, 0,
                        (uint)buffer.Length, bufferPointer, sizePointer);

                if (hr < 0 && ixclrLocator is not null)
                {
                    dataSize = 0;
                    hr = ixclrLocator.GetMetaData(pathPointer, timestamp, imageSize, null, rva, 0,
                        (uint)buffer.Length, bufferPointer, sizePointer);
                }
            }

            if (hr < 0 && corDebugLocator is not null)
                hr = ReadLocatedMetadata(corDebugLocator, path, timestamp, imageSize, buffer, out dataSize);

            return hr;
        };
    }

    private static int ReadLocatedMetadata(ICorDebugMetaDataLocator locator, string path, uint timestamp,
        uint imageSize, Span<byte> buffer, out uint dataSize)
    {
        const int InitialPathBufferSize = 260;
        dataSize = 0;
        char[] pathBuffer = new char[InitialPathBufferSize];
        uint pathLength = 0;
        int hr;
        fixed (char* pathPointer = path)
        {
            fixed (char* bufferPointer = pathBuffer)
            {
                hr = locator.GetMetaData(pathPointer, timestamp, imageSize,
                    (uint)pathBuffer.Length, &pathLength, bufferPointer);
            }

            if (hr < 0 && pathLength >= pathBuffer.Length)
            {
                // Some hosts report the character count without the terminator.
                pathBuffer = new char[checked((int)pathLength + 1)];
                pathLength = 0;
                fixed (char* bufferPointer = pathBuffer)
                {
                    hr = locator.GetMetaData(pathPointer, timestamp, imageSize,
                        (uint)pathBuffer.Length, &pathLength, bufferPointer);
                }
            }
        }

        if (hr < 0)
            return hr;
        if (pathLength == 0 || pathLength > pathBuffer.Length)
            return HResults.E_FAIL;

        int terminator = pathBuffer.AsSpan(0, checked((int)pathLength)).IndexOf('\0');
        if (terminator < 0 && pathLength < pathBuffer.Length && pathBuffer[pathLength] == '\0')
            terminator = (int)pathLength;
        if (terminator <= 0)
            return HResults.E_FAIL;

        try
        {
            using FileStream stream = File.OpenRead(new string(pathBuffer, 0, terminator));
            using PEReader peReader = new(stream);
            if (!peReader.HasMetadata)
                return HResults.E_FAIL;

            PEMemoryBlock metadata = peReader.GetMetadata();
            dataSize = (uint)metadata.Length;
            if (metadata.Length > buffer.Length)
                return CorDbgHResults.ERROR_INSUFFICIENT_BUFFER;

            metadata.GetContent().AsSpan().CopyTo(buffer);
            return HResults.S_OK;
        }
        catch (IOException ex)
        {
            return ex.HResult;
        }
        catch (BadImageFormatException ex)
        {
            return ex.HResult;
        }
        catch (UnauthorizedAccessException ex)
        {
            return ex.HResult;
        }
    }

    private static int SetThreadContext(ICLRDataTarget dataTarget, uint threadId, ReadOnlySpan<byte> context)
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
    }

    private static int CreateSosInterface(ContractDescriptorTarget target, IntPtr legacyImplPtr, Guid iid, void** iface)
    {
        Lock apiLock = new();
        CoreCLRContracts.ValidateForDataAccess(target, apiLock);
        object? legacyImpl = legacyImplPtr != IntPtr.Zero
            ? ComInterfaceMarshaller<ISOSDacInterface>.ConvertToManaged((void*)legacyImplPtr)
            : null;

        SOSDacImpl impl = new(target, legacyImpl, apiLock);
        void* ccw = ComInterfaceMarshaller<IXCLRDataProcess>.ConvertToUnmanaged(impl);
        try
        {
            int hr = Marshal.QueryInterface((nint)ccw, iid, out nint requestedInterface);
            if (hr < 0)
                return hr;

            *iface = (void*)requestedInterface;
            return HResults.S_OK;
        }
        finally
        {
            ComInterfaceMarshaller<IXCLRDataProcess>.Free(ccw);
        }
    }
}
