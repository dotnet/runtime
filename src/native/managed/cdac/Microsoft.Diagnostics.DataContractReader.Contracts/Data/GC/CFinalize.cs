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

        uint fillPointersLength = target.ReadGlobal<uint>(Constants.Globals.CFinalizeFillPointersLength);
        TargetPointer fillPointersArrayStart = address + (ulong)type.Fields[nameof(FillPointers)].Offset;

        TargetPointer[] fillPointers = new TargetPointer[fillPointersLength];
        for (uint i = 0; i < fillPointersLength; i++)
            fillPointers[i] = target.ReadPointer(fillPointersArrayStart + i * (ulong)target.PointerSize);
        FillPointers = fillPointers.AsReadOnly();
    }

    public IReadOnlyList<TargetPointer> FillPointers { get; }
}
