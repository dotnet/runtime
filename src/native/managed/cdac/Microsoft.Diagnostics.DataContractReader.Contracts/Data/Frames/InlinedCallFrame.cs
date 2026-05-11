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
        CallSiteSP = target.ReadPointerField(address, type, nameof(CallSiteSP));
        CallerReturnAddress = target.ReadPointerField(address, type, nameof(CallerReturnAddress));
        CalleeSavedFP = target.ReadPointerField(address, type, nameof(CalleeSavedFP));
        if (type.Fields.ContainsKey(nameof(SPAfterProlog)))
            SPAfterProlog = target.ReadPointerField(address, type, nameof(SPAfterProlog));
        Datum = target.ReadPointerField(address, type, nameof(Datum));
        Address = address;
    }

    public TargetPointer Address { get; }
    public TargetPointer CallSiteSP { get; }
    public TargetPointer CallerReturnAddress { get; }
    public TargetPointer CalleeSavedFP { get; }
    public TargetPointer? SPAfterProlog { get; }
    public TargetPointer Datum { get; }
}
