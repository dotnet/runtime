// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.EnCEEClassData))]
internal sealed partial class EnCEEClassData : IData<EnCEEClassData>
{
    [Field] public TargetPointer MethodTable { get; }
    [Field] public TargetPointer AddedInstanceFields { get; }
    [Field] public TargetPointer AddedStaticFields { get; }
}
