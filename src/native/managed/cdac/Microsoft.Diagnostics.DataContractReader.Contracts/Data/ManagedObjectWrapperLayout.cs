// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ManagedObjectWrapperLayout : IData<ManagedObjectWrapperLayout>
{
    static ManagedObjectWrapperLayout IData<ManagedObjectWrapperLayout>.Create(Target target, TargetPointer address) => new ManagedObjectWrapperLayout(target, address);
    public ManagedObjectWrapperLayout(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ManagedObjectWrapperLayout);
        RefCount = target.Read<long>(address + (ulong)type.Fields[nameof(RefCount)].Offset);
        Flags = target.Read<int>(address + (ulong)type.Fields[nameof(Flags)].Offset);
        UserDefinedCount = target.Read<int>(address + (ulong)type.Fields[nameof(UserDefinedCount)].Offset);
        UserDefined = target.ReadPointer(address + (ulong)type.Fields[nameof(UserDefined)].Offset);
        Dispatches = target.ReadPointer(address + (ulong)type.Fields[nameof(Dispatches)].Offset);
    }

    public long RefCount { get; init; }
    public int Flags { get; init; }
    public int UserDefinedCount { get; init; }
    public TargetPointer UserDefined { get; init; }
    public TargetPointer Dispatches { get; init; }
}
