// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.NET.HostModel.MachO;

internal interface ISuperBlob : IBlob
{
    uint BlobCount { get; }
    ImmutableArray<BlobIndex> BlobIndices { get; }
    ImmutableArray<IBlob> Blobs { get; }
}
