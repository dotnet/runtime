// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;

namespace LibObjectFile.Dwarf
{
    public abstract class DwarfReaderWriterContext
    {
        public bool IsLittleEndian { get; set; }

        public DwarfAddressSize AddressSize { get; set; }
        
        public Stream DebugAbbrevStream { get; set; }

        public Stream DebugStringStream { get; set; }

        public Stream DebugAddressRangeStream { get; set; }

        public Stream DebugLineStream { get; set; }

        public TextWriter DebugLinePrinter { get; set; }

        public Stream DebugInfoStream { get; set; }

        public Stream DebugLocationStream { get; set; }
    }
}