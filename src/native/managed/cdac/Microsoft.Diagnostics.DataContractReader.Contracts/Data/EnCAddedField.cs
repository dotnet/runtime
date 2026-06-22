// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.EnCAddedField))]
internal sealed partial class EnCAddedField : IData<EnCAddedField>
{
    [Field] public TargetPointer Next { get; }
    [Field] public TargetPointer FieldDesc { get; }
    [Field] public ObjectHandle FieldData { get; }
}
