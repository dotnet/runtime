// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class CFinalize : IData<CFinalize>
{
    static CFinalize IData<CFinalize>.Create(Target target, TargetPointer address) => new CFinalize(target, address);
    public CFinalize(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.CFinalize);

        FillPointers = address + (ulong)type.Fields[nameof(FillPointers)].Offset;
    }

    public TargetPointer FillPointers { get; }
}
