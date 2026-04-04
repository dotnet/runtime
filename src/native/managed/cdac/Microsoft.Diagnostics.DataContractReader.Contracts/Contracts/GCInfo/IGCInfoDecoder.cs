// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;

/// <summary>
/// Flags controlling GC reference reporting behavior.
/// These match the native ICodeManager flags in eetwain.h.
/// </summary>
[Flags]
internal enum CodeManagerFlags : uint
{
    ActiveStackFrame = 0x1,
    ExecutionAborted = 0x2,
    ParentOfFuncletStackFrame = 0x40,
    NoReportUntracked = 0x80,
    ReportFPBasedSlotsOnly = 0x200,
}

internal interface IGCInfoDecoder : IGCInfoHandle
{
    uint GetCodeLength();
    uint StackBaseRegister { get; }

    /// <summary>
    /// Enumerates all live GC slots at the given instruction offset.
    /// </summary>
    /// <param name="instructionOffset">Relative offset from method start.</param>
    /// <param name="flags">CodeManagerFlags controlling reporting.</param>
    /// <param name="reportSlot">Callback: (isRegister, registerNumber, spOffset, spBase, gcFlags).</param>
    bool EnumerateLiveSlots(
        uint instructionOffset,
        CodeManagerFlags flags,
        LiveSlotCallback reportSlot);
}

internal delegate void LiveSlotCallback(bool isRegister, uint registerNumber, int spOffset, uint spBase, uint gcFlags);
