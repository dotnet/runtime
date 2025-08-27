// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class EEJitManager : IData<EEJitManager>
{
    static EEJitManager IData<EEJitManager>.Create(Target target, TargetPointer address) => new EEJitManager(target, address);
    public EEJitManager(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.EEJitManager);

        StoreRichDebugInfo = target.Read<byte>(address + (ulong)type.Fields[nameof(StoreRichDebugInfo)].Offset) != 0;
    }

    public bool StoreRichDebugInfo { get; init; }
}
