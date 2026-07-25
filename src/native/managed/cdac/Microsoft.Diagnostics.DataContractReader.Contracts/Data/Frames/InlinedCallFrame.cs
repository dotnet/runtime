// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.InlinedCallFrame))]
internal partial class InlinedCallFrame : IData<InlinedCallFrame>
{
    [Field] public partial TargetPointer CallSiteSP { get; }
    [Field] public partial TargetCodePointer CallerReturnAddress { get; }
    [Field] public partial TargetPointer CalleeSavedFP { get; }
    [Field] public partial TargetPointer? SPAfterProlog { get; }
    [Field] public partial TargetPointer Datum { get; }
}
