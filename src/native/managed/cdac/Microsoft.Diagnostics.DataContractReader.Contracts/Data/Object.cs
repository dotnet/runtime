// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Object))]
internal sealed partial class Object : IData<Object>
{
    [Field("m_pMethTab", Pointer = true)]
    public MethodTable MethodTable { get; }

    [InstanceDataStart]
    public TargetPointer Data { get; }
}
