// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Implementation of ICLRDataEnumMemoryRegions interface intended to be passed out to consumers
/// interacting with the DAC via those COM interfaces.
/// </summary>
internal sealed unsafe partial class SOSDacImpl : ICLRDataEnumMemoryRegions
{
    int ICLRDataEnumMemoryRegions.EnumMemoryRegions(void* callback, uint miniDumpFlags, int clrFlags)
        => _legacyEnumMemory is not null ? _legacyEnumMemory.EnumMemoryRegions(callback, miniDumpFlags, clrFlags) : HResults.E_NOTIMPL;
}
