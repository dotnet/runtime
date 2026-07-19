// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.String))]
internal sealed partial class String : IData<String>
{
    [FieldAddress("m_FirstChar")]
    public TargetPointer FirstChar { get; }

    [Field("m_StringLength")]
    public uint StringLength { get; }
}
