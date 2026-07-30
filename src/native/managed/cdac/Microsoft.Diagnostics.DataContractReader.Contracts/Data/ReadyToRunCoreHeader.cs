// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ReadyToRunCoreHeader))]
internal sealed partial class ReadyToRunCoreHeader : IData<ReadyToRunCoreHeader>
{
    [Field] public partial uint NumberOfSections { get; }
    public IReadOnlyList<ReadyToRunSection> Sections { get; private set; } = [];

    [MemberNotNull(nameof(Sections))]
    partial void OnInit(Target target, TargetPointer address)
    {
        uint headerSize = GetSize(target);
        uint sectionSize = ReadyToRunSection.GetSize(target);
        List<ReadyToRunSection> sections = new((int)NumberOfSections);
        for (int i = 0; i < NumberOfSections; i++)
        {
            TargetPointer sectionAddress = address + headerSize + (ulong)i * sectionSize;
            sections.Add(target.ProcessedData.GetOrAdd<ReadyToRunSection>(sectionAddress));
        }

        Sections = sections;
    }
}
