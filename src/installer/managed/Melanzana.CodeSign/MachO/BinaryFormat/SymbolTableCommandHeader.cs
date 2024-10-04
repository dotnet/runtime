// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class SymbolTableCommandHeader
    {
        public uint SymbolTableOffset { get; set; }
        public uint NumberOfSymbols { get; set; }
        public uint StringTableOffset { get; set; }
        public uint StringTableSize { get; set; }
    }
}
