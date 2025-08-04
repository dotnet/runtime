// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class InflightTLSData : IData<InflightTLSData>
{
    static InflightTLSData IData<InflightTLSData>.Create(Target target, TargetPointer address) => new InflightTLSData(target, address);
    public InflightTLSData(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.InFlightTLSData);
        Next = target.ReadPointer(address + (ulong)type.Fields[nameof(Next)].Offset);
        TlsIndex = address + (ulong)type.Fields[nameof(TlsIndex)].Offset;
        TLSData = target.ReadPointer(address + (ulong)type.Fields[nameof(TLSData)].Offset);
    }
    public TargetPointer Next { get; init; }
    public TargetPointer TlsIndex { get; init; }
    public TargetPointer TLSData { get; init; }
}
