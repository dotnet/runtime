// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class Assembly : IData<Assembly>
{
    public enum FileLoadLevel : uint
    {
        // Note that semantics here are description is the LAST step done, not what is
        // currently being done.

        FILE_LOAD_CREATE,
        FILE_LOAD_BEGIN,
        FILE_LOAD_BEFORE_TYPE_LOAD,
        FILE_LOAD_EAGER_FIXUPS,
        FILE_LOAD_DELIVER_EVENTS,
        FILE_LOAD_VTABLE_FIXUPS,
        FILE_LOADED, // Loaded by not yet active
        FILE_ACTIVE, // Fully active (constructors run & security checked)
    };

    static Assembly IData<Assembly>.Create(Target target, TargetPointer address) => new Assembly(target, address);
    public Assembly(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Assembly);

        Module = target.ReadPointer(address + (ulong)type.Fields[nameof(Module)].Offset);
        IsCollectible = target.Read<byte>(address + (ulong)type.Fields[nameof(IsCollectible)].Offset);
        Error = target.ReadPointer(address + (ulong)type.Fields[nameof(Error)].Offset);
        NotifyFlags = target.Read<uint>(address + (ulong)type.Fields[nameof(NotifyFlags)].Offset);
        Level = target.Read<uint>(address + (ulong)type.Fields[nameof(Level)].Offset);
    }

    public TargetPointer Module { get; init; }
    public byte IsCollectible { get; init; }
    public TargetPointer Error { get; init; }
    public uint NotifyFlags { get; init; }
    public uint Level { get; init; }

    public bool IsError => Error != TargetPointer.Null;
    public bool IsLoaded => Level >= (uint)FileLoadLevel.FILE_LOAD_DELIVER_EVENTS;
}
