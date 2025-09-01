// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ThreadStaticsInfo : IData<ThreadStaticsInfo>
{
    static ThreadStaticsInfo IData<ThreadStaticsInfo>.Create(Target target, TargetPointer address) => new ThreadStaticsInfo(target, address);
    public ThreadStaticsInfo(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ThreadStaticsInfo);
        GCTlsIndex = address + (ulong)type.Fields[nameof(GCTlsIndex)].Offset;
        NonGCTlsIndex = address + (ulong)type.Fields[nameof(NonGCTlsIndex)].Offset;
    }
    public TargetPointer GCTlsIndex { get; init; }
    public TargetPointer NonGCTlsIndex { get; init; }
}
