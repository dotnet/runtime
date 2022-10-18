// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public abstract class DwarfReaderWriter : ObjectFileReaderWriter
    {
        internal DwarfReaderWriter(DwarfFile file, DiagnosticBag diagnostics) : base(null, diagnostics)
        {
            File = file;
        }

        public DwarfFile File { get; }

        public bool Is64BitEncoding { get; set; }

        public DwarfAddressSize AddressSize { get; internal set; }

        public DwarfSection CurrentSection { get; internal set; }

        public DwarfUnit CurrentUnit { get; internal set; }

        public DwarfAddressSize SizeOfUIntEncoding()
        {
            return Is64BitEncoding ? DwarfAddressSize.Bit64 : DwarfAddressSize.Bit32;
        }

        public DwarfAddressSize ReadAddressSize()
        {
            var address_size = (DwarfAddressSize)ReadU8();
            switch (address_size)
            {
                case DwarfAddressSize.Bit8:
                case DwarfAddressSize.Bit16:
                case DwarfAddressSize.Bit32:
                case DwarfAddressSize.Bit64:
                    break;
                default:
                    Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidAddressSize, $"Unsupported address size {(uint)address_size}.");
                    break;
            }

            return address_size;
        }

        public void WriteAddressSize(DwarfAddressSize addressSize)
        {
            WriteU8((byte)addressSize);
        }

        public ulong ReadUnitLength()
        {
            Is64BitEncoding = false;
            uint length = ReadU32();
            if (length >= 0xFFFFFFF0)
            {
                if (length != 0xFFFFFFFF)
                {
                    throw new InvalidOperationException($"Unsupported unit length prefix 0x{length:x8}");
                }

                Is64BitEncoding = true;
                return ReadU64();
            }
            return length;
        }

        public void WriteUnitLength(ulong length)
        {
            if (Is64BitEncoding)
            {
                WriteU32(0xFFFFFFFF);
                WriteU64(length);
            }
            else
            {
                if (length >= 0xFFFFFFF0)
                {
                    throw new ArgumentOutOfRangeException(nameof(length), $"Must be < 0xFFFFFFF0 but is 0x{length:X}");
                }
                WriteU32((uint)length);
            }
        }

        public ulong ReadUIntFromEncoding()
        {
            return Is64BitEncoding ? ReadU64() : ReadU32();
        }

        public void WriteUIntFromEncoding(ulong value)
        {
            if (Is64BitEncoding)
            {
                WriteU64(value);
            }
            else
            {
                WriteU32((uint)value);
            }
        }

        public ulong ReadUInt()
        {
            switch (AddressSize)
            {
                case DwarfAddressSize.Bit8:
                    return ReadU8();
                case DwarfAddressSize.Bit16:
                    return ReadU16();
                case DwarfAddressSize.Bit32:
                    return ReadU32();
                case DwarfAddressSize.Bit64:
                    return ReadU64();
                default:
                    throw new ArgumentOutOfRangeException($"Invalid AddressSize {AddressSize}");
            }
        }

        public void WriteUInt(ulong target)
        {
            switch (AddressSize)
            {
                case DwarfAddressSize.Bit8:
                    WriteU8((byte)target);
                    break;
                case DwarfAddressSize.Bit16:
                    WriteU16((ushort)target);
                    break;
                case DwarfAddressSize.Bit32:
                    WriteU32((uint)target);
                    break;
                case DwarfAddressSize.Bit64:
                    WriteU64(target);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Invalid AddressSize {AddressSize}");
            }
        }

        public ulong ReadULEB128()
        {
            return Stream.ReadULEB128();
        }

        public uint ReadULEB128AsU32()
        {
            return Stream.ReadULEB128AsU32();
        }

        public int ReadLEB128AsI32()
        {
            return Stream.ReadLEB128AsI32();
        }

        public long ReadILEB128()
        {
            return Stream.ReadSignedLEB128();
        }

        public void WriteULEB128(ulong value)
        {
            Stream.WriteULEB128(value);
        }
        public void WriteILEB128(long value)
        {
            Stream.WriteILEB128(value);
        }
    }
}