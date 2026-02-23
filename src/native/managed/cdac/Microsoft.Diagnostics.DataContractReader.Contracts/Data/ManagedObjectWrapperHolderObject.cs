// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ManagedObjectWrapperHolderObject : IData<ManagedObjectWrapperHolderObject>
{
    static ManagedObjectWrapperHolderObject IData<ManagedObjectWrapperHolderObject>.Create(Target target, TargetPointer address) => new ManagedObjectWrapperHolderObject(target, address);
    public ManagedObjectWrapperHolderObject(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ManagedObjectWrapperHolderObject);
        WrappedObject = target.ReadPointer(address + (ulong)type.Fields[nameof(WrappedObject)].Offset);
    }

    public TargetPointer WrappedObject { get; init; }
}
