// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Data;
using static Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.AMD64Context;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal class AMD64FrameHandler(Target target, ContextHolder<AMD64Context> contextHolder) : IPlatformFrameHandler
{
    private readonly Target _target = target;
    private readonly ContextHolder<AMD64Context> _holder = contextHolder;

    bool IPlatformFrameHandler.HandleInlinedCallFrame(InlinedCallFrame inlinedCallFrame)
    {
        // if the caller return address is 0, then the call is not active
        // and we should not update the context
        if (inlinedCallFrame.CallerReturnAddress == 0)
        {
            return false;
        }

        _holder.InstructionPointer = inlinedCallFrame.CallerReturnAddress;
        _holder.StackPointer = inlinedCallFrame.CallSiteSP;
        _holder.FramePointer = inlinedCallFrame.CalleeSavedFP;

        return true;
    }

    bool IPlatformFrameHandler.HandleSoftwareExceptionFrame(SoftwareExceptionFrame softwareExceptionFrame)
    {
        ContextHolder<AMD64Context> otherContextHolder = new();
        otherContextHolder.ReadFromAddress(_target, softwareExceptionFrame.TargetContext);

        UpdateCalleeSavedRegistersFromOtherContext(otherContextHolder);

        _holder.InstructionPointer = otherContextHolder.Context.InstructionPointer;
        _holder.StackPointer = otherContextHolder.Context.StackPointer;

        return true;
    }

    bool IPlatformFrameHandler.HandleTransitionFrame(FramedMethodFrame framedMethodFrame, TransitionBlock transitionBlock, uint transitionBlockSize)
    {
        _holder.InstructionPointer = transitionBlock.ReturnAddress;
        _holder.StackPointer = framedMethodFrame.TransitionBlockPtr + transitionBlockSize;
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
        _holder.ReadFromAddress(_target, debuggerEval.TargetContext);
        return true;
    }

    bool IPlatformFrameHandler.HandleResumableFrame(ResumableFrame frame)
    {
        _holder.ReadFromAddress(_target, frame.TargetContextPtr);
        return true;
    }

    bool IPlatformFrameHandler.HandleFaultingExceptionFrame(FaultingExceptionFrame frame)
    {
        if (frame.TargetContext is not TargetPointer targetContext)
        {
            throw new InvalidOperationException("Unexpected null context pointer on FaultingExceptionFrame");
        }
        _holder.ReadFromAddress(_target, targetContext);

        // Clear the CONTEXT_XSTATE, since the AMD64Context contains just plain CONTEXT structure
        // that does not support holding any extended state.
        _holder.Context.ContextFlags &= ~(uint)(ContextFlagsValues.CONTEXT_XSTATE & ContextFlagsValues.CONTEXT_AREA_MASK);
        return true;
    }

    bool IPlatformFrameHandler.HandleHijackFrame(HijackFrame frame)
    {
        HijackArgsAMD64 args = _target.ProcessedData.GetOrAdd<Data.HijackArgsAMD64>(frame.HijackArgsPtr);

        _holder.InstructionPointer = frame.ReturnAddress;
        if (args.Rsp is TargetPointer rsp)
        {
            // Windows case, Rsp is passed directly
            _holder.StackPointer = rsp;
        }
        else
        {
            // Non-Windows case, the stack pointer is the address immediately following HijacksArgs
            uint hijackArgsSize = _target.GetTypeInfo(DataType.HijackArgs).Size ?? throw new InvalidOperationException("HijackArgs size is not set");
            _holder.StackPointer = frame.HijackArgsPtr + hijackArgsSize;
        }

        UpdateFromCalleeSavedRegisters(args.CalleeSavedRegisters);

        return true;
    }

    private void UpdateFromCalleeSavedRegisters(TargetPointer calleeSavedRegistersPtr)
    {
        Data.CalleeSavedRegisters calleeSavedRegisters = _target.ProcessedData.GetOrAdd<Data.CalleeSavedRegisters>(calleeSavedRegistersPtr);
        foreach ((string name, TargetNUInt value) in calleeSavedRegisters.Registers)
        {
            if (!_holder.TrySetRegister(_target, name, value))
            {
                throw new InvalidOperationException($"Unexpected register {name} in callee saved registers");
            }
        }
    }

    private void UpdateCalleeSavedRegistersFromOtherContext(ContextHolder<AMD64Context> otherContext)
    {
        foreach (string name in _target.GetTypeInfo(DataType.CalleeSavedRegisters).Fields.Keys)
        {
            if (!otherContext.TryReadRegister(_target, name, out TargetNUInt value))
            {
                throw new InvalidOperationException($"Unexpected register {name} in callee saved registers");
            }
            if (!_holder.TrySetRegister(_target, name, value))
            {
                throw new InvalidOperationException($"Unexpected register {name} in callee saved registers");
            }

        }
    }
}
