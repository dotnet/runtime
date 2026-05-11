// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IGCInfoHandle { }

/// <summary>
/// Describes a code region where the GC can safely interrupt execution.
/// </summary>
/// <param name="StartOffset">Start of the interruptible region, as a byte offset from the method start.</param>
/// <param name="EndOffset">End of the interruptible region (exclusive), as a byte offset from the method start.</param>
public readonly record struct InterruptibleRange(uint StartOffset, uint EndOffset);

/// <summary>
/// Describes a live GC slot at a given instruction offset.
/// </summary>
/// <param name="IsRegister">True if the slot is a CPU register; false if it is a stack location.</param>
/// <param name="RegisterNumber">Register number (meaningful only when IsRegister is true).</param>
/// <param name="SpOffset">Stack offset from the base (meaningful only when IsRegister is false).</param>
/// <param name="SpBase">Stack base: 0 = CALLER_SP_REL, 1 = SP_REL, 2 = FRAMEREG_REL.</param>
/// <param name="GcFlags">GC slot flags: 0x1 = interior pointer, 0x2 = pinned.</param>
public readonly record struct LiveSlot(bool IsRegister, uint RegisterNumber, int SpOffset, uint SpBase, uint GcFlags);

/// <summary>
/// Options controlling which GC slots are reported by <see cref="IGCInfo.EnumerateLiveSlots"/>.
/// </summary>
public record struct GcSlotEnumerationOptions
{
    /// <summary>True if this is the active (leaf) stack frame. When false, scratch register and stack slots are excluded.</summary>
    public bool IsActiveFrame { get; set; }
    /// <summary>True if execution was aborted (e.g., interrupted by exception). Skips live slot reporting at non-interruptible offsets.</summary>
    public bool IsExecutionAborted { get; set; }
    /// <summary>True if the frame is a parent of a funclet that already reported GC references.</summary>
    public bool IsParentOfFuncletStackFrame { get; set; }
    /// <summary>True to suppress reporting of untracked slots (e.g., for filter funclets).</summary>
    public bool SuppressUntrackedSlots { get; set; }
    /// <summary>True to report only frame-register-relative stack slots (skips all register slots and non-frame-relative stack slots).</summary>
    public bool ReportFPBasedSlotsOnly { get; set; }
}

public interface IGCInfo : IContract
{
    static string IContract.Name { get; } = nameof(GCInfo);

    IGCInfoHandle DecodePlatformSpecificGCInfo(TargetPointer gcInfoAddress, uint gcVersion) => throw new NotImplementedException();
    IGCInfoHandle DecodeInterpreterGCInfo(TargetPointer gcInfoAddress, uint gcVersion) => throw new NotImplementedException();

    uint GetCodeLength(IGCInfoHandle handle) => throw new NotImplementedException();
    uint GetStackBaseRegister(IGCInfoHandle handle) => throw new NotImplementedException();
    IReadOnlyList<InterruptibleRange> GetInterruptibleRanges(IGCInfoHandle handle) => throw new NotImplementedException();
    IReadOnlyList<LiveSlot> EnumerateLiveSlots(IGCInfoHandle handle, uint instructionOffset, GcSlotEnumerationOptions options) => throw new NotImplementedException();
}

public readonly struct GCInfo : IGCInfo
{
    // Everything throws NotImplementedException
}
