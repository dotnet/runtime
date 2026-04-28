// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ObjectHandle : IData<ObjectHandle>
{
    static ObjectHandle IData<ObjectHandle>.Create(Target target, TargetPointer address)
        => new ObjectHandle(target, address);

    public ObjectHandle(Target target, TargetPointer address)
    {
        if (address != TargetPointer.Null)
        {
            Handle = target.ReadPointer(address);
            if (Handle != TargetPointer.Null && target.TryReadPointer(Handle, out TargetPointer obj))
                Object = obj;
        }
    }

    public TargetPointer Handle { get; init; } = TargetPointer.Null;
    public TargetPointer Object { get; init; } = TargetPointer.Null;
}
