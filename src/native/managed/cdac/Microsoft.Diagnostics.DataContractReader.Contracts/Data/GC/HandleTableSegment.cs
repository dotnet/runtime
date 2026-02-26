// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class HandleTableSegment : IData<HandleTableSegment>
{
    static HandleTableSegment IData<HandleTableSegment>.Create(Target target, TargetPointer address) => new HandleTableSegment(target, address);
    public HandleTableSegment(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.HandleTableSegment);

        NextSegment = target.ReadPointer(address + (ulong)type.Fields[nameof(NextSegment)].Offset);
    }

    public TargetPointer NextSegment { get; }
}
