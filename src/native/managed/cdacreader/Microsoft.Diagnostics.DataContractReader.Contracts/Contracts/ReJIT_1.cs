// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly partial struct ReJIT_1 : IReJIT
{
    internal readonly Target _target;
    private readonly Data.ProfControlBlock _profControlBlock;

    public ReJIT_1(Target target, Data.ProfControlBlock profControlBlock)
    {
        _target = target;
        _profControlBlock = profControlBlock;
    }

    bool IReJIT.IsEnabled()
    {
        bool profEnabledReJIT = (_profControlBlock.GlobalEventMask & (ulong)Legacy.COR_PRF_MONITOR.COR_PRF_ENABLE_REJIT) != 0;
        // FIXME: it is very likely this is always true in the DAC
        // Most people don't set DOTNET_ProfAPI_RejitOnAttach = 0
        // See https://github.com/dotnet/runtime/issues/106148
        bool clrConfigEnabledReJIT = true;
        return profEnabledReJIT || clrConfigEnabledReJIT;
    }
}
