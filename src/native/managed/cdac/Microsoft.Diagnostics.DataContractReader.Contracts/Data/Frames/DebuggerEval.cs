// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.DebuggerEval))]
internal sealed partial class DebuggerEval : IData<DebuggerEval>
{
    [FieldAddress]
    public partial TargetPointer TargetContext { get; }

    [Field] public partial bool EvalUsesHijack { get; }
    [Field] public partial uint MethodToken { get; }
    [Field] public partial TargetPointer AssemblyPtr { get; }
}
