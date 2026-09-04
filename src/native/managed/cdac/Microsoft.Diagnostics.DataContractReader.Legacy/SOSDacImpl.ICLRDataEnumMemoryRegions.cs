// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using System.Threading;
namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Implementation of ICLRDataEnumMemoryRegions interface intended to be passed out to consumers
/// interacting with the DAC via those COM interfaces.
/// </summary>
public sealed unsafe partial class SOSDacImpl : ICLRDataEnumMemoryRegions
{
    int ICLRDataEnumMemoryRegions.EnumMemoryRegions(void* callback, uint miniDumpFlags, int clrFlags)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return LegacyFallbackHelper.CanFallback() && _legacyEnumMemory is not null ? _legacyEnumMemory.EnumMemoryRegions(callback, miniDumpFlags, clrFlags) : HResults.E_NOTIMPL;
    }
}
