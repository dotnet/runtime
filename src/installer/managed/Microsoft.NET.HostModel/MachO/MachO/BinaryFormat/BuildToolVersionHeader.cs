// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    internal sealed partial class BuildToolVersionHeader
    {
        public MachBuildTool BuildTool;
        public uint Version { get; set; }
    }
}
