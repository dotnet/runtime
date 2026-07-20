// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// IGCInfo implementation for x86. x86 uses the legacy bit-packed InfoHdr GC info format,
/// which is fundamentally different from the GcInfoDecoder format used by every other
/// architecture, so it cannot share <see cref="GCInfo_1{TTraits}"/>.
/// The decoder lives at <see cref="GCInfoHelpers.X86.X86GCInfo"/> and is shared with the
/// x86 stack walker.
/// </summary>
internal sealed class GCInfoX86_1 : IGCInfo
{
    private readonly Target _target;

    internal GCInfoX86_1(Target target)
    {
        _target = target;
    }

    IGCInfoHandle IGCInfo.DecodePlatformSpecificGCInfo(TargetPointer gcInfoAddress, uint gcVersion)
        => new GCInfoHelpers.X86.X86GCInfo(_target, gcInfoAddress, gcVersion);

    IGCInfoHandle IGCInfo.DecodeInterpreterGCInfo(TargetPointer gcInfoAddress, uint gcVersion)
        => new GcInfoDecoder<InterpreterGCInfoTraits>(_target, gcInfoAddress, gcVersion);

    uint IGCInfo.GetCodeLength(IGCInfoHandle gcInfoHandle)
        => AssertCorrectHandle(gcInfoHandle).GetCodeLength();

    uint IGCInfo.GetStackBaseRegister(IGCInfoHandle gcInfoHandle)
        => AssertCorrectHandle(gcInfoHandle).GetStackBaseRegister();

    uint IGCInfo.GetSizeOfStackParameterArea(IGCInfoHandle gcInfoHandle)
        => AssertCorrectHandle(gcInfoHandle).GetSizeOfStackParameterArea();

    uint IGCInfo.GetCalleePoppedArgumentsSize(IGCInfoHandle gcInfoHandle)
        => AssertCorrectHandle(gcInfoHandle).GetCalleePoppedArgumentsSize();

    IReadOnlyList<InterruptibleRange> IGCInfo.GetInterruptibleRanges(IGCInfoHandle gcInfoHandle)
        => AssertCorrectHandle(gcInfoHandle).GetInterruptibleRanges();

    IReadOnlyList<LiveSlot> IGCInfo.EnumerateLiveSlots(IGCInfoHandle gcInfoHandle, uint instructionOffset, GcSlotEnumerationOptions options)
        => AssertCorrectHandle(gcInfoHandle).EnumerateLiveSlots(instructionOffset, options);

    bool IGCInfo.IsGcSafe(IGCInfoHandle gcInfoHandle, uint instructionOffset)
        => AssertCorrectHandle(gcInfoHandle).IsGcSafe(instructionOffset);

    private static IGCInfoDecoder AssertCorrectHandle(IGCInfoHandle gcInfoHandle)
    {
        if (gcInfoHandle is not IGCInfoDecoder handle)
            throw new System.ArgumentException("Invalid GC info handle", nameof(gcInfoHandle));

        return handle;
    }
}
