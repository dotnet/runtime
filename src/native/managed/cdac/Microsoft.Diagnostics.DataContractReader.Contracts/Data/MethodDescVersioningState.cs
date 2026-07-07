// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.MethodDescVersioningState))]
internal sealed partial class MethodDescVersioningState : IData<MethodDescVersioningState>
{
    [Field] public TargetPointer NativeCodeVersionNode { get; }
    [Field] public byte Flags { get; }
}
