// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.StackTraceElement))]
internal sealed partial class StackTraceElement : IData<StackTraceElement>
{
    [Field] public partial TargetPointer Ip { get; }
    [Field] public partial TargetPointer MethodDesc { get; }
    [Field] public partial int Flags { get; }
}
