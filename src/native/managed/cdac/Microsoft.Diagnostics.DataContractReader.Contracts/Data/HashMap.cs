// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.HashMap))]
internal sealed partial class HashMap : IData<HashMap>
{
    [Field] public TargetPointer Buckets { get; }
}
