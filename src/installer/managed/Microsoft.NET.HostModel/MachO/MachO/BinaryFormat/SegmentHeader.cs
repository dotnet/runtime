// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    internal sealed partial class SegmentHeader
    {
        public MachFixedName Name { get; set; } = MachFixedName.Empty;
        public uint Address { get; set; }
        public uint Size { get; set; }
        public uint FileOffset { get; set; }
        public uint FileSize { get; set; }
        public MachVmProtection MaximumProtection { get; set; }
        public MachVmProtection InitialProtection { get; set; }
        public uint NumberOfSections { get; set; }
        public MachSegmentFlags Flags { get; set; }
    }
}
