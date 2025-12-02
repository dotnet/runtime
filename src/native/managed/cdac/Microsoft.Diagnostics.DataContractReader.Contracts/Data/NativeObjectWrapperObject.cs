// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class NativeObjectWrapperObject : IData<NativeObjectWrapperObject>
{
    static NativeObjectWrapperObject IData<NativeObjectWrapperObject>.Create(Target target, TargetPointer address) => new NativeObjectWrapperObject(target, address);
    public NativeObjectWrapperObject(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.NativeObjectWrapperObject);
        ExternalComObject = target.ReadPointer(address + (ulong)type.Fields[nameof(ExternalComObject)].Offset);
    }

    public TargetPointer ExternalComObject { get; init; }
}
