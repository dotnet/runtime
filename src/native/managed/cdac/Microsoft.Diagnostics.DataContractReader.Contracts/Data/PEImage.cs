// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.PEImage))]
internal sealed partial class PEImage : IData<PEImage>
{
    // The flat image layout (m_pLayouts[IMAGE_FLAT]). Present since the field was added to the
    // descriptor; nullable so older descriptors that predate it simply read as null.
    [Field] public TargetPointer? FlatImageLayout { get; }
    [Field] public TargetPointer LoadedImageLayout { get; }
    [Field] public ProbeExtensionResult ProbeExtensionResult { get; }
}
