// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class StressLogModuleDesc : IData<StressLogModuleDesc>
{
    static StressLogModuleDesc IData<StressLogModuleDesc>.Create(Target target, TargetPointer address)
        => new StressLogModuleDesc(target, address);

    public StressLogModuleDesc(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.StressLogModuleDesc);

        BaseAddress = target.ReadPointer(address + (ulong)type.Fields[nameof(BaseAddress)].Offset);
        Size = target.ReadNUInt(address + (ulong)type.Fields[nameof(Size)].Offset);
    }

    public TargetPointer BaseAddress { get; init; }

    public TargetNUInt Size { get; init; }
}
