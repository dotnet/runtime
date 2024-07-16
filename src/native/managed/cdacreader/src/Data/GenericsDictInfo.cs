// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class GenericsDictInfo : IData<GenericsDictInfo>
{
    static GenericsDictInfo IData<GenericsDictInfo>.Create(Target target, TargetPointer address) => new GenericsDictInfo(target, address);
    public GenericsDictInfo(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.GenericsDictInfo);

        NumTypeArgs = target.Read<ushort>(address + (ulong)type.Fields[nameof(NumTypeArgs)].Offset);
    }

    public ushort NumTypeArgs { get; init; }
}
