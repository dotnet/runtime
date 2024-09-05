// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class SymbolHeader
    {
        public uint NameIndex { get; set; }
        public byte Type { get; set; }
        public byte Section { get; set; }
        public ushort Descriptor { get; set; }
    }
}