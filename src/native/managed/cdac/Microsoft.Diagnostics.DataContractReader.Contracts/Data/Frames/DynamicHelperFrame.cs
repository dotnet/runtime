// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class DynamicHelperFrame : IData<DynamicHelperFrame>
{
    static DynamicHelperFrame IData<DynamicHelperFrame>.Create(Target target, TargetPointer address)
        => new DynamicHelperFrame(target, address);

    public DynamicHelperFrame(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.DynamicHelperFrame);
        DynamicHelperFrameFlags = target.ReadField<int>(address, type, nameof(DynamicHelperFrameFlags));
    }

    public int DynamicHelperFrameFlags { get; }
}
