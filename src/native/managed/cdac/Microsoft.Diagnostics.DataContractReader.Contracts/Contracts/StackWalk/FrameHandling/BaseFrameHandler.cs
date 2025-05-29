// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Abstract base class providing platform agnostic functionality for handling frames.
/// </summary>
internal abstract class BaseFrameHandler(Target target, IPlatformAgnosticContext context)
{
    protected readonly Target _target = target;
    private readonly IPlatformAgnosticContext _context = context;

    public virtual void HandleInlinedCallFrame(InlinedCallFrame inlinedCallFrame)
    {
        // if the caller return address is 0, then the call is not active
        // and we should not update the context
        if (inlinedCallFrame.CallerReturnAddress == 0)
        {
            return;
        }

        _context.InstructionPointer = inlinedCallFrame.CallerReturnAddress;
        _context.StackPointer = inlinedCallFrame.CallSiteSP;
        _context.FramePointer = inlinedCallFrame.CalleeSavedFP;
    }

    public virtual void HandleSoftwareExceptionFrame(SoftwareExceptionFrame softwareExceptionFrame)
    {
        IPlatformAgnosticContext otherContextHolder = IPlatformAgnosticContext.GetContextForPlatform(_target);
        otherContextHolder.ReadFromAddress(_target, softwareExceptionFrame.TargetContext);

        UpdateCalleeSavedRegistersFromOtherContext(otherContextHolder);

        _context.InstructionPointer = otherContextHolder.InstructionPointer;
        _context.StackPointer = otherContextHolder.StackPointer;
    }

    public virtual void HandleTransitionFrame(FramedMethodFrame framedMethodFrame)
    {
        Data.TransitionBlock transitionBlock = _target.ProcessedData.GetOrAdd<Data.TransitionBlock>(framedMethodFrame.TransitionBlockPtr);
        if (_target.GetTypeInfo(DataType.TransitionBlock).Size is not uint transitionBlockSize)
        {
            throw new InvalidOperationException("TransitionBlock size is not set");
        }

        _context.InstructionPointer = transitionBlock.ReturnAddress;
        _context.StackPointer = framedMethodFrame.TransitionBlockPtr + transitionBlockSize;

        Data.CalleeSavedRegisters calleeSavedRegisters = _target.ProcessedData.GetOrAdd<Data.CalleeSavedRegisters>(transitionBlock.CalleeSavedRegisters);
        UpdateFromRegisterDict(calleeSavedRegisters.Registers);
    }

    public virtual void HandleFuncEvalFrame(FuncEvalFrame funcEvalFrame)
    {
        Data.DebuggerEval debuggerEval = _target.ProcessedData.GetOrAdd<Data.DebuggerEval>(funcEvalFrame.DebuggerEvalPtr);

        // No context to update if we're doing a func eval from within exception processing.
        if (debuggerEval.EvalDuringException)
        {
            return;
        }
        _context.ReadFromAddress(_target, debuggerEval.TargetContext);
    }

    public virtual void HandleResumableFrame(ResumableFrame frame)
    {
        _context.ReadFromAddress(_target, frame.TargetContextPtr);
    }

    protected void UpdateFromRegisterDict(IReadOnlyDictionary<string, TargetNUInt> registers)
    {
        foreach ((string name, TargetNUInt value) in registers)
        {
            if (!_context.TrySetRegister(_target, name, value))
            {
                throw new InvalidOperationException($"Unexpected register {name} found when trying to update context");
            }
        }
    }

    protected void UpdateCalleeSavedRegistersFromOtherContext(IPlatformAgnosticContext otherContext)
    {
        foreach (string name in _target.GetTypeInfo(DataType.CalleeSavedRegisters).Fields.Keys)
        {
            if (!otherContext.TryReadRegister(_target, name, out TargetNUInt value))
            {
                throw new InvalidOperationException($"Unexpected register {name} in callee saved registers");
            }
            if (!_context.TrySetRegister(_target, name, value))
            {
                throw new InvalidOperationException($"Unexpected register {name} in callee saved registers");
            }
        }
    }
}
