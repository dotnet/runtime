// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class Debugger : IData<Debugger>
{
    static Debugger IData<Debugger>.Create(Target target, TargetPointer address)
        => new Debugger(target, address);

    public Debugger(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Debugger);

        LeftSideInitialized = target.Read<int>(address + (ulong)type.Fields[nameof(LeftSideInitialized)].Offset);
        Defines = target.Read<uint>(address + (ulong)type.Fields[nameof(Defines)].Offset);
        MDStructuresVersion = target.Read<uint>(address + (ulong)type.Fields[nameof(MDStructuresVersion)].Offset);
    }

    public int LeftSideInitialized { get; init; }
    public uint Defines { get; init; }
    public uint MDStructuresVersion { get; init; }
}
