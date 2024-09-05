// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class LinkEditHeader
    {
        public uint FileOffset { get; set; }
        public uint FileSize { get; set; }
    }
}