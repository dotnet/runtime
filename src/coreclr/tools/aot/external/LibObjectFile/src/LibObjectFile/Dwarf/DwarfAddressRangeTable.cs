// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using LibObjectFile.Utils;

namespace LibObjectFile.Dwarf
{
    [DebuggerDisplay("Count = {Ranges.Count,nq}")]
    public class DwarfAddressRangeTable : DwarfRelocatableSection
    {
        public DwarfAddressRangeTable()
        {
            Ranges = new List<DwarfAddressRange>();
            Version = 2;
        }

        public ushort Version { get; set; }

        public bool Is64BitEncoding { get; set; }

        public DwarfAddressSize AddressSize { get; set; }

        public DwarfAddressSize SegmentSelectorSize { get; set; }

        public ulong DebugInfoOffset { get; private set; }

        public DwarfUnit Unit { get; set; }
        
        public List<DwarfAddressRange> Ranges { get; }

        public ulong HeaderLength => Size - DwarfHelper.SizeOfUnitLength(Is64BitEncoding);

        protected override void Read(DwarfReader reader)
        {
            Offset = reader.Offset;
            var unitLength = reader.ReadUnitLength();
            Is64BitEncoding = reader.Is64BitEncoding;
            Version = reader.ReadU16();

            if (Version != 2)
            {
                reader.Diagnostics.Error(DiagnosticId.DWARF_ERR_VersionNotSupported, $"Version {Version} for .debug_aranges not supported");
                return;
            }

            DebugInfoOffset = reader.ReadUIntFromEncoding();

            AddressSize = reader.ReadAddressSize();

            var segment_selector_size = (DwarfAddressSize)reader.ReadU8();
            SegmentSelectorSize = segment_selector_size;

            var align = (ulong)segment_selector_size + (ulong)AddressSize * 2;

            // SPECS 7.21: The first tuple following the header in each set begins at an offset that is a multiple of the size of a single tuple
            reader.Offset = AlignHelper.AlignToUpper(reader.Offset, align);

            while (true)
            {
                ulong segment = 0;
                switch (segment_selector_size)
                {
                    case DwarfAddressSize.Bit8:
                        segment = reader.ReadU8();
                        break;

                    case DwarfAddressSize.Bit16:
                        segment = reader.ReadU16();
                        break;

                    case DwarfAddressSize.Bit32:
                        segment = reader.ReadU32();
                        break;

                    case DwarfAddressSize.Bit64:
                        segment = reader.ReadU64();
                        break;

                    case DwarfAddressSize.None:
                        break;
                }

                ulong address = 0;
                ulong length = 0;
                switch (AddressSize)
                {
                    case DwarfAddressSize.Bit8:
                        address = reader.ReadU8();
                        length = reader.ReadU8();
                        break;
                    case DwarfAddressSize.Bit16:
                        address = reader.ReadU16();
                        length = reader.ReadU16();
                        break;
                    case DwarfAddressSize.Bit32:
                        address = reader.ReadU32();
                        length = reader.ReadU32();
                        break;
                    case DwarfAddressSize.Bit64:
                        address = reader.ReadU64();
                        length = reader.ReadU64();
                        break;
                }

                if (segment == 0 && address == 0 && length == 0)
                {
                    break;
                }

                Ranges.Add(new DwarfAddressRange(segment, address, length));
            }

            Size = reader.Offset - Offset;
        }

