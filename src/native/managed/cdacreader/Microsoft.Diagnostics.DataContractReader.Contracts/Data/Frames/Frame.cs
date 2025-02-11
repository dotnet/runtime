// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class Frame : IData<Frame>
{
    static Frame IData<Frame>.Create(Target target, TargetPointer address)
        => new Frame(target, address);

    public Frame(Target target, TargetPointer address)
    {
        Address = address;
        Target.TypeInfo type = target.GetTypeInfo(DataType.Frame);
        Next = target.ReadPointer(address + (ulong)type.Fields[nameof(Next)].Offset);
        VPtr = target.ReadPointer(address);
    }

    public TargetPointer Address { get; init; }
    public TargetPointer VPtr { get; init; }
    public TargetPointer Next { get; init; }
}
