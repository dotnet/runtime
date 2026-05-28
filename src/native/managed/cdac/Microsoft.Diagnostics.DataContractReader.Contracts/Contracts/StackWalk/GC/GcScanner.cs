// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.SignatureHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Handles all GC reference scanning for stack frames.
/// Covers both managed (frameless) frames via GCInfo and
/// capital "F" Frames via GCRefMap/signature-based scanning.
/// </summary>
internal class GcScanner
{
    private readonly Target _target;
    private readonly IExecutionManager _eman;
    private readonly IGCInfo _gcInfo;
    private readonly FrameHelpers _frameHelpers;

    internal GcScanner(Target target)
    {
        _target = target;
        _eman = target.Contracts.ExecutionManager;
        _gcInfo = target.Contracts.GCInfo;
        _frameHelpers = new FrameHelpers(target);
    }

    /// <summary>
    /// Enumerates live GC slots for a managed (frameless) code frame.
    /// Port of native EECodeManager::EnumGcRefs (eetwain.cpp).
    /// </summary>
    public void EnumGcRefsForManagedFrame(
        IPlatformAgnosticContext context,
        CodeBlockHandle cbh,
        GcSlotEnumerationOptions options,
        GcScanContext scanContext,
        uint? relOffsetOverride = null)
    {
        TargetNUInt relativeOffset = _eman.GetRelativeOffset(cbh);
        _eman.GetGCInfo(cbh, out TargetPointer gcInfoAddr, out uint gcVersion);

        IGCInfoHandle handle = _gcInfo.DecodePlatformSpecificGCInfo(gcInfoAddr, gcVersion);

        uint stackBaseRegister = _gcInfo.GetStackBaseRegister(handle);
        TargetPointer? callerSP = null;
        uint offsetToUse = relOffsetOverride ?? (uint)relativeOffset.Value;

        IReadOnlyList<LiveSlot> liveSlots = _gcInfo.EnumerateLiveSlots(handle, offsetToUse, options);
        foreach (LiveSlot slot in liveSlots)
        {
            GcScanFlags scanFlags = GcScanFlags.None;
            if ((slot.GcFlags & 0x1) != 0)
                scanFlags |= GcScanFlags.GC_CALL_INTERIOR;
            if ((slot.GcFlags & 0x2) != 0)
                scanFlags |= GcScanFlags.GC_CALL_PINNED;

            if (slot.IsRegister)
            {
                if (!context.TryReadRegister((int)slot.RegisterNumber, out TargetNUInt regValue))
                    continue;
                GcScanSlotLocation loc = new((int)slot.RegisterNumber, 0, false);
                scanContext.GCEnumCallback(new TargetPointer(regValue.Value), scanFlags, loc);
            }
            else
            {
                int spReg = context.StackPointerRegister;
                int reg = slot.SpBase switch
                {
                    1 => spReg,
                    2 => (int)stackBaseRegister,
                    0 => -(spReg + 1),
                    _ => throw new InvalidOperationException($"Unknown stack slot base: {slot.SpBase}"),
                };
                TargetPointer baseAddr = slot.SpBase switch
                {
                    1 => context.StackPointer,
                    2 => context.TryReadRegister((int)stackBaseRegister, out TargetNUInt val)
                        ? new TargetPointer(val.Value)
                        : throw new InvalidOperationException($"Failed to read register {stackBaseRegister}"),
                    0 => GetCallerSP(context, ref callerSP),
                    _ => throw new InvalidOperationException($"Unknown stack slot base: {slot.SpBase}"),
                };

                TargetPointer addr = new(baseAddr.Value + (ulong)(long)slot.SpOffset);
                GcScanSlotLocation loc = new(reg, slot.SpOffset, true);
                scanContext.GCEnumCallback(addr, scanFlags, loc);
            }
        }
    }

