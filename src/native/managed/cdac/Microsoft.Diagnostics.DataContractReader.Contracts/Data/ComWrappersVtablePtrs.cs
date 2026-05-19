// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Generated;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ComWrappersVtablePtrs))]
internal sealed partial class ComWrappersVtablePtrs : IData<ComWrappersVtablePtrs>
{
    public List<TargetCodePointer> ComWrappersInterfacePointers { get; } = new();

    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ComWrappersVtablePtrs);
        for (int i = 0; i < type.Size / target.PointerSize; i++)
        {
            ComWrappersInterfacePointers.Add(target.ReadCodePointer(address + (ulong)(i * target.PointerSize)));
        }
    }
}
