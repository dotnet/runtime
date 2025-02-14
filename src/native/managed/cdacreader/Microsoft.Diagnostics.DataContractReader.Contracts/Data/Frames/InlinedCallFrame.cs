// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class InlinedCallFrame : IData<InlinedCallFrame>
{
    static InlinedCallFrame IData<InlinedCallFrame>.Create(Target target, TargetPointer address)
        => new InlinedCallFrame(target, address);

    public InlinedCallFrame(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.InlinedCallFrame);
        CallSiteSP = target.ReadPointer(address + (ulong)type.Fields[nameof(CallSiteSP)].Offset);
        CallerReturnAddress = target.ReadPointer(address + (ulong)type.Fields[nameof(CallerReturnAddress)].Offset);
        CalleeSavedFP = target.ReadPointer(address + (ulong)type.Fields[nameof(CalleeSavedFP)].Offset);
        Address = address;
    }

    public TargetPointer Address { get;}
    public TargetPointer CallSiteSP { get; }
    public TargetPointer CallerReturnAddress { get; }
    public TargetPointer CalleeSavedFP { get; }
}
