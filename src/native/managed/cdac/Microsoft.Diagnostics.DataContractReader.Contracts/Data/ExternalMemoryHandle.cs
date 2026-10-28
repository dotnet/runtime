// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ExternalMemoryHandle))]
internal sealed partial class ExternalMemoryHandle : IData<ExternalMemoryHandle>
{
    [Field] public partial TargetPointer Next { get; }
    [Field] public partial TargetPointer MethodTable { get; }
    [Field] public partial TargetPointer Memory { get; }
    [Field] public partial uint GCFlags { get; }
}
