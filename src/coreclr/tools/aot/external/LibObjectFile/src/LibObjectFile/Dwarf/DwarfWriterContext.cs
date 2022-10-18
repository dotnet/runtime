// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using LibObjectFile.Elf;

namespace LibObjectFile.Dwarf
{
    public class DwarfWriterContext : DwarfReaderWriterContext
    {
        public DwarfWriterContext() : this(new DwarfLayoutConfig())
        {
        }

        public DwarfWriterContext(DwarfLayoutConfig layoutConfig)
        {
            LayoutConfig = layoutConfig ?? throw new ArgumentNullException(nameof(layoutConfig));
            EnableRelocation = true;
        }

        public DwarfLayoutConfig LayoutConfig { get; }
        
        public bool EnableRelocation { get; set; }
    }
}