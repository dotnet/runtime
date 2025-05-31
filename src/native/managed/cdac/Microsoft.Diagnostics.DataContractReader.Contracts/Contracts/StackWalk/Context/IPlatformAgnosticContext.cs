// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

public interface IPlatformAgnosticContext
{
    public abstract uint Size { get; }
    public abstract uint DefaultContextFlags { get; }

    public TargetPointer StackPointer { get; set; }
    public TargetPointer InstructionPointer { get; set; }
    public TargetPointer FramePointer { get; set; }

    public abstract void Clear();
    public abstract void ReadFromAddress(Target target, TargetPointer address);
    public abstract void FillFromBuffer(Span<byte> buffer);
    public abstract byte[] GetBytes();
    public abstract IPlatformAgnosticContext Clone();
    public abstract bool TrySetRegister(Target target, string fieldName, TargetNUInt value);
    public abstract bool TryReadRegister(Target target, string fieldName, out TargetNUInt value);
    public abstract void Unwind(Target target);

    public static IPlatformAgnosticContext GetContextForPlatform(Target target)
    {
        IRuntimeInfo runtimeInfo = target.Contracts.RuntimeInfo;
        return runtimeInfo.GetTargetArchitecture() switch
        {
            RuntimeInfoArchitecture.X86 => new ContextHolder<X86Context>(),
            RuntimeInfoArchitecture.X64 => new ContextHolder<AMD64Context>(),
            RuntimeInfoArchitecture.Arm64 => new ContextHolder<ARM64Context>(),
            RuntimeInfoArchitecture.Unknown => throw new InvalidOperationException($"Processor architecture is required for creating a platform specific context and is not provided by the target"),
            _ => throw new InvalidOperationException($"Unsupported architecture {runtimeInfo.GetTargetArchitecture()}"),
        };
    }
}
