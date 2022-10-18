// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public class DwarfCompilationUnit : DwarfUnit
    {
        public DwarfCompilationUnit()
        {
            Kind = DwarfUnitKind.Compile;
            // Default to version 4
            Version = 4;
        }

        protected override void ReadHeader(DwarfReader reader)
        {
            if (Version < 5)
            {
                // 3. debug_abbrev_offset (section offset) 
                DebugAbbreviationOffset = reader.ReadUIntFromEncoding();

                // 4. address_size (ubyte) 
                AddressSize = reader.ReadAddressSize();
                reader.AddressSize = AddressSize;
            }
            else
            {
                // NOTE: order of address_size/debug_abbrev_offset are different from Dwarf 4

                // 4. address_size (ubyte) 
                AddressSize = reader.ReadAddressSize();
                reader.AddressSize = AddressSize;

                // 5. debug_abbrev_offset (section offset) 
                DebugAbbreviationOffset = reader.ReadUIntFromEncoding();
            }
        }

        protected override void WriteHeader(DwarfWriter writer)
        {
            if (Version < 5)
            {
                // 3. debug_abbrev_offset (section offset) 
                var abbrevOffset = Abbreviation.Offset;
                if (writer.EnableRelocation)
                {
                    writer.RecordRelocation(DwarfRelocationTarget.DebugAbbrev, writer.SizeOfUIntEncoding(), abbrevOffset);
                    abbrevOffset = 0;
                }
                writer.WriteUIntFromEncoding(abbrevOffset);

                // 4. address_size (ubyte) 
                writer.WriteAddressSize(AddressSize);
            }
            else
            {
                // NOTE: order of address_size/debug_abbrev_offset are different from Dwarf 4

                // 4. address_size (ubyte) 
                writer.WriteAddressSize(AddressSize);

                // 5. debug_abbrev_offset (section offset) 
                var abbrevOffset = Abbreviation.Offset;
                if (writer.EnableRelocation)
                {
                    writer.RecordRelocation(DwarfRelocationTarget.DebugAbbrev, writer.SizeOfUIntEncoding(), abbrevOffset);
                    abbrevOffset = 0;
                }
                writer.WriteUIntFromEncoding(abbrevOffset);
            }
        }

        protected override ulong GetLayoutHeaderSize()
        {
            // 3. debug_abbrev_offset (section offset) 
            // 4. address_size (ubyte) 
            return DwarfHelper.SizeOfUInt(Is64BitEncoding) + 1;
        }
    }
}