// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.SoftwareExceptionFrame))]
internal partial class SoftwareExceptionFrame : IData<SoftwareExceptionFrame>
{
    [FieldAddress]
    public TargetPointer TargetContext { get; }

    [Field] public TargetCodePointer ReturnAddress { get; }
}
