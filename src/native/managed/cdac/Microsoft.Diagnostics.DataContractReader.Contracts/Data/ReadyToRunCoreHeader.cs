// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ReadyToRunCoreHeader : IData<ReadyToRunCoreHeader>
{
    static ReadyToRunCoreHeader IData<ReadyToRunCoreHeader>.Create(Target target, TargetPointer address)
        => new ReadyToRunCoreHeader(target, address);

    public ReadyToRunCoreHeader(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ReadyToRunCoreHeader);

        NumberOfSections = target.Read<uint>(address + (ulong)type.Fields[nameof(NumberOfSections)].Offset);
        Target.TypeInfo sectionType = target.GetTypeInfo(DataType.ReadyToRunSection);
        for (int i = 0; i < NumberOfSections; i++)
        {
            TargetPointer sectionAddress = address + (ulong)(type.Size!.Value + i * sectionType.Size!.Value);
            Sections.Add(target.ProcessedData.GetOrAdd<ReadyToRunSection>(sectionAddress));
        }
    }

    public uint NumberOfSections { get; init; }
    public List<ReadyToRunSection> Sections { get; } = [];
}
