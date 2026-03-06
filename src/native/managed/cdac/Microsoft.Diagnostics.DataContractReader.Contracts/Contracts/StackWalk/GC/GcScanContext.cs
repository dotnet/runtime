// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Data;
using static Microsoft.Diagnostics.DataContractReader.Contracts.StackWalk_1;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal class GcScanContext
{


    private readonly Target _target;
    public bool ResolveInteriorPointers { get; }
    public List<StackRefData> StackRefs { get; } = [];
    public TargetPointer StackPointer { get; private set; }
    public TargetPointer InstructionPointer { get; private set; }
    public TargetPointer Frame { get; private set; }

    public GcScanContext(Target target, bool resolveInteriorPointers)
    {
        _target = target;
        ResolveInteriorPointers = resolveInteriorPointers;
    }

    public void UpdateScanContext(TargetPointer sp, TargetPointer ip, TargetPointer frame)
    {
        StackPointer = sp;
        InstructionPointer = ip;
        Frame = frame;
    }

    public void GCEnumCallback(TargetPointer pObject, GcScanFlags flags, GcScanSlotLocation loc)
    {
        // Yuck.  The GcInfoDecoder reports a local pointer for registers (as it's reading out of the REGDISPLAY
        // in the stack walk), and it reports a TADDR for stack locations.  This is architecturally difficulty
        // to fix, so we are leaving it for now.
        TargetPointer addr;
        TargetPointer obj;

        if (loc.TargetPtr)
        {
            addr = pObject;
            obj = _target.ReadPointer(addr);
        }
        else
        {
            addr = 0;
            obj = pObject;
        }

        if (flags.HasFlag(GcScanFlags.GC_CALL_INTERIOR) && ResolveInteriorPointers)
        {
            // TODO(stackref): handle interior pointers
            throw new NotImplementedException();
        }

        StackRefData data = new()
        {
            HasRegisterInformation = true,
            Register = loc.Reg,
            Offset = loc.RegOffset,
            Address = addr,
            Object = obj,
            Flags = flags,
            StackPointer = StackPointer,
        };

        if (Frame != TargetPointer.Null)
        {
            data.SourceType = StackRefData.SourceTypes.StackSourceFrame;
            data.Source = Frame;
        }
        else
        {
            data.SourceType = StackRefData.SourceTypes.StackSourceIP;
            data.Source = InstructionPointer;
        }

        StackRefs.Add(data);
    }

    public void GCReportCallback(TargetPointer ppObj, GcScanFlags flags)
    {
        if (flags.HasFlag(GcScanFlags.GC_CALL_INTERIOR) && ResolveInteriorPointers)
        {
            // TODO(stackref): handle interior pointers
            throw new NotImplementedException();
        }

        StackRefData data = new()
        {
            HasRegisterInformation = false,
            Register = 0,
            Offset = 0,
            Address = ppObj,
            Object = TargetPointer.Null,
            Flags = flags,
            StackPointer = StackPointer,
        };

        if (Frame != TargetPointer.Null)
        {
            data.SourceType = StackRefData.SourceTypes.StackSourceFrame;
            data.Source = Frame;
        }
        else
        {
            data.SourceType = StackRefData.SourceTypes.StackSourceIP;
            data.Source = InstructionPointer;
        }

        StackRefs.Add(data);
    }
}
