// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.DNMDData))]
internal sealed partial class DNMDData : IData<DNMDData>
{
    [Field] public partial TargetPointer Ptr { get; }
    [Field] public partial TargetNUInt Size { get; }
}
