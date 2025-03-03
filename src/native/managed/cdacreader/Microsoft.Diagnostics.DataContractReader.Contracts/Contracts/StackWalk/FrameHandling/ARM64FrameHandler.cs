// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Data;
using static Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.ARM64Context;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal class ARM64FrameHandler(Target target, ContextHolder<ARM64Context> contextHolder) : IPlatformFrameHandler
{
    private readonly Target _target = target;
    private readonly ContextHolder<ARM64Context> _context = contextHolder;

    bool IPlatformFrameHandler.HandleInlinedCallFrame(InlinedCallFrame inlinedCallFrame)
    {
        // if the caller return address is 0, then the call is not active
        // and we should not update the context
        if (inlinedCallFrame.CallerReturnAddress == 0)
        {
            return false;
        }

        _context.InstructionPointer = inlinedCallFrame.CallerReturnAddress;
        _context.StackPointer = inlinedCallFrame.CallSiteSP;
        _context.FramePointer = inlinedCallFrame.CalleeSavedFP;

        _context.Context.X19 = 0;
        _context.Context.X20 = 0;
        _context.Context.X21 = 0;
        _context.Context.X22 = 0;
        _context.Context.X23 = 0;
        _context.Context.X24 = 0;
        _context.Context.X25 = 0;
        _context.Context.X26 = 0;
        _context.Context.X27 = 0;
        _context.Context.X28 = 0;

        return true;
    }

    bool IPlatformFrameHandler.HandleSoftwareExceptionFrame(SoftwareExceptionFrame softwareExceptionFrame)
    {
        ContextHolder<ARM64Context> otherContextHolder = new();
        otherContextHolder.ReadFromAddress(_target, softwareExceptionFrame.TargetContext);

        UpdateCalleeSavedRegistersFromOtherContext(otherContextHolder);

        _context.InstructionPointer = otherContextHolder.Context.InstructionPointer;
        _context.StackPointer = otherContextHolder.Context.StackPointer;

        return true;
    }

    public bool HandleTransitionFrame(FramedMethodFrame framedMethodFrame, TransitionBlock transitionBlock, uint transitionBlockSize)
    {
        _context.InstructionPointer = transitionBlock.ReturnAddress;
        _context.StackPointer = framedMethodFrame.TransitionBlockPtr + transitionBlockSize;
        UpdateFromCalleeSavedRegisters(transitionBlock.CalleeSavedRegisters);

        return true;
    }

    bool IPlatformFrameHandler.HandleFuncEvalFrame(FuncEvalFrame frame, DebuggerEval debuggerEval)
    {
        // No context to update if we're doing a func eval from within exception processing.
        if (debuggerEval.EvalDuringException)
        {
            return false;
        }
        _context.ReadFromAddress(_target, debuggerEval.TargetContext);
        return true;
    }

    bool IPlatformFrameHandler.HandleFaultingExceptionFrame(FaultingExceptionFrame frame)
    {
        if (frame.TargetContext is not TargetPointer targetContext)
        {
            throw new InvalidOperationException("Unexpected null context pointer on FaultingExceptionFrame");
        }
        _context.ReadFromAddress(_target, targetContext);

        // Clear the CONTEXT_XSTATE, since the AMD64Context contains just plain CONTEXT structure
        // that does not support holding any extended state.
        _context.Context.ContextFlags &= ~(uint)(ContextFlagsValues.CONTEXT_XSTATE & ContextFlagsValues.CONTEXT_AREA_MASK);
        return true;
    }

    bool IPlatformFrameHandler.HandleResumableFrame(ResumableFrame frame)
    {
        _context.ReadFromAddress(_target, frame.TargetContextPtr);
        return true;
    }
    private void UpdateFromCalleeSavedRegisters(TargetPointer calleeSavedRegisters)
    {
        // Order of registers is hardcoded in the runtime. See vm/arm64/cgencpu.h CalleeSavedRegisters
        _context.Context.Fp = _target.ReadPointer(calleeSavedRegisters + (ulong)_target.PointerSize * 0);
        _context.Context.Lr = _target.ReadPointer(calleeSavedRegisters + (ulong)_target.PointerSize * 1);
        _context.Context.X19 = _target.ReadPointer(calleeSavedRegisters + (ulong)_target.PointerSize * 2);
        _context.Context.X20 = _target.ReadPointer(calleeSavedRegisters + (ulong)_target.PointerSize * 3);
        _context.Context.X21 = _target.ReadPointer(calleeSavedRegisters + (ulong)_target.PointerSize * 4);
        _context.Context.X22 = _target.ReadPointer(calleeSavedRegisters + (ulong)_target.PointerSize * 5);
        _context.Context.X23 = _target.ReadPointer(calleeSavedRegisters + (ulong)_target.PointerSize * 6);
        _context.Context.X24 = _target.ReadPointer(calleeSavedRegisters + (ulong)_target.PointerSize * 7);
        _context.Context.X25 = _target.ReadPointer(calleeSavedRegisters + (ulong)_target.PointerSize * 8);
        _context.Context.X26 = _target.ReadPointer(calleeSavedRegisters + (ulong)_target.PointerSize * 9);
        _context.Context.X27 = _target.ReadPointer(calleeSavedRegisters + (ulong)_target.PointerSize * 10);
        _context.Context.X28 = _target.ReadPointer(calleeSavedRegisters + (ulong)_target.PointerSize * 11);
    }

    private void UpdateCalleeSavedRegistersFromOtherContext(ContextHolder<ARM64Context> otherContext)
    {
        _context.Context.Fp = otherContext.Context.Fp;
        _context.Context.Lr = otherContext.Context.Lr;
        _context.Context.X19 = otherContext.Context.X19;
        _context.Context.X20 = otherContext.Context.X20;
        _context.Context.X21 = otherContext.Context.X21;
        _context.Context.X22 = otherContext.Context.X22;
        _context.Context.X23 = otherContext.Context.X23;
        _context.Context.X24 = otherContext.Context.X24;
        _context.Context.X25 = otherContext.Context.X25;
        _context.Context.X26 = otherContext.Context.X26;
        _context.Context.X27 = otherContext.Context.X27;
        _context.Context.X28 = otherContext.Context.X28;
    }
}
