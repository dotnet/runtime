// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.InterfaceEntry))]
internal sealed partial class InterfaceEntry : IData<InterfaceEntry>
{
    [Field] public TargetPointer MethodTable { get; }
    [Field] public TargetPointer Unknown { get; }
}
