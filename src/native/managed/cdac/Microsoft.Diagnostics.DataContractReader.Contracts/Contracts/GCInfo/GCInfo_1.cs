// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal class GCInfo_1<TTraits> : IGCInfo where TTraits : IGCInfoTraits
{
    private readonly Target _target;

    internal GCInfo_1(Target target)
    {
        _target = target;
    }

    IGCInfoHandle IGCInfo.DecodePlatformSpecificGCInfo(TargetPointer gcInfoAddress, uint gcVersion)
        => new GcInfoDecoder<TTraits>(_target, gcInfoAddress, gcVersion);

    IGCInfoHandle IGCInfo.DecodeInterpreterGCInfo(TargetPointer gcInfoAddress, uint gcVersion)
        => new GcInfoDecoder<InterpreterGCInfoTraits>(_target, gcInfoAddress, gcVersion);

    uint IGCInfo.GetCodeLength(IGCInfoHandle gcInfoHandle)
    {
        IGCInfoDecoder handle = AssertCorrectHandle(gcInfoHandle);
        return handle.GetCodeLength();
    }

    uint IGCInfo.GetStackBaseRegister(IGCInfoHandle gcInfoHandle)
    {
        IGCInfoDecoder handle = AssertCorrectHandle(gcInfoHandle);
        return handle.GetStackBaseRegister();
    }

    IReadOnlyList<InterruptibleRange> IGCInfo.GetInterruptibleRanges(IGCInfoHandle gcInfoHandle)
    {
        IGCInfoDecoder handle = AssertCorrectHandle(gcInfoHandle);
        return handle.GetInterruptibleRanges();
    }

    IReadOnlyList<LiveSlot> IGCInfo.EnumerateLiveSlots(IGCInfoHandle gcInfoHandle, uint instructionOffset, GcSlotEnumerationOptions options)
    {
        IGCInfoDecoder handle = AssertCorrectHandle(gcInfoHandle);
        return handle.EnumerateLiveSlots(instructionOffset, options);
    }

    private static IGCInfoDecoder AssertCorrectHandle(IGCInfoHandle gcInfoHandle)
    {
        if (gcInfoHandle is not IGCInfoDecoder handle)
            throw new ArgumentException("Invalid GC info handle", nameof(gcInfoHandle));

        return handle;
    }
}
