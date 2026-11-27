// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ILCodeVersionNode))]
internal sealed partial class ILCodeVersionNode : IData<ILCodeVersionNode>
{
    [Field] public partial TargetNUInt VersionId { get; }
    [Field] public partial TargetPointer Next { get; }
    [Field] public partial uint RejitState { get; }
    [Field] public partial TargetPointer ILAddress { get; }
    [Field] public partial uint Deoptimized { get; }
    [Field] public partial uint Source { get; }
    [Field] public partial TargetNUInt EnCVersion { get; }
    [Field] public partial InstrumentedILOffsetMapping InstrumentedILMap { get; }
}
