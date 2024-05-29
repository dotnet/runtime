// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class MethodTable
{
    public MethodTable(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.MethodTable);

        //Id = target.Read<uint>(address + (ulong)type.Fields[nameof(Id)].Offset);
        //LinkNext = target.ReadPointer(address + (ulong)type.Fields[nameof(LinkNext)].Offset);
    }

    public uint DwFlags2 => throw new NotImplementedException();
}
