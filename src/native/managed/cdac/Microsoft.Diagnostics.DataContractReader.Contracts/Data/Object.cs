// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class Object : IData<Object>
{
    static Object IData<Object>.Create(Target target, TargetPointer address) => new Object(target, address);
    public Object(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Object);

        Address = address;
        MethodTable = target.ProcessedData.GetOrAdd<Data.MethodTable>(target.ReadPointer(address + (ulong)type.Fields["m_pMethTab"].Offset));
        Data = address + (ulong)type.Size!; // Data starts immediately after the Object header, which is the size of the Object type.
    }

    public TargetPointer Address { get; init; }
    public MethodTable MethodTable { get; init; }
    public TargetPointer Data { get; init; }
}
