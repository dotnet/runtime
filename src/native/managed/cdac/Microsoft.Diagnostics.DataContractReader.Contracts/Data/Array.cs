// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Array))]
internal sealed partial class Array : IData<Array>
{
    [Field("m_NumComponents")]
    public uint NumComponents { get; }

    [InstanceDataStart]
    public TargetPointer DataPointer { get; }
}
