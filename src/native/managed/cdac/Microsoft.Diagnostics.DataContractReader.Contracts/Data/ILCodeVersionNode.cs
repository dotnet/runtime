// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ILCodeVersionNode))]
internal sealed partial class ILCodeVersionNode : IData<ILCodeVersionNode>
{
    [Field] public TargetNUInt VersionId { get; }
    [Field] public TargetPointer Next { get; }
    [Field] public uint RejitState { get; }
    [Field] public TargetPointer ILAddress { get; }
    [Field] public uint Deoptimized { get; }
    [Field] public uint Source { get; }
    [Field] public TargetNUInt EnCVersion { get; }
    [Field] public InstrumentedILOffsetMapping InstrumentedILMap { get; }
}
