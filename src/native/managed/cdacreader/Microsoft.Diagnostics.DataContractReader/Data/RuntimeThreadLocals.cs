// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class RuntimeThreadLocals : IData<RuntimeThreadLocals>
{
    static RuntimeThreadLocals IData<RuntimeThreadLocals>.Create(ITarget target, TargetPointer address)
        => new RuntimeThreadLocals((Target)target, address);

    public RuntimeThreadLocals(Target target, TargetPointer address)
    {
        ITarget.TypeInfo type = target.GetTypeInfo(DataType.RuntimeThreadLocals);
        AllocContext = target.ProcessedData.GetOrAdd<GCAllocContext>(address + (ulong)type.Fields[nameof(AllocContext)].Offset);
    }

    public GCAllocContext AllocContext { get; init; }
}
