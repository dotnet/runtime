// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Text;

namespace LibObjectFile.Elf
{
    public class ElfGnuNoteABITag : ElfGnuNote
    {
        public ElfGnuNoteOSKind OSKind { get; set; }

        public uint MajorVersion { get; set; }

        public uint MinorVersion { get; set; }

        public uint SubMinorVersion { get; set; }

        public override ElfNoteTypeEx GetNoteType() => new ElfNoteTypeEx(ElfNoteType.GNU_ABI_TAG);

        public override uint GetDescriptorSize() => 4 * sizeof(int);

        public override string GetDescriptorAsText()
        {
            var builder = new StringBuilder();
            builder.Append("OS: ");
            switch (OSKind)
            {
                case ElfGnuNoteOSKind.Linux:
                    builder.Append("Linux");
                    break;
                case ElfGnuNoteOSKind.Gnu:
                    builder.Append("Gnu");
                    break;
                case ElfGnuNoteOSKind.Solaris:
                    builder.Append("Solaris");
                    break;
                case ElfGnuNoteOSKind.FreeBSD:
                    builder.Append("FreeBSD");
                    break;
                default:
                    builder.Append($"0x{(uint) OSKind:x8}");
                    break;
            }

            builder.Append($", ABI: {MajorVersion}.{MinorVersion}.{SubMinorVersion}");
            return builder.ToString();
        }

        protected override void ReadDescriptor(ElfReader reader, uint descriptorLength)
        {
            NativeGnuNoteOS nativeGnuNote;
            if (!reader.TryReadData((int)descriptorLength, out nativeGnuNote))
            {
                reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleNoteGnuAbiTag, $"The {nameof(ElfGnuNoteABITag)} is incomplete in size. Expecting: {GetDescriptorSize()} but got {descriptorLength}");
            }

            OSKind = (ElfGnuNoteOSKind) reader.Decode(nativeGnuNote.OS);
            MajorVersion = reader.Decode(nativeGnuNote.MajorVersion);
            MinorVersion = reader.Decode(nativeGnuNote.MinorVersion);
            SubMinorVersion = reader.Decode(nativeGnuNote.SubMinorVersion);
        }

        protected override void WriteDescriptor(ElfWriter writer)
        {
            NativeGnuNoteOS nativeGnuNote;
            writer.Encode(out nativeGnuNote.OS, (uint) OSKind);
            writer.Encode(out nativeGnuNote.MajorVersion, (uint)MajorVersion);
            writer.Encode(out nativeGnuNote.MinorVersion, (uint)MinorVersion);
            writer.Encode(out nativeGnuNote.SubMinorVersion, (uint)SubMinorVersion);
            writer.Write(nativeGnuNote);
        }

        private struct NativeGnuNoteOS
        {
            public ElfNative.Elf32_Word OS;
            public ElfNative.Elf32_Word MajorVersion;
            public ElfNative.Elf32_Word MinorVersion;
            public ElfNative.Elf32_Word SubMinorVersion;
        }
    }
}