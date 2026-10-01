// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.GCFrame))]
internal sealed partial class GCFrame : IData<GCFrame>
{
    [Field] public partial TargetPointer Next { get; }
    [Field] public partial TargetPointer ObjRefs { get; }
    [Field] public partial uint NumObjRefs { get; }
    [Field] public partial uint GCFlags { get; }
}