        public override void Verify(DiagnosticBag diagnostics)
        {
            base.Verify(diagnostics);

            if (Version != 2)
            {
                diagnostics.Error(DiagnosticId.DWARF_ERR_VersionNotSupported, $"Non supported version {Version} for .debug_aranges");
            }

            if (Unit == null)
            {
                diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidNullUnitForAddressRangeTable, $"Invalid {nameof(Unit)} for .debug_aranges that cannot be null");
            }
            else
            {
                var parentFile = Unit.GetParentFile();
                if (this.Parent != parentFile)
                {
                    diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidParentUnitForAddressRangeTable, $"Invalid parent {nameof(DwarfFile)} of {nameof(Unit)} for .debug_aranges that doesn't match the parent of instance");
                }
            }
        }

        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
            ulong sizeOf = 0;
            // unit_length
            sizeOf += DwarfHelper.SizeOfUnitLength(Is64BitEncoding);

            // version
            sizeOf += 2;

            // debug_info_offset
            sizeOf += DwarfHelper.SizeOfUInt(Is64BitEncoding);

            // Address size
            sizeOf += 1;

            // segment selector size
            sizeOf += 1;

            var align = (ulong)SegmentSelectorSize + (ulong)AddressSize * 2;

            // SPECS 7.21: The first tuple following the header in each set begins at an offset that is a multiple of the size of a single tuple
            sizeOf = AlignHelper.AlignToUpper(sizeOf, align);

            // SizeOf ranges + 1 (for last 0 entry)
            sizeOf += ((ulong)Ranges.Count + 1UL) * align;

            Size = sizeOf;

            if (Unit != null)
            {
                DebugInfoOffset = Unit.Offset;
            }
        }

        protected override void Write(DwarfWriter writer)
        {
            var startOffset = writer.Offset;

            // unit_length
            writer.WriteUnitLength(Size - DwarfHelper.SizeOfUnitLength(Is64BitEncoding));

            // version
            writer.WriteU16(Version);

            // debug_info_offset
            var debugInfoOffset = DebugInfoOffset;
            if (writer.EnableRelocation)
            {
                writer.RecordRelocation(DwarfRelocationTarget.DebugInfo, writer.SizeOfUIntEncoding(), debugInfoOffset);
                debugInfoOffset = 0;
            }
            writer.WriteUIntFromEncoding(debugInfoOffset);

            // address_size
            writer.AddressSize = AddressSize;
            writer.WriteU8((byte)AddressSize);

            writer.WriteU8((byte)SegmentSelectorSize);

            var align = (ulong)SegmentSelectorSize + (ulong)AddressSize * 2;

            // SPECS 7.21: The first tuple following the header in each set begins at an offset that is a multiple of the size of a single tuple
            var nextOffset = AlignHelper.AlignToUpper(writer.Offset, align);
            for (ulong offset = writer.Offset; offset < nextOffset; offset++)
            {
                writer.WriteU8(0);
            }
            Debug.Assert(writer.Offset == nextOffset);

            foreach (var range in Ranges)
            {
                if (SegmentSelectorSize != 0)
                {
                    switch (SegmentSelectorSize)
                    {
                        case DwarfAddressSize.Bit8:
                            writer.WriteU8((byte)range.Segment);
                            break;
                        case DwarfAddressSize.Bit16:
                            writer.WriteU16((ushort)range.Segment);
                            break;
                        case DwarfAddressSize.Bit32:
                            writer.WriteU32((uint)range.Segment);
                            break;
                        case DwarfAddressSize.Bit64:
                            writer.WriteU64((ulong)range.Segment);
                            break;
                    }
                }

                writer.WriteAddress(DwarfRelocationTarget.Code, range.Address);
                writer.WriteUInt(range.Length);
            }

            if (SegmentSelectorSize != 0)
            {
                switch (SegmentSelectorSize)
                {
                    case DwarfAddressSize.Bit8:
                        writer.WriteU8(0);
                        break;
                    case DwarfAddressSize.Bit16:
                        writer.WriteU16(0);
                        break;
                    case DwarfAddressSize.Bit32:
                        writer.WriteU32(0);
                        break;
                    case DwarfAddressSize.Bit64:
                        writer.WriteU64(0);
                        break;
                }
            }

            switch (AddressSize)
            {
                case DwarfAddressSize.Bit8:
                    writer.WriteU16(0);
                    break;
                case DwarfAddressSize.Bit16:
                    writer.WriteU32(0);
                    break;
                case DwarfAddressSize.Bit32:
                    writer.WriteU64(0);
                    break;
                case DwarfAddressSize.Bit64:
                    writer.WriteU64(0);
                    writer.WriteU64(0);
                    break;
            }

            Debug.Assert(writer.Offset - startOffset == Size);
        }
    }
}