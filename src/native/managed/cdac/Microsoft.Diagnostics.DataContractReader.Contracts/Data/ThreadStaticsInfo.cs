// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ThreadStaticsInfo))]
internal sealed partial class ThreadStaticsInfo : IData<ThreadStaticsInfo>
{
    [FieldAddress]
    public TargetPointer GCTlsIndex { get; }

    [FieldAddress]
    public TargetPointer NonGCTlsIndex { get; }
}
