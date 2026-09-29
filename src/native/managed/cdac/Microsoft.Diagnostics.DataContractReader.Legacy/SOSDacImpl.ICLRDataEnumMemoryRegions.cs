// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Diagnostics.DataContractReader.Legacy.EnumMemory;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Implementation of ICLRDataEnumMemoryRegions interface intended to be passed out to consumers
/// interacting with the DAC via those COM interfaces.
/// </summary>
public sealed unsafe partial class SOSDacImpl : ICLRDataEnumMemoryRegions
{
    int ICLRDataEnumMemoryRegions.EnumMemoryRegions(void* callback, uint miniDumpFlags, CLRDataEnumMemoryFlags clrFlags)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        if (callback is null)
            return HResults.E_INVALIDARG;

        try
        {
            // Like the native DAC, ignore the reserved clrFlags argument.
            return MemoryRegionEnumerator.Enumerate(_target, callback, miniDumpFlags);
        }
        catch (Exception ex)
        {
            int hr = ex.HResult;
            return hr < 0 ? hr : HResults.E_FAIL;
        }
    }
}
