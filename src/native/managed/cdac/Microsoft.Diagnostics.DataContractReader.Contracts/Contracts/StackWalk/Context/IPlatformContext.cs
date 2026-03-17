// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

public interface IPlatformContext
{
    uint Size { get; }
    uint DefaultContextFlags { get; }

    TargetPointer StackPointer { get; set; }
    TargetPointer InstructionPointer { get; set; }
    TargetPointer FramePointer { get; set; }

    void Unwind(Target target);

    bool TrySetRegister(string name, TargetNUInt value);
    bool TryReadRegister(string name, out TargetNUInt value);
    bool TryGetRegisterName(int number, [NotNullWhen(true)] out string? name);

    bool TrySetRegister(int number, TargetNUInt value)
    {
        if (!TryGetRegisterName(number, out string? name))
            return false;
        return TrySetRegister(name, value);
    }

    bool TryReadRegister(int number, out TargetNUInt value)
    {
        value = default;
        return TryGetRegisterName(number, out string? name) && TryReadRegister(name, out value);
    }
}
