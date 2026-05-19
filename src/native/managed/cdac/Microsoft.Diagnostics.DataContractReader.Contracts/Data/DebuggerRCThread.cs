// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.DebuggerRCThread))]
internal sealed partial class DebuggerRCThread : IData<DebuggerRCThread>
{
    [Field] public TargetPointer DCB { get; }
}
