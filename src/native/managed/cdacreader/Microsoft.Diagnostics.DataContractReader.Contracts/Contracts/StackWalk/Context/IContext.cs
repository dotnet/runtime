// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

public interface IPlatformContext
{
    public abstract uint Size { get; }
    public abstract uint DefaultContextFlags { get; }

    public TargetPointer StackPointer { get; set; }
    public TargetPointer InstructionPointer { get; set; }
    public TargetPointer FramePointer { get; set; }
    public abstract void Unwind(Target target);
}
