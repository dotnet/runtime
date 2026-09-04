// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ReadyToRunCoreHeader))]
internal sealed partial class ReadyToRunCoreHeader : IData<ReadyToRunCoreHeader>
{
    [Field] public partial uint NumberOfSections { get; }
    [CustomInit(nameof(InitSections))] public partial IReadOnlyList<ReadyToRunSection> Sections { get; }

    private partial IReadOnlyList<ReadyToRunSection> InitSections(Target target, TargetPointer address)
    {
        uint headerSize = GetSize(target);
        uint sectionSize = ReadyToRunSection.GetSize(target);
        List<ReadyToRunSection> sections = new((int)NumberOfSections);
        for (int i = 0; i < NumberOfSections; i++)
        {
            TargetPointer sectionAddress = address + headerSize + (ulong)i * sectionSize;
            sections.Add(target.ProcessedData.GetOrAdd<ReadyToRunSection>(sectionAddress));
        }

        return sections;
    }
}
