// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.EnCFieldDesc))]
internal sealed partial class EnCFieldDesc : IData<EnCFieldDesc>
{
    [Field] public int NeedsFixup { get; }
    [Field] public TargetPointer StaticFieldData { get; }
}
