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
        if (frame.TargetContext is not TargetPointer targetContext)
        {
            throw new InvalidOperationException("Unexpected null context pointer on FaultingExceptionFrame");
        }
        _context.ReadFromAddress(_target, targetContext);

        // Clear the CONTEXT_XSTATE, since the X86Context contains just plain CONTEXT structure
        // that does not support holding any extended state.
        _context.Context.ContextFlags &= ~(uint)(ContextFlagsValues.CONTEXT_XSTATE & ContextFlagsValues.CONTEXT_AREA_MASK);
    }

    public void HandleHijackFrame(HijackFrame frame)
    {
        // TODO(cdacX86): Implement handling for HijackFrame
        throw new NotImplementedException();
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
