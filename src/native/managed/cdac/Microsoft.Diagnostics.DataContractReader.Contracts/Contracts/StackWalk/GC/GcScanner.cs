// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal class GcScanner
{
    public enum CodeManagerFlags : uint
    {
        ActiveStackFrame = 0x1,
        ExecutionAborted = 0x2,
        ParentOfFuncletStackFrame = 0x40,
        NoReportUntracked = 0x80,
        ReportFPBasedSlotsOnly = 0x200,
    }

    private readonly Target _target;
    private readonly IExecutionManager _eman;
    private readonly IGCInfo _gcInfo;

    internal GcScanner(Target target)
    {
        _target = target;
        _eman = target.Contracts.ExecutionManager;
        _gcInfo = target.Contracts.GCInfo;
    }

    public bool EnumGcRefs(
        IPlatformAgnosticContext context,
        CodeBlockHandle cbh,
        CodeManagerFlags flags,
        GcScanContext scanContext)
    {
        TargetNUInt relativeOffset = _eman.GetRelativeOffset(cbh);
        _eman.GetGCInfo(cbh, out TargetPointer gcInfoAddr, out uint gcVersion);

        if (_eman.IsFilterFunclet(cbh))
            flags |= CodeManagerFlags.NoReportUntracked;

        IGCInfoHandle handle = _gcInfo.DecodePlatformSpecificGCInfo(gcInfoAddr, gcVersion);
        if (handle is not IGCInfoDecoder decoder)
            return false;

        uint stackBaseRegister = decoder.StackBaseRegister;

        return decoder.EnumerateLiveSlots(
            (uint)relativeOffset.Value,
            (uint)flags,
            (bool isRegister, uint registerNumber, int spOffset, uint spBase, uint gcFlags) =>
            {
                GcScanFlags scanFlags = GcScanFlags.None;
                if ((gcFlags & 0x1) != 0) // GC_SLOT_INTERIOR
                    scanFlags |= GcScanFlags.GC_CALL_INTERIOR;
                if ((gcFlags & 0x2) != 0) // GC_SLOT_PINNED
                    scanFlags |= GcScanFlags.GC_CALL_PINNED;

                if (isRegister)
                {
                    TargetPointer regValue = GetRegisterValue(context, registerNumber);
                    GcScanSlotLocation loc = new((int)registerNumber, 0, false);
                    scanContext.GCEnumCallback(regValue, scanFlags, loc);
                }
                else
                {
                    TargetPointer baseAddr = spBase switch
                    {
                        1 => context.StackPointer,                                  // GC_SP_REL
                        2 => GetRegisterValue(context, stackBaseRegister),           // GC_FRAMEREG_REL
                        0 => context.StackPointer,                                  // GC_CALLER_SP_REL (TODO: use actual caller SP)
                        _ => throw new InvalidOperationException($"Unknown stack slot base: {spBase}"),
                    };

                    TargetPointer addr = new(baseAddr.Value + (ulong)(long)spOffset);
                    int regForBase = spBase switch
                    {
                        1 => 4,                          // GC_SP_REL → RSP (reg 4 on AMD64)
                        2 => (int)stackBaseRegister,      // GC_FRAMEREG_REL → stack base register (e.g., RBP=5)
                        0 => 4,                          // GC_CALLER_SP_REL → RSP
                        _ => 0,
                    };
                    GcScanSlotLocation loc = new(regForBase, spOffset, true);
                    scanContext.GCEnumCallback(addr, scanFlags, loc);
                }
            });
    }

    private static TargetPointer GetRegisterValue(IPlatformAgnosticContext context, uint registerNumber)
    {
        if (registerNumber == 4) return context.StackPointer;
        if (registerNumber == 5) return context.FramePointer;

        // Map register number to context field name (AMD64 ordering)
        // TODO: Support ARM64 and other architectures
        string? fieldName = registerNumber switch
        {
            0 => "Rax", 1 => "Rcx", 2 => "Rdx", 3 => "Rbx",
            6 => "Rsi", 7 => "Rdi",
            8 => "R8", 9 => "R9", 10 => "R10", 11 => "R11",
            12 => "R12", 13 => "R13", 14 => "R14", 15 => "R15",
            _ => null,
        };

        if (fieldName is not null && context.TryReadRegister(null!, fieldName, out TargetNUInt value))
            return new TargetPointer(value.Value);

        throw new InvalidOperationException($"Failed to read register #{registerNumber} from context");
    }
}
