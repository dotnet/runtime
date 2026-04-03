// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

public interface IPlatformContext
{
    uint Size { get; }
    uint DefaultContextFlags { get; }

    int StackPointerRegister { get; }

    TargetPointer StackPointer { get; set; }
    TargetPointer InstructionPointer { get; set; }
    TargetPointer FramePointer { get; set; }

    void Unwind(Target target);

    bool TrySetRegister(string name, TargetNUInt value);
    bool TryReadRegister(string name, out TargetNUInt value);

    bool TrySetRegister(int number, TargetNUInt value);
    bool TryReadRegister(int number, out TargetNUInt value);
}
