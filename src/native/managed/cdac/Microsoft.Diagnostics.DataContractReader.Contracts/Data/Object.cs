// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class Object : IData<Object>
{
    static Object IData<Object>.Create(Target target, TargetPointer address) => new Object(target, address);
    public Object(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Object);

        MethodTable = target.ProcessedData.GetOrAdd<Data.MethodTable>(target.ReadPointer(address + (ulong)type.Fields["m_pMethTab"].Offset));
    }

    public MethodTable MethodTable { get; init; }
}
