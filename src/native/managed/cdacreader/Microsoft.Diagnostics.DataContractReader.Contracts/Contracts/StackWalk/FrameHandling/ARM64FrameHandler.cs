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
    private void UpdateFromCalleeSavedRegisters(TargetPointer calleeSavedRegistersPtr)
    {
        Data.CalleeSavedRegisters calleeSavedRegisters = _target.ProcessedData.GetOrAdd<Data.CalleeSavedRegisters>(calleeSavedRegistersPtr);
        foreach ((string name, TargetNUInt value) in calleeSavedRegisters.Registers)
        {
            if (!_context.TrySetField(name, value))
            {
                throw new InvalidOperationException($"Unexpected register {name} in callee saved registers");
            }
        }
    }

    private void UpdateCalleeSavedRegistersFromOtherContext(ContextHolder<ARM64Context> otherContext)
    {
        foreach (string name in _target.GetTypeInfo(DataType.CalleeSavedRegisters).Fields.Keys)
        {
            if (!otherContext.TryReadField(name, out TargetNUInt value))
            {
                throw new InvalidOperationException($"Unexpected register {name} in callee saved registers");
            }
            if (!_context.TrySetField(name, value))
            {
                throw new InvalidOperationException($"Unexpected register {name} in callee saved registers");
            }
        }
    }
}
