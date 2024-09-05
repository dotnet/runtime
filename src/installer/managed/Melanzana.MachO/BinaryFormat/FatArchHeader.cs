// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class FatArchHeader
    {
        public MachCpuType CpuType { get; set; }
        public uint CpuSubType { get; set; }
        public uint Offset { get; set; }
        public uint Size { get; set; }
        public uint Alignment { get; set; }
    }
}
