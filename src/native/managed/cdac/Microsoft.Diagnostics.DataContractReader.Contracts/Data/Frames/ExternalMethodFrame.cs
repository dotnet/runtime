// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class ExternalMethodFrame : IData<ExternalMethodFrame>
{
    static ExternalMethodFrame IData<ExternalMethodFrame>.Create(Target target, TargetPointer address)
        => new ExternalMethodFrame(target, address);

    public ExternalMethodFrame(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ExternalMethodFrame);
        GCRefMap = target.ReadPointer(address + (ulong)type.Fields[nameof(GCRefMap)].Offset);
    }

    public TargetPointer GCRefMap { get; }
}
