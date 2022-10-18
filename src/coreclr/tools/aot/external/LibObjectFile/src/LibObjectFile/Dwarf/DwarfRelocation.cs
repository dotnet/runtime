// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    [DebuggerDisplay("{" + nameof(ToString) + "(),nq}")]
    public struct DwarfRelocation
    {
        public DwarfRelocation(ulong offset, DwarfRelocationTarget target, DwarfAddressSize size, ulong addend)
        {
            Offset = offset;
            Target = target;
            Size = size;
            Addend = addend;
        }

        public ulong Offset { get; set; }

        public DwarfRelocationTarget Target { get; set; }

        public DwarfAddressSize Size { get; set; }

        public ulong Addend { get; set; }

        public override string ToString()
        {
            return $"{nameof(Offset)}: {Offset}, {nameof(Target)}: {Target}, {nameof(Size)}: {Size}, {nameof(Addend)}: {Addend}";
        }
    }
}