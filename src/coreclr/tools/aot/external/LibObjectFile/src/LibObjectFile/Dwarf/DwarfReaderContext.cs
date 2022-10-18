// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public class DwarfReaderContext : DwarfReaderWriterContext
    {
        public DwarfReaderContext()
        {
        }

        public DwarfReaderContext(DwarfElfContext elfContext)
        {
            if (elfContext == null) throw new ArgumentNullException(nameof(elfContext));
            IsLittleEndian = elfContext.IsLittleEndian;
            AddressSize = elfContext.AddressSize;
            DebugLineStream = elfContext.LineTable?.Stream;
            DebugStringStream = elfContext.StringTable?.Stream;
            DebugAbbrevStream = elfContext.AbbreviationTable?.Stream;
            DebugInfoStream = elfContext.InfoSection?.Stream;
            DebugAddressRangeStream = elfContext.AddressRangeTable?.Stream;
            DebugLocationStream = elfContext.LocationSection?.Stream;
        }

        public bool IsInputReadOnly { get; set; }
    }
}