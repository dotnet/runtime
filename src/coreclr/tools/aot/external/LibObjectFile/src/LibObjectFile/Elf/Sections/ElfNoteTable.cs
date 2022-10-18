// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using LibObjectFile.Utils;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// A note section with the type <see cref="ElfSectionType.Note"/>.
    /// </summary>
    public sealed class ElfNoteTable : ElfSection
    {
        public ElfNoteTable() : base(ElfSectionType.Note)
        {
            Entries = new List<ElfNote>();
        }

        /// <summary>
        /// Gets a list of <see cref="ElfNote"/> entries.
        /// </summary>
        public List<ElfNote> Entries { get; }
        
        public override ElfSectionType Type
        {
            get => base.Type;
            set
            {
                if (value != ElfSectionType.Note)
                {
                    throw new ArgumentException($"Invalid type `{Type}` of the section [{Index}] `{nameof(ElfNoteTable)}` while `{ElfSectionType.Note}` is expected");
                }
                base.Type = value;
            }
        }

        public override unsafe void UpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            ulong size = 0;
            ulong entrySize = (ulong)sizeof(ElfNative.Elf32_Nhdr);

            foreach (var elfNote in Entries)
            {
                var name = elfNote.GetName();
                if (name != null)
                {
                    size += (ulong)Encoding.UTF8.GetByteCount(name) + 1;
                    size = AlignHelper.AlignToUpper(size, 4);
                }

                size += (ulong)elfNote.GetDescriptorSize();
                size = AlignHelper.AlignToUpper(size, 4);

                size += entrySize;
            }
            Size = size;
        }
        
        protected override unsafe void Read(ElfReader reader)
        {
            var sizeToRead = (long)base.Size;

            var entrySize = (long)sizeof(ElfNative.Elf32_Nhdr);

            var startPosition = (ulong)reader.Stream.Position;
            while (sizeToRead >= entrySize)
            {
                ElfNative.Elf32_Nhdr nativeNote;
                ulong noteStartOffset = (ulong)reader.Stream.Position;
                if (!reader.TryReadData((int)entrySize, out nativeNote))
                {
                    reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteNoteEntrySize, $"Unable to read entirely the note entry [{Entries.Count}] from {Type} section [{Index}]. Not enough data (size: {entrySize}) read at offset {noteStartOffset} from the stream");
                    break;
                }

                var nameLength = reader.Decode(nativeNote.n_namesz);
                var descriptorLength = reader.Decode(nativeNote.n_descsz);
                
                var noteType = new ElfNoteTypeEx(reader.Decode(nativeNote.n_type));
                var noteName = reader.ReadStringUTF8NullTerminated(nameLength);
                SkipPaddingAlignedTo4Bytes(reader, (ulong)reader.Stream.Position - startPosition);

                var note = CreateNote(reader, noteName, noteType);

                note.ReadDescriptorInternal(reader, descriptorLength);

                SkipPaddingAlignedTo4Bytes(reader, (ulong)reader.Stream.Position - startPosition);

                Entries.Add(note);

                ulong noteEndOffset = (ulong)reader.Stream.Position;
                sizeToRead = sizeToRead - (long)(noteEndOffset - noteStartOffset);
            }
        }

        private void SkipPaddingAlignedTo4Bytes(ElfReader reader, ulong offset)
        {
            if ((offset & 3) != 0)
            {
                var toWrite = 4 - (int)(offset & 3);
                reader.Stream.Position += toWrite;
            }
        }

        protected override void Write(ElfWriter writer)
        {
            var expectedSizeWritten = Size;
            var startPosition = (ulong) writer.Stream.Position;
            foreach (var elfNote in Entries)
            {
                ElfNative.Elf32_Nhdr nativeNote;
                
                var noteName = elfNote.GetName();
                writer.Encode(out nativeNote.n_namesz, noteName == null ? 0 : ((uint) Encoding.UTF8.GetByteCount(noteName) + 1));
                writer.Encode(out nativeNote.n_descsz, elfNote.GetDescriptorSize());
                writer.Encode(out nativeNote.n_type, (uint)elfNote.GetNoteType().Value);

                writer.Write(nativeNote);

                if (noteName != null)
                {
                    writer.WriteStringUTF8NullTerminated(noteName);
                    WritePaddingAlignedTo4Bytes(writer, (ulong)writer.Stream.Position - startPosition);
                }

                elfNote.WriteDescriptorInternal(writer);
                WritePaddingAlignedTo4Bytes(writer, (ulong)writer.Stream.Position - startPosition);
            }

            var sizeWritten = (ulong) writer.Stream.Position - startPosition;
            Debug.Assert(expectedSizeWritten == sizeWritten);
        }

        private void WritePaddingAlignedTo4Bytes(ElfWriter writer, ulong offset)
        {
            if ((offset & 3) != 0)
            {
                var toWrite = 4 - (int)(offset & 3);
                for (int i = 0; i < toWrite; i++) writer.Stream.WriteByte(0);
            }
        }

        private static ElfNote CreateNote(ElfReader reader, string name, ElfNoteType type)
        {
            if (name == "GNU")
            {
                switch (type)
                {
                    case ElfNoteType.GNU_ABI_TAG:
                        return new ElfGnuNoteABITag();
                    case ElfNoteType.GNU_BUILD_ID:
                        return new ElfGnuNoteBuildId();
                }
            }

            ElfNote note = null;

            if (reader.Options.TryCreateNote != null)
            {
                note = reader.Options.TryCreateNote(name, type);
            }

            return note ?? new ElfCustomNote()
            {
                Name = name,
                Type = type
            };
        }
    }
}