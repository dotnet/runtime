// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Diagnostics.DataContractReader.Data;
using static Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.RISCV64Context;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal class RISCV64FrameHandler(Target target, ContextHolder<RISCV64Context> contextHolder) : BaseFrameHandler(target, contextHolder), IPlatformFrameHandler
{
    private readonly ContextHolder<RISCV64Context> _holder = contextHolder;

    public void HandleHijackFrame(HijackFrame frame)
    {
        HijackArgs args = _target.ProcessedData.GetOrAdd<Data.HijackArgs>(frame.HijackArgsPtr);

        _holder.InstructionPointer = frame.ReturnAddress;

        // The stack pointer is the address immediately following HijackArgs
        uint hijackArgsSize = _target.GetTypeInfo(DataType.HijackArgs).Size ?? throw new InvalidOperationException("HijackArgs size is not set");
        Debug.Assert(hijackArgsSize % 8 == 0, "HijackArgs contains register values and should be a multiple of the pointer size (8 bytes for RISC-V64)");
        // The stack must be multiple of 16. So if hijackArgsSize is not multiple of 16 then there must be padding of 8 bytes
        hijackArgsSize += hijackArgsSize % 16;
        _holder.StackPointer = frame.HijackArgsPtr + hijackArgsSize;

        UpdateFromRegisterDict(args.Registers);
    }
}
