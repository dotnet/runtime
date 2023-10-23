// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.Elf
{
    using static ElfNative;

    /// <summary>
    /// Internal implementation of <see cref="ElfWriter"/> to write to a stream an <see cref="ElfObjectFile"/> instance.
    /// </summary>
    /// <typeparam name="TEncoder">The encoder used for LSB/MSB conversion</typeparam>
    internal abstract class ElfWriter<TEncoder> : ElfWriter where TEncoder : struct, IElfEncoder 
    {
        private TEncoder _encoder;
        private ulong _startOfFile;

        protected ElfWriter(ElfObjectFile objectFile, Stream stream) : base(objectFile, stream)
        {
            _encoder = new TEncoder();
        }

        internal override void Write()
        {
            _startOfFile = (ulong)Stream.Position;
            WriteHeader();
            CheckProgramHeaders();
            WriteSections();
        }

        private ElfObjectFile.ElfObjectLayout Layout => ObjectFile.Layout;

        private void WriteHeader()
        {
            if (ObjectFile.FileClass == ElfFileClass.Is32)
            {
                WriteSectionHeader32();
            }
            else
            {
                WriteSectionHeader64();
            }
        }

        public override void Encode(out Elf32_Half dest, ushort value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf64_Half dest, ushort value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf32_Word dest, uint value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf64_Word dest, uint value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf32_Sword dest, int value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf64_Sword dest, int value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf32_Xword dest, ulong value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf32_Sxword dest, long value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf64_Xword dest, ulong value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf64_Sxword dest, long value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf32_Addr dest, uint value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf64_Addr dest, ulong value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf32_Off dest, uint offset)
        {
            _encoder.Encode(out dest, offset);
        }

        public override void Encode(out Elf64_Off dest, ulong offset)
        {
            _encoder.Encode(out dest, offset);
        }

        public override void Encode(out Elf32_Section dest, ushort index)
        {
            _encoder.Encode(out dest, index);
        }

        public override void Encode(out Elf64_Section dest, ushort index)
        {
            _encoder.Encode(out dest, index);
        }

        public override void Encode(out Elf32_Versym dest, ushort value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf64_Versym dest, ushort value)
        {
            _encoder.Encode(out dest, value);
        }

        private unsafe void WriteSectionHeader32()
        {
            var hdr = new Elf32_Ehdr();
            ObjectFile.CopyIdentTo(new Span<byte>(hdr.e_ident, EI_NIDENT));

            _encoder.Encode(out hdr.e_type, (ushort)ObjectFile.FileType);
            _encoder.Encode(out hdr.e_machine, (ushort)ObjectFile.Arch.Value);
            _encoder.Encode(out hdr.e_version, EV_CURRENT);
            _encoder.Encode(out hdr.e_entry, (uint)ObjectFile.EntryPointAddress);
            _encoder.Encode(out hdr.e_ehsize, Layout.SizeOfElfHeader);
            _encoder.Encode(out hdr.e_flags, (uint)ObjectFile.Flags);

            // program headers
            _encoder.Encode(out hdr.e_phoff, (uint)Layout.OffsetOfProgramHeaderTable);
            _encoder.Encode(out hdr.e_phentsize, Layout.SizeOfProgramHeaderEntry);
            _encoder.Encode(out hdr.e_phnum, (ushort) ObjectFile.Segments.Count);

            // entries for sections
            _encoder.Encode(out hdr.e_shoff, (uint)Layout.OffsetOfSectionHeaderTable);
            _encoder.Encode(out hdr.e_shentsize, Layout.SizeOfSectionHeaderEntry);
            _encoder.Encode(out hdr.e_shnum, ObjectFile.VisibleSectionCount >= ElfNative.SHN_LORESERVE ? (ushort)0 : (ushort)ObjectFile.VisibleSectionCount);
            uint shstrSectionIndex = ObjectFile.SectionHeaderStringTable?.SectionIndex ?? 0u;
            _encoder.Encode(out hdr.e_shstrndx, shstrSectionIndex >= ElfNative.SHN_LORESERVE ? (ushort)ElfNative.SHN_XINDEX : (ushort)shstrSectionIndex);

            Write(hdr);
        }

        private unsafe void WriteSectionHeader64()
        {
            var hdr = new Elf64_Ehdr();
            ObjectFile.CopyIdentTo(new Span<byte>(hdr.e_ident, EI_NIDENT));

            _encoder.Encode(out hdr.e_type, (ushort)ObjectFile.FileType);
            _encoder.Encode(out hdr.e_machine, (ushort)ObjectFile.Arch.Value);
            _encoder.Encode(out hdr.e_version, EV_CURRENT);
            _encoder.Encode(out hdr.e_entry, ObjectFile.EntryPointAddress);
            _encoder.Encode(out hdr.e_ehsize, Layout.SizeOfElfHeader);
            _encoder.Encode(out hdr.e_flags, (uint)ObjectFile.Flags);

            // program headers
            _encoder.Encode(out hdr.e_phoff, Layout.OffsetOfProgramHeaderTable);
            _encoder.Encode(out hdr.e_phentsize, Layout.SizeOfProgramHeaderEntry);
            _encoder.Encode(out hdr.e_phnum, (ushort)ObjectFile.Segments.Count);

            // entries for sections
            _encoder.Encode(out hdr.e_shoff, Layout.OffsetOfSectionHeaderTable);
            _encoder.Encode(out hdr.e_shentsize, (ushort)sizeof(Elf64_Shdr));
            _encoder.Encode(out hdr.e_shnum, ObjectFile.VisibleSectionCount >= ElfNative.SHN_LORESERVE ? (ushort)0 : (ushort)ObjectFile.VisibleSectionCount);
            uint shstrSectionIndex = ObjectFile.SectionHeaderStringTable?.SectionIndex ?? 0u;
            _encoder.Encode(out hdr.e_shstrndx, shstrSectionIndex >= ElfNative.SHN_LORESERVE ? (ushort)ElfNative.SHN_XINDEX : (ushort)shstrSectionIndex);

            Write(hdr);
        }

        private void CheckProgramHeaders()
        {
            if (ObjectFile.Segments.Count == 0)
            {
                return;
            }

            var offset = (ulong)Stream.Position - _startOfFile;
            if (offset != Layout.OffsetOfProgramHeaderTable)
            {
                throw new InvalidOperationException("Internal error. Unexpected offset for ProgramHeaderTable");
            }
        }

        private void WriteSections()
        {
            var sections = ObjectFile.Sections;
            if (sections.Count == 0) return;

            sections = ObjectFile.GetSectionsOrderedByStreamIndex();

            // We write the content all sections including shadows
            for (var i = 0; i < sections.Count; i++)
            {
                var section = sections[i];

                // Write only section with content
                if (section.HasContent)
                {
                    Stream.Position = (long)(_startOfFile + section.Offset);
                    section.WriteInternal(this);
                }
            }

            // Write section header table
            WriteSectionHeaderTable();
        }

        private void WriteSectionHeaderTable()
        {
            var offset = (ulong)Stream.Position - _startOfFile;
            var diff = Layout.OffsetOfSectionHeaderTable - offset;
            if (diff < 0 || diff > 8)
            {
                throw new InvalidOperationException("Internal error. Unexpected offset for SectionHeaderTable");
            }
            else if (diff != 0)
            {
                // Alignment
                Stream.Write(stackalloc byte[(int)diff]);
            }
            
            // Then write all regular sections
            var sections = ObjectFile.Sections;
            for (var i = 0; i < sections.Count; i++)
            {
                var section = sections[i];
                if (section.IsShadow) continue;
                WriteSectionTableEntry(section);
            }
        }

        private void WriteSectionTableEntry(ElfSection section)
        {
            if (ObjectFile.FileClass == ElfFileClass.Is32)
            {
                WriteSectionTableEntry32(section);
            }
            else
            {
                WriteSectionTableEntry64(section);
            }
        }

        private void WriteSectionTableEntry32(ElfSection section)
        {
            var shdr = new Elf32_Shdr();
            _encoder.Encode(out shdr.sh_name, ObjectFile.SectionHeaderStringTable?.GetOrCreateIndex(section.Name) ?? 0);
            _encoder.Encode(out shdr.sh_type, (uint)section.Type);
            _encoder.Encode(out shdr.sh_flags, (uint)section.Flags);
            _encoder.Encode(out shdr.sh_addr, (uint)section.VirtualAddress);
            _encoder.Encode(out shdr.sh_offset, (uint)section.Offset);
            if (section.Index == 0 && ObjectFile.VisibleSectionCount >= ElfNative.SHN_LORESERVE)
            {
                _encoder.Encode(out shdr.sh_size, ObjectFile.VisibleSectionCount);
                uint shstrSectionIndex = ObjectFile.SectionHeaderStringTable?.SectionIndex ?? 0u;
                _encoder.Encode(out shdr.sh_link, shstrSectionIndex >= ElfNative.SHN_LORESERVE ? shstrSectionIndex : 0);
            }
            else
            {
                _encoder.Encode(out shdr.sh_size, (uint)section.Size);
                _encoder.Encode(out shdr.sh_link, section.Link.GetIndex());
            }
            _encoder.Encode(out shdr.sh_info, section.Info.GetIndex());
            _encoder.Encode(out shdr.sh_addralign, (uint)section.Alignment);
            _encoder.Encode(out shdr.sh_entsize, (uint)section.TableEntrySize);
            Write(shdr);
        }

        private void WriteSectionTableEntry64(ElfSection section)
        {
            var shdr = new Elf64_Shdr();
            _encoder.Encode(out shdr.sh_name, ObjectFile.SectionHeaderStringTable?.GetOrCreateIndex(section.Name) ?? 0);
            _encoder.Encode(out shdr.sh_type, (uint)section.Type);
            _encoder.Encode(out shdr.sh_flags, (uint)section.Flags);
            _encoder.Encode(out shdr.sh_addr, section.VirtualAddress);
            _encoder.Encode(out shdr.sh_offset, section.Offset);
            if (section.Index == 0 && ObjectFile.VisibleSectionCount >= ElfNative.SHN_LORESERVE)
            {
                _encoder.Encode(out shdr.sh_size, ObjectFile.VisibleSectionCount);
                uint shstrSectionIndex = ObjectFile.SectionHeaderStringTable?.SectionIndex ?? 0u;
                _encoder.Encode(out shdr.sh_link, shstrSectionIndex >= ElfNative.SHN_LORESERVE ? shstrSectionIndex : 0);
            }
            else
            {
                _encoder.Encode(out shdr.sh_size, section.Size);
                _encoder.Encode(out shdr.sh_link, section.Link.GetIndex());
            }
            _encoder.Encode(out shdr.sh_info, section.Info.GetIndex());
            _encoder.Encode(out shdr.sh_addralign, section.Alignment);
            _encoder.Encode(out shdr.sh_entsize, section.TableEntrySize);
            Write(shdr);
        }
    }
}