// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

// A linked-list node tracking a range of WASM R2R function table indices, mirroring the native
// FunctionTableIndexRangeSection in src/coreclr/vm/codeman.h. The list head is the
// FunctionTableIndexRangeList global (ExecutionManager::s_pFunctionTableIndexRangeList).
[CdacType(nameof(DataType.FunctionTableIndexRangeSection))]
internal sealed partial class FunctionTableIndexRangeSection : IData<FunctionTableIndexRangeSection>
{
    [Field] public partial uint MinFunctionTableIndex { get; }
    [Field] public partial uint NumRuntimeFunctions { get; }
    [Field] public partial TargetPointer R2RModule { get; }
    [Field] public partial TargetPointer Next { get; }
}
