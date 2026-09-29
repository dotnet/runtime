// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.Legacy.EnumMemory;

namespace Microsoft.Diagnostics.DataContractReader;

internal static class Entrypoints
{
    [UnmanagedCallersOnly(EntryPoint = "CLRDataCreateInstance")]
    private static unsafe int CLRDataCreateInstance(Guid* pIID, IntPtr pDataTarget, void** iface)
    {
        if (iface == null)
            return HResults.E_INVALIDARG;

        *iface = null;
        if (pIID == null || pDataTarget == IntPtr.Zero)
            return HResults.E_INVALIDARG;

        try
        {
            ICLRDataTarget dataTarget = ComInterfaceMarshaller<ICLRDataTarget>.ConvertToManaged((void*)pDataTarget)!;

            if (!EntrypointHelpers.TryGetRuntimeImageBase(dataTarget, out TargetPointer runtimeImageBase))
                return HResults.E_FAIL;

            // WER does not provide ICLRContractLocator, so locate the descriptor through the runtime's PE exports if necessary.
            EntrypointHelpers.TryGetContractDescriptorAddress(dataTarget, out TargetPointer contractAddress);
            if (contractAddress == TargetPointer.Null)
            {
                RuntimeModuleInfo.TryReadMemory readMemory = (address, buffer) => TryReadTarget(dataTarget, address, buffer);
                if (!RuntimeModuleInfo.TryCreate(runtimeImageBase, readMemory, out RuntimeModuleInfo module)
                    || !module.TryGetExport(readMemory, "DotNetRuntimeContractDescriptor"u8, out ulong exportAddress))
                {
                    return CdacHResults.CDAC_E_DESCRIPTOR_NOT_FOUND;
                }
                contractAddress = new(exportAddress);
            }

            return EntrypointHelpers.CreateInstance(pIID, pDataTarget, IntPtr.Zero, iface, contractAddress, runtimeImageBase);
        }
        catch (Exception ex)
        {
            int hr = ex.HResult;
            return hr < 0 ? hr : HResults.E_FAIL;
        }
    }

    private static unsafe bool TryReadTarget(ICLRDataTarget dataTarget, ulong address, Span<byte> buffer)
    {
        fixed (byte* bufferPointer = buffer)
        {
            uint bytesRead;
            return dataTarget.ReadVirtual(address, bufferPointer, (uint)buffer.Length, &bytesRead) >= 0
                && bytesRead == (uint)buffer.Length;
        }
    }
}
