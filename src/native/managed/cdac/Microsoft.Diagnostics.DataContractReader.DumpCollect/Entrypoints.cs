// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Legacy;

namespace Microsoft.Diagnostics.DataContractReader.DumpCollect;

internal static class Entrypoints
{
    private static readonly Guid IClrDataEnumMemoryRegions = new("471c35b4-7c2f-4ef0-a945-00f8c38056f1");

    [UnmanagedCallersOnly(EntryPoint = "CLRDataCreateInstance")]
    private static unsafe int CLRDataCreateInstance(Guid* pIID, IntPtr pDataTarget, void** iface)
    {
        if (pIID == null || pDataTarget == IntPtr.Zero || iface == null)
            return HResults.E_INVALIDARG;

        DumpCollectLogger.Log($"CLRDataCreateInstance requested interface {*pIID}.");
        *iface = null;
        if (*pIID != IClrDataEnumMemoryRegions)
            return HResults.COR_E_INVALIDCAST;

        try
        {
            ICLRDataTarget dataTarget = ComInterfaceMarshaller<ICLRDataTarget>.ConvertToManaged((void*)pDataTarget)!;
            if (!ContractDescriptorLocator.TryGetFromPE(dataTarget, out ulong contractAddress))
            {
                DumpCollectLogger.Log("Failed to locate DotNetRuntimeContractDescriptor.");
                return HResults.E_FAIL;
            }

            var enumerator = new MemoryRegionEnumerator(dataTarget, contractAddress);
            *iface = ComInterfaceMarshaller<ICLRDataEnumMemoryRegionsRaw>.ConvertToUnmanaged(enumerator);
            DumpCollectLogger.Log($"Created memory-region enumerator for contract descriptor 0x{contractAddress:x}.");
            return 0;
        }
        catch (Exception ex)
        {
            DumpCollectLogger.LogException(nameof(CLRDataCreateInstance), ex);
            int hr = ex.HResult;
            return hr < 0 ? hr : HResults.E_FAIL;
        }
    }
}
