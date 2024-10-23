// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class NativeCodeVersionNode : IData<NativeCodeVersionNode>
{
    static NativeCodeVersionNode IData<NativeCodeVersionNode>.Create(Target target, TargetPointer address) => new NativeCodeVersionNode(target, address);
    public NativeCodeVersionNode(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.NativeCodeVersionNode);

        Next = target.ReadPointer(address + (ulong)type.Fields[nameof(Next)].Offset);
        MethodDesc = target.ReadPointer(address + (ulong)type.Fields[nameof(MethodDesc)].Offset);
        NativeCode = target.ReadCodePointer(address + (ulong)type.Fields[nameof(NativeCode)].Offset);
    }

    public TargetPointer Next { get; init; }
    public TargetPointer MethodDesc { get; init; }

    public TargetCodePointer NativeCode { get; init; }
}