    /// <summary>
    /// Scans GC roots for a capital "F" Frame based on its type.
    /// Port of native Frame::GcScanRoots (frames.cpp).
    /// </summary>
    public void GcScanRoots(TargetPointer frameAddress, GcScanContext scanContext)
    {
        if (frameAddress == TargetPointer.Null)
            return;

        Data.Frame frameData = _target.ProcessedData.GetOrAdd<Data.Frame>(frameAddress);
        FrameType frameType = _frameHelpers.GetFrameType(frameData.Identifier);

        switch (frameType)
        {
            case FrameType.StubDispatchFrame:
            {
                Data.FramedMethodFrame fmf = _target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frameAddress);
                Data.StubDispatchFrame sdf = _target.ProcessedData.GetOrAdd<Data.StubDispatchFrame>(frameAddress);

                TargetPointer gcRefMap = sdf.Indirection != TargetPointer.Null
                    ? FindGCRefMap(sdf.Indirection)
                    : TargetPointer.Null;

                if (gcRefMap != TargetPointer.Null)
                    PromoteCallerStackUsingGCRefMap(fmf.TransitionBlockPtr, gcRefMap, scanContext);
                else
                    PromoteCallerStack(frameAddress, fmf.TransitionBlockPtr, scanContext);
                break;
            }

            case FrameType.ExternalMethodFrame:
            {
                Data.FramedMethodFrame fmf = _target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frameAddress);
                Data.ExternalMethodFrame emf = _target.ProcessedData.GetOrAdd<Data.ExternalMethodFrame>(frameAddress);

                TargetPointer gcRefMap = emf.Indirection != TargetPointer.Null
                    ? FindGCRefMap(emf.Indirection)
                    : TargetPointer.Null;

                if (gcRefMap != TargetPointer.Null)
                    PromoteCallerStackUsingGCRefMap(fmf.TransitionBlockPtr, gcRefMap, scanContext);
                else
                    PromoteCallerStack(frameAddress, fmf.TransitionBlockPtr, scanContext);
                break;
            }

