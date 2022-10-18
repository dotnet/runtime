// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public sealed class DwarfWriter : DwarfReaderWriter
    {
        internal DwarfWriter(DwarfFile file, bool isLittleEndian, DiagnosticBag diagnostics) : base(file, diagnostics)
        {
            IsLittleEndian = isLittleEndian;
        }

        public override bool IsReadOnly => false;

        public bool EnableRelocation { get; internal set; }

        public void RecordRelocation(DwarfRelocationTarget target, DwarfAddressSize addressSize, ulong address)
        {
            if (CurrentSection is DwarfRelocatableSection relocSection)
            {

                relocSection.Relocations.Add(new DwarfRelocation(Offset, target, addressSize, address));

            }
            else
            {
                throw new InvalidOperationException($"Invalid {nameof(CurrentSection)} in {nameof(DwarfWriter)}. It must be a {nameof(DwarfRelocatableSection)}.");
            }
        }

        public void WriteAddress(DwarfRelocationTarget target, ulong address)
        {
            if (EnableRelocation)
            {
                RecordRelocation(target, AddressSize, address);
                // If the relocation is recorded, we write 0 as an address
                address = 0;
            }
            WriteUInt(address);
        }
    }
}