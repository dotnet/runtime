// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.EEJitManager))]
internal sealed partial class EEJitManager : IData<EEJitManager>
{
    [Field] public bool StoreRichDebugInfo { get; }
    [Field] public TargetPointer AllCodeHeaps { get; }
}
