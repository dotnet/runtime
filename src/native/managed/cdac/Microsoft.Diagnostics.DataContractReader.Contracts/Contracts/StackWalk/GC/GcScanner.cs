// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal class GcScanner
{
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

        // Lazily compute the caller SP for GC_CALLER_SP_REL slots.
        // The native code uses GET_CALLER_SP(pRD) which comes from EnsureCallerContextIsValid.
        TargetPointer? callerSP = null;

        return decoder.EnumerateLiveSlots(
            (uint)relativeOffset.Value,
            flags,
            (bool isRegister, uint registerNumber, int spOffset, uint spBase, uint gcFlags) =>
            {
                GcScanFlags scanFlags = GcScanFlags.None;
                if ((gcFlags & 0x1) != 0) // GC_SLOT_INTERIOR
                    scanFlags |= GcScanFlags.GC_CALL_INTERIOR;
                if ((gcFlags & 0x2) != 0) // GC_SLOT_PINNED
                    scanFlags |= GcScanFlags.GC_CALL_PINNED;

                if (isRegister)
                {
                    TargetPointer regValue = ReadRegisterValue(context, (int)registerNumber);
                    GcScanSlotLocation loc = new((int)registerNumber, 0, false);
                    scanContext.GCEnumCallback(regValue, scanFlags, loc);
                }
                else
                {
                    int spReg = context.StackPointerRegister;
                    int reg = spBase switch
                    {
                        1 => spReg,                     // GC_SP_REL → SP register number
                        2 => (int)stackBaseRegister,     // GC_FRAMEREG_REL → frame base register
                        0 => -(spReg + 1),               // GC_CALLER_SP_REL → -(SP + 1)
                        _ => throw new InvalidOperationException($"Unknown stack slot base: {spBase}"),
                    };
                    TargetPointer baseAddr = spBase switch
                    {
                        1 => context.StackPointer,                                  // GC_SP_REL
                        2 => ReadRegisterValue(context, (int)stackBaseRegister),     // GC_FRAMEREG_REL
                        0 => GetCallerSP(context, ref callerSP),                    // GC_CALLER_SP_REL
                        _ => throw new InvalidOperationException($"Unknown stack slot base: {spBase}"),
                    };

                    TargetPointer addr = new(baseAddr.Value + (ulong)(long)spOffset);
                    GcScanSlotLocation loc = new(reg, spOffset, true);
                    scanContext.GCEnumCallback(addr, scanFlags, loc);
                }
            });
    }

    /// <summary>
    /// Compute the caller's SP by unwinding the current context one frame.
    /// Cached in <paramref name="cached"/> to avoid repeated unwinds for the same frame.
    /// </summary>
    private TargetPointer GetCallerSP(IPlatformAgnosticContext context, ref TargetPointer? cached)
    {
        if (cached is null)
        {
            IPlatformAgnosticContext callerContext = context.Clone();
            callerContext.Unwind(_target);
            cached = callerContext.StackPointer;
        }
        return cached.Value;
    }

    private static TargetPointer ReadRegisterValue(IPlatformAgnosticContext context, int registerNumber)
    {
        if (!context.TryReadRegister(registerNumber, out TargetNUInt value))
            throw new ArgumentOutOfRangeException(nameof(registerNumber), $"Register number {registerNumber} not found");

        return new TargetPointer(value.Value);
    }

}