            case FrameType.DynamicHelperFrame:
            {
                Data.FramedMethodFrame fmf = _target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frameAddress);
                Data.DynamicHelperFrame dhf = _target.ProcessedData.GetOrAdd<Data.DynamicHelperFrame>(frameAddress);
                ScanDynamicHelperFrame(fmf.TransitionBlockPtr, dhf.DynamicHelperFrameFlags, scanContext);
                break;
            }

            case FrameType.CallCountingHelperFrame:
            case FrameType.PrestubMethodFrame:
            {
                Data.FramedMethodFrame fmf = _target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frameAddress);
                PromoteCallerStack(frameAddress, fmf.TransitionBlockPtr, scanContext);
                break;
            }

            case FrameType.HijackFrame:
                // TODO(stackref): Implement HijackFrame scanning (X86 only with FEATURE_HIJACK)
                break;

            case FrameType.ProtectValueClassFrame:
                // TODO(stackref): Implement ProtectValueClassFrame scanning
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Decodes a GCRefMap bitstream and reports GC references in the transition block.
    /// Port of native TransitionFrame::PromoteCallerStackUsingGCRefMap (frames.cpp).
    /// </summary>
    private void PromoteCallerStackUsingGCRefMap(
        TargetPointer transitionBlock,
        TargetPointer gcRefMapBlob,
        GcScanContext scanContext)
    {
        Data.TransitionBlock tb = _target.ProcessedData.GetOrAdd<Data.TransitionBlock>(transitionBlock);
        GCRefMapDecoder decoder = new(_target, gcRefMapBlob);

        if (_target.Contracts.RuntimeInfo.GetTargetArchitecture() is RuntimeInfoArchitecture.X86)
            decoder.ReadStackPop();

        while (!decoder.AtEnd)
        {
            int pos = decoder.CurrentPos;
            GCRefMapToken token = decoder.ReadToken();
            TargetPointer slotAddress = AddressFromGCRefMapPos(tb, pos);

            switch (token)
            {
                case GCRefMapToken.Skip:
                    break;
                case GCRefMapToken.Ref:
                    scanContext.GCReportCallback(slotAddress, GcScanFlags.None);
                    break;
                case GCRefMapToken.Interior:
                    scanContext.GCReportCallback(slotAddress, GcScanFlags.GC_CALL_INTERIOR);
                    break;
                case GCRefMapToken.MethodParam:
                case GCRefMapToken.TypeParam:
                    break;
                case GCRefMapToken.VASigCookie:
                    break;
            }
        }
    }

    /// <summary>
    /// Scans GC roots for a DynamicHelperFrame based on its flags.
    /// Port of native DynamicHelperFrame::GcScanRoots_Impl (frames.cpp).
    /// </summary>
    private void ScanDynamicHelperFrame(
        TargetPointer transitionBlock,
        int dynamicHelperFrameFlags,
        GcScanContext scanContext)
    {
        const int DynamicHelperFrameFlags_ObjectArg = 1;
        const int DynamicHelperFrameFlags_ObjectArg2 = 2;

        Data.TransitionBlock tb = _target.ProcessedData.GetOrAdd<Data.TransitionBlock>(transitionBlock);
        TargetPointer argRegStart = tb.ArgumentRegisters;

        if ((dynamicHelperFrameFlags & DynamicHelperFrameFlags_ObjectArg) != 0)
        {
            scanContext.GCReportCallback(argRegStart, GcScanFlags.None);
        }

        if ((dynamicHelperFrameFlags & DynamicHelperFrameFlags_ObjectArg2) != 0)
        {
            TargetPointer argAddr = new(argRegStart.Value + (uint)_target.PointerSize);
            scanContext.GCReportCallback(argAddr, GcScanFlags.None);
        }
    }

    /// <summary>
    /// Resolves the GCRefMap for a Frame with m_pIndirection.
    /// Port of native FindGCRefMap (frames.cpp).
    /// Always resolves the module via FindReadyToRunModule.
    /// </summary>
    private TargetPointer FindGCRefMap(TargetPointer indirection)
    {
        if (indirection == TargetPointer.Null)
            return TargetPointer.Null;

        TargetPointer zapModule = _eman.FindReadyToRunModule(indirection);
        if (zapModule == TargetPointer.Null)
            return TargetPointer.Null;

        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(zapModule);
        if (module.ReadyToRunInfo == TargetPointer.Null)
            return TargetPointer.Null;

        Data.ReadyToRunInfo r2rInfo = _target.ProcessedData.GetOrAdd<Data.ReadyToRunInfo>(module.ReadyToRunInfo);
        if (r2rInfo.ImportSections == TargetPointer.Null || r2rInfo.NumImportSections == 0)
            return TargetPointer.Null;

        ulong imageBase = r2rInfo.LoadedImageBase.Value;
        if (indirection.Value < imageBase)
            return TargetPointer.Null;
        ulong diff = indirection.Value - imageBase;
        if (diff > uint.MaxValue)
            return TargetPointer.Null;
        uint rva = (uint)diff;

        const int ImportSectionSize = 20;
        const int SectionVAOffset = 0;
        const int SectionSizeOffset = 4;
        const int EntrySizeOffset = 11;
        const int AuxiliaryDataOffset = 16;

        TargetPointer sectionsBase = r2rInfo.ImportSections;
        for (uint i = 0; i < r2rInfo.NumImportSections; i++)
        {
            TargetPointer sectionAddr = new(sectionsBase.Value + i * ImportSectionSize);
            uint sectionVA = _target.Read<uint>(sectionAddr + SectionVAOffset);
            uint sectionSize = _target.Read<uint>(sectionAddr + SectionSizeOffset);

            if (rva >= sectionVA && rva < sectionVA + sectionSize)
            {
                byte entrySize = _target.Read<byte>(sectionAddr + EntrySizeOffset);
                if (entrySize == 0)
                    return TargetPointer.Null;

                uint index = (rva - sectionVA) / entrySize;
                uint auxDataRva = _target.Read<uint>(sectionAddr + AuxiliaryDataOffset);
                if (auxDataRva == 0)
                    return TargetPointer.Null;

                TargetPointer gcRefMapBase = new(imageBase + auxDataRva);

                const uint GCREFMAP_LOOKUP_STRIDE = 1024;
                uint lookupIndex = index / GCREFMAP_LOOKUP_STRIDE;
                uint remaining = index % GCREFMAP_LOOKUP_STRIDE;

                uint lookupOffset = _target.Read<uint>(new TargetPointer(gcRefMapBase.Value + lookupIndex * 4));
                TargetPointer p = new(gcRefMapBase.Value + lookupOffset);

                while (remaining > 0)
                {
                    while ((_target.Read<byte>(p) & 0x80) != 0)
                        p = new(p.Value + 1);
                    p = new(p.Value + 1);
                    remaining--;
                }

                return p;
            }
        }

        return TargetPointer.Null;
    }

    /// <summary>
    /// Entry point for promoting caller stack GC references via method signature.
    /// Matches native TransitionFrame::PromoteCallerStack (frames.cpp:1494).
    /// </summary>
    private void PromoteCallerStack(
        TargetPointer frameAddress,
        TargetPointer transitionBlock,
        GcScanContext scanContext)
    {
        Data.FramedMethodFrame fmf = _target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frameAddress);
        TargetPointer methodDescPtr = fmf.MethodDescPtr;
        if (methodDescPtr == TargetPointer.Null)
            return;

        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        ICallingConvention cc = _target.Contracts.CallingConvention;
        MethodDescHandle mdh = rts.GetMethodDescHandle(methodDescPtr);

        foreach (ArgumentLocation arg in cc.EnumerateArguments(mdh))
        {
            TargetPointer slotAddress = transitionBlock + (ulong)arg.Offset;

            if (arg.IsParamType)
            {
                // Generic instantiation arg -- not a GC reference itself but may need pinning
                scanContext.GCReportCallback(slotAddress, GcScanFlags.GC_CALL_INTERIOR);
                continue;
            }

            if (arg.IsThis)
            {
                GcScanFlags thisFlags = arg.IsValueTypeThis ? GcScanFlags.GC_CALL_INTERIOR : GcScanFlags.None;
                scanContext.GCReportCallback(slotAddress, thisFlags);
                continue;
            }

            switch ((CorElementType)arg.ElementType)
            {
                case CorElementType.Class:
                case CorElementType.String:
                case CorElementType.Object:
                case CorElementType.Array:
                case CorElementType.SzArray:
                    scanContext.GCReportCallback(slotAddress, GcScanFlags.None);
                    break;

                case CorElementType.Byref:
                    scanContext.GCReportCallback(slotAddress, GcScanFlags.GC_CALL_INTERIOR);
                    break;

                case CorElementType.ValueType:
                    if (arg.IsPassedByRef)
                    {
                        scanContext.GCReportCallback(slotAddress, GcScanFlags.GC_CALL_INTERIOR);
                    }
                    else
                    {
                        ReportPointersFromValueType(rts, arg.TypeHandle, slotAddress, scanContext);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Report GC references within an unboxed value type using GCDesc series.
    /// Port of native ReportPointersFromValueType (siginfo.cpp).
    /// </summary>
    private void ReportPointersFromValueType(
        IRuntimeTypeSystem rts, TypeHandle typeHandle, TargetPointer baseAddress, GcScanContext scanContext)
    {
        if (!rts.ContainsGCPointers(typeHandle))
            return;

        int pointerSize = _target.PointerSize;

        foreach ((uint seriesOffset, uint seriesSize) in rts.GetGCDescSeries(typeHandle))
        {
            // GCDesc offset includes the MethodTable pointer; subtract it for unboxed layout.
            int fieldOffset = (int)seriesOffset - pointerSize;
            int runBytes = (int)seriesSize;

            for (int off = 0; off < runBytes; off += pointerSize)
            {
                TargetPointer refAddr = baseAddress + (ulong)(fieldOffset + off);
                scanContext.GCReportCallback(refAddr, GcScanFlags.None);
            }
        }
    }

    private TargetPointer AddressFromGCRefMapPos(Data.TransitionBlock tb, int pos)
    {
        return new TargetPointer(tb.FirstGCRefMapSlot.Value + (ulong)(pos * _target.PointerSize));
    }

    private bool IsTargetArm64()
    {
        return _target.Contracts.RuntimeInfo.GetTargetArchitecture() is RuntimeInfoArchitecture.Arm64;
    }

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
}
