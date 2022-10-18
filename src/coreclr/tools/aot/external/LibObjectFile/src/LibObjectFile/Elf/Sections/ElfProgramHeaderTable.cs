// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// The program header table as a <see cref="ElfShadowSection"/>.
    /// </summary>
    public sealed class ElfProgramHeaderTable : ElfShadowSection
    {
        public ElfProgramHeaderTable()
        {
            Name = ".shadow.phdrtab";
        }

        protected override void Read(ElfReader reader)
        {
            // This is not read by this instance but by ElfReader directly
        }

        public override unsafe ulong TableEntrySize
        {
            get
            {
                if (Parent == null) return 0;
                return Parent.FileClass == ElfFileClass.Is32 ? (ulong)sizeof(ElfNative.Elf32_Phdr) : (ulong)sizeof(ElfNative.Elf64_Phdr);
            }
        }

        public override void UpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            Size = 0;

            if (Parent == null) return;

            Size = (ulong) Parent.Segments.Count * Parent.Layout.SizeOfProgramHeaderEntry;
        }


        protected override void Write(ElfWriter writer)
        {
            for (int i = 0; i < Parent.Segments.Count; i++)
            {
                var header = Parent.Segments[i];
                if (Parent.FileClass == ElfFileClass.Is32)
                {
                    WriteProgramHeader32(writer, ref header);
                }
                else
                {
                    WriteProgramHeader64(writer, ref header);
                }
            }
        }
        
        private void WriteProgramHeader32(ElfWriter writer, ref ElfSegment segment)
        {
            var hdr = new ElfNative.Elf32_Phdr();

            writer.Encode(out hdr.p_type, segment.Type.Value);
            writer.Encode(out hdr.p_offset, (uint)segment.Offset);
            writer.Encode(out hdr.p_vaddr, (uint)segment.VirtualAddress);
            writer.Encode(out hdr.p_paddr, (uint)segment.PhysicalAddress);
            writer.Encode(out hdr.p_filesz, (uint)segment.Size);
            writer.Encode(out hdr.p_memsz, (uint)segment.SizeInMemory);
            writer.Encode(out hdr.p_flags, segment.Flags.Value);
            writer.Encode(out hdr.p_align, (uint)segment.Alignment);

            writer.Write(hdr);
        }

        private void WriteProgramHeader64(ElfWriter writer, ref ElfSegment segment)
        {
            var hdr = new ElfNative.Elf64_Phdr();

            writer.Encode(out hdr.p_type, segment.Type.Value);
            writer.Encode(out hdr.p_offset, segment.Offset);
            writer.Encode(out hdr.p_vaddr, segment.VirtualAddress);
            writer.Encode(out hdr.p_paddr, segment.PhysicalAddress);
            writer.Encode(out hdr.p_filesz, segment.Size);
            writer.Encode(out hdr.p_memsz, segment.SizeInMemory);
            writer.Encode(out hdr.p_flags, segment.Flags.Value);
            writer.Encode(out hdr.p_align, segment.Alignment);

            writer.Write(hdr);
        }
    }
}