// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal class GcScanContext
{

    private readonly Target _target;
    private readonly IGC _gc;
    private readonly IRuntimeTypeSystem _rts;

    private readonly LinearReadCache _cache;
    private readonly uint _numComponentsOffsetArray;
    private readonly uint _numComponentsOffsetString;
    private readonly ulong _methodTableOffset;
    private readonly byte _objectToMethodTableUnmask;
    public bool ResolveInteriorPointers { get; }
    public List<StackRefData> StackRefs { get; } = [];
    public TargetPointer StackPointer { get; private set; }
    public TargetCodePointer InstructionPointer { get; private set; }
    public TargetPointer Frame { get; private set; }

    // When set, overrides the default IP/Frame source-type classification for reported roots.
    // Used to mark roots reported outside the frame walk (GCFrame/GCPROTECT and ExInfo chains)
    // as StackSourceOther, since their Source is a node address rather than a capital-F Frame.
    private StackRefData.SourceTypes? _sourceTypeOverride;

    public GcScanContext(Target target, bool resolveInteriorPointers)
    {
        _target = target;
        ResolveInteriorPointers = resolveInteriorPointers;
        _gc = target.Contracts.GC;
        _rts = target.Contracts.RuntimeTypeSystem;
        _cache = new LinearReadCache(target);
        _numComponentsOffsetArray = (uint)target.GetTypeInfo(DataType.Array).Fields[Constants.FieldNames.Array.NumComponents].Offset;
        _numComponentsOffsetString = (uint)target.GetTypeInfo(DataType.String).Fields["m_StringLength"].Offset;
        _methodTableOffset = (ulong)target.GetTypeInfo(DataType.Object).Fields["m_pMethTab"].Offset;
        _objectToMethodTableUnmask = target.ReadGlobal<byte>(Constants.Globals.ObjectToMethodTableUnmask);
    }

    public void UpdateScanContext(TargetPointer sp, TargetCodePointer ip, TargetPointer frame, StackRefData.SourceTypes? sourceTypeOverride = null)
    {
        StackPointer = sp;
        InstructionPointer = ip;
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
            data.Source = CodePointerUtils.AddressFromCodePointer(InstructionPointer, _target);
        }
    }

    public void RecordDeferredFrame(TargetPointer frameAddress)
    {
        StackRefs.Add(new StackRefData
        {
            HasRegisterInformation = false,
            IsInteriorPointer = false,
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
            TargetPointer interiorObj = GetInteriorPointer(obj);
            if (interiorObj == TargetPointer.Null)
                return;
            obj = interiorObj;
        }

        StackRefData data = new()
        {
            HasRegisterInformation = true,
            IsInteriorPointer = flags.HasFlag(GcScanFlags.GC_CALL_INTERIOR),
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

    private TargetPointer GetInteriorPointer(TargetPointer obj)
    {
        TargetPointer outerObj = TargetPointer.Null;
        foreach ((GCHeapSegmentInfo seg, GCHeapData _) in _gc.EnumerateAllSegments())
        {
            if (obj.Value < seg.Start.Value || obj.Value >= seg.End.Value)
                continue;

            TargetPointer currentObj = _gc.GetPotentialNextObjectAddress(seg.Start, 0, seg);
            ulong size = 0;
            while (currentObj.Value <= obj.Value)
            {
                // Replicate IObject.GetMethodTableAddress in fast path with linear read cache
                if (!_cache.TryReadPointer(currentObj.Value + _methodTableOffset, out TargetPointer mt))
                {
                    return TargetPointer.Null;
                }
                mt = mt.Value & (ulong)~_objectToMethodTableUnmask;

                // Replicate IObject.GetSize in fast path with linear read cache
                if (!TryGetObjectSize(currentObj, mt, out size) || size == 0)
                {
                    return TargetPointer.Null;
                }

                size = _gc.AlignObjectSize(size, seg.Generation);
                if (currentObj.Value + size > seg.End.Value || size == 0)
                {
                    return TargetPointer.Null;
                }
                outerObj = currentObj;
                currentObj = _gc.GetPotentialNextObjectAddress(currentObj, size, seg);
            }
            return outerObj + size > obj ? outerObj : TargetPointer.Null;
        }
        return outerObj;
    }

    private bool TryGetObjectSize(TargetPointer objAddr, TargetPointer mt, out ulong size)
    {
        size = 0;
        try
        {
            ITypeHandle handle = _rts.GetTypeHandle(mt);
            ulong baseSize = _rts.GetBaseSize(handle);
            uint componentSize = _rts.GetComponentSize(handle);
            uint numComponentsOffset = 0;
            if (componentSize != 0)
            {
                if (_rts.IsArray(handle, out _) || _rts.IsFreeObjectMethodTable(handle))
                    numComponentsOffset = _numComponentsOffsetArray;
                else if (_rts.IsString(handle))
                    numComponentsOffset = _numComponentsOffsetString;
                else
                    return false; // unrecognized component type
                if (!_cache.TryReadUInt32(objAddr.Value + numComponentsOffset, out uint numComponents))
                    return false;
                baseSize += (ulong)componentSize * numComponents;
            }
            size = baseSize;
            return true;
        }
        catch
        {
            // The MT may be corrupt — surface as a read failure.
            return false;
        }
    }

    public void GCReportCallback(TargetPointer ppObj, GcScanFlags flags)
    {
        // Read the object pointer from the stack slot.
        TargetPointer obj = _target.ReadPointer(ppObj);
        if (flags.HasFlag(GcScanFlags.GC_CALL_INTERIOR) && ResolveInteriorPointers)
        {
            TargetPointer interiorObj = GetInteriorPointer(obj);
            if (interiorObj != TargetPointer.Null)
                obj = interiorObj;
        }

        StackRefData data = new()
        {
            HasRegisterInformation = false,
            IsInteriorPointer = flags.HasFlag(GcScanFlags.GC_CALL_INTERIOR),
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
