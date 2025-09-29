// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class CorCompileExceptionLookupEntry : IData<CorCompileExceptionLookupEntry>
{
    static CorCompileExceptionLookupEntry IData<CorCompileExceptionLookupEntry>.Create(Target target, TargetPointer address)
        => new CorCompileExceptionLookupEntry(target, address);

    public CorCompileExceptionLookupEntry(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.CorCompileExceptionLookupEntry);

        MethodStartRva = target.Read<uint>(address + (ulong)type.Fields[nameof(MethodStartRva)].Offset);
        ExceptionInfoRva = target.Read<uint>(address + (ulong)type.Fields[nameof(ExceptionInfoRva)].Offset);
    }

    public uint MethodStartRva { get; }
    public uint ExceptionInfoRva { get; }
}
