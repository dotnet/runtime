// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Data;
using static Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.X86Context;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal class X86FrameHandler(Target target, ContextHolder<X86Context> contextHolder) : BaseFrameHandler(target, contextHolder), IPlatformFrameHandler
{
    internal const uint CallInstructionSize = 5;

    private readonly ContextHolder<X86Context> _context = contextHolder;

    public void HandleHijackFrame(HijackFrame frame)
    {
        HijackArgs args = _target.ProcessedData.GetOrAdd<HijackArgs>(frame.HijackArgsPtr);

        // The stack pointer is the address immediately following HijackArgs
        uint hijackArgsSize = Data.HijackArgs.GetSize(_target);
        _context.Context.Esp = (uint)frame.HijackArgsPtr + hijackArgsSize;

        UpdateFromRegisterDict(args.Registers);
    }

    public override void HandleTransitionFrame(FramedMethodFrame framedMethodFrame)
    {
        // Set IP, SP and callee-saved registers from the transition block (shared logic).
        base.HandleTransitionFrame(framedMethodFrame);

        // x86: the base implementation skips the callee-popped argument byte count
        // (cbStackPop) that the runtime's TransitionFrame::UpdateRegDisplay_Impl adds
        // to CallerSP.
        FrameType frameType = _frameHelpers.GetFrameType(
            _target.ProcessedData.GetOrAdd<Frame>(framedMethodFrame.Address).Identifier);

        GCRefMapDecoder decoder;
        bool hasDecoder;
        switch (frameType)
        {
            case FrameType.ExternalMethodFrame:
                ExternalMethodFrame externalFrame = _target.ProcessedData.GetOrAdd<ExternalMethodFrame>(framedMethodFrame.Address);
                hasDecoder = TryCreateGCRefMapDecoder(externalFrame.Indirection, out decoder) ||
                    TryCreateSignatureGCRefMapDecoder(framedMethodFrame.MethodDescPtr, out decoder);
                break;

            case FrameType.StubDispatchFrame:
                StubDispatchFrame dispatchFrame = _target.ProcessedData.GetOrAdd<StubDispatchFrame>(framedMethodFrame.Address);
                hasDecoder = TryCreateGCRefMapDecoder(dispatchFrame.Indirection, out decoder);
                if (!hasDecoder)
                {
                    TargetPointer methodDescPtr = _frameHelpers.GetMethodDescPtr(framedMethodFrame.Address);
                    if (methodDescPtr == TargetPointer.Null)
                    {
                        // Native uses the call instruction for unwinding failures during stub resolution.
                        _context.Context.Eip -= CallInstructionSize;
                    }
                    else
                    {
                        hasDecoder = TryCreateSignatureGCRefMapDecoder(methodDescPtr, out decoder);
                    }
                }
                break;

            case FrameType.PInvokeCalliFrame:
                PInvokeCalliFrame frame = _target.ProcessedData.GetOrAdd<PInvokeCalliFrame>(framedMethodFrame.Address);
                if (frame.VASigCookiePtr != TargetPointer.Null)
                {
                    VASigCookie cookie = _target.ProcessedData.GetOrAdd<VASigCookie>(frame.VASigCookiePtr);
                    _context.Context.Esp += cookie.SizeOfArgs;
                }
                return;

            default:
                hasDecoder = TryCreateSignatureGCRefMapDecoder(framedMethodFrame.MethodDescPtr, out decoder);
                break;
        }

        // ReadStackPop returns the count in pointer-size units (4 bytes on x86).
        if (hasDecoder)
            _context.Context.Esp += decoder.ReadStackPop() * (uint)_target.PointerSize;
    }

    private bool TryCreateSignatureGCRefMapDecoder(TargetPointer methodDescPtr, out GCRefMapDecoder decoder)
    {
        decoder = default;
        if (methodDescPtr == TargetPointer.Null)
            return false;

        MethodDescHandle md = _target.Contracts.RuntimeTypeSystem.GetMethodDescHandle(methodDescPtr);
        if (!_target.Contracts.CallingConvention.TryComputeArgGCRefMapBlob(md, out byte[] blob) || blob.Length == 0)
            return false;

        decoder = new(blob);
        return true;
    }

    private bool TryCreateGCRefMapDecoder(TargetPointer indirection, out GCRefMapDecoder decoder)
    {
        decoder = default;
        TargetPointer gcRefMap = _frameHelpers.FindGCRefMap(indirection);
        if (gcRefMap == TargetPointer.Null)
            return false;

        decoder = new(_target, gcRefMap);
        return true;
    }

    public override void HandleTailCallFrame(TailCallFrame frame)
    {
        _context.Context.Eip = (uint)frame.ReturnAddress;

        // The stack pointer is set to the address immediately after the TailCallFrame structure.
        uint tailCallFrameSize = Data.TailCallFrame.GetSize(_target);
        _context.Context.Esp = (uint)(frame.Address + tailCallFrameSize);

        CalleeSavedRegisters calleeSavedRegisters = _target.ProcessedData.GetOrAdd<Data.CalleeSavedRegisters>(frame.CalleeSavedRegisters);
        UpdateFromRegisterDict(calleeSavedRegisters.Registers);
    }

    public override void HandleFaultingExceptionFrame(FaultingExceptionFrame frame)
    {
        base.HandleFaultingExceptionFrame(frame);

        // Clear the CONTEXT_XSTATE, since the X86Context contains just plain CONTEXT structure
        // that does not support holding any extended state.
        _context.Context.ContextFlags &= ~(uint)(ContextFlagsValues.CONTEXT_XSTATE & ContextFlagsValues.CONTEXT_AREA_MASK);
    }

    public override void HandleFuncEvalFrame(FuncEvalFrame funcEvalFrame)
    {
        Data.DebuggerEval debuggerEval = _target.ProcessedData.GetOrAdd<Data.DebuggerEval>(funcEvalFrame.DebuggerEvalPtr);

        // No context to update if the eval doesn't use a hijack (exception or interpreter path).
        if (!debuggerEval.EvalUsesHijack)
        {
            return;
        }

        // Unlike other platforms, X86 doesn't copy the entire context
        ContextHolder<X86Context> evalContext = new ContextHolder<X86Context>();
        evalContext.ReadFromAddress(_target, debuggerEval.TargetContext);

        _context.Context.Edi = evalContext.Context.Edi;
        _context.Context.Esi = evalContext.Context.Esi;
        _context.Context.Ebx = evalContext.Context.Ebx;
        _context.Context.Edx = evalContext.Context.Edx;
        _context.Context.Ecx = evalContext.Context.Ecx;
        _context.Context.Eax = evalContext.Context.Eax;
        _context.Context.Ebp = evalContext.Context.Ebp;
        _context.Context.Eip = evalContext.Context.Eip;
        _context.Context.Esp = evalContext.Context.Esp;
    }
}
