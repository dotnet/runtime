// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class DynamicMetadata : IData<DynamicMetadata>
{
    static DynamicMetadata IData<DynamicMetadata>.Create(Target target, TargetPointer address) => new DynamicMetadata(target, address);
    public DynamicMetadata(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.DynamicMetadata);

        Size = target.Read<uint>(address + (ulong)type.Fields[nameof(Size)].Offset);
        Data = address + (ulong)type.Fields[nameof(Data)].Offset;
    }

    public uint Size { get; init; }
    public TargetPointer Data { get; init; }
}
