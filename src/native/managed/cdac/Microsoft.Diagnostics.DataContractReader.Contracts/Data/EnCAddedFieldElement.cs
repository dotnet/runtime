// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.EnCAddedFieldElement))]
internal sealed partial class EnCAddedFieldElement : IData<EnCAddedFieldElement>
{
    [Field] public TargetPointer Next { get; }
    [FieldAddress] public TargetPointer FieldDesc { get; }
}
