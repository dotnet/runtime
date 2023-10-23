// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// A section with the type <see cref="ElfSectionType.SymbolTableSectionHeaderIndices"/>
    /// </summary>
    public sealed class ElfSymbolTableSectionHeaderIndices : ElfSection
    {
        public const string DefaultName = ".symtab_shndx";

        private readonly List<uint> _entries;

        public ElfSymbolTableSectionHeaderIndices() : base(ElfSectionType.SymbolTableSectionHeaderIndices)
        {
            Name = DefaultName;
            _entries = new List<uint>();
        }

        public override ElfSectionType Type
        {
            get => base.Type;
            set
            {
                if (value != ElfSectionType.SymbolTableSectionHeaderIndices)
                {
                    throw new ArgumentException($"Invalid type `{Type}` of the section [{Index}] `{nameof(ElfSymbolTableSectionHeaderIndices)}`. Only `{ElfSectionType.SymbolTableSectionHeaderIndices}` is valid");
                }
                base.Type = value;
            }
        }

        public override unsafe ulong TableEntrySize => sizeof(uint);

        protected override void Read(ElfReader reader)
        {
            var numberOfEntries = base.Size / TableEntrySize;
            _entries.Clear();
            _entries.Capacity = (int)numberOfEntries;
            for (ulong i = 0; i < numberOfEntries; i++)
            {
                _entries.Add(reader.ReadU32());
            }
        }

        protected override void Write(ElfWriter writer)
        {
            // Write all entries
            for (int i = 0; i < _entries.Count; i++)
            {
                writer.WriteU32(_entries[i]);
            }
        }

        protected override void AfterRead(ElfReader reader)
        {
            // Verify that the link is safe and configured as expected
            Link.TryGetSectionSafe<ElfSymbolTable>(nameof(ElfSymbolTableSectionHeaderIndices), nameof(Link), this, reader.Diagnostics, out var symbolTable, ElfSectionType.SymbolTable, ElfSectionType.DynamicLinkerSymbolTable);

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry != 0)
                {
                    var resolvedLink = reader.ResolveLink(new ElfSectionLink(entry), $"Invalid link section index {entry} for symbol table entry [{i}] from symbol table section [{this}]");

                    // Update the link in symbol table
                    var symbolTableEntry = symbolTable.Entries[i];
                    symbolTableEntry.Section = resolvedLink;
                    symbolTable.Entries[i] = symbolTableEntry;
                }
            }
        }

        public override void Verify(DiagnosticBag diagnostics)
        {
            base.Verify(diagnostics);

            // Verify that the link is safe and configured as expected
            if (!Link.TryGetSectionSafe<ElfSymbolTable>(nameof(ElfSymbolTableSectionHeaderIndices), nameof(Link), this, diagnostics, out var symbolTable, ElfSectionType.SymbolTable, ElfSectionType.DynamicLinkerSymbolTable))
            {
                return;
            }
        }

        public override unsafe void UpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            // Verify that the link is safe and configured as expected
            Link.TryGetSectionSafe<ElfSymbolTable>(nameof(ElfSymbolTableSectionHeaderIndices), nameof(Link), this, diagnostics, out var symbolTable, ElfSectionType.SymbolTable, ElfSectionType.DynamicLinkerSymbolTable);

            int numberOfEntries = 0;
            for (int i = 0; i < symbolTable.Entries.Count; i++)
            {
                if (symbolTable.Entries[i].Section.Section is { SectionIndex: >= ElfNative.SHN_LORESERVE })
                {
                    numberOfEntries = i + 1;
                }
            }

            _entries.Capacity = numberOfEntries;
            _entries.Clear();

            for (int i = 0; i < numberOfEntries; i++)
            {
                var section = symbolTable.Entries[i].Section.Section;
                if (section is { SectionIndex: >= ElfNative.SHN_LORESERVE })
                {
                    _entries.Add(section.SectionIndex);
                }
                else
                {
                    _entries.Add(0);
                }
            }

            Size = Parent == null || Parent.FileClass == ElfFileClass.None ? 0 : (ulong)numberOfEntries * sizeof(uint);
        }
    }
}