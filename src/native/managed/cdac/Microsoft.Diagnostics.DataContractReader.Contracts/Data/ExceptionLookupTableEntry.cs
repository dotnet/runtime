// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ExceptionLookupTableEntry : IData<ExceptionLookupTableEntry>
{
    static ExceptionLookupTableEntry IData<ExceptionLookupTableEntry>.Create(Target target, TargetPointer address) => new ExceptionLookupTableEntry(target, address);
    public ExceptionLookupTableEntry(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ExceptionLookupTableEntry);
        MethodStartRVA = target.ReadField<uint>(address, type, nameof(MethodStartRVA));
        ExceptionInfoRVA = target.ReadField<uint>(address, type, nameof(ExceptionInfoRVA));
    }

    public uint MethodStartRVA { get; init; }
    public uint ExceptionInfoRVA { get; init; }
}
