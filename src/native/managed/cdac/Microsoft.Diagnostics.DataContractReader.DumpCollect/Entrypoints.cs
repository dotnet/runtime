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

        *iface = null;
        if (*pIID != IClrDataEnumMemoryRegions)
            return HResults.COR_E_INVALIDCAST;

        try
        {
            ICLRDataTarget dataTarget = ComInterfaceMarshaller<ICLRDataTarget>.ConvertToManaged((void*)pDataTarget)!;
            if (!RuntimeModuleInfo.TryCreate(dataTarget, out RuntimeModuleInfo runtimeModule))
                return HResults.E_FAIL;

            ulong contractAddress;
            if (dataTarget is ICLRContractLocator contractLocator)
            {
                if (contractLocator.GetContractDescriptor(&contractAddress) < 0)
                    contractAddress = 0;
            }
            else if (!runtimeModule.TryGetExport(
                RuntimeModuleInfo.ContractDescriptorSymbolName,
                out contractAddress))
            {
                contractAddress = 0;
            }

            if (contractAddress == 0)
                return HResults.E_FAIL;

            var enumerator = new MemoryRegionEnumerator(dataTarget, contractAddress, runtimeModule);
            *iface = ComInterfaceMarshaller<ICLRDataEnumMemoryRegions>.ConvertToUnmanaged(enumerator);
            return 0;
        }
        catch (Exception ex)
        {
            int hr = ex.HResult;
            return hr < 0 ? hr : HResults.E_FAIL;
        }
    }
}
