// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

public interface IPlatformContext
{
    uint Size { get; }

    uint SizeWithoutExtendedRegisters => Size;

    uint ExtendedRegistersFlag => 0;

    uint ContextControlFlags { get; }
    uint FullContextFlags { get; }
    uint AllContextFlags { get; }

    int StackPointerRegister { get; }

    TargetPointer StackPointer { get; set; }
    TargetCodePointer InstructionPointer { get; set; }
    TargetPointer FramePointer { get; set; }

    uint RawContextFlags { get; set; }

    void Unwind(Target target);

    /// <summary>
    /// Clears the hardware single-step (trace) flag in the context, if the architecture
    /// supports a hardware single-step flag. Architectures that emulate single-stepping
    /// throw <see cref="System.NotSupportedException"/>.
    /// </summary>
    void UnsetSingleStepFlag();

    bool TrySetRegister(string name, TargetNUInt value);
    bool TryReadRegister(string name, out TargetNUInt value);

    bool TrySetRegister(int number, TargetNUInt value);
    bool TryReadRegister(int number, out TargetNUInt value);
    bool TryReadFloatingPointRegister(ReadOnlySpan<byte> context, int index, out double value);
    bool TryWriteFloatingPointRegister(Span<byte> context, int index, ReadOnlySpan<byte> value);

    (uint Flag, string Name)[] GetScalarRegisters();
    (uint Flag, int Start, int End)[] GetWideSpans();
}
