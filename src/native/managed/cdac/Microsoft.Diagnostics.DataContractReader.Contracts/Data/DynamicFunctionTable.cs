// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.DynamicFunctionTable))]
internal sealed partial class DynamicFunctionTable : IData<DynamicFunctionTable>
{
    [Field] public partial TargetPointer MinimumAddress { get; }
    [Field] public partial TargetPointer Context { get; }
}
