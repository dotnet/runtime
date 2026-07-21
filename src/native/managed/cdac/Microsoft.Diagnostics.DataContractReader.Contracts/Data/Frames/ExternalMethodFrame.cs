// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ExternalMethodFrame))]
internal partial class ExternalMethodFrame : IData<ExternalMethodFrame>
{
    [Field] public TargetPointer Indirection { get; }
}
