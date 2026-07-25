// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.InterpreterFrame))]
internal sealed partial class InterpreterFrame : IData<InterpreterFrame>
{
    [Field] public partial TargetPointer TopInterpMethodContextFrame { get; }
    [Field] public partial bool IsFaulting { get; }
}
