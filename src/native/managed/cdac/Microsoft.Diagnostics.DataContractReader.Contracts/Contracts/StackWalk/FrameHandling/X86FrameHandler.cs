// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Data;
using static Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.X86Context;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal class X86FrameHandler(Target target, ContextHolder<X86Context> contextHolder) : BaseFrameHandler(target, contextHolder), IPlatformFrameHandler
{
    private readonly ContextHolder<X86Context> _context = contextHolder;


    public void HandleFaultingExceptionFrame(FaultingExceptionFrame frame)
    {
        _context.ReadFromAddress(_target, frame.TargetContext);

        // Clear the CONTEXT_XSTATE, since the X86Context contains just plain CONTEXT structure
        // that does not support holding any extended state.
        _context.Context.ContextFlags &= ~(uint)(ContextFlagsValues.CONTEXT_XSTATE & ContextFlagsValues.CONTEXT_AREA_MASK);
    }

    public void HandleHijackFrame(HijackFrame frame)
    {
        HijackArgsX86 args = _target.ProcessedData.GetOrAdd<HijackArgsX86>(frame.HijackArgsPtr);

        // The stack pointer is the address immediately following HijackArgs
        uint hijackArgsSize = _target.GetTypeInfo(DataType.HijackArgs).Size ?? throw new InvalidOperationException("HijackArgs size is not set");
        _context.Context.Esp = (uint)frame.HijackArgsPtr + hijackArgsSize;

        UpdateFromRegisterDict(args.Registers);
    }

    public override void HandleTailCallFrame(TailCallFrame frame)
    {
        _context.Context.Eip = (uint)frame.ReturnAddress;

        // The stack pointer is set to the address immediately after the TailCallFrame structure.
        if (_target.GetTypeInfo(DataType.TailCallFrame).Size is not uint tailCallFrameSize)
        {
            throw new InvalidOperationException("TailCallFrame missing size information");
        }
        _context.Context.Esp = (uint)(frame.Address + tailCallFrameSize);

        CalleeSavedRegisters calleeSavedRegisters = _target.ProcessedData.GetOrAdd<Data.CalleeSavedRegisters>(frame.CalleeSavedRegisters);
        UpdateFromRegisterDict(calleeSavedRegisters.Registers);
    }

    public override void HandleFuncEvalFrame(FuncEvalFrame funcEvalFrame)
    {
        Data.DebuggerEval debuggerEval = _target.ProcessedData.GetOrAdd<Data.DebuggerEval>(funcEvalFrame.DebuggerEvalPtr);

        // No context to update if we're doing a func eval from within exception processing.
        if (debuggerEval.EvalDuringException)
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
