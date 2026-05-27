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
        delegate* unmanaged<ulong, byte*, uint, void*, int> writeToTarget,
        delegate* unmanaged<uint, uint, uint, byte*, void*, int> readThreadContext,
        delegate* unmanaged<uint, ulong*, void*, int> allocVirtual,
        void* delegateContext,
        IntPtr* handle)
    {
        // Build the allocVirtual delegate if the caller provided a callback
        ContractDescriptorTarget.AllocVirtualDelegate allocDelegate = (ulong size, out ulong allocatedAddress) =>
        {
            allocatedAddress = 0;
            return HResults.E_NOTIMPL;
        };

        if (allocVirtual != null)
        {
            allocDelegate = (ulong size, out ulong allocatedAddress) =>
            {
                if (size > uint.MaxValue)
                {
                    allocatedAddress = 0;
                    return HResults.E_INVALIDARG;
                }

                fixed (ulong* addrPtr = &allocatedAddress)
                {
                    return allocVirtual((uint)size, addrPtr, delegateContext);
                }
            };
        }

        // TODO: [cdac] Better error code/details
        if (!ContractDescriptorTarget.TryCreate(
            descriptor,
            (address, buffer) =>
            {
                fixed (byte* bufferPtr = buffer)
                {
                    return readFromTarget(address, bufferPtr, (uint)buffer.Length, delegateContext);
                }
            },
            (address, buffer) =>
            {
                fixed (byte* bufferPtr = buffer)
                {
                    return writeToTarget(address, bufferPtr, (uint)buffer.Length, delegateContext);
                }
            },
            (threadId, contextFlags, buffer) =>
            {
                fixed (byte* bufferPtr = buffer)
                {
                    return readThreadContext(threadId, contextFlags, (uint)buffer.Length, bufferPtr, delegateContext);
                }
            },
            allocDelegate,
            [Contracts.CoreCLRContracts.Register],
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
        Target? target = GCHandle.FromIntPtr(handle).Target as Target;
        if (target == null)
            return -1;

        object? legacyImpl = legacyImplPtr != IntPtr.Zero
            ? ComInterfaceMarshaller<ISOSDacInterface>.ConvertToManaged((void*)legacyImplPtr)
            : null;
        Legacy.SOSDacImpl impl = new(target, legacyImpl);
        nint ptr = (nint)ComInterfaceMarshaller<ISOSDacInterface>.ConvertToUnmanaged(impl);
        *obj = ptr;
        return 0;
    }

    /// <summary>
    /// Create the DacDbi interface implementation.
    /// </summary>
    /// <param name="handle">Handle created via cdac initialization</param>
    /// <param name="legacyImplPtr">Optional. Pointer to legacy implementation of IDacDbiInterface</param>
    /// <param name="obj"><c>IUnknown</c> pointer that can be queried for IDacDbiInterface</param>
    [UnmanagedCallersOnly(EntryPoint = $"{CDAC}create_dacdbi_interface")]
    private static unsafe int CreateDacDbiInterface(IntPtr handle, IntPtr legacyImplPtr, nint* obj)
    {
        if (obj == null)
            return HResults.E_INVALIDARG;
        if (handle == IntPtr.Zero)
        {
            *obj = IntPtr.Zero;
            return HResults.E_NOTIMPL;
        }

        Target? target = GCHandle.FromIntPtr(handle).Target as Target;
        if (target is null)
        {
            *obj = IntPtr.Zero;
            return HResults.E_INVALIDARG;
        }

        object? legacyObj = null;
        if (legacyImplPtr != IntPtr.Zero)
        {
            legacyObj = ComInterfaceMarshaller<IDacDbiInterface>.ConvertToManaged((void*)legacyImplPtr);
            if (legacyObj is not Legacy.IDacDbiInterface)
            {
                *obj = IntPtr.Zero;
                return HResults.COR_E_INVALIDCAST; // E_NOINTERFACE
            }
        }

        Legacy.DacDbiImpl impl = new(target, legacyObj);
        *obj = (nint)ComInterfaceMarshaller<IDacDbiInterface>.ConvertToUnmanaged(impl);
        return HResults.S_OK;
    }

    [UnmanagedCallersOnly(EntryPoint = "CLRDataCreateInstanceWithFallback")]
    private static unsafe int CLRDataCreateInstanceWithFallback(Guid* pIID, IntPtr /*ICLRDataTarget*/ pLegacyTarget, IntPtr pLegacyImpl, void** iface)
    {
        return CLRDataCreateInstanceImpl(pIID, pLegacyTarget, pLegacyImpl, iface);
    }

    // Same export name and signature as DAC CLRDataCreateInstance in daccess.cpp
    [UnmanagedCallersOnly(EntryPoint = "CLRDataCreateInstance")]
    private static unsafe int CLRDataCreateInstance(Guid* pIID, IntPtr /*ICLRDataTarget*/ pLegacyTarget, void** iface)
    {
        return CLRDataCreateInstanceImpl(pIID, pLegacyTarget, IntPtr.Zero, iface);
    }

    private static unsafe int CLRDataCreateInstanceImpl(Guid* pIID, IntPtr /*ICLRDataTarget*/ pLegacyTarget, IntPtr pLegacyImpl, void** iface)
    {
        if (pLegacyTarget == IntPtr.Zero || iface == null)
            return HResults.E_INVALIDARG;
        *iface = null;

        object legacyTarget = ComInterfaceMarshaller<ICLRDataTarget>.ConvertToManaged((void*)pLegacyTarget)!;
        object? legacyImpl = pLegacyImpl != IntPtr.Zero ?
            ComInterfaceMarshaller<ISOSDacInterface>.ConvertToManaged((void*)pLegacyImpl) : null;

        ICLRDataTarget dataTarget = legacyTarget as ICLRDataTarget ?? throw new ArgumentException(
            $"{nameof(pLegacyTarget)} does not implement {nameof(ICLRDataTarget)}", nameof(pLegacyTarget));
        ICLRContractLocator contractLocator = legacyTarget as ICLRContractLocator ?? throw new ArgumentException(
            $"{nameof(pLegacyTarget)} does not implement {nameof(ICLRContractLocator)}", nameof(pLegacyTarget));

        // Try to get ICLRDataTarget2 for memory allocation support (optional)
        ICLRDataTarget2? dataTarget2 = legacyTarget as ICLRDataTarget2;

        ulong contractAddress;
        int hr = contractLocator.GetContractDescriptor(&contractAddress);
        if (hr != 0)
        {
            throw new InvalidOperationException(
                $"{nameof(ICLRContractLocator)} failed to fetch the contract descriptor with HRESULT: 0x{hr:x}.");
        }

        // Build the allocVirtual delegate if the target supports ICLRDataTarget2
        ContractDescriptorTarget.AllocVirtualDelegate allocVirtual = (ulong size, out ulong allocatedAddress) =>
        {
            allocatedAddress = 0;
            return HResults.E_NOTIMPL;
        };

        if (dataTarget2 is not null)
        {
            // Windows virtual memory allocation flags used by ICLRDataTarget2::AllocVirtual.
            const uint MEM_COMMIT = 0x1000;
            const uint PAGE_READWRITE = 0x04;

            allocVirtual = (ulong size, out ulong allocatedAddress) =>
            {
                ClrDataAddress addr;
                int result = dataTarget2.AllocVirtual(0, (uint)size, MEM_COMMIT, PAGE_READWRITE, &addr);
                allocatedAddress = (ulong)addr;
                return result;
            };
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
            (address, buffer) =>
            {
                fixed (byte* bufferPtr = buffer)
                {
                    uint bytesWritten;
                    return dataTarget.WriteVirtual(address, bufferPtr, (uint)buffer.Length, &bytesWritten);
                }
            },
            (threadId, contextFlags, bufferToFill) =>
            {
                fixed (byte* bufferPtr = bufferToFill)
                {
                    return dataTarget.GetThreadContext(threadId, contextFlags, (uint)bufferToFill.Length, bufferPtr);
                }
            },
            allocVirtual,
            [Contracts.CoreCLRContracts.Register],
            out ContractDescriptorTarget? target))
        {
            return -1;
        }

        Legacy.SOSDacImpl impl = new(target, legacyImpl);
        void* ccw = ComInterfaceMarshaller<IXCLRDataProcess>.ConvertToUnmanaged(impl);
        Marshal.QueryInterface((nint)ccw, *pIID, out nint ptrToIface);
        *iface = (void*)ptrToIface;

        // Decrement reference count on ccw because QI incremented it
        ComInterfaceMarshaller<IXCLRDataProcess>.Free(ccw);

        return 0;
    }
}
