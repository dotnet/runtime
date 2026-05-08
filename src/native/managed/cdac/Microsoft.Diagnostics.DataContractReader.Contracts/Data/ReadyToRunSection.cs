// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ReadyToRunSection : IData<ReadyToRunSection>
{
    static ReadyToRunSection IData<ReadyToRunSection>.Create(Target target, TargetPointer address)
        => new ReadyToRunSection(target, address);

    public ReadyToRunSection(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ReadyToRunSection);

        Type = target.ReadField<uint>(address, type, nameof(Type));
        Section = target.ProcessedData.GetOrAdd<ImageDataDirectory>(address + (ulong)type.Fields[nameof(Section)].Offset);
    }

    public uint Type { get; init; }
    public ImageDataDirectory Section { get; init; }
}
