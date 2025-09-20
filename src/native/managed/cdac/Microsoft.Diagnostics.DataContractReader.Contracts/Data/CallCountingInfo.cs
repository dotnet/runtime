// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class CallCountingInfo : IData<CallCountingInfo>
{
    static CallCountingInfo IData<CallCountingInfo>.Create(Target target, TargetPointer address)
        => new CallCountingInfo(target, address);

    public CallCountingInfo(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.CallCountingInfo);

        Stage = target.Read<byte>(address + (ulong)type.Fields[nameof(Stage)].Offset);
        CodeVersion = target.ReadPointer(address + (ulong)type.Fields[nameof(CodeVersion)].Offset);
        Address = address;
    }

    public CallCountingInfo(TargetPointer address)
    {
        Address = address;
    }

    public byte Stage { get; init; }
    public TargetPointer CodeVersion { get; init; }
    public TargetPointer Address { get; init; }
}
