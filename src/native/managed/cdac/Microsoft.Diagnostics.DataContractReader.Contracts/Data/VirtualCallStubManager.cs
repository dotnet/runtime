// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.VirtualCallStubManager))]
internal sealed partial class VirtualCallStubManager : IData<VirtualCallStubManager>
{
    [Field] public TargetPointer IndcellHeap { get; }
    [Field] public TargetPointer? CacheEntryHeap { get; }
}
