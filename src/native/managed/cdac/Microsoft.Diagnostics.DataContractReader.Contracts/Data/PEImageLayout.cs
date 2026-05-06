// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class PEImageLayout : IData<PEImageLayout>
{
    static PEImageLayout IData<PEImageLayout>.Create(Target target, TargetPointer address) => new PEImageLayout(target, address);
    public PEImageLayout(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.PEImageLayout);

        Base = target.ReadPointerField(address, type, nameof(Base));
        Size = target.ReadField<uint>(address, type, nameof(Size));
        Flags = target.ReadField<uint>(address, type, nameof(Flags));
        Format = target.ReadField<uint>(address, type, nameof(Format));
    }

    public TargetPointer Base { get; init; }
    public uint Size { get; init; }
    public uint Flags { get; init; }
    public uint Format { get; init; }
}
