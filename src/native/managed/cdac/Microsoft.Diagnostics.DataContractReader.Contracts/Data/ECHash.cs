// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ECHash : IData<ECHash>
{
    static ECHash IData<ECHash>.Create(Target target, TargetPointer address) => new ECHash(target, address);
    public ECHash(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ECHash);

        Next = target.ReadPointer(address + (ulong)type.Fields[nameof(Next)].Offset);
        Implementation = target.ReadCodePointer(address + (ulong)type.Fields[nameof(Implementation)].Offset);
        MethodDesc = target.ReadPointer(address + (ulong)type.Fields[nameof(MethodDesc)].Offset);
    }

    public TargetPointer Next { get; init; }
    public TargetCodePointer Implementation { get; init; }
    public TargetPointer MethodDesc { get; init; }
}
