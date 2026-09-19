// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ValueClassInfo))]
internal sealed partial class ValueClassInfo : IData<ValueClassInfo>
{
    [Field] public partial TargetPointer Next { get; }
    [Field] public partial TargetPointer MethodTable { get; }
    [Field] public partial TargetPointer Data { get; }
}
