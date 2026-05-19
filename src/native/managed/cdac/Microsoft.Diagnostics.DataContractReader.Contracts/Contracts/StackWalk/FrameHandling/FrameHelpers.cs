// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

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
    InterpreterFrame,

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
}

/// <summary>
/// Helpers for inspecting and operating on a single capital-F <see cref="Data.Frame"/>.
/// These do not depend on iterator state — given a frame (or frame pointer/identifier) they
/// return information about that one frame or use it to update a context.
/// <see cref="FrameIterator"/> drives the chain traversal and uses these helpers to
/// classify and decode each frame as it walks.
/// </summary>
internal sealed class FrameHelpers
{
    private readonly Target _target;

    public FrameHelpers(Target target)
    {
        _target = target;
    }

    public string GetFrameName(TargetPointer frameIdentifier)
    {
        FrameType frameType = GetFrameType(frameIdentifier);
        if (frameType == FrameType.Unknown)
        {
            return string.Empty;
        }
        return frameType.ToString();
    }

    public FrameType GetFrameType(TargetPointer frameIdentifier)
    {
        foreach (FrameType frameType in Enum.GetValues<FrameType>())
        {
            if (_target.TryReadGlobalPointer(frameType.ToString() + "Identifier", out TargetPointer? id))
            {
                if (frameIdentifier == id)
                {
                    return frameType;
                }
            }
        }

        return FrameType.Unknown;
    }

