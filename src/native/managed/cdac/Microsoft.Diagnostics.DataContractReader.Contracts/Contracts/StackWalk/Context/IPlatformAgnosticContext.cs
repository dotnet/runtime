// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

public interface IPlatformAgnosticContext
{
    public abstract uint Size { get; }
    public abstract uint ContextControlFlags { get; }
    public abstract uint FullContextFlags { get; }
    public abstract uint AllContextFlags { get; }

    public int StackPointerRegister { get; }

    public TargetPointer StackPointer { get; set; }
    public TargetCodePointer InstructionPointer { get; set; }
    public TargetPointer FramePointer { get; set; }

    public uint RawContextFlags { get; set; }

    public abstract void Clear();
    public abstract void ReadFromAddress(Target target, TargetPointer address);
    public abstract void FillFromBuffer(Span<byte> buffer);
    public abstract byte[] GetBytes();
    public abstract IPlatformAgnosticContext Clone();
    public abstract bool TrySetRegister(string fieldName, TargetNUInt value);
    public abstract bool TryReadRegister(string fieldName, out TargetNUInt value);
    public abstract bool TrySetRegister(int number, TargetNUInt value);
    public abstract bool TryReadRegister(int number, out TargetNUInt value);

    public abstract void Unwind(Target target);

    /// <summary>
    /// Clears the hardware single-step (trace) flag in the context, if the architecture
    /// supports a hardware single-step flag. Architectures that emulate single-stepping
    /// throw <see cref="NotSupportedException"/>.
    /// </summary>
    public abstract void UnsetSingleStepFlag();

    public static IPlatformAgnosticContext GetContextForPlatform(Target target)
    {
        IRuntimeInfo runtimeInfo = target.Contracts.RuntimeInfo;
        RuntimeInfoArchitecture architecture = runtimeInfo.GetTargetArchitecture();

        if (TargetArchitectureSupport.IsX86Supported && architecture == RuntimeInfoArchitecture.X86)
            return new ContextHolder<X86Context>();

        if (TargetArchitectureSupport.IsX64Supported && architecture == RuntimeInfoArchitecture.X64)
            return new ContextHolder<AMD64Context>();

        if (TargetArchitectureSupport.IsArmSupported && architecture == RuntimeInfoArchitecture.Arm)
            return new ContextHolder<ARMContext>();

        if (TargetArchitectureSupport.IsArm64Supported && architecture == RuntimeInfoArchitecture.Arm64)
            return new ContextHolder<ARM64Context>();

        if (TargetArchitectureSupport.IsLoongArch64Supported && architecture == RuntimeInfoArchitecture.LoongArch64)
            return new ContextHolder<LoongArch64Context>();

        if (TargetArchitectureSupport.IsRiscV64Supported && architecture == RuntimeInfoArchitecture.RiscV64)
            return new ContextHolder<RISCV64Context>();

        if (TargetArchitectureSupport.IsWasmSupported && architecture == RuntimeInfoArchitecture.Wasm)
            return new ContextHolder<WasmContext>();

        throw new InvalidOperationException(
            architecture == RuntimeInfoArchitecture.Unknown
                ? "Processor architecture is required for creating a platform specific context and is not provided by the target"
                : $"Unsupported architecture {architecture}");
    }
}
