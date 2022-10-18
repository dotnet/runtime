// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public sealed class DwarfLayoutContext
    {
        internal DwarfLayoutContext(DwarfFile file, DwarfLayoutConfig config, DiagnosticBag diagnostics)
        {
            File = file;
            Config = config;
            Diagnostics = diagnostics;
        }

        public DwarfFile File { get; }

        public DiagnosticBag Diagnostics { get; }

        public DwarfLayoutConfig Config { get; }

        public bool HasErrors => Diagnostics.HasErrors;
        
        public DwarfUnit CurrentUnit { get; internal set; }
    }
}