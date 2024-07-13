// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class RuntimeThreadLocals : IData<RuntimeThreadLocals>
{
    static RuntimeThreadLocals IData<RuntimeThreadLocals>.Create(Target target, TargetPointer address)
        => new RuntimeThreadLocals(target, address);

    public RuntimeThreadLocals(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.RuntimeThreadLocals);
        AllocContext = target.ProcessedData.GetOrAdd<EEAllocContext>(address + (ulong)type.Fields[nameof(AllocContext)].Offset);
    }

    public EEAllocContext AllocContext { get; init; }
}
