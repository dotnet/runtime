// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal class GcScanContext
{

    private readonly Target _target;
    private readonly bool _isArm32;
    public bool ResolveInteriorPointers { get; }
    public List<StackRefData> StackRefs { get; } = [];
    public TargetPointer StackPointer { get; private set; }
    public TargetPointer InstructionPointer { get; private set; }
    public TargetPointer Frame { get; private set; }

    // When set, overrides the default IP/Frame source-type classification for reported roots.
    // Used to mark roots reported outside the frame walk (GCFrame/GCPROTECT and ExInfo chains)
    // as StackSourceOther, since their Source is a node address rather than a capital-F Frame.
    private StackRefData.SourceTypes? _sourceTypeOverride;

    public GcScanContext(Target target, bool resolveInteriorPointers)
    {
        _target = target;
        _isArm32 = target.Contracts.RuntimeInfo.GetTargetArchitecture() == RuntimeInfoArchitecture.Arm;
        ResolveInteriorPointers = resolveInteriorPointers;
    }

    public void UpdateScanContext(TargetPointer sp, TargetPointer ip, TargetPointer frame, StackRefData.SourceTypes? sourceTypeOverride = null)
    {
        StackPointer = sp;
        // On ARM32 the control PC carries the Thumb bit (LSB) to indicate execution mode.
        // The native runtime applies PCODEToPINSTR (utilcode.h) before reporting the IP as
        // a StackRefData.Source so consumers compare data addresses, not PCODE values. Mirror
        // that here: without this mask, every cDAC ref on arm32 would be keyed at IP|1 while
        // the runtime reports at IP, producing universal mismatches in GC root verification.
        InstructionPointer = _isArm32 ? new TargetPointer(ip.Value & ~1ul) : ip;
        Frame = frame;
        _sourceTypeOverride = sourceTypeOverride;
    }

    private void SetSource(StackRefData data)
    {
        if (_sourceTypeOverride is StackRefData.SourceTypes sourceType)
        {
            data.SourceType = sourceType;
            data.Source = Frame;
        }
        else if (Frame != TargetPointer.Null)
        {
            data.SourceType = StackRefData.SourceTypes.StackSourceFrame;
            data.Source = Frame;
        }
        else
        {
            data.SourceType = StackRefData.SourceTypes.StackSourceIP;
            data.Source = InstructionPointer;
        }
    }

    public void RecordDeferredFrame(TargetPointer frameAddress)
    {
        StackRefs.Add(new StackRefData
        {
            HasRegisterInformation = false,
            Register = 0,
            Offset = 0,
            Address = 0,
            Object = 0,
            Flags = GcScanFlags.CDAC_DEFERRED_FRAME,
            SourceType = StackRefData.SourceTypes.StackSourceFrame,
            Source = frameAddress,
            StackPointer = StackPointer,
        });
    }

    public void GCEnumCallback(TargetPointer pObject, GcScanFlags flags, GcScanSlotLocation loc)
    {
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
            // https://github.com/dotnet/runtime/issues/125728
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

        SetSource(data);

        StackRefs.Add(data);
    }

    public void GCReportCallback(TargetPointer ppObj, GcScanFlags flags)
    {
        if (flags.HasFlag(GcScanFlags.GC_CALL_INTERIOR) && ResolveInteriorPointers)
        {
            // TODO(stackref): handle interior pointers
            // https://github.com/dotnet/runtime/issues/125728
            throw new NotImplementedException();
        }

        // Read the object pointer from the stack slot.
        TargetPointer obj = _target.ReadPointer(ppObj);

        StackRefData data = new()
        {
            HasRegisterInformation = false,
            Register = 0,
            Offset = 0,
            Address = ppObj,
            Object = obj,
            Flags = flags,
            StackPointer = StackPointer,
        };

        SetSource(data);

        StackRefs.Add(data);
    }
}
