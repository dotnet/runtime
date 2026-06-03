// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ReadyToRunCoreHeader))]
internal sealed partial class ReadyToRunCoreHeader : IData<ReadyToRunCoreHeader>
{
    [Field] public uint NumberOfSections { get; }

    public IReadOnlyList<ReadyToRunSection> Sections { get; private set; } = [];

    [MemberNotNull(nameof(Sections))]
    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ReadyToRunCoreHeader);
        Target.TypeInfo sectionType = target.GetTypeInfo(DataType.ReadyToRunSection);
        List<ReadyToRunSection> sections = new((int)NumberOfSections);
        for (int i = 0; i < NumberOfSections; i++)
        {
            TargetPointer sectionAddress = address + (ulong)(type.Size!.Value + i * sectionType.Size!.Value);
            sections.Add(target.ProcessedData.GetOrAdd<ReadyToRunSection>(sectionAddress));
        }

        Sections = sections;
    }
}
