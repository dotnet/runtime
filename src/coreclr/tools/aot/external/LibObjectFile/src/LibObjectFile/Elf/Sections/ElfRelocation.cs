// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// A relocation entry in the <see cref="ElfRelocationTable"/>
    /// This is the value seen in <see cref="ElfNative.Elf32_Rel"/> or <see cref="ElfNative.Elf64_Rel"/>
    /// </summary>
    public struct ElfRelocation
    {
        public ElfRelocation(ulong offset, ElfRelocationType type, uint symbolIndex, long addend)
        {
            Offset = offset;
            Type = type;
            SymbolIndex = symbolIndex;
            Addend = addend;
        }

        /// <summary>
        /// Gets or sets the offset.
        /// </summary>
        public ulong Offset { get; set; }

        /// <summary>
        /// Gets or sets the type of relocation.
        /// </summary>
        public ElfRelocationType Type { get; set; }

        /// <summary>
        /// Gets or sets the symbol index associated with the symbol table.
        /// </summary>
        public uint SymbolIndex { get; set; }

        /// <summary>
        /// Gets or sets the addend value.
        /// </summary>
        public long Addend { get; set; }

        /// <summary>
        /// Gets the computed Info value as expected by <see cref="ElfNative.Elf32_Rel.r_info"/>
        /// </summary>
        public uint Info32 =>
            ((uint) SymbolIndex << 8) | ((Type.Value & 0xFF));

        /// <summary>
        /// Gets the computed Info value as expected by <see cref="ElfNative.Elf64_Rel.r_info"/>
        /// </summary>
        public ulong Info64 =>
            ((ulong)SymbolIndex << 32) | (Type.Value);
        
        public override string ToString()
        {
            return $"{nameof(Offset)}: 0x{Offset:X16}, {nameof(Type)}: {Type}, {nameof(SymbolIndex)}: {SymbolIndex}, {nameof(Addend)}: 0x{Addend:X16}";
        }
    }
}