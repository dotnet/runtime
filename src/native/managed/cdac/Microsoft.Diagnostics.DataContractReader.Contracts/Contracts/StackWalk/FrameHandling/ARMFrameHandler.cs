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

    void IPlatformFrameHandler.HandleFaultingExceptionFrame(FaultingExceptionFrame frame)
    {
        if (frame.TargetContext is not TargetPointer targetContext)
        {
            throw new InvalidOperationException("Unexpected null context pointer on FaultingExceptionFrame");
        }
        _holder.ReadFromAddress(_target, targetContext);
    }

    void IPlatformFrameHandler.HandleHijackFrame(HijackFrame frame)
    {
        // TODO(cdacarm)
        throw new NotImplementedException();
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
