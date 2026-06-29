// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ThisPtrRetBufPrecodeData))]
internal sealed partial class ThisPtrRetBufPrecodeData : IData<ThisPtrRetBufPrecodeData>
{
    [Field] public TargetPointer MethodDesc { get; }
}
