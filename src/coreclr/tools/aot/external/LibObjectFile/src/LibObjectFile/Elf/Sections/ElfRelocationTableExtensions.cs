// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using LibObjectFile.Dwarf;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Extension methods for <see cref="ElfRelocationTable"/>
    /// </summary>
    public static class ElfRelocationTableExtensions
    {
        /// <summary>
        /// Applies the relocation defined by this table to the specified stream. The stream must be seekable and writeable.
        /// </summary>
        public static void Relocate(this ElfRelocationTable relocTable, in ElfRelocationContext context)
        {
            var relocTarget = relocTable.Info.Section;
            if (!(relocTarget is ElfBinarySection relocTargetBinarySection))
            {
                throw new InvalidOperationException($"Invalid ElfRelocationTable.Info section. Can only relocate a section that inherits from {nameof(ElfBinarySection)}.");
            }

            Relocate(relocTable, relocTargetBinarySection.Stream, context);
        }

        /// <summary>
        /// Applies the relocation defined by this table to the specified stream. The stream must be seekable and writeable.
        /// </summary>
        /// <param name="stream"></param>
        public static void Relocate(this ElfRelocationTable relocTable, Stream stream, in ElfRelocationContext context)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            switch (relocTable.Parent.Arch.Value)
            {
                case ElfArch.X86_64:
                    ApplyX86_64(relocTable, stream, context);
                    break;
                default:
                    throw new NotImplementedException($"The relocation for architecture {relocTable.Parent.Arch} is not supported/implemented.");
            }
            stream.Position = 0;
        }

        /// <summary>
        /// Applies the relocation defined by this table to the specified stream. The stream must be seekable and writeable.
        /// </summary>
        /// <param name="stream"></param>
        private static void ApplyX86_64(this ElfRelocationTable relocTable, Stream stream, in ElfRelocationContext context)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            bool isLsb = relocTable.Parent.Encoding == ElfEncoding.Lsb;

            var GOT = (long)context.GlobalObjectTableAddress;
            var B = (long)context.BaseAddress;
            var G = (long)context.GlobalObjectTableOffset;

            var symbolTable = (ElfSymbolTable)relocTable.Link.Section;
            
            foreach (var reloc in relocTable.Entries)
            {
                var P = (long)reloc.Offset;
                var L = (long)context.ProcedureLinkageTableAddress + P; // TODO: Is it really that?
                var A = reloc.Addend;
                var symbol = symbolTable.Entries[(int)reloc.SymbolIndex - 1];
                var Z = (long)symbol.Size;

                var patchOffset = (long)reloc.Offset;
                stream.Position = patchOffset;

                switch (reloc.Type.Value)
                {
                    case ElfNative.R_X86_64_NONE:
                    case ElfNative.R_X86_64_COPY:
                    case ElfNative.R_X86_64_DTPMOD64:
                    case ElfNative.R_X86_64_DTPOFF64:
                    case ElfNative.R_X86_64_TPOFF64:
                    case ElfNative.R_X86_64_TLSGD:
                    case ElfNative.R_X86_64_TLSLD:
                    case ElfNative.R_X86_64_DTPOFF32:
                    case ElfNative.R_X86_64_GOTTPOFF:
                    case ElfNative.R_X86_64_TPOFF32:
                        break;

                    case ElfNative.R_X86_64_64: // S + A
                    {
                        var S = (long) stream.ReadU64(isLsb);
                        stream.Position = patchOffset;
                        stream.WriteU64(isLsb, (ulong) (S + A));
                        break;
                    }
                    case ElfNative.R_X86_64_PC32: // S + A - P
                    {
                        var S = (long) stream.ReadU32(isLsb);
                        stream.Position = patchOffset;
                        stream.WriteU32(isLsb, (uint) (S + A - P));
                        break;
                    }
                    case ElfNative.R_X86_64_GOT32: // G + A
                    {
                        stream.WriteU32(isLsb, (uint) (G + A));
                        break;
                    }
                    case ElfNative.R_X86_64_PLT32: // L + A - P
                    {
                        stream.WriteU32(isLsb, (uint) (L + A - P));
                        break;
                    }
                    case ElfNative.R_X86_64_GLOB_DAT: // S
                    case ElfNative.R_X86_64_JUMP_SLOT: // S
                        break;

                    case ElfNative.R_X86_64_RELATIVE: // B + A
                    {
                        stream.WriteU64(isLsb, (ulong) (B + A));
                        break;
                    }
                    case ElfNative.R_X86_64_GOTPCREL: // G + GOT + A - P
                    {
                        stream.WriteU32(isLsb, (uint) (G + GOT + A - P));
                        break;
                    }
                    case ElfNative.R_X86_64_32: // S + A
                    {
                        var S = (long) stream.ReadU32(isLsb);
                        stream.Position = patchOffset;
                        stream.WriteU32(isLsb, (uint) (S + A));
                        break;
                    }
                    case ElfNative.R_X86_64_32S: // S + A
                    {
                        var S = (long) stream.ReadI32(isLsb);
                        stream.Position = patchOffset;
                        stream.WriteI32(isLsb, (int) (S + A));
                        break;
                    }
                    case ElfNative.R_X86_64_16: // S + A
                    {
                        var S = (long) stream.ReadU16(isLsb);
                        stream.Position = patchOffset;
                        stream.WriteU16(isLsb, (ushort) (S + A));
                        break;
                    }
                    case ElfNative.R_X86_64_PC16: // S + A - P
                    {
                        var S = (long) stream.ReadU16(isLsb);
                        stream.Position = patchOffset;
                        stream.WriteU16(isLsb, (ushort) (S + A - P));
                        break;
                    }
                    case ElfNative.R_X86_64_8: // S + A
                    {
                        var S = (long) stream.ReadU8();
                        stream.Position = patchOffset;
                        stream.WriteU8((byte) (S + A));
                        break;
                    }
                    case ElfNative.R_X86_64_PC8: // S + A - P
                    {
                        var S = (long)stream.ReadU8();
                        stream.Position = patchOffset;
                        stream.WriteU8((byte)(S + A - P));
                        break;
                    }

                    case ElfNative.R_X86_64_PC64: // S + A - P
                    {
                        var S = (long)stream.ReadU64(isLsb);
                        stream.Position = patchOffset;
                        stream.WriteU64(isLsb, (ulong)(S + A - P));
                        break;
                    }

                    case ElfNative.R_X86_64_GOTOFF64: // S + A - GOT
                    {
                        var S = (long)stream.ReadU64(isLsb);
                        stream.Position = patchOffset;
                        stream.WriteU64(isLsb, (ulong)(S + A - GOT));
                        break;
                    }
                    case ElfNative.R_X86_64_GOTPC32: // GOT + A - P
                        stream.WriteU32(isLsb, (uint)(GOT + A - P));
                        break;

                    case ElfNative.R_X86_64_GOT64: // G + A
                        stream.WriteU64(isLsb, (ulong)(G + A));
                        break;

                    case ElfNative.R_X86_64_GOTPCREL64: // G + GOT - P + A
                        stream.WriteU64(isLsb, (ulong)(G + GOT - P + A));
                        break;

                    case ElfNative.R_X86_64_GOTPC64: // GOT - P + A
                        stream.WriteU64(isLsb, (ulong)(GOT - P + A));
                        break;

                    case ElfNative.R_X86_64_GOTPLT64: // G + A
                        stream.WriteU64(isLsb, (ulong)(G + A));
                        break;

                    case ElfNative.R_X86_64_PLTOFF64: // L - GOT + A
                        stream.WriteU64(isLsb, (ulong)(L - GOT + A));
                        break;

                    case ElfNative.R_X86_64_GOTPC32_TLSDESC:
                    case ElfNative.R_X86_64_TLSDESC_CALL:
                    case ElfNative.R_X86_64_TLSDESC:
                        break;

                    case ElfNative.R_X86_64_RELATIVE64: // B + A
                        stream.WriteU64(isLsb, (ulong)(B + A));
                        break;

                    case ElfNative.R_X86_64_SIZE32:
                        stream.WriteU32(isLsb, (uint)(Z + A));
                        break;
                    case ElfNative.R_X86_64_SIZE64:
                        stream.WriteU64(isLsb, (ulong)(Z + A));
                        break;

                    case ElfNative.R_X86_64_IRELATIVE:
                    default:
                        throw new NotImplementedException($"Relocation {reloc} not implemented/supported");
                }
            }

            stream.Position = 0;
        }
    }
}