    public TargetPointer GetMethodDescPtr(TargetPointer framePtr)
    {
        Data.Frame frame = _target.ProcessedData.GetOrAdd<Data.Frame>(framePtr);
        FrameType frameType = GetFrameType(frame.Identifier);
        switch (frameType)
        {
            case FrameType.FramedMethodFrame:
            case FrameType.DynamicHelperFrame:
            case FrameType.ExternalMethodFrame:
            case FrameType.PrestubMethodFrame:
            case FrameType.CallCountingHelperFrame:
                Data.FramedMethodFrame framedMethodFrame = _target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frame.Address);
                return framedMethodFrame.MethodDescPtr;
            case FrameType.InterpreterFrame:
                // InterpreterFrame is constructed with pMD=NULL in native.
                // The interpreted methods are yielded as frameless frames via
                // interpreter virtual unwind, not through the Frame's MethodDesc.
                return TargetPointer.Null;
            case FrameType.PInvokeCalliFrame:
                return TargetPointer.Null;
            case FrameType.StubDispatchFrame:
                Data.StubDispatchFrame stubDispatchFrame = _target.ProcessedData.GetOrAdd<Data.StubDispatchFrame>(frame.Address);
                if (stubDispatchFrame.MethodDescPtr != TargetPointer.Null)
                {
                    return stubDispatchFrame.MethodDescPtr;
                }
                else if (stubDispatchFrame.RepresentativeMTPtr != TargetPointer.Null)
                {
                    IRuntimeTypeSystem rtsContract = _target.Contracts.RuntimeTypeSystem;
                    TypeHandle mtHandle = rtsContract.GetTypeHandle(stubDispatchFrame.RepresentativeMTPtr);
                    return rtsContract.GetMethodDescForSlot(mtHandle, (ushort)stubDispatchFrame.RepresentativeSlot);
                }
                else
                {
                    return TargetPointer.Null;
                }
            case FrameType.InlinedCallFrame:
                Data.InlinedCallFrame inlinedCallFrame = _target.ProcessedData.GetOrAdd<Data.InlinedCallFrame>(frame.Address);
                if (InlinedCallFrameHasActiveCall(inlinedCallFrame) && InlinedCallFrameHasFunction(inlinedCallFrame))
                    return inlinedCallFrame.Datum & ~(ulong)(_target.PointerSize - 1);
                else
                    return TargetPointer.Null;
            default:
                return TargetPointer.Null;
        }
    }

    /// <summary>
    /// Updates <paramref name="context"/> based on <paramref name="frame"/>'s type, replicating
    /// the per-frame context update performed by the native stack walker before it yields a
    /// SW_FRAME (or transitions to SW_FRAMELESS for InterpreterFrame).
    /// </summary>
    public void UpdateContextFromFrame(Data.Frame frame, IPlatformAgnosticContext context)
    {
        switch (GetFrameType(frame.Identifier))
        {
            case FrameType.InlinedCallFrame:
                Data.InlinedCallFrame inlinedCallFrame = _target.ProcessedData.GetOrAdd<Data.InlinedCallFrame>(frame.Address);
                GetFrameHandler(context).HandleInlinedCallFrame(inlinedCallFrame);
                return;

            case FrameType.SoftwareExceptionFrame:
                Data.SoftwareExceptionFrame softwareExceptionFrame = _target.ProcessedData.GetOrAdd<Data.SoftwareExceptionFrame>(frame.Address);
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
                Data.FramedMethodFrame framedMethodFrame = _target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frame.Address);
                GetFrameHandler(context).HandleTransitionFrame(framedMethodFrame);
                return;

            case FrameType.InterpreterFrame:
                {
                    // Mirrors native InterpreterFrame::SetContextToInterpMethodContextFrame
                    // (frames.cpp). Sets context to the top InterpMethodContextFrame so the
                    // walker transitions to SW_FRAMELESS and yields each interpreted method
                    // individually via virtual unwind.
                    Data.InterpreterFrame interpreterFrame = _target.ProcessedData.GetOrAdd<Data.InterpreterFrame>(frame.Address);
                    SetContextToInterpMethodContextFrame(context, interpreterFrame);
                    return;
                }

            case FrameType.FuncEvalFrame:
                Data.FuncEvalFrame funcEvalFrame = _target.ProcessedData.GetOrAdd<Data.FuncEvalFrame>(frame.Address);
                GetFrameHandler(context).HandleFuncEvalFrame(funcEvalFrame);
                return;

            // ResumableFrame type frames
            case FrameType.ResumableFrame:
            case FrameType.RedirectedThreadFrame:
                Data.ResumableFrame resumableFrame = _target.ProcessedData.GetOrAdd<Data.ResumableFrame>(frame.Address);
                GetFrameHandler(context).HandleResumableFrame(resumableFrame);
                return;

            case FrameType.FaultingExceptionFrame:
                Data.FaultingExceptionFrame faultingExceptionFrame = _target.ProcessedData.GetOrAdd<Data.FaultingExceptionFrame>(frame.Address);
                GetFrameHandler(context).HandleFaultingExceptionFrame(faultingExceptionFrame);
                return;

            case FrameType.HijackFrame:
                Data.HijackFrame hijackFrame = _target.ProcessedData.GetOrAdd<Data.HijackFrame>(frame.Address);
                GetFrameHandler(context).HandleHijackFrame(hijackFrame);
                return;
            case FrameType.TailCallFrame:
                Data.TailCallFrame tailCallFrame = _target.ProcessedData.GetOrAdd<Data.TailCallFrame>(frame.Address);
                GetFrameHandler(context).HandleTailCallFrame(tailCallFrame);
                return;
            default:
                // Unknown Frame type. This could either be a Frame that we don't know how to handle,
                // or a Frame that does not update the context.
                return;
        }
    }

    /// <summary>
    /// Returns the return address for <paramref name="frame"/>, matching native Frame::GetReturnAddress().
    /// Returns TargetPointer.Null if the Frame has no return address (e.g., non-active ICF,
    /// base Frame types, FuncEvalFrame during exception eval).
    /// </summary>
    public TargetPointer GetReturnAddress(Data.Frame frame)
    {
        FrameType frameType = GetFrameType(frame.Identifier);
        switch (frameType)
        {
            // InlinedCallFrame: returns 0 if inactive, else m_pCallerReturnAddress
            case FrameType.InlinedCallFrame:
                Data.InlinedCallFrame icf = _target.ProcessedData.GetOrAdd<Data.InlinedCallFrame>(frame.Address);
                return InlinedCallFrameHasActiveCall(icf) ? icf.CallerReturnAddress : TargetPointer.Null;

            // TransitionFrame types: read return address from the transition block
            case FrameType.FramedMethodFrame:
            case FrameType.PInvokeCalliFrame:
            case FrameType.PrestubMethodFrame:
            case FrameType.StubDispatchFrame:
            case FrameType.CallCountingHelperFrame:
            case FrameType.ExternalMethodFrame:
            case FrameType.DynamicHelperFrame:
                Data.FramedMethodFrame fmf = _target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frame.Address);
                Data.TransitionBlock tb = _target.ProcessedData.GetOrAdd<Data.TransitionBlock>(fmf.TransitionBlockPtr);
                return tb.ReturnAddress;

            // SoftwareExceptionFrame: stored m_ReturnAddress
            case FrameType.SoftwareExceptionFrame:
                Data.SoftwareExceptionFrame sef = _target.ProcessedData.GetOrAdd<Data.SoftwareExceptionFrame>(frame.Address);
                return sef.ReturnAddress;

            // ResumableFrame / RedirectedThreadFrame: RIP from captured context
            case FrameType.ResumableFrame:
            case FrameType.RedirectedThreadFrame:
            {
                Data.ResumableFrame rf = _target.ProcessedData.GetOrAdd<Data.ResumableFrame>(frame.Address);
                IPlatformAgnosticContext ctx = IPlatformAgnosticContext.GetContextForPlatform(_target);
                ctx.ReadFromAddress(_target, rf.TargetContextPtr);
                return ctx.InstructionPointer;
            }

            // FaultingExceptionFrame: RIP from embedded context
            case FrameType.FaultingExceptionFrame:
            {
                Data.FaultingExceptionFrame fef = _target.ProcessedData.GetOrAdd<Data.FaultingExceptionFrame>(frame.Address);
                IPlatformAgnosticContext ctx = IPlatformAgnosticContext.GetContextForPlatform(_target);
                ctx.ReadFromAddress(_target, fef.TargetContext);
                return ctx.InstructionPointer;
            }

            // HijackFrame: stored m_ReturnAddress
            case FrameType.HijackFrame:
                Data.HijackFrame hf = _target.ProcessedData.GetOrAdd<Data.HijackFrame>(frame.Address);
                return hf.ReturnAddress;

            // TailCallFrame: stored m_ReturnAddress
            case FrameType.TailCallFrame:
                Data.TailCallFrame tcf = _target.ProcessedData.GetOrAdd<Data.TailCallFrame>(frame.Address);
                return tcf.ReturnAddress;

            // FuncEvalFrame: returns 0 during exception eval, else from transition block
            case FrameType.FuncEvalFrame:
                Data.FuncEvalFrame funcEval = _target.ProcessedData.GetOrAdd<Data.FuncEvalFrame>(frame.Address);
                Data.DebuggerEval dbgEval = _target.ProcessedData.GetOrAdd<Data.DebuggerEval>(funcEval.DebuggerEvalPtr);
                if (dbgEval.EvalUsesHijack)
                    return TargetPointer.Null;
                Data.FramedMethodFrame funcEvalFmf = _target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frame.Address);
                Data.TransitionBlock funcEvalTb = _target.ProcessedData.GetOrAdd<Data.TransitionBlock>(funcEvalFmf.TransitionBlockPtr);
                return funcEvalTb.ReturnAddress;

            // Base Frame and unknown types: return 0 (matches native Frame::GetReturnAddressPtr_Impl)
            default:
                return TargetPointer.Null;
        }
    }

    /// <summary>
    /// Returns the InternalFrameType (CorDebugInternalFrameType) of the frame at <paramref name="framePtr"/>.
    /// Mirrors the native DacDbiInterfaceImpl::GetInternalFrameType logic.
    /// </summary>
    public InternalFrameType GetInternalFrameType(TargetPointer framePtr)
    {
        Data.Frame frame = _target.ProcessedData.GetOrAdd<Data.Frame>(framePtr);
        FrameType frameType = GetFrameType(frame.Identifier);

        // Mirrors Frame::GetStubFrameType_Impl per subclass in src/coreclr/vm/frames.h.
        switch (frameType)
        {
            case FrameType.FaultingExceptionFrame:
            case FrameType.SoftwareExceptionFrame:
                return InternalFrameType.Exception;

            case FrameType.DebuggerClassInitMarkFrame:
                return InternalFrameType.ClassInit;

            case FrameType.PrestubMethodFrame:
                return InternalFrameType.JitCompilation;

            case FrameType.FuncEvalFrame:
                return InternalFrameType.FuncEval;

            case FrameType.DebuggerU2MCatchHandlerFrame:
                return InternalFrameType.U2M;

            case FrameType.DynamicHelperFrame:
                return InternalFrameType.InternalCall;

            case FrameType.DebuggerExitFrame:
            case FrameType.FramedMethodFrame:
            case FrameType.PInvokeCalliFrame:
            case FrameType.CallCountingHelperFrame:
            case FrameType.ExternalMethodFrame:
            case FrameType.InterpreterFrame:
                return InternalFrameType.M2U;

            // InlinedCallFrame inherits M2U from its _Impl, but the
            // debugger gates the value on FrameHasActiveCall.
            case FrameType.InlinedCallFrame:
                Data.InlinedCallFrame icf = _target.ProcessedData.GetOrAdd<Data.InlinedCallFrame>(frame.Address);
                return InlinedCallFrameHasActiveCall(icf)
                    ? InternalFrameType.M2U
                    : InternalFrameType.None;

            default:
                return InternalFrameType.None;
        }
    }

    /// <summary>
    /// Returns true if the frame at <paramref name="framePtr"/> is an InlinedCallFrame that was
    /// set up by the new exception handling helpers (i.e. its Datum field is tagged with the
    /// ExceptionHandlingHelper marker).
    /// </summary>
    public bool IsExceptionHandlingHelperInlinedCallFrame(TargetPointer framePtr)
    {
        Data.Frame frame = _target.ProcessedData.GetOrAdd<Data.Frame>(framePtr);
        FrameType frameType = GetFrameType(frame.Identifier);
        if (frameType != FrameType.InlinedCallFrame)
            return false;

        //   ExceptionHandlingHelper = 2 on 64-bit, 1 on 32-bit. Mask == ExceptionHandlingHelper.
        Data.InlinedCallFrame icf = _target.ProcessedData.GetOrAdd<Data.InlinedCallFrame>(frame.Address);
        if (!InlinedCallFrameHasActiveCall(icf))
            return false;

        ulong mask = (ulong)(_target.PointerSize == 8 ? 2 : 1);
        return (icf.Datum.Value & mask) == mask;
    }

    private IPlatformFrameHandler GetFrameHandler(IPlatformAgnosticContext context)
    {
        return context switch
        {
            ContextHolder<X86Context> contextHolder => new X86FrameHandler(_target, contextHolder),
            ContextHolder<AMD64Context> contextHolder => new AMD64FrameHandler(_target, contextHolder),
            ContextHolder<ARMContext> contextHolder => new ARMFrameHandler(_target, contextHolder),
            ContextHolder<ARM64Context> contextHolder => new ARM64FrameHandler(_target, contextHolder),
            ContextHolder<RISCV64Context> contextHolder => new RISCV64FrameHandler(_target, contextHolder),
            ContextHolder<LoongArch64Context> contextHolder => new LoongArch64FrameHandler(_target, contextHolder),
            _ => throw new InvalidOperationException("Unsupported context type"),
        };
    }

    private static bool InlinedCallFrameHasActiveCall(Data.InlinedCallFrame frame)
    {
        return frame.CallerReturnAddress != TargetPointer.Null;
    }

    private bool InlinedCallFrameHasFunction(Data.InlinedCallFrame frame)
    {
        if (_target.PointerSize == sizeof(ulong))
        {
            return frame.Datum != TargetPointer.Null && (frame.Datum.Value & 0x1) == 0;
        }
        else
        {
            return ((long)frame.Datum.Value & ~0xffff) != 0;
        }
    }

    /// <summary>
    /// Resolves the actual top InterpMethodContextFrame from the hint stored in InterpreterFrame,
    /// replicating InterpreterFrame::GetTopInterpMethodContextFrame() from frames.cpp.
    /// The stored TopInterpMethodContextFrame is only an approximate hint; during dump or native
    /// debugging it may point to a stale frame. This method seeks to the correct top frame using
    /// the Ip field (null = inactive, non-null = active) and the NextPtr/ParentPtr chains.
    /// </summary>
    public TargetPointer ResolveTopInterpMethodContextFrame(Data.InterpreterFrame interpreterFrame)
    {
        TargetPointer hintPtr = interpreterFrame.TopInterpMethodContextFrame;
        if (hintPtr == TargetPointer.Null)
            return TargetPointer.Null;

        Data.InterpMethodContextFrame frame = _target.ProcessedData.GetOrAdd<Data.InterpMethodContextFrame>(hintPtr);
        TargetPointer currentPtr = hintPtr;

        if (frame.Ip != TargetPointer.Null)
        {
            // Active frame — seek upward via NextPtr while next frame is also active
            while (frame.NextPtr != TargetPointer.Null)
            {
                Data.InterpMethodContextFrame next = _target.ProcessedData.GetOrAdd<Data.InterpMethodContextFrame>(frame.NextPtr);
                if (next.Ip == TargetPointer.Null)
                    break;
                currentPtr = frame.NextPtr;
                frame = next;
            }
        }
        else
        {
            // Inactive frame — seek downward via ParentPtr to find first active frame
            while (frame.ParentPtr != TargetPointer.Null && frame.Ip == TargetPointer.Null)
            {
                currentPtr = frame.ParentPtr;
                frame = _target.ProcessedData.GetOrAdd<Data.InterpMethodContextFrame>(currentPtr);
            }
        }

        return currentPtr;
    }

    /// <summary>
    /// Walks the InterpMethodContextFrame chain for an InterpreterFrame,
    /// yielding one context frame pointer per active interpreted method in the call chain.
    /// The TopInterpMethodContextFrame hint is first resolved to the actual top frame
    /// via <see cref="ResolveTopInterpMethodContextFrame"/>, then the ParentPtr chain is walked.
    /// Only active frames (Ip != null) are yielded.
    /// </summary>
    public IEnumerable<TargetPointer> WalkInterpreterFrameChain(TargetPointer frameAddress)
    {
        Data.InterpreterFrame interpFrame = _target.ProcessedData.GetOrAdd<Data.InterpreterFrame>(frameAddress);
        TargetPointer interpMethodFramePtr = ResolveTopInterpMethodContextFrame(interpFrame);
        while (interpMethodFramePtr != TargetPointer.Null)
        {
            Data.InterpMethodContextFrame contextFrame = _target.ProcessedData.GetOrAdd<Data.InterpMethodContextFrame>(interpMethodFramePtr);
            if (contextFrame.Ip != TargetPointer.Null)
                yield return interpMethodFramePtr;
            interpMethodFramePtr = contextFrame.ParentPtr;
        }
    }

    // Matches the Windows CONTEXT_EXCEPTION_ACTIVE flag value. The PAL CONTEXT structures
    // on Linux/macOS use the same bit so a single constant is sufficient across platforms.
    private const uint CONTEXT_EXCEPTION_ACTIVE = 0x8000000;

    /// <summary>
    /// Mirrors native <c>InterpreterFrame::SetContextToInterpMethodContextFrame</c> (frames.cpp).
    /// </summary>
    public void SetContextToInterpMethodContextFrame(
        IPlatformAgnosticContext context,
        Data.InterpreterFrame interpreterFrame)
    {
        TargetPointer topContextFramePtr = ResolveTopInterpMethodContextFrame(interpreterFrame);
        if (topContextFramePtr == TargetPointer.Null)
            return;

        Data.InterpMethodContextFrame topContextFrame = _target.ProcessedData.GetOrAdd<Data.InterpMethodContextFrame>(topContextFramePtr);
        context.InstructionPointer = new TargetPointer((ulong)topContextFrame.Ip);
        context.StackPointer = topContextFramePtr;
        context.FramePointer = topContextFrame.Stack;
        SetFirstArgRegister(context, interpreterFrame.Address);

        uint flags = context.FullContextFlags;
        if (interpreterFrame.IsFaulting)
            flags |= CONTEXT_EXCEPTION_ACTIVE;
        context.RawContextFlags = flags;
    }

    /// <summary>
    /// Performs interpreter virtual unwind, matching the native
    /// <c>VirtualUnwindInterpreterCallFrame</c> in eetwain.cpp.
    ///
    /// When unwinding from a frameless interpreter frame, the SP points to the
    /// current InterpMethodContextFrame. We follow pParent to get to the next
    /// interpreted method in the call chain. If pParent is null, the interpreter
    /// chain under the current InterpreterFrame is exhausted, we apply the
    /// InterpreterFrame's transition-block state to restore the context to the
    /// native caller of InterpExecMethod.
    /// </summary>
    public void InterpreterVirtualUnwind(IPlatformAgnosticContext context)
    {
        if (VirtualUnwindInterpreterCallFrame(context))
            return;

        // No active parent: interpreter chain under this InterpreterFrame is exhausted.
        // The owning InterpreterFrame address lives in the first-argument register --
        // populated by SetContextToInterpMethodContextFrame
        TargetPointer interpreterFrame = GetFirstArgRegister(context);
        if (interpreterFrame != TargetPointer.Null)
        {
            ApplyInterpreterFrameTransition(context, interpreterFrame);
        }
    }

    private bool VirtualUnwindInterpreterCallFrame(IPlatformAgnosticContext context)
    {
        TargetPointer currentFramePtr = context.StackPointer;
        Data.InterpMethodContextFrame currentFrame = _target.ProcessedData.GetOrAdd<Data.InterpMethodContextFrame>(currentFramePtr);

        if (currentFrame.ParentPtr == TargetPointer.Null)
            return false;

        Data.InterpMethodContextFrame parentFrame = _target.ProcessedData.GetOrAdd<Data.InterpMethodContextFrame>(currentFrame.ParentPtr);
        if (parentFrame.Ip == TargetPointer.Null)
            return false;

        context.InstructionPointer = new TargetPointer((ulong)parentFrame.Ip);
        context.StackPointer = currentFrame.ParentPtr;
        context.FramePointer = parentFrame.Stack;
        context.RawContextFlags = context.FullContextFlags;
        return true;
    }

    private void ApplyInterpreterFrameTransition(IPlatformAgnosticContext context, TargetPointer interpreterFrameAddress)
    {
        Data.FramedMethodFrame framedMethodFrame = _target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(interpreterFrameAddress);
        GetFrameHandler(context).HandleTransitionFrame(framedMethodFrame);
    }

    private TargetPointer GetFirstArgRegister(IPlatformAgnosticContext context)
    {
        string registerName = GetFirstArgRegisterName();
        if (!context.TryReadRegister(registerName, out TargetNUInt value))
        {
            throw new InvalidOperationException(
                $"Failed to read first argument register '{registerName}' from the context.");
        }
        return new TargetPointer(value.Value);
    }

    private void SetFirstArgRegister(IPlatformAgnosticContext context, TargetPointer value)
    {
        string registerName = GetFirstArgRegisterName();
        if (!context.TrySetRegister(registerName, new TargetNUInt(value.Value)))
        {
            throw new InvalidOperationException(
                $"Failed to set first argument register '{registerName}' on the context.");
        }
    }

    private string GetFirstArgRegisterName()
    {
        IRuntimeInfo runtimeInfo = _target.Contracts.RuntimeInfo;
        return runtimeInfo.GetTargetArchitecture() switch
        {
            RuntimeInfoArchitecture.X64 =>
                runtimeInfo.GetTargetOperatingSystem() == RuntimeInfoOperatingSystem.Windows ? "rcx" : "rdi",
            RuntimeInfoArchitecture.Arm64 => "x0",
            RuntimeInfoArchitecture.Arm => "r0",
            RuntimeInfoArchitecture.X86 => "ecx",
            RuntimeInfoArchitecture.LoongArch64 => "a0",
            RuntimeInfoArchitecture.RiscV64 => "a0",
            var arch => throw new NotSupportedException(
                $"Unsupported architecture for first argument register: {arch}"),
        };
    }
}
