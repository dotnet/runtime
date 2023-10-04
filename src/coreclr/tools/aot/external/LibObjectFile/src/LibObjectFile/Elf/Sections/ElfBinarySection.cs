// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// A custom section associated with its stream of data to read/write.
    /// </summary>
    public sealed class ElfBinarySection : ElfSection
    {
        public ElfBinarySection()
        {
        }

        public ElfBinarySection(Stream stream)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public override ElfSectionType Type
        {
            get => base.Type;
            set
            {
                // Don't allow relocation or symbol table to enforce proper usage
                if (value == ElfSectionType.Relocation || value == ElfSectionType.RelocationAddends)
                {
                    throw new ArgumentException($"Invalid type `{Type}` of the section [{Index}]. Must be used on a `{nameof(ElfRelocationTable)}` instead");
                }

                if (value == ElfSectionType.SymbolTable || value == ElfSectionType.DynamicLinkerSymbolTable)
                {
                    throw new ArgumentException($"Invalid type `{Type}` of the section [{Index}]. Must be used on a `{nameof(ElfSymbolTable)}` instead");
                }
                
                base.Type = value;
            }
        }

        public override ulong TableEntrySize => OriginalTableEntrySize;

        /// <summary>
        /// Gets or sets the associated stream to this section.
        /// </summary>
        public Stream Stream { get; set; }

        protected override void Read(ElfReader reader)
        {
            Stream = reader.ReadAsStream(Size);
        }

        protected override void Write(ElfWriter writer)
        {
            if (Stream == null) return;
            writer.Write(Stream);
        }

        public override void UpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            if (Type != ElfSectionType.NoBits)
            {
                Size = Stream != null ? (ulong)Stream.Length : 0;
            }
        }

        public override void Verify(DiagnosticBag diagnostics)
        {
            base.Verify(diagnostics);

            if (Type == ElfSectionType.NoBits && Stream != null)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidStreamForSectionNoBits, $"The {Type} section {this} must have a null stream");
            }
        }
    }
}