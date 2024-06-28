// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection.PortableExecutable;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class PEImage : IData<PEImage>
{
    static PEImage IData<PEImage>.Create(Target target, TargetPointer address)
        => new PEImage(target, address);

    private readonly Target _target;

    public PEImage(Target target, TargetPointer address)
    {
        _target = target;
        Target.TypeInfo type = target.GetTypeInfo(DataType.PEImage);
        LoadedLayout = target.ReadPointer(address + (ulong)type.Fields[nameof(LoadedLayout)].Offset);
        if (LoadedLayout != TargetPointer.Null)
        {
            Target.TypeInfo layoutType = target.GetTypeInfo(DataType.PEImageLayout);
            Base = target.ReadPointer(LoadedLayout + (ulong)layoutType.Fields[nameof(Base)].Offset);
            Size = target.Read<uint>(LoadedLayout + (ulong)layoutType.Fields[nameof(Size)].Offset);
        }
    }

    public TargetPointer Base { get; init; } = TargetPointer.Null;
    public uint Size { get; init; }

    private TargetPointer LoadedLayout { get; init; }
}
