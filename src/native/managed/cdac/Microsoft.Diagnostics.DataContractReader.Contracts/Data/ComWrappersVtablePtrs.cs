// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ComWrappersVtablePtrs : IData<ComWrappersVtablePtrs>
{
    static ComWrappersVtablePtrs IData<ComWrappersVtablePtrs>.Create(Target target, TargetPointer address) => new ComWrappersVtablePtrs(target, address);
    public ComWrappersVtablePtrs(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ComWrappersVtablePtrs);
        for (int i = 0; i < type.Size / target.PointerSize; i++)
        {
            ComWrappersInterfacePointers.Add(target.ReadCodePointer(address + (ulong)(i * target.PointerSize)));
        }
    }

    public List<TargetCodePointer> ComWrappersInterfacePointers { get; init; } = new List<TargetCodePointer>();
}
