// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly partial struct ReJIT_1 : IReJIT
{
    internal readonly Target _target;
    private readonly Data.ProfControlBlock _profControlBlock;

    // see src/coreclr/inc/corprof.idl
    [Flags]
    private enum COR_PRF_MONITOR
    {
        COR_PRF_ENABLE_REJIT = 0x00040000,
    }

    // see src/coreclr/vm/codeversion.h
    [Flags]
    public enum RejitFlags : uint
    {
        kStateRequested = 0x00000000,

        kStateGettingReJITParameters = 0x00000001,

        kStateActive = 0x00000002,

        kStateMask = 0x0000000F,

        kSuppressParams = 0x80000000
    }

    public ReJIT_1(Target target, Data.ProfControlBlock profControlBlock)
    {
        _target = target;
        _profControlBlock = profControlBlock;
    }

    bool IReJIT.IsEnabled()
    {
        bool profEnabledReJIT = (_profControlBlock.GlobalEventMask & (ulong)COR_PRF_MONITOR.COR_PRF_ENABLE_REJIT) != 0;
        // FIXME: it is very likely this is always true in the DAC
        // Most people don't set DOTNET_ProfAPI_RejitOnAttach = 0
        // See https://github.com/dotnet/runtime/issues/106148
        bool clrConfigEnabledReJIT = true;
        return profEnabledReJIT || clrConfigEnabledReJIT;
    }

    RejitState IReJIT.GetRejitState(ILCodeVersionHandle ilCodeVersionHandle)
    {
        if (!ilCodeVersionHandle.IsExplicit)
        {
            // for non explicit ILCodeVersions, ReJITState is always kStateActive
            return RejitState.Active;
        }
        ILCodeVersionNode ilCodeVersionNode = AsNode(ilCodeVersionHandle);
        return ((RejitFlags)ilCodeVersionNode.RejitState & RejitFlags.kStateMask) switch
        {
            RejitFlags.kStateRequested => RejitState.Requested,
            RejitFlags.kStateActive => RejitState.Active,
            _ => throw new InvalidOperationException($"Unknown ReJIT state: {ilCodeVersionNode.RejitState}"),
        };
    }

    TargetNUInt IReJIT.GetRejitId(ILCodeVersionHandle ilCodeVersionHandle)
    {
        if (ilCodeVersionHandle.ILCodeVersionNode == TargetPointer.Null)
        {
            // for non explicit ILCodeVersions, ReJITId is always 0
            return new TargetNUInt(0);
        }
        ILCodeVersionNode ilCodeVersionNode = AsNode(ilCodeVersionHandle);
        return ilCodeVersionNode.VersionId;
    }

    private ILCodeVersionNode AsNode(ILCodeVersionHandle ilCodeVersionHandle)
    {
        if (ilCodeVersionHandle.ILCodeVersionNode == TargetPointer.Null)
        {
            throw new InvalidOperationException("Synthetic ILCodeVersion does not have a backing node.");
        }

        return _target.ProcessedData.GetOrAdd<ILCodeVersionNode>(ilCodeVersionHandle.ILCodeVersionNode);
    }
}
