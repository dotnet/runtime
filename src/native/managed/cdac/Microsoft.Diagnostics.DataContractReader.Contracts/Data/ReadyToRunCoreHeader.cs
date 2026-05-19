// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ReadyToRunCoreHeader))]
internal sealed partial class ReadyToRunCoreHeader : IData<ReadyToRunCoreHeader>
{
    [Field] public uint NumberOfSections { get; }

    public List<ReadyToRunSection> Sections { get; } = [];

    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ReadyToRunCoreHeader);
        Target.TypeInfo sectionType = target.GetTypeInfo(DataType.ReadyToRunSection);
        for (int i = 0; i < NumberOfSections; i++)
        {
            TargetPointer sectionAddress = address + (ulong)(type.Size!.Value + i * sectionType.Size!.Value);
            Sections.Add(target.ProcessedData.GetOrAdd<ReadyToRunSection>(sectionAddress));
        }
    }
}
