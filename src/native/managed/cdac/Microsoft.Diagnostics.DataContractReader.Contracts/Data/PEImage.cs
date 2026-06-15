// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.PEImage))]
internal sealed partial class PEImage : IData<PEImage>
{
    [Field] public TargetPointer LoadedImageLayout { get; }
    [Field] public ProbeExtensionResult ProbeExtensionResult { get; }
    [Field] public TargetPointer Path { get; }
    [Field] public TargetPointer ModuleFileNameHint { get; }
}
