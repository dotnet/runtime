// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// A symbol table section with the type <see cref="ElfSectionType.SymbolTable"/> or <see cref="ElfSectionType.DynamicLinkerSymbolTable"/>
    /// </summary>
    public sealed class ElfSymbolTable : ElfSection
    {
        public const string DefaultName = ".symtab";

        public ElfSymbolTable() : base(ElfSectionType.SymbolTable)
        {
            Name = DefaultName;
            Entries = new List<ElfSymbol>();
            Entries.Add(new ElfSymbol());
        }

        public override ElfSectionType Type
        {
            get => base.Type;
            set
            {
                if (value != ElfSectionType.SymbolTable && value != ElfSectionType.DynamicLinkerSymbolTable)
                {
                    throw new ArgumentException($"Invalid type `{Type}` of the section [{Index}] `{nameof(ElfSymbolTable)}`. Only `{ElfSectionType.SymbolTable}` or `{ElfSectionType.DynamicLinkerSymbolTable}` are valid");
                }
                base.Type = value;
            }
        }
        
        /// <summary>
        /// Gets a list of <see cref="ElfSymbol"/> entries.
        /// </summary>
        public List<ElfSymbol> Entries { get;  }

        public override unsafe ulong TableEntrySize =>
            Parent == null || Parent.FileClass == ElfFileClass.None ? 0 :
            Parent.FileClass == ElfFileClass.Is32 ? (ulong) sizeof(ElfNative.Elf32_Sym) : (ulong) sizeof(ElfNative.Elf64_Sym);

        protected override void Read(ElfReader reader)
        {
            if (Parent.FileClass == ElfFileClass.Is32)
            {
                Read32(reader);
            }
            else
            {
                Read64(reader);
            }
        }

        protected override void Write(ElfWriter writer)
        {
            if (Parent.FileClass == ElfFileClass.Is32)
            {
                Write32(writer);
            }
            else
            {
                Write64(writer);
            }
        }

        private void Read32(ElfReader reader)
        {
            var numberOfEntries = base.Size / OriginalTableEntrySize;
            for (ulong i = 0; i < numberOfEntries; i++)
            {
                ElfNative.Elf32_Sym sym;
                ulong streamOffset = (ulong)reader.Stream.Position;
                if (!reader.TryReadData((int)OriginalTableEntrySize, out sym))
                {
                    reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteSymbolEntry32Size, $"Unable to read entirely the symbol entry [{i}] from {Type} section [{Index}]. Not enough data (size: {OriginalTableEntrySize}) read at offset {streamOffset} from the stream");
                }

                var entry = new ElfSymbol();
                entry.Name = new ElfString(reader.Decode(sym.st_name));
                entry.Value = reader.Decode(sym.st_value);
                entry.Size = reader.Decode(sym.st_size);

                var st_info = sym.st_info;
                entry.Type = (ElfSymbolType) (st_info & 0xF);
                entry.Bind = (ElfSymbolBind)(st_info >> 4);
                entry.Visibility = (ElfSymbolVisibility) sym.st_other;
                entry.Section = new ElfSectionLink(reader.Decode(sym.st_shndx));

                // If the entry 0 was validated
                if (i == 0 && entry == ElfSymbol.Empty)
                {
                    continue;
                }

                Entries.Add(entry);
            }
        }

        private void Read64(ElfReader reader)
        {
            var numberOfEntries = base.Size / OriginalTableEntrySize;
            for (ulong i = 0; i < numberOfEntries; i++)
            {
                ElfNative.Elf64_Sym sym;
                ulong streamOffset = (ulong)reader.Stream.Position;
                if (!reader.TryReadData((int)OriginalTableEntrySize, out sym))
                {
                    reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteSymbolEntry64Size, $"Unable to read entirely the symbol entry [{i}] from {Type} section [{Index}]. Not enough data (size: {OriginalTableEntrySize}) read at offset {streamOffset} from the stream");
                }

                var entry = new ElfSymbol();
                entry.Name = new ElfString(reader.Decode(sym.st_name));
                entry.Value = reader.Decode(sym.st_value);
                entry.Size = reader.Decode(sym.st_size);

                var st_info = sym.st_info;
                entry.Type = (ElfSymbolType)(st_info & 0xF);
                entry.Bind = (ElfSymbolBind)(st_info >> 4);
                entry.Visibility = (ElfSymbolVisibility)sym.st_other;
                entry.Section = new ElfSectionLink(reader.Decode(sym.st_shndx));

                // If the entry 0 was validated
                if (i == 0 && entry == ElfSymbol.Empty)
                {
                    continue;
                }

                Entries.Add(entry);
            }
        }

        
        private void Write32(ElfWriter writer)
        {
            var stringTable = (ElfStringTable)Link.Section;

            // Write all entries
            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];

                var sym = new ElfNative.Elf32_Sym();
                writer.Encode(out sym.st_name, (ushort)stringTable.GetOrCreateIndex(entry.Name));
                writer.Encode(out sym.st_value, (uint)entry.Value);
                writer.Encode(out sym.st_size, (uint)entry.Size);
                sym.st_info = (byte)(((byte) entry.Bind << 4) | (byte) entry.Type);
                sym.st_other = (byte) ((byte) entry.Visibility & 3);
                writer.Encode(out sym.st_shndx, (ElfNative.Elf32_Half) entry.Section.GetIndex());

                writer.Write(sym);
            }
        }

        private void Write64(ElfWriter writer)
        {
            var stringTable = (ElfStringTable)Link.Section;

            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];

                var sym = new ElfNative.Elf64_Sym();
                writer.Encode(out sym.st_name, stringTable.GetOrCreateIndex(entry.Name));
                writer.Encode(out sym.st_value, entry.Value);
                writer.Encode(out sym.st_size, entry.Size);
                sym.st_info = (byte)(((byte)entry.Bind << 4) | (byte)entry.Type);
                sym.st_other = (byte)((byte)entry.Visibility & 3);
                writer.Encode(out sym.st_shndx, (ElfNative.Elf64_Half)entry.Section.GetIndex());

                writer.Write(sym);
            }
        }

        protected override void AfterRead(ElfReader reader)
        {
            // Verify that the link is safe and configured as expected
            Link.TryGetSectionSafe<ElfStringTable>(nameof(ElfSymbolTable), nameof(Link), this, reader.Diagnostics, out var stringTable, ElfSectionType.StringTable);

            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                if (stringTable != null)
                {
                    if (stringTable.TryResolve(entry.Name, out var newEntry))
                    {
                        entry.Name = newEntry;
                    }
                    else
                    {
                        reader.Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSymbolEntryNameIndex, $"Invalid name index [{entry.Name.Index}] for symbol [{i}] in section [{this}]");
                    }
                }

                entry.Section = reader.ResolveLink(entry.Section, $"Invalid link section index {entry.Section.SpecialIndex} for  symbol table entry [{i}] from symbol table section [{this}]");

                Entries[i] = entry;
            }
        }

        public override void Verify(DiagnosticBag diagnostics)
        {
            base.Verify(diagnostics);

            // Verify that the link is safe and configured as expected
            if (!Link.TryGetSectionSafe<ElfStringTable>(nameof(ElfSymbolTable), nameof(Link), this, diagnostics, out var stringTable, ElfSectionType.StringTable))
            {
                return;
            }

            bool isAllowingLocal = true;

            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];

                if (i == 0 && entry != ElfSymbol.Empty)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidFirstSymbolEntryNonNull, $"Invalid entry #{i} in the {nameof(ElfSymbolTable)} section [{Index}]. The first entry must be null/undefined");
                }

                if (entry.Section.Section != null && entry.Section.Section.Parent != Parent)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSymbolEntrySectionParent, $"Invalid section for the symbol entry #{i} in the {nameof(ElfSymbolTable)} section [{Index}]. The section of the entry `{entry}` must the same than this symbol table section");
                }

                stringTable.ReserveString(entry.Name);

                // Update the last local index
                if (entry.Bind == ElfSymbolBind.Local)
                {
                    // + 1 For the plus one
                    Info = new ElfSectionLink((uint)(i + 1));
                    if (!isAllowingLocal)
                    {
                        diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSymbolEntryLocalPosition, $"Invalid position for the LOCAL symbol entry #{i} in the {nameof(ElfSymbolTable)} section [{Index}]. A LOCAL symbol entry must be before any other symbol entry");
                    }
                }
                else
                {
                    isAllowingLocal = false;
                }
            }
        }

        public override unsafe void UpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            Size = Parent == null || Parent.FileClass == ElfFileClass.None ? 0 :
                Parent.FileClass == ElfFileClass.Is32 ? (ulong)(Entries.Count * sizeof(ElfNative.Elf32_Sym)) : (ulong)(Entries.Count * sizeof(ElfNative.Elf64_Sym));
        }
    }
}