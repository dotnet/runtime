// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.EnCAddedStaticField))]
internal sealed partial class EnCAddedStaticField : IData<EnCAddedStaticField>
{
    [Field] public TargetPointer FieldDesc { get; }
    [FieldAddress] public TargetPointer FieldData { get; }
}
