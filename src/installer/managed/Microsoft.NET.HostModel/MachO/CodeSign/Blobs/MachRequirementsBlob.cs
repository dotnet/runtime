// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.NET.HostModel.MachO.CodeSign.Blobs;

namespace Microsoft.NET.HostModel.MachO.CodeSign.Blobs
{
    [GenerateReaderWriter]
    internal partial class EmptyRequirementsBlob
    {
        public BlobMagic Magic = BlobMagic.Requirements;
        public uint RequirementsBlobLength = BinarySize;
        public uint EntitlementsBlobLength;
    }
}
