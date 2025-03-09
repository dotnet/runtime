// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class FaultingExceptionFrame : IData<FaultingExceptionFrame>
{
    static FaultingExceptionFrame IData<FaultingExceptionFrame>.Create(Target target, TargetPointer address)
        => new FaultingExceptionFrame(target, address);

    public FaultingExceptionFrame(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.FaultingExceptionFrame);

        // TargetContextPtr only exists when FEATURE_EH_FUNCLETS is defined
        if (type.Fields.ContainsKey(nameof(TargetContext)))
        {
            TargetContext = address + (ulong)type.Fields[nameof(TargetContext)].Offset;
        }
        Address = address;
    }

    public TargetPointer Address { get; }
    public TargetPointer? TargetContext { get; }
}
