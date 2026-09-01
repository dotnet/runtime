// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.PlatformMetadata))]
internal sealed partial class PlatformMetadata : IData<PlatformMetadata>
{
    /// <summary>Address of the embedded PrecodeMachineDescriptor within this PlatformMetadata object.</summary>
    [FieldAddress]
    public partial TargetPointer PrecodeMachineDescriptor { get; }

    [Field] public partial byte CodePointerFlags { get; }
}
