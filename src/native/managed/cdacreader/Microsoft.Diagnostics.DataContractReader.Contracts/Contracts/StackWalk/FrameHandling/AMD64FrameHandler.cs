// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Data;
using static Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.AMD64Context;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal class AMD64FrameHandler(Target target, ContextHolder<AMD64Context> contextHolder) : BaseFrameHandler(target, contextHolder), IPlatformFrameHandler
{
    private readonly ContextHolder<AMD64Context> _holder = contextHolder;

    void IPlatformFrameHandler.HandleFaultingExceptionFrame(FaultingExceptionFrame frame)
    {
        if (frame.TargetContext is not TargetPointer targetContext)
        {
            throw new InvalidOperationException("Unexpected null context pointer on FaultingExceptionFrame");
        }
        _holder.ReadFromAddress(_target, targetContext);

        // Clear the CONTEXT_XSTATE, since the AMD64Context contains just plain CONTEXT structure
        // that does not support holding any extended state.
        _holder.Context.ContextFlags &= ~(uint)(ContextFlagsValues.CONTEXT_XSTATE & ContextFlagsValues.CONTEXT_AREA_MASK);
    }

    void IPlatformFrameHandler.HandleHijackFrame(HijackFrame frame)
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

        Data.CalleeSavedRegisters calleeSavedRegisters = _target.ProcessedData.GetOrAdd<Data.CalleeSavedRegisters>(args.CalleeSavedRegisters);
        UpdateFromRegisterDict(calleeSavedRegisters.Registers);
    }
}
