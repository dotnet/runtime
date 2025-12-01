// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Diagnostics.DataContractReader.Data;
using static Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.ARM64Context;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal class ARM64FrameHandler(Target target, ContextHolder<ARM64Context> contextHolder) : BaseFrameHandler(target, contextHolder), IPlatformFrameHandler
{
    private readonly ContextHolder<ARM64Context> _holder = contextHolder;

    public void HandleHijackFrame(HijackFrame frame)
    {
        HijackArgs args = _target.ProcessedData.GetOrAdd<Data.HijackArgs>(frame.HijackArgsPtr);

        _holder.InstructionPointer = frame.ReturnAddress;

        // The stack pointer is the address immediately following HijackArgs
        uint hijackArgsSize = _target.GetTypeInfo(DataType.HijackArgs).Size ?? throw new InvalidOperationException("HijackArgs size is not set");
        Debug.Assert(hijackArgsSize % 8 == 0, "HijackArgs contains register values and should be a multiple of the pointer size (8 bytes for ARM64)");
        // The stack must be multiple of 16. So if hijackArgsSize is not multiple of 16 then there must be padding of 8 bytes
        hijackArgsSize += hijackArgsSize % 16;
        _holder.StackPointer = frame.HijackArgsPtr + hijackArgsSize;

        UpdateFromRegisterDict(args.Registers);
    }

    public override void HandleFaultingExceptionFrame(FaultingExceptionFrame frame)
    {
        base.HandleFaultingExceptionFrame(frame);

        // Clear the CONTEXT_XSTATE, since the ARM64Context contains just plain CONTEXT structure
        // that does not support holding any extended state.
        _holder.Context.ContextFlags &= ~(uint)(ContextFlagsValues.CONTEXT_XSTATE & ContextFlagsValues.CONTEXT_AREA_MASK);
    }
}
