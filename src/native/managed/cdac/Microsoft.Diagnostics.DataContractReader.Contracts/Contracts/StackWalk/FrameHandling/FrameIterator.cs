// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.CallingConvention;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal sealed class FrameIterator
{
    internal enum FrameType
    {
        Unknown,

        InlinedCallFrame,
        SoftwareExceptionFrame,

        /* TransitionFrame Types */
        FramedMethodFrame,
        PInvokeCalliFrame,
        PrestubMethodFrame,
        StubDispatchFrame,
        CallCountingHelperFrame,
        ExternalMethodFrame,
        DynamicHelperFrame,

        FuncEvalFrame,

        /* ResumableFrame Types */
        ResumableFrame,
        RedirectedThreadFrame,

        FaultingExceptionFrame,

        HijackFrame,

        TailCallFrame,

        /* Other Frame Types not handled by the iterator */
        ProtectValueClassFrame,
        DebuggerClassInitMarkFrame,
        DebuggerExitFrame,
        DebuggerU2MCatchHandlerFrame,
        ExceptionFilterFrame,
        InterpreterFrame,
    }

    private readonly Target target;
    private readonly TargetPointer terminator;
    private TargetPointer currentFramePointer;
    private CallingConventionInfo? _callingConventionInfo;

    internal Data.Frame CurrentFrame => target.ProcessedData.GetOrAdd<Data.Frame>(currentFramePointer);

    public TargetPointer CurrentFrameAddress => currentFramePointer;

    private CallingConventionInfo GetCallingConventionInfo()
        => _callingConventionInfo ??= new CallingConventionInfo(target);

    public FrameIterator(Target target, ThreadData threadData)
    {
        this.target = target;
        terminator = new TargetPointer(target.PointerSize == 8 ? ulong.MaxValue : uint.MaxValue);
        currentFramePointer = threadData.Frame;
    }

    public bool IsValid()
    {
        return currentFramePointer != terminator;
    }

    public bool Next()
    {
        if (currentFramePointer == terminator)
            return false;

        currentFramePointer = CurrentFrame.Next;
        return currentFramePointer != terminator;
    }

    public void UpdateContextFromFrame(IPlatformAgnosticContext context)
    {
        switch (GetFrameType(target, CurrentFrame.Identifier))
        {
            case FrameType.InlinedCallFrame:
                Data.InlinedCallFrame inlinedCallFrame = target.ProcessedData.GetOrAdd<Data.InlinedCallFrame>(CurrentFrame.Address);
                GetFrameHandler(context).HandleInlinedCallFrame(inlinedCallFrame);
                return;

            case FrameType.SoftwareExceptionFrame:
                Data.SoftwareExceptionFrame softwareExceptionFrame = target.ProcessedData.GetOrAdd<Data.SoftwareExceptionFrame>(CurrentFrame.Address);
                GetFrameHandler(context).HandleSoftwareExceptionFrame(softwareExceptionFrame);
                return;

            // TransitionFrame type frames
            case FrameType.FramedMethodFrame:
            case FrameType.PInvokeCalliFrame:
            case FrameType.PrestubMethodFrame:
            case FrameType.StubDispatchFrame:
            case FrameType.CallCountingHelperFrame:
            case FrameType.ExternalMethodFrame:
            case FrameType.DynamicHelperFrame:
                // FrameMethodFrame is the base type for all transition Frames
                Data.FramedMethodFrame framedMethodFrame = target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(CurrentFrame.Address);
                GetFrameHandler(context).HandleTransitionFrame(framedMethodFrame);
                return;

            case FrameType.FuncEvalFrame:
                Data.FuncEvalFrame funcEvalFrame = target.ProcessedData.GetOrAdd<Data.FuncEvalFrame>(CurrentFrame.Address);
                GetFrameHandler(context).HandleFuncEvalFrame(funcEvalFrame);
                return;

            // ResumableFrame type frames
            case FrameType.ResumableFrame:
            case FrameType.RedirectedThreadFrame:
                Data.ResumableFrame resumableFrame = target.ProcessedData.GetOrAdd<Data.ResumableFrame>(CurrentFrame.Address);
                GetFrameHandler(context).HandleResumableFrame(resumableFrame);
                return;

            case FrameType.FaultingExceptionFrame:
                Data.FaultingExceptionFrame faultingExceptionFrame = target.ProcessedData.GetOrAdd<Data.FaultingExceptionFrame>(CurrentFrame.Address);
                GetFrameHandler(context).HandleFaultingExceptionFrame(faultingExceptionFrame);
                return;

            case FrameType.HijackFrame:
                Data.HijackFrame hijackFrame = target.ProcessedData.GetOrAdd<Data.HijackFrame>(CurrentFrame.Address);
                GetFrameHandler(context).HandleHijackFrame(hijackFrame);
                return;
            case FrameType.TailCallFrame:
                Data.TailCallFrame tailCallFrame = target.ProcessedData.GetOrAdd<Data.TailCallFrame>(CurrentFrame.Address);
                GetFrameHandler(context).HandleTailCallFrame(tailCallFrame);
                return;
            default:
                // Unknown Frame type. This could either be a Frame that we don't know how to handle,
                // or a Frame that does not update the context.
                return;
        }
    }

    /// <summary>
    /// Returns the return address for the current Frame, matching native Frame::GetReturnAddress().
    /// Returns TargetPointer.Null if the Frame has no return address (e.g., non-active ICF,
    /// base Frame types, FuncEvalFrame during exception eval).
    /// </summary>
    public TargetPointer GetReturnAddress()
    {
        FrameType frameType = GetCurrentFrameType();
        switch (frameType)
        {
            // InlinedCallFrame: returns 0 if inactive, else m_pCallerReturnAddress
            case FrameType.InlinedCallFrame:
                Data.InlinedCallFrame icf = target.ProcessedData.GetOrAdd<Data.InlinedCallFrame>(currentFramePointer);
                return InlinedCallFrameHasActiveCall(icf) ? new TargetPointer(icf.CallerReturnAddress) : TargetPointer.Null;

            // TransitionFrame types: read return address from the transition block
            case FrameType.FramedMethodFrame:
            case FrameType.PInvokeCalliFrame:
            case FrameType.PrestubMethodFrame:
            case FrameType.StubDispatchFrame:
            case FrameType.CallCountingHelperFrame:
            case FrameType.ExternalMethodFrame:
            case FrameType.DynamicHelperFrame:
                Data.FramedMethodFrame fmf = target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(currentFramePointer);
                Data.TransitionBlock tb = target.ProcessedData.GetOrAdd<Data.TransitionBlock>(fmf.TransitionBlockPtr);
                return tb.ReturnAddress;

            // SoftwareExceptionFrame: stored m_ReturnAddress
            case FrameType.SoftwareExceptionFrame:
                Data.SoftwareExceptionFrame sef = target.ProcessedData.GetOrAdd<Data.SoftwareExceptionFrame>(currentFramePointer);
                return sef.ReturnAddress;

            // ResumableFrame / RedirectedThreadFrame: RIP from captured context
            case FrameType.ResumableFrame:
            case FrameType.RedirectedThreadFrame:
            {
                Data.ResumableFrame rf = target.ProcessedData.GetOrAdd<Data.ResumableFrame>(currentFramePointer);
                IPlatformAgnosticContext ctx = IPlatformAgnosticContext.GetContextForPlatform(target);
                ctx.ReadFromAddress(target, rf.TargetContextPtr);
                return ctx.InstructionPointer;
            }

            // FaultingExceptionFrame: RIP from embedded context
            case FrameType.FaultingExceptionFrame:
            {
                Data.FaultingExceptionFrame fef = target.ProcessedData.GetOrAdd<Data.FaultingExceptionFrame>(currentFramePointer);
                IPlatformAgnosticContext ctx = IPlatformAgnosticContext.GetContextForPlatform(target);
                ctx.ReadFromAddress(target, fef.TargetContext);
                return ctx.InstructionPointer;
            }

            // HijackFrame: stored m_ReturnAddress
            case FrameType.HijackFrame:
                Data.HijackFrame hf = target.ProcessedData.GetOrAdd<Data.HijackFrame>(currentFramePointer);
                return hf.ReturnAddress;

            // TailCallFrame: stored m_ReturnAddress
            case FrameType.TailCallFrame:
                Data.TailCallFrame tcf = target.ProcessedData.GetOrAdd<Data.TailCallFrame>(currentFramePointer);
                return tcf.ReturnAddress;

            // FuncEvalFrame: returns 0 during exception eval, else from transition block
            case FrameType.FuncEvalFrame:
                Data.FuncEvalFrame funcEval = target.ProcessedData.GetOrAdd<Data.FuncEvalFrame>(currentFramePointer);
                Data.DebuggerEval dbgEval = target.ProcessedData.GetOrAdd<Data.DebuggerEval>(funcEval.DebuggerEvalPtr);
                if (dbgEval.EvalDuringException)
                    return TargetPointer.Null;
                Data.FramedMethodFrame funcEvalFmf = target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(currentFramePointer);
                Data.TransitionBlock funcEvalTb = target.ProcessedData.GetOrAdd<Data.TransitionBlock>(funcEvalFmf.TransitionBlockPtr);
                return funcEvalTb.ReturnAddress;

            // Base Frame and unknown types: return 0 (matches native Frame::GetReturnAddressPtr_Impl)
            default:
                return TargetPointer.Null;
        }
    }

    public static string GetFrameName(Target target, TargetPointer frameIdentifier)
    {
        FrameType frameType = GetFrameType(target, frameIdentifier);
        if (frameType == FrameType.Unknown)
        {
            return string.Empty;
        }
        return frameType.ToString();
    }

    public FrameType GetCurrentFrameType() => GetFrameType(target, CurrentFrame.Identifier);

    internal static FrameType GetFrameType(Target target, TargetPointer frameIdentifier)
    {
        foreach (FrameType frameType in Enum.GetValues<FrameType>())
        {
            if (target.TryReadGlobalPointer(frameType.ToString() + "Identifier", out TargetPointer? id))
            {
                if (frameIdentifier == id)
                {
                    return frameType;
                }
            }
        }

        return FrameType.Unknown;
    }

    private IPlatformFrameHandler GetFrameHandler(IPlatformAgnosticContext context)
    {
        return context switch
        {
            ContextHolder<X86Context> contextHolder => new X86FrameHandler(target, contextHolder),
            ContextHolder<AMD64Context> contextHolder => new AMD64FrameHandler(target, contextHolder),
            ContextHolder<ARMContext> contextHolder => new ARMFrameHandler(target, contextHolder),
            ContextHolder<ARM64Context> contextHolder => new ARM64FrameHandler(target, contextHolder),
            ContextHolder<RISCV64Context> contextHolder => new RISCV64FrameHandler(target, contextHolder),
            ContextHolder<LoongArch64Context> contextHolder => new LoongArch64FrameHandler(target, contextHolder),
            _ => throw new InvalidOperationException("Unsupported context type"),
        };
    }

    public static TargetPointer GetMethodDescPtr(Target target, TargetPointer framePtr)
    {
        Data.Frame frame = target.ProcessedData.GetOrAdd<Data.Frame>(framePtr);
        FrameType frameType = GetFrameType(target, frame.Identifier);
        switch (frameType)
        {
            case FrameType.FramedMethodFrame:
            case FrameType.DynamicHelperFrame:
            case FrameType.ExternalMethodFrame:
            case FrameType.PrestubMethodFrame:
            case FrameType.CallCountingHelperFrame:
            case FrameType.InterpreterFrame:
                Data.FramedMethodFrame framedMethodFrame = target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frame.Address);
                return framedMethodFrame.MethodDescPtr;
            case FrameType.PInvokeCalliFrame:
                return TargetPointer.Null;
            case FrameType.StubDispatchFrame:
                Data.StubDispatchFrame stubDispatchFrame = target.ProcessedData.GetOrAdd<Data.StubDispatchFrame>(frame.Address);
                if (stubDispatchFrame.MethodDescPtr != TargetPointer.Null)
                {
                    return stubDispatchFrame.MethodDescPtr;
                }
                else if (stubDispatchFrame.RepresentativeMTPtr != TargetPointer.Null)
                {
                    IRuntimeTypeSystem rtsContract = target.Contracts.RuntimeTypeSystem;
                    TypeHandle mtHandle = rtsContract.GetTypeHandle(stubDispatchFrame.RepresentativeMTPtr);
                    return rtsContract.GetMethodDescForSlot(mtHandle, (ushort)stubDispatchFrame.RepresentativeSlot);
                }
                else
                {
                    return TargetPointer.Null;
                }
            case FrameType.InlinedCallFrame:
                Data.InlinedCallFrame inlinedCallFrame = target.ProcessedData.GetOrAdd<Data.InlinedCallFrame>(frame.Address);
                if (InlinedCallFrameHasActiveCall(inlinedCallFrame) && InlinedCallFrameHasFunction(inlinedCallFrame, target))
                    return inlinedCallFrame.Datum & ~(ulong)(target.PointerSize - 1);
                else
                    return TargetPointer.Null;
            default:
                return TargetPointer.Null;
        }
    }

    private static bool InlinedCallFrameHasFunction(Data.InlinedCallFrame frame, Target target)
    {
        if (target.PointerSize == sizeof(ulong))
        {
            return frame.Datum != TargetPointer.Null && (frame.Datum.Value & 0x1) == 0;
        }
        else
        {
            return ((long)frame.Datum.Value & ~0xffff) != 0;
        }
    }

    private static bool InlinedCallFrameHasActiveCall(Data.InlinedCallFrame frame)
    {
        return frame.CallerReturnAddress != TargetPointer.Null;
    }

    // ===== Frame GC Root Scanning =====

    /// <summary>
    /// Scans GC roots for a Frame based on its type.
    /// Dispatches to the appropriate scanning method (GCRefMap, MetaSig, or custom).
    /// Matches native Frame::GcScanRoots_Impl virtual dispatch.
    /// </summary>
    internal void GcScanRoots(TargetPointer frameAddress, GcScanContext scanContext)
    {
        if (frameAddress == TargetPointer.Null)
            return;

        Data.Frame frameData = target.ProcessedData.GetOrAdd<Data.Frame>(frameAddress);
        FrameType frameType = GetFrameType(target, frameData.Identifier);

        switch (frameType)
        {
            case FrameType.StubDispatchFrame:
            {
                Data.FramedMethodFrame fmf = target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frameAddress);
                Data.StubDispatchFrame sdf = target.ProcessedData.GetOrAdd<Data.StubDispatchFrame>(frameAddress);
                TargetPointer gcRefMap = sdf.GCRefMap;

                // Resolve GCRefMap via indirection if not yet cached
                if (gcRefMap == TargetPointer.Null && sdf.Indirection != TargetPointer.Null)
                    gcRefMap = FindGCRefMap(sdf.ZapModule, sdf.Indirection);

                if (gcRefMap != TargetPointer.Null)
                    PromoteCallerStackUsingGCRefMap(fmf.TransitionBlockPtr, gcRefMap, scanContext);
                else
                    PromoteCallerStack(frameAddress, fmf.TransitionBlockPtr, scanContext);
                break;
            }

            case FrameType.ExternalMethodFrame:
            {
                Data.FramedMethodFrame fmf = target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frameAddress);
                Data.ExternalMethodFrame emf = target.ProcessedData.GetOrAdd<Data.ExternalMethodFrame>(frameAddress);
                TargetPointer gcRefMap = emf.GCRefMap;

                // Resolve GCRefMap via FindGCRefMap if not yet cached by the runtime
                if (gcRefMap == TargetPointer.Null && emf.Indirection != TargetPointer.Null)
                    gcRefMap = FindGCRefMap(emf.ZapModule, emf.Indirection);

                if (gcRefMap != TargetPointer.Null)
                    PromoteCallerStackUsingGCRefMap(fmf.TransitionBlockPtr, gcRefMap, scanContext);
                else
                    PromoteCallerStack(frameAddress, fmf.TransitionBlockPtr, scanContext);
                break;
            }

            case FrameType.DynamicHelperFrame:
            {
                Data.FramedMethodFrame fmf = target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frameAddress);
                Data.DynamicHelperFrame dhf = target.ProcessedData.GetOrAdd<Data.DynamicHelperFrame>(frameAddress);
                ScanDynamicHelperFrame(fmf.TransitionBlockPtr, dhf.DynamicHelperFrameFlags, scanContext);
                break;
            }

            case FrameType.CallCountingHelperFrame:
            case FrameType.PrestubMethodFrame:
            {
                Data.FramedMethodFrame fmf = target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frameAddress);
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
                // Base Frame::GcScanRoots_Impl is a no-op for most frame types.
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
        GCRefMapDecoder decoder = new(target, gcRefMapBlob);

        if (target.PointerSize == 4)
            decoder.ReadStackPop();

        while (!decoder.AtEnd)
        {
            int pos = decoder.CurrentPos;
            GCRefMapToken token = decoder.ReadToken();
            int offset = GetCallingConventionInfo().OffsetFromGCRefMapPos(pos);
            TargetPointer slotAddress = new(transitionBlock.Value + (ulong)offset);

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
                    // TODO(stackref): Implement VASIG_COOKIE handling
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

        Target.TypeInfo tbType = target.GetTypeInfo(DataType.TransitionBlock);
        uint argRegOffset = (uint)tbType.Fields[nameof(Data.TransitionBlock.ArgumentRegistersOffset)].Offset;

        if ((dynamicHelperFrameFlags & DynamicHelperFrameFlags_ObjectArg) != 0)
        {
            TargetPointer argAddr = new(transitionBlock.Value + argRegOffset);
            scanContext.GCReportCallback(argAddr, GcScanFlags.None);
        }

        if ((dynamicHelperFrameFlags & DynamicHelperFrameFlags_ObjectArg2) != 0)
        {
            TargetPointer argAddr = new(transitionBlock.Value + argRegOffset + (uint)target.PointerSize);
            scanContext.GCReportCallback(argAddr, GcScanFlags.None);
        }
    }

    /// <summary>
    /// Resolves the GCRefMap for a Frame with m_pIndirection set but m_pGCRefMap not yet cached.
    /// Port of native FindGCRefMap (frames.cpp:853).
    /// </summary>
    private TargetPointer FindGCRefMap(TargetPointer zapModule, TargetPointer indirection)
    {
        if (indirection == TargetPointer.Null)
            return TargetPointer.Null;

        // If ZapModule is null, resolve it from the indirection address.
        // Matches native GetGCRefMap which calls FindModuleForGCRefMap(m_pIndirection)
        // → ExecutionManager::FindReadyToRunModule.
        if (zapModule == TargetPointer.Null)
        {
            IExecutionManager eman = target.Contracts.ExecutionManager;
            zapModule = eman.FindReadyToRunModule(indirection);
            if (zapModule == TargetPointer.Null)
                return TargetPointer.Null;
        }

        // Get the ReadyToRunInfo from the module
        Data.Module module = target.ProcessedData.GetOrAdd<Data.Module>(zapModule);
        if (module.ReadyToRunInfo == TargetPointer.Null)
            return TargetPointer.Null;

        Data.ReadyToRunInfo r2rInfo = target.ProcessedData.GetOrAdd<Data.ReadyToRunInfo>(module.ReadyToRunInfo);
        if (r2rInfo.ImportSections == TargetPointer.Null || r2rInfo.NumImportSections == 0)
            return TargetPointer.Null;

        // Compute RVA = indirection - imageBase
        ulong imageBase = r2rInfo.LoadedImageBase.Value;
        if (indirection.Value < imageBase)
            return TargetPointer.Null;
        ulong diff = indirection.Value - imageBase;
        if (diff > uint.MaxValue)
            return TargetPointer.Null;
        uint rva = (uint)diff;

        // READYTORUN_IMPORT_SECTION layout:
        //   IMAGE_DATA_DIRECTORY Section (VirtualAddress:4, Size:4) = 8 bytes
        //   ReadyToRunImportSectionFlags Flags (2 bytes)
        //   ReadyToRunImportSectionType Type (1 byte)
        //   BYTE EntrySize (1 byte)
        //   DWORD Signatures (4 bytes)
        //   DWORD AuxiliaryData (4 bytes)
        // Total: 20 bytes
        const int ImportSectionSize = 20;
        const int SectionVAOffset = 0;
        const int SectionSizeOffset = 4;
        const int EntrySizeOffset = 11;
        const int AuxiliaryDataOffset = 16;

        TargetPointer sectionsBase = r2rInfo.ImportSections;
        for (uint i = 0; i < r2rInfo.NumImportSections; i++)
        {
            TargetPointer sectionAddr = new(sectionsBase.Value + i * ImportSectionSize);
            uint sectionVA = target.Read<uint>(sectionAddr + SectionVAOffset);
            uint sectionSize = target.Read<uint>(sectionAddr + SectionSizeOffset);

            if (rva >= sectionVA && rva < sectionVA + sectionSize)
            {
                byte entrySize = target.Read<byte>(sectionAddr + EntrySizeOffset);
                if (entrySize == 0)
                    return TargetPointer.Null;

                uint index = (rva - sectionVA) / entrySize;
                uint auxDataRva = target.Read<uint>(sectionAddr + AuxiliaryDataOffset);
                if (auxDataRva == 0)
                    return TargetPointer.Null;

                TargetPointer gcRefMapBase = new(imageBase + auxDataRva);

                // GCRefMap starts with a lookup index for stride-based access.
                // GCREFMAP_LOOKUP_STRIDE is 1024 in the native code.
                const uint GCREFMAP_LOOKUP_STRIDE = 1024;
                uint lookupIndex = index / GCREFMAP_LOOKUP_STRIDE;
                uint remaining = index % GCREFMAP_LOOKUP_STRIDE;

                // Read the offset from the lookup table (array of DWORDs)
                uint lookupOffset = target.Read<uint>(new TargetPointer(gcRefMapBase.Value + lookupIndex * 4));
                TargetPointer p = new(gcRefMapBase.Value + lookupOffset);

                // Linear scan past 'remaining' entries
                while (remaining > 0)
                {
                    // Each entry is a variable-length sequence of bytes where the high bit
                    // indicates continuation. Skip until we find a byte without the high bit set.
                    while ((target.Read<byte>(p) & 0x80) != 0)
                        p = new(p.Value + 1);
                    p = new(p.Value + 1); // skip the final byte of this entry

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
        Data.FramedMethodFrame fmf = target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frameAddress);
        TargetPointer methodDescPtr = fmf.MethodDescPtr;
        if (methodDescPtr == TargetPointer.Null)
            return;

        ReadOnlySpan<byte> signature;
        MetadataReader? metadataReader;
        try
        {
            signature = GetMethodSignatureBytes(methodDescPtr, out metadataReader);
        }
        catch (System.Exception)
        {
            return;
        }

        if (signature.IsEmpty)
            return;

        MethodSignature<GcTypeKind> methodSig;
        try
        {
            RuntimeSignatureDecoder<GcTypeKind, object?, SpanSignatureReader> decoder = new(
                GcSignatureTypeProvider.Instance, target, genericContext: null,
                new SpanSignatureReader(signature, target.IsLittleEndian), metadataReader);
            methodSig = decoder.DecodeMethodSignature();
        }
        catch (System.Exception)
        {
            // If signature decoding fails for any reason, skip this frame.
            // The GCRefMap path handles these cases when available.
            return;
        }

        if (methodSig.Header.CallingConvention is SignatureCallingConvention.VarArgs)
        {
            // TODO(stackref): VarArg path — read VASigCookie from frame
            return;
        }

        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
        MethodDescHandle mdh = rts.GetMethodDescHandle(methodDescPtr);

        bool hasThis = methodSig.Header.IsInstance;
        bool requiresInstArg = false;
        bool isAsync = false;
        bool isValueTypeThis = false;

        try
        {
            requiresInstArg = rts.RequiresInstArg(mdh);
            isAsync = rts.IsAsyncMethod(mdh);

            // TODO(stackref): Detect value type 'this' (needs IRuntimeTypeSystem.IsValueType)
            // TODO(stackref): String constructor clears HasThis
        }
        catch
        {
        }

        PromoteCallerStackHelper(transitionBlock, methodSig, hasThis,
            requiresInstArg, isAsync, isValueTypeThis, scanContext);
    }

    /// <summary>
    /// Core logic for promoting caller stack GC references.
    /// Uses <see cref="ArgIterator"/> to correctly map arguments to their
    /// register/stack locations per the target's calling convention.
    /// Matches native TransitionFrame::PromoteCallerStackHelper (frames.cpp:1546).
    /// </summary>
    private void PromoteCallerStackHelper(
        TargetPointer transitionBlock,
        MethodSignature<GcTypeKind> methodSig,
        bool hasThis,
        bool requiresInstArg,
        bool isAsync,
        bool isValueTypeThis,
        GcScanContext scanContext)
    {
        CallingConventionInfo ccInfo;
        try
        {
            ccInfo = GetCallingConventionInfo();
        }
        catch
        {
            return;
        }

        // Build ArgTypeInfo array from decoded signature
        ArgTypeInfo[] paramTypes = new ArgTypeInfo[methodSig.ParameterTypes.Length];
        for (int i = 0; i < paramTypes.Length; i++)
        {
            paramTypes[i] = GcTypeKindToArgTypeInfo(methodSig.ParameterTypes[i], ccInfo.PointerSize);
        }
        ArgTypeInfo returnTypeInfo = GcTypeKindToArgTypeInfo(methodSig.ReturnType, ccInfo.PointerSize);

        ArgIteratorData argData = new(hasThis, methodSig.Header.CallingConvention is SignatureCallingConvention.VarArgs, paramTypes, returnTypeInfo);
        CallingConvention.ArgIterator argit = new(ccInfo, argData, hasParamType: requiresInstArg, hasAsyncContinuation: isAsync, forcedByRefParams: System.Array.Empty<bool>());

        // Promote 'this' for non-static methods
        if (argit.HasThis)
        {
            int thisOffset = argit.GetThisOffset();
            TargetPointer thisAddr = new(transitionBlock.Value + (ulong)thisOffset);
            GcScanFlags thisFlags = isValueTypeThis ? GcScanFlags.GC_CALL_INTERIOR : GcScanFlags.None;
            scanContext.GCReportCallback(thisAddr, thisFlags);
        }

        // Promote async continuation
        if (argit.HasAsyncContinuation)
        {
            int asyncOffset = argit.GetAsyncContinuationArgOffset();
            TargetPointer asyncAddr = new(transitionBlock.Value + (ulong)asyncOffset);
            scanContext.GCReportCallback(asyncAddr, GcScanFlags.None);
        }

        // Walk each argument using ArgIterator for correct offsets
        int argIndex = 0;
        int argOffset;
        while ((argOffset = argit.GetNextOffset()) != CallingConventionInfo.InvalidOffset)
        {
            if (argIndex >= methodSig.ParameterTypes.Length)
                break;

            GcTypeKind kind = methodSig.ParameterTypes[argIndex];
            TargetPointer slotAddress = new(transitionBlock.Value + (ulong)argOffset);

            switch (kind)
            {
                case GcTypeKind.Ref:
                    scanContext.GCReportCallback(slotAddress, GcScanFlags.None);
                    break;
                case GcTypeKind.Interior:
                    scanContext.GCReportCallback(slotAddress, GcScanFlags.GC_CALL_INTERIOR);
                    break;
                case GcTypeKind.Other:
                    // Value type: if passed by ref, report as interior pointer
                    if (argit.IsArgPassedByRef())
                    {
                        scanContext.GCReportCallback(slotAddress, GcScanFlags.GC_CALL_INTERIOR);
                    }
                    // TODO: For value types passed by value, enumerate fields for embedded GC refs
                    break;
                case GcTypeKind.None:
                    break;
            }
            argIndex++;
        }
    }

    /// <summary>
    /// Converts a <see cref="GcTypeKind"/> to a minimal <see cref="ArgTypeInfo"/> for
    /// ArgIterator consumption. This is a bridge until we have a full type-aware provider.
    /// </summary>
    private static ArgTypeInfo GcTypeKindToArgTypeInfo(GcTypeKind kind, int pointerSize)
    {
        return kind switch
        {
            GcTypeKind.None => ArgTypeInfo.ForPrimitive(CorElementType.I, pointerSize),
            GcTypeKind.Ref => ArgTypeInfo.ForPrimitive(CorElementType.Class, pointerSize),
            GcTypeKind.Interior => ArgTypeInfo.ForPrimitive(CorElementType.Byref, pointerSize),
            GcTypeKind.Other => new ArgTypeInfo
            {
                CorElementType = CorElementType.ValueType,
                Size = pointerSize, // Conservative: assume pointer-sized for now
                IsValueType = true,
            },
            _ => ArgTypeInfo.ForPrimitive(CorElementType.I, pointerSize),
        };
    }

    private ReadOnlySpan<byte> GetMethodSignatureBytes(TargetPointer methodDescPtr, out MetadataReader? metadataReader)
    {
        metadataReader = null;
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
        MethodDescHandle mdh = rts.GetMethodDescHandle(methodDescPtr);

        if (rts.IsStoredSigMethodDesc(mdh, out ReadOnlySpan<byte> storedSig))
            return storedSig;

        uint methodToken = rts.GetMethodToken(mdh);
        if (methodToken == 0x06000000)
            return default;

        TargetPointer methodTablePtr = rts.GetMethodTable(mdh);
        TypeHandle typeHandle = rts.GetTypeHandle(methodTablePtr);
        TargetPointer modulePtr = rts.GetModule(typeHandle);

        ILoader loader = target.Contracts.Loader;
        ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(modulePtr);

        IEcmaMetadata ecmaMetadata = target.Contracts.EcmaMetadata;
        metadataReader = ecmaMetadata.GetMetadata(moduleHandle);
        if (metadataReader is null)
            return default;

        MethodDefinitionHandle methodDefHandle = MetadataTokens.MethodDefinitionHandle((int)(methodToken & 0x00FFFFFF));
        MethodDefinition methodDef = metadataReader.GetMethodDefinition(methodDefHandle);
        BlobReader blobReader = metadataReader.GetBlobReader(methodDef.Signature);
        return blobReader.ReadBytes(blobReader.Length);
    }

    private bool IsTargetArm64()
    {
        return target.Contracts.RuntimeInfo.GetTargetArchitecture() is RuntimeInfoArchitecture.Arm64;
    }
}
