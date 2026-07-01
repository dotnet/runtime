// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.MDInternalRW))]
internal sealed partial class MDInternalRW : IData<MDInternalRW>
{
    [Field] public TargetPointer Stgdb { get; }
}
