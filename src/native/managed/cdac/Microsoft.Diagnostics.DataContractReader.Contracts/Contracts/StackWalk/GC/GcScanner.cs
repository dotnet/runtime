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
        uint scratchAreaSize = _gcInfo.GetSizeOfStackParameterArea(handle);
        bool filterScratchStackSlots = !options.IsActiveFrame;
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

                // Mirror native IsScratchStackSlot (gcinfodecoder.cpp, post-PR #119446 unified form):
                // for non-leaf frames, drop any stack slot whose resolved address lies in the
                // outgoing/scratch area [SP, SP + SizeOfStackOutgoingAndScratchArea). This applies
                // to all stack base kinds (GC_SP_REL, GC_FRAMEREG_REL, GC_CALLER_SP_REL) because
                // the filter is address-based, not offset-based.
                if (filterScratchStackSlots && scratchAreaSize > 0)
                {
                    ulong sp = context.StackPointer.Value;
                    if (addr.Value >= sp && addr.Value < sp + scratchAreaSize)
                        continue;
                }

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
                    PromoteCallerStack(frameAddress, scanContext);
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
                    PromoteCallerStack(frameAddress, scanContext);
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
                PromoteCallerStack(frameAddress, scanContext);
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

        // x86 lays out ArgumentRegisters as { EDX, ECX } (see ENUM_ARGUMENT_REGISTERS_BACKWARD
        // in src/coreclr/vm/i386/cgencpu.h). Native DynamicHelperFrame::GcScanRoots_Impl reports
        // ObjectArg at offsetof(ArgumentRegisters, ECX) and ObjectArg2 at offsetof(EDX), which
        // is the reverse of the layout-order indices used on other architectures.
        bool isX86 = _target.Contracts.RuntimeInfo.GetTargetArchitecture() is RuntimeInfoArchitecture.X86;
        int objectArgOffset = isX86 ? _target.PointerSize : 0;
        int objectArg2Offset = isX86 ? 0 : _target.PointerSize;

        if ((dynamicHelperFrameFlags & DynamicHelperFrameFlags_ObjectArg) != 0)
        {
            TargetPointer argAddr = new(argRegStart.Value + (uint)objectArgOffset);
            scanContext.GCReportCallback(argAddr, GcScanFlags.None);
        }

        if ((dynamicHelperFrameFlags & DynamicHelperFrameFlags_ObjectArg2) != 0)
        {
            TargetPointer argAddr = new(argRegStart.Value + (uint)objectArg2Offset);
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
    /// <remarks>
    /// Not yet ported. Every call records a deferred frame so the stress harness
    /// buckets the resulting cDAC-vs-runtime diff at this frame as a known issue
    /// rather than a real cDAC bug. Will be replaced with a real port once the
    /// signature- and ArgIterator-based ref enumeration lands.
    /// </remarks>
    private static void PromoteCallerStack(TargetPointer frameAddress, GcScanContext scanContext)
    {
        scanContext.RecordDeferredFrame(frameAddress);
    }

    private TargetPointer AddressFromGCRefMapPos(Data.TransitionBlock tb, int pos)
    {
        return new TargetPointer(tb.FirstGCRefMapSlot.Value + (ulong)(pos * _target.PointerSize));
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
