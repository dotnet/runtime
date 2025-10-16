// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class StressMsg : IData<StressMsg>
{
    static StressMsg IData<StressMsg>.Create(Target target, TargetPointer address)
        => new StressMsg(target, address);

    public StressMsg(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.StressMsg);

        Header = address + (ulong)type.Fields[nameof(Header)].Offset;
        Args = address + (ulong)type.Fields[nameof(Args)].Offset;
    }

    public TargetPointer Header { get; init; }
    public TargetPointer Args { get; init; }
}
