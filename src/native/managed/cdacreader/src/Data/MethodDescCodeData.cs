// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class MethodDescCodeData : IData<MethodDescCodeData>
{
    static MethodDescCodeData IData<MethodDescCodeData>.Create(Target target, TargetPointer address) => new MethodDescCodeData(target, address);
    public MethodDescCodeData(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.MethodDescCodeData);

        TemporaryEntryPoint = target.ReadCodePointer(address + (ulong)type.Fields[nameof(TemporaryEntryPoint)].Offset);
    }

    public TargetCodePointer TemporaryEntryPoint { get; set; }
}
