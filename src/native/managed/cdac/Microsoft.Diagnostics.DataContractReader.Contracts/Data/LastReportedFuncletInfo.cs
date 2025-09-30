// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class LastReportedFuncletInfo : IData<LastReportedFuncletInfo>
{
    static LastReportedFuncletInfo IData<LastReportedFuncletInfo>.Create(Target target, TargetPointer address)
        => new LastReportedFuncletInfo(target, address);

    public LastReportedFuncletInfo(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.LastReportedFuncletInfo);

        IP = target.ReadCodePointer(address + (ulong)type.Fields[nameof(IP)].Offset);
    }

    public TargetCodePointer IP { get; }
}
