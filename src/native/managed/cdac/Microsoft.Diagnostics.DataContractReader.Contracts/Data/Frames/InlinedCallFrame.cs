// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.InlinedCallFrame))]
internal partial class InlinedCallFrame : IData<InlinedCallFrame>
{
    [Field] public TargetPointer CallSiteSP { get; }
    [Field] public TargetCodePointer CallerReturnAddress { get; }
    [Field] public TargetPointer CalleeSavedFP { get; }
    [Field] public TargetPointer? SPAfterProlog { get; }
    [Field] public TargetPointer Datum { get; }
}
