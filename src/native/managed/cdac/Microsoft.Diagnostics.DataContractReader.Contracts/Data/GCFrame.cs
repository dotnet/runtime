// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.GCFrame))]
internal sealed partial class GCFrame : IData<GCFrame>
{
    [Field] public TargetPointer Next { get; }
    [Field] public TargetPointer ObjRefs { get; }
    [Field] public uint NumObjRefs { get; }
    [Field] public uint GCFlags { get; }
}
