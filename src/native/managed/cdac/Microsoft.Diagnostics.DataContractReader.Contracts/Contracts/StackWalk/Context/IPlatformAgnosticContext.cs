// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

public interface IPlatformAgnosticContext
{
    abstract uint Size { get; }
    abstract uint DefaultContextFlags { get; }

    TargetPointer StackPointer { get; set; }
    TargetPointer InstructionPointer { get; set; }
    TargetPointer FramePointer { get; set; }

    uint SPRegisterNumber { get; }
    TargetPointer GetRegisterValue(uint registerNumber);

    abstract void Clear();
    abstract void ReadFromAddress(Target target, TargetPointer address);
    abstract void FillFromBuffer(Span<byte> buffer);
    abstract byte[] GetBytes();
    abstract IPlatformAgnosticContext Clone();
    abstract bool TrySetRegister(Target target, string fieldName, TargetNUInt value);
    abstract bool TryReadRegister(Target target, string fieldName, out TargetNUInt value);
    abstract void Unwind(Target target);

    static IPlatformAgnosticContext GetContextForPlatform(Target target)
    {
        IRuntimeInfo runtimeInfo = target.Contracts.RuntimeInfo;
        return runtimeInfo.GetTargetArchitecture() switch
        {
            RuntimeInfoArchitecture.X86 => new ContextHolder<X86Context>(),
            RuntimeInfoArchitecture.X64 => new ContextHolder<AMD64Context>(),
            RuntimeInfoArchitecture.Arm => new ContextHolder<ARMContext>(),
            RuntimeInfoArchitecture.Arm64 => new ContextHolder<ARM64Context>(),
            RuntimeInfoArchitecture.RiscV64 => new ContextHolder<RISCV64Context>(),
            RuntimeInfoArchitecture.Unknown => throw new InvalidOperationException($"Processor architecture is required for creating a platform specific context and is not provided by the target"),
            _ => throw new InvalidOperationException($"Unsupported architecture {runtimeInfo.GetTargetArchitecture()}"),
        };
    }
}
