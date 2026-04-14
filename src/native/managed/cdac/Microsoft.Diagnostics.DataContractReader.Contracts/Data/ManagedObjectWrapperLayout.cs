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
        RefCount = target.ReadField<long>(address, type, nameof(RefCount));
        Flags = target.ReadField<int>(address, type, nameof(Flags));
        UserDefinedCount = target.ReadField<int>(address, type, nameof(UserDefinedCount));
        UserDefined = target.ReadPointerField(address, type, nameof(UserDefined));
        Dispatches = target.ReadPointerField(address, type, nameof(Dispatches));
    }

    public long RefCount { get; init; }
    public int Flags { get; init; }
    public int UserDefinedCount { get; init; }
    public TargetPointer UserDefined { get; init; }
    public TargetPointer Dispatches { get; init; }
}
