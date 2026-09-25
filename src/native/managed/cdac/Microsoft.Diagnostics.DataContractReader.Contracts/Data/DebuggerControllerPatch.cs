// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.DebuggerControllerPatch))]
internal sealed partial class DebuggerControllerPatch : IData<DebuggerControllerPatch>
{
    [Field("Address")] public partial TargetPointer CodeAddress { get; }
    [Field] public partial TargetNUInt Opcode { get; }
}
