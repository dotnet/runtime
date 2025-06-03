// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Data;
using static Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.X86Context;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal class X86FrameHandler(Target target, ContextHolder<X86Context> contextHolder) : BaseFrameHandler(target, contextHolder), IPlatformFrameHandler
{
    private readonly ContextHolder<X86Context> _context = contextHolder;

    void IPlatformFrameHandler.HandleFaultingExceptionFrame(FaultingExceptionFrame frame)
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

    void IPlatformFrameHandler.HandleHijackFrame(HijackFrame frame)
    {
        // TODO(cdacX86): Implement handling for HijackFrame
        throw new NotImplementedException();
    }
}
