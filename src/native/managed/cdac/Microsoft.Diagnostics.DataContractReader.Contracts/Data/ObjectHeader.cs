// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ObjectHeader))]
internal sealed partial class ObjectHeader : IData<ObjectHeader>
{
    [Field] public uint SyncBlockValue { get; }
}
