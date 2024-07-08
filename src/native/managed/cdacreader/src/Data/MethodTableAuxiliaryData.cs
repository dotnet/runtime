// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class MethodTableAuxiliaryData : IData<MethodTableAuxiliaryData>
{
    static MethodTableAuxiliaryData IData<MethodTableAuxiliaryData>.Create(Target target, TargetPointer address) => new MethodTableAuxiliaryData(target, address);

    private MethodTableAuxiliaryData(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.MethodTableAuxiliaryData);

        AuxFlags = target.Read<uint>(address + (ulong)type.Fields[nameof(AuxFlags)].Offset);

    }

    public uint AuxFlags { get; init; }
}
