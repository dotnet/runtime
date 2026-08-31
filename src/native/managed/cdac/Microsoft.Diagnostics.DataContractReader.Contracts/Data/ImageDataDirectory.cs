// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ImageDataDirectory))]
internal sealed partial class ImageDataDirectory : IData<ImageDataDirectory>
{
    [Field] public partial uint VirtualAddress { get; }
    [Field] public partial uint Size { get; }
}
