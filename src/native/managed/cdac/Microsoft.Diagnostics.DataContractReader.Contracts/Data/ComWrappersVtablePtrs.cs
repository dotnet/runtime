// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ComWrappersVtablePtrs))]
internal sealed partial class ComWrappersVtablePtrs : IData<ComWrappersVtablePtrs>
{
    public IReadOnlyList<TargetCodePointer> ComWrappersInterfacePointers { get; private set; } = [];

    [MemberNotNull(nameof(ComWrappersInterfacePointers))]
    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ComWrappersVtablePtrs);
        List<TargetCodePointer> pointers = new((int)(type.Size!.Value / (uint)target.PointerSize));
        for (int i = 0; i < type.Size / target.PointerSize; i++)
        {
            pointers.Add(target.ReadCodePointer(address + (ulong)(i * target.PointerSize)));
        }

        ComWrappersInterfacePointers = pointers;
    }
}
