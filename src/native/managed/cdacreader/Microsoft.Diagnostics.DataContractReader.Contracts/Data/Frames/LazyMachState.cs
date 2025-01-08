// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class LazyMachState : IData<LazyMachState>
{
    static LazyMachState IData<LazyMachState>.Create(Target target, TargetPointer address)
        => new LazyMachState(target, address);

    public LazyMachState(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.LazyMachState);
        InstructionPointer = target.ReadPointer(address + (ulong)type.Fields[nameof(InstructionPointer)].Offset);
        StackPointer = target.ReadPointer(address + (ulong)type.Fields[nameof(StackPointer)].Offset);
        ReturnAddress = target.ReadPointer(address + (ulong)type.Fields[nameof(ReturnAddress)].Offset);
    }

    public TargetPointer InstructionPointer { get; }
    public TargetPointer StackPointer { get; }
    public TargetPointer ReturnAddress { get; }
}
