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

    internal GcScanner(Target target)
    {
        _target = target;
        _eman = target.Contracts.ExecutionManager;
        _gcInfo = target.Contracts.GCInfo;
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
        FrameIterator.FrameType frameType = FrameIterator.GetFrameType(_target, frameData.Identifier);

        switch (frameType)
        {
            case FrameIterator.FrameType.StubDispatchFrame:
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

            case FrameIterator.FrameType.ExternalMethodFrame:
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

            case FrameIterator.FrameType.DynamicHelperFrame:
            {
                Data.FramedMethodFrame fmf = _target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frameAddress);
                Data.DynamicHelperFrame dhf = _target.ProcessedData.GetOrAdd<Data.DynamicHelperFrame>(frameAddress);
                ScanDynamicHelperFrame(fmf.TransitionBlockPtr, dhf.DynamicHelperFrameFlags, scanContext);
                break;
            }

            case FrameIterator.FrameType.CallCountingHelperFrame:
            case FrameIterator.FrameType.PrestubMethodFrame:
            {
                Data.FramedMethodFrame fmf = _target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frameAddress);
                PromoteCallerStack(frameAddress, fmf.TransitionBlockPtr, scanContext);
                break;
            }

            case FrameIterator.FrameType.HijackFrame:
                // TODO(stackref): Implement HijackFrame scanning (X86 only with FEATURE_HIJACK)
                break;

            case FrameIterator.FrameType.ProtectValueClassFrame:
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
        MethodDescHandle mdh = rts.GetMethodDescHandle(methodDescPtr);

        MethodSignature<GcTypeKind> methodSig;
        try
        {
            TargetPointer methodTablePtr = rts.GetMethodTable(mdh);
            TypeHandle typeHandle = rts.GetTypeHandle(methodTablePtr);
            TargetPointer modulePtr = rts.GetModule(typeHandle);

            ModuleHandle moduleHandle = _target.Contracts.Loader.GetModuleHandleFromModulePtr(modulePtr);
            MetadataReader? mdReader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle);
            if (mdReader is null)
                return;

            GcSignatureTypeProvider provider = new(_target, moduleHandle);
            GcSignatureContext genericContext = new(typeHandle, mdh);
            RuntimeSignatureDecoder<GcTypeKind, GcSignatureContext> decoder = new(
                provider, _target, mdReader, genericContext);

            // Match native MethodDesc::GetSig: prefer stored signature (dynamic, EEImpl,
            // and array method descs) before falling back to a metadata token lookup.
            if (rts.IsStoredSigMethodDesc(mdh, out ReadOnlySpan<byte> storedSig))
            {
                unsafe
                {
                    fixed (byte* pStoredSig = storedSig)
                    {
                        BlobReader blobReader = new BlobReader(pStoredSig, storedSig.Length);
                        methodSig = decoder.DecodeMethodSignature(ref blobReader);
                    }
                }
            }
            else
            {
                uint methodToken = rts.GetMethodToken(mdh);
                if (methodToken == (uint)EcmaMetadataUtils.TokenType.mdtMethodDef)
                    return;

                MethodDefinitionHandle methodDefHandle = MetadataTokens.MethodDefinitionHandle((int)EcmaMetadataUtils.GetRowId(methodToken));
                MethodDefinition methodDef = mdReader.GetMethodDefinition(methodDefHandle);

                BlobReader blobReader = mdReader.GetBlobReader(methodDef.Signature);
                methodSig = decoder.DecodeMethodSignature(ref blobReader);
            }
        }
        catch (System.Exception)
        {
            return;
        }

        if (methodSig.Header.CallingConvention is SignatureCallingConvention.VarArgs)
            return;

        bool hasThis = methodSig.Header.IsInstance;
        bool hasRetBuf = methodSig.ReturnType is GcTypeKind.Other;
        bool requiresInstArg = false;
        bool isAsync = false;
        bool isValueTypeThis = false;

        try
        {
            requiresInstArg = rts.RequiresInstArg(mdh);
            isAsync = rts.IsAsyncMethod(mdh);
        }
        catch
        {
        }

        PromoteCallerStackHelper(transitionBlock, methodSig, hasThis, hasRetBuf,
            requiresInstArg, isAsync, isValueTypeThis, scanContext);
    }

    /// <summary>
    /// Core logic for promoting caller stack GC references.
    /// Matches native TransitionFrame::PromoteCallerStackHelper (frames.cpp:1560).
    /// </summary>
    private void PromoteCallerStackHelper(
        TargetPointer transitionBlock,
        MethodSignature<GcTypeKind> methodSig,
        bool hasThis,
        bool hasRetBuf,
        bool requiresInstArg,
        bool isAsync,
        bool isValueTypeThis,
        GcScanContext scanContext)
    {
        Data.TransitionBlock tb = _target.ProcessedData.GetOrAdd<Data.TransitionBlock>(transitionBlock);

        int numRegistersUsed = 0;
        if (hasThis)
            numRegistersUsed++;
        if (hasRetBuf)
            numRegistersUsed++;
        if (requiresInstArg)
            numRegistersUsed++;
        if (isAsync)
            numRegistersUsed++;

        bool isArm64 = IsTargetArm64();
        if (isArm64)
            numRegistersUsed++;

        if (hasThis)
        {
            int thisPos = isArm64 ? 1 : 0;
            TargetPointer thisAddr = AddressFromGCRefMapPos(tb, thisPos);
            GcScanFlags thisFlags = isValueTypeThis ? GcScanFlags.GC_CALL_INTERIOR : GcScanFlags.None;
            scanContext.GCReportCallback(thisAddr, thisFlags);
        }

        int pos = numRegistersUsed;
        foreach (GcTypeKind kind in methodSig.ParameterTypes)
        {
            TargetPointer slotAddress = AddressFromGCRefMapPos(tb, pos);

            switch (kind)
            {
                case GcTypeKind.Ref:
                    scanContext.GCReportCallback(slotAddress, GcScanFlags.None);
                    break;
                case GcTypeKind.Interior:
                    scanContext.GCReportCallback(slotAddress, GcScanFlags.GC_CALL_INTERIOR);
                    break;
                case GcTypeKind.Other:
                    break;
                case GcTypeKind.None:
                    break;
            }
            pos++;
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
