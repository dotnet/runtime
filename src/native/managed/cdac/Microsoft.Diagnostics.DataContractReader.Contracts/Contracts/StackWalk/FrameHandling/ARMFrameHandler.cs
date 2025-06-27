// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Diagnostics.DataContractReader.Data;
using static Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.ARMContext;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal class ARMFrameHandler(Target target, ContextHolder<ARMContext> contextHolder) : BaseFrameHandler(target, contextHolder), IPlatformFrameHandler
{
    private readonly ContextHolder<ARMContext> _holder = contextHolder;

    public void HandleHijackFrame(HijackFrame frame)
    {
        HijackArgs args = _target.ProcessedData.GetOrAdd<Data.HijackArgs>(frame.HijackArgsPtr);

        _holder.InstructionPointer = frame.ReturnAddress;

        // The stack pointer is the address immediately following HijackArgs
        uint hijackArgsSize = _target.GetTypeInfo(DataType.HijackArgs).Size ?? throw new InvalidOperationException("HijackArgs size is not set");
        Debug.Assert(hijackArgsSize % 4 == 0, "HijackArgs contains register values and should be a multiple of the pointer size (4 bytes for ARM)");
        // The stack must be multiple of 8. So if hijackArgsSize is not multiple of 8 then there must be padding of 4 bytes
        hijackArgsSize += hijackArgsSize % 8;
        _holder.StackPointer = frame.HijackArgsPtr + hijackArgsSize;

        UpdateFromRegisterDict(args.Registers);
    }

    public override void HandleInlinedCallFrame(InlinedCallFrame inlinedCallFrame)
    {
        base.HandleInlinedCallFrame(inlinedCallFrame);

        // On ARM, the InlinedCallFrame stores the value of the SP after the prolog
        // to allow unwinding for functions with stackalloc. When a function uses
        // stackalloc, the CallSiteSP can already have been adjusted.

        if (inlinedCallFrame.SPAfterProlog is not TargetPointer spAfterProlog)
        {
            throw new InvalidOperationException("ARM InlinedCallFrame does not have SPAfterProlog set");
        }

        _holder.Context.R9 = (uint)spAfterProlog;
    }

    public override void HandleTransitionFrame(FramedMethodFrame framedMethodFrame)
    {
        // Call the base method to handle common logic
        base.HandleTransitionFrame(framedMethodFrame);

        Data.TransitionBlock transitionBlock = _target.ProcessedData.GetOrAdd<Data.TransitionBlock>(framedMethodFrame.TransitionBlockPtr);

        if (transitionBlock.ArgumentRegisters is not TargetPointer argumentRegistersPtr)
        {
            throw new InvalidOperationException("ARM TransitionBlock does not have ArgumentRegisters set");
        }

        // On ARM, TransitionFrames update the argument registers
        Data.ArgumentRegisters argumentRegisters = _target.ProcessedData.GetOrAdd<Data.ArgumentRegisters>(argumentRegistersPtr);
        UpdateFromRegisterDict(argumentRegisters.Registers);
    }
}
