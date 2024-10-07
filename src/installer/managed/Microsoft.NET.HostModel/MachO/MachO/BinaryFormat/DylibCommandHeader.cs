// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    internal sealed partial class DylibCommandHeader
    {
        public uint NameOffset { get; set; }
        public uint Timestamp { get; set; }
        public uint CurrentVersion { get; set; }
        public uint CompatibilityVersion { get; set; }
    }
}
