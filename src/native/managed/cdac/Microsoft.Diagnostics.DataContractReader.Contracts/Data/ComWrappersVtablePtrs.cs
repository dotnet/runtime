// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ComWrappersVtablePtrs))]
internal sealed partial class ComWrappersVtablePtrs : IData<ComWrappersVtablePtrs>
{
    [CustomInit(nameof(InitComWrappersInterfacePointers))] public partial IReadOnlyList<TargetCodePointer> ComWrappersInterfacePointers { get; }

    private partial IReadOnlyList<TargetCodePointer> InitComWrappersInterfacePointers(Target target, TargetPointer address)
    {
        int count = (int)(GetSize(target) / (uint)target.PointerSize);
        List<TargetCodePointer> pointers = new(count);
        for (int i = 0; i < count; i++)
        {
            pointers.Add(target.ReadCodePointer(address + (ulong)(i * target.PointerSize)));
        }

        return pointers;
    }
}
