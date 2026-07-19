// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType]
internal sealed partial class ObjectHandle : IData<ObjectHandle>
{
    public TargetPointer Handle { get; private set; } = TargetPointer.Null;
    public TargetPointer Object { get; private set; } = TargetPointer.Null;

    partial void OnInit(Target target, TargetPointer address)
    {
        if (address != TargetPointer.Null)
        {
            Handle = target.ReadPointer(address);
            if (Handle != TargetPointer.Null && target.TryReadPointer(Handle, out TargetPointer obj))
                Object = obj;
        }
    }
}
