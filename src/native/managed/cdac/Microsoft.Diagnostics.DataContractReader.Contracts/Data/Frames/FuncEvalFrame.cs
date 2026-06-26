// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

/// <summary>
/// Only exists if DEBUGGING_SUPPORTED defined in the target runtime.
/// </summary>
[CdacType(nameof(DataType.FuncEvalFrame))]
internal partial class FuncEvalFrame : IData<FuncEvalFrame>
{
    [Field] public TargetPointer DebuggerEvalPtr { get; }
    [Field] public TargetPointer ReturnAddress { get; }
}
