// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// A relocation section with the type <see cref="ElfSectionType.Relocation"/> or <see cref="ElfSectionType.RelocationAddends"/>
    /// </summary>
    public sealed class ElfRelocationTable : ElfSection
    {
        private readonly List<ElfRelocation> _entries;
        public const string DefaultName = ".rel";
        public const string DefaultNameWithAddends = ".rela";

        public ElfRelocationTable() : base(ElfSectionType.RelocationAddends)
        {
            Name = DefaultNameWithAddends;
            _entries = new List<ElfRelocation>();
        }

        /// <summary>
        /// Gets a list of <see cref="ElfRelocation"/> entries.
        /// </summary>
        public List<ElfRelocation> Entries => _entries;

        private static string GetDefaultName(ElfSectionType type)
        {
            return type == ElfSectionType.Relocation? DefaultName : DefaultNameWithAddends;
        }

        public override ElfSectionType Type
        {
            get => base.Type;
            set
            {
                if (value != ElfSectionType.Relocation && value != ElfSectionType.RelocationAddends)
                {
                    throw new ArgumentException($"Invalid type `{Type}` of the section [{Index}] `{nameof(ElfRelocationTable)}` while `{ElfSectionType.Relocation}` or `{ElfSectionType.RelocationAddends}` are expected");
                }
                base.Type = value;
            }
        }

        public bool IsRelocationWithAddends => this.Type == ElfSectionType.RelocationAddends;

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

        public override unsafe ulong TableEntrySize =>
            Parent == null || Parent.FileClass == ElfFileClass.None ? 0 :
            Parent.FileClass == ElfFileClass.Is32 ? (ulong) (IsRelocationWithAddends ? sizeof(ElfNative.Elf32_Rela) : sizeof(ElfNative.Elf32_Rel)) : (ulong) (IsRelocationWithAddends ? sizeof(ElfNative.Elf64_Rela) : sizeof(ElfNative.Elf64_Rel));

        private void Read32(ElfReader reader)
        {
            var numberOfEntries = base.Size / OriginalTableEntrySize;
            if (IsRelocationWithAddends)
            {
                for (ulong i = 0; i < numberOfEntries; i++)
                {
                    ElfNative.Elf32_Rela rel;
                    ulong streamOffset = (ulong)reader.Stream.Position;
                    if (!reader.TryReadData((int)OriginalTableEntrySize, out rel))
                    {
                        reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteRelocationAddendsEntry32Size, $"Unable to read entirely the relocation entry [{i}] from {Type} section [{Index}]. Not enough data (size: {OriginalTableEntrySize}) read at offset {streamOffset} from the stream");
                    }

                    var offset = reader.Decode(rel.r_offset);
                    var r_info = reader.Decode(rel.r_info);
                    var type = new ElfRelocationType(Parent.Arch, r_info & 0xFF);
                    var symbolIndex = r_info >> 8;
                    var addend = reader.Decode(rel.r_addend);

                    var entry = new ElfRelocation(offset, type, symbolIndex, addend);
                    _entries.Add(entry);
                }
            }
            else
            {
                for (ulong i = 0; i < numberOfEntries; i++)
                {
                    ElfNative.Elf32_Rel rel;
                    ulong streamOffset = (ulong)reader.Stream.Position;
                    if (!reader.TryReadData((int)OriginalTableEntrySize, out rel))
                    {
                        reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteRelocationEntry32Size, $"Unable to read entirely the relocation entry [{i}] from {Type} section [{Index}]. Not enough data (size: {OriginalTableEntrySize}) read at offset {streamOffset} from the stream");
                    }

                    var offset = reader.Decode(rel.r_offset);

                    var r_info = reader.Decode(rel.r_info);
                    var type = new ElfRelocationType(Parent.Arch, r_info & 0xFF);
                    var symbolIndex = r_info >> 8;

                    var entry = new ElfRelocation(offset, type, symbolIndex, 0);
                    _entries.Add(entry);
                }
            }
        }

        private void Read64(ElfReader reader)
        {
            var numberOfEntries = base.Size / OriginalTableEntrySize;
            if (IsRelocationWithAddends)
            {
                for (ulong i = 0; i < numberOfEntries; i++)
                {
                    ElfNative.Elf64_Rela rel;
                    ulong streamOffset = (ulong)reader.Stream.Position;
                    if (!reader.TryReadData((int)OriginalTableEntrySize, out rel))
                    {
                        reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteRelocationAddendsEntry64Size, $"Unable to read entirely the relocation entry [{i}] from {Type} section [{Index}]. Not enough data (size: {OriginalTableEntrySize}) read at offset {streamOffset} from the stream");
                    }

                    var offset = reader.Decode(rel.r_offset);

                    var r_info = reader.Decode(rel.r_info);
                    var type = new ElfRelocationType(Parent.Arch, (uint)(r_info & 0xFFFFFFFF));
                    var symbolIndex = (uint)(r_info >> 32);
                    var addend = reader.Decode(rel.r_addend);

                    var entry = new ElfRelocation(offset, type, symbolIndex, addend);
                    _entries.Add(entry);
                }
            }
            else
            {
                for (ulong i = 0; i < numberOfEntries; i++)
                {
                    ElfNative.Elf64_Rel rel;
                    ulong streamOffset = (ulong)reader.Stream.Position;
                    if (!reader.TryReadData((int)OriginalTableEntrySize, out rel))
                    {
                        reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteRelocationEntry64Size, $"Unable to read entirely the relocation entry [{i}] from {Type} section [{Index}]. Not enough data (size: {OriginalTableEntrySize}) read at offset {streamOffset} from the stream");
                    }

                    var offset = reader.Decode(rel.r_offset);

                    var r_info = reader.Decode(rel.r_info);
                    var type = new ElfRelocationType(Parent.Arch, (uint)(r_info & 0xFFFFFFFF));
                    var symbolIndex = (uint)(r_info >> 32);

                    var entry = new ElfRelocation(offset, type, symbolIndex, 0);
                    _entries.Add(entry);
                }
            }
        }
        
        private void Write32(ElfWriter writer)
        {
            if (IsRelocationWithAddends)
            {
                // Write all entries
                for (int i = 0; i < Entries.Count; i++)
                {
                    var entry = Entries[i];

                    var rel = new ElfNative.Elf32_Rela();
                    writer.Encode(out rel.r_offset, (uint)entry.Offset);
                    uint r_info = entry.Info32;
                    writer.Encode(out rel.r_info, r_info);
                    writer.Encode(out rel.r_addend, (int)entry.Addend);
                    writer.Write(rel);
                }
            }
            else
            {
                // Write all entries
                for (int i = 0; i < Entries.Count; i++)
                {
                    var entry = Entries[i];

                    var rel = new ElfNative.Elf32_Rel();
                    writer.Encode(out rel.r_offset, (uint)entry.Offset);
                    uint r_info = entry.Info32;
                    writer.Encode(out rel.r_info, r_info);
                    writer.Write(rel);
                }
            }
        }

        private void Write64(ElfWriter writer)
        {
            if (IsRelocationWithAddends)
            {
                // Write all entries
                for (int i = 0; i < Entries.Count; i++)
                {
                    var entry = Entries[i];

                    var rel = new ElfNative.Elf64_Rela();
                    writer.Encode(out rel.r_offset, entry.Offset);
                    ulong r_info = entry.Info64;
                    writer.Encode(out rel.r_info, r_info);
                    writer.Encode(out rel.r_addend, entry.Addend);
                    writer.Write(rel);
                }
            }
            else
            {
                // Write all entries
                for (int i = 0; i < Entries.Count; i++)
                {
                    var entry = Entries[i];

                    var rel = new ElfNative.Elf64_Rel();
                    writer.Encode(out rel.r_offset, (uint)entry.Offset);
                    ulong r_info = entry.Info64;
                    writer.Encode(out rel.r_info, r_info);
                    writer.Write(rel);
                }
            }
        }

        protected override void AfterRead(ElfReader reader)
        {
            var name = Name.Value;
            if (name == null)
            {
                return;
            }

            var defaultName = GetDefaultName(Type);

            if (!name.StartsWith(defaultName))
            {
                reader.Diagnostics.Warning(DiagnosticId.ELF_WRN_InvalidRelocationTablePrefixName, $"The name of the {Type} section `{this}` doesn't start with `{DefaultName}`");
            }
            else
            {
                // Check the name of relocation
                var currentTargetName = name.Substring(defaultName.Length);
                var sectionTargetName = Info.Section?.Name.Value;
                if (sectionTargetName != null && currentTargetName != sectionTargetName)
                {
                    reader.Diagnostics.Warning(DiagnosticId.ELF_WRN_InvalidRelocationTablePrefixTargetName, $"Invalid name `{name}` for relocation table  [{Index}] the current link section is named `{sectionTargetName}` so the expected name should be `{defaultName}{sectionTargetName}`", this);
                }
            }
        }

        public override void Verify(DiagnosticBag diagnostics)
        {
            base.Verify(diagnostics);

            //if (Info.Section == null)
            //{
            //    diagnostics.Error($"Invalid {nameof(Info)} of the section [{Index}] `{nameof(ElfRelocationTable)}` that cannot be null and must point to a valid section", this);
            //}
            //else 
            if (Info.Section != null && Info.Section.Parent != Parent)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidRelocationInfoParent, $"Invalid parent for the {nameof(Info)} of the section [{Index}] `{nameof(ElfRelocationTable)}`. It must point to the same {nameof(ElfObjectFile)} parent instance than this section parent", this);
            }

            var symbolTable = Link.Section as ElfSymbolTable;

            // Write all entries
            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                if (entry.Addend != 0 && !IsRelocationWithAddends)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidRelocationEntryAddend, $"Invalid relocation entry {i} in section [{Index}] `{nameof(ElfRelocationTable)}`. The addend != 0 while the section is not a `{ElfSectionType.RelocationAddends}`", this);
                }

                if (entry.Type.Arch != Parent.Arch)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidRelocationEntryArch, $"Invalid Arch `{entry.Type.Arch}` for relocation entry {i} in section [{Index}] `{nameof(ElfRelocationTable)}`. The arch doesn't match the arch `{Parent.Arch}`", this);
                }

                if (symbolTable != null && entry.SymbolIndex > (uint)symbolTable.Entries.Count)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidRelocationSymbolIndex, $"Out of range symbol index `{entry.SymbolIndex}` (max: {symbolTable.Entries.Count + 1} from symbol table {symbolTable}) for relocation entry {i} in section [{Index}] `{nameof(ElfRelocationTable)}`", this);
                }
            }
        }

        public override unsafe void UpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            Size = Parent == null || Parent.FileClass == ElfFileClass.None ? 0 :
                Parent.FileClass == ElfFileClass.Is32
                    ? (ulong)Entries.Count * (IsRelocationWithAddends ? (ulong)sizeof(ElfNative.Elf32_Rela) : (ulong)sizeof(ElfNative.Elf32_Rel))
                    : (ulong)Entries.Count * (IsRelocationWithAddends ? (ulong)sizeof(ElfNative.Elf64_Rela) : (ulong)sizeof(ElfNative.Elf64_Rel));

        }
    }
}