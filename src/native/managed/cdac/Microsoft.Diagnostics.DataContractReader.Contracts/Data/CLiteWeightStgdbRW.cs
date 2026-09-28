// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.CLiteWeightStgdbRW))]
internal sealed partial class CLiteWeightStgdbRW : IData<CLiteWeightStgdbRW>
{
    [FieldAddress] public partial TargetPointer MiniMd { get; }
    [Field] public partial TargetPointer MetadataAddress { get; }
}
