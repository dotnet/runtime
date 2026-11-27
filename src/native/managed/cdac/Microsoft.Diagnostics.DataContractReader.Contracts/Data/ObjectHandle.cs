// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType]
internal sealed partial class ObjectHandle : IData<ObjectHandle>
{
    [CustomInit(nameof(InitHandle))] public partial TargetPointer Handle { get; }
    [CustomInit(nameof(InitObject))] public partial TargetPointer Object { get; }

    private partial TargetPointer InitHandle(Target target, TargetPointer address)
    {
        return address != TargetPointer.Null ? target.ReadPointer(address) : TargetPointer.Null;
    }

    private partial TargetPointer InitObject(Target target, TargetPointer address)
    {
        return Handle != TargetPointer.Null && target.TryReadPointer(Handle, out TargetPointer obj)
            ? obj
            : TargetPointer.Null;
    }
}
