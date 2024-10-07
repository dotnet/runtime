// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO.CodeSign.Blobs
{
    [GenerateReaderWriter]
    [BigEndian]
    internal sealed partial class CodeDirectoryTeamIdHeader
    {
#pragma warning disable CS0649
        internal uint TeamIdOffset;
#pragma warning restore CS0649
    }
}
