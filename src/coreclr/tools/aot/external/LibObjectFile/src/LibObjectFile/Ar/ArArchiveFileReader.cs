// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using LibObjectFile.Elf;

namespace LibObjectFile.Ar
{
    /// <summary>
    /// Class for reading and building an <see cref="ArArchiveFile"/> from a <see cref="Stream"/>.
    /// </summary>
    public class ArArchiveFileReader : ObjectFileReaderWriter
    {
        private ArLongNamesTable _futureHeaders;

        internal ArArchiveFileReader(ArArchiveFile arArchiveFile, Stream stream, ArArchiveFileReaderOptions options) : base(stream)
        {
            ArArchiveFile = arArchiveFile;
            Options = options;
            IsReadOnly = options.IsReadOnly;
        }

        public ArArchiveFileReaderOptions Options { get; }
        
        public override bool IsReadOnly { get; }

        internal ArArchiveFile ArArchiveFile { get; }

        internal static bool IsAr(Stream stream, DiagnosticBag diagnostics)
        {
            Span<byte> magic = stackalloc byte[ArArchiveFile.Magic.Length];
            int magicLength = stream.Read(magic);
            if (magicLength != magic.Length)
            {
                if (diagnostics != null)
                {
                    diagnostics.Error(DiagnosticId.AR_ERR_InvalidMagicLength, $"Invalid length {magicLength} while trying to read !<arch> from stream while expecting at least {magic.Length} bytes");
                }
                return false;
            }

            if (!magic.SequenceEqual(ArArchiveFile.Magic))
            {
                if (diagnostics != null)
                {
                    diagnostics.Error(DiagnosticId.AR_ERR_MagicNotFound, $"Magic !<arch>\\n not found");
                }
                return false;
            }

            return true;
        }

        internal void Read()
        {
            if (!IsAr(Stream, Diagnostics))
            {
                return;
            }

            Span<byte> entryBuffer = stackalloc byte[ArFile.FileEntrySizeInBytes];

            _futureHeaders = null;
            
            while (TryReadFileEntry(entryBuffer, out var fileEntry))
            {
                if (fileEntry is ArLongNamesTable arGnuFutureHeaders)
                {
                    if (_futureHeaders != null)
                    {
                        Diagnostics.Error(DiagnosticId.AR_ERR_InvalidDuplicatedFutureHeadersTable, $"Invalid duplicated future headers table found at offset {fileEntry.Offset} while another table was already found at offset {_futureHeaders.Offset}. This file is invalid.");
                        break;
                    }

                    _futureHeaders = arGnuFutureHeaders;
                }
                else
                {
                    ArArchiveFile.AddFile(fileEntry);
                }
            }

            if (Diagnostics.HasErrors) return;

            // Perform a pass after all entries have been read
            foreach (var arFileEntry in ArArchiveFile.Files)
            {
                arFileEntry.AfterReadInternal(this.Diagnostics);
            }
        }

        private bool TryReadFileEntry(Span<byte> buffer, out ArFile entry)
        {
            entry = null;

            Debug.Assert((Stream.Position & 1) == 0);

            long entryOffset = Stream.Position;
            int length = Stream.Read(buffer);
            if (length == 0)
            {
                return false;
            }

            if (length < buffer.Length)
            {
                Diagnostics.Error(DiagnosticId.AR_ERR_InvalidFileEntryLength, $"Invalid length {length} while trying to read a file entry from stream at offset {entryOffset}. Expecting {buffer.Length} bytes");
                return false;
            }
            // 0	16	File identifier ASCII
            // discard right padding characters
            int idLength = 16;
            while (idLength > 0)
            {
                if (buffer[idLength - 1] != ' ')
                {
                    break;
                }
                idLength--;
            }

            string name = null;
            ulong? bsdNameLength = null;

            if (idLength > 3 && ArArchiveFile.Kind == ArArchiveKind.BSD)
            {
                if (buffer[0] == '#' && buffer[1] == '1' && buffer[2] == '/')
                {
                    // If we have a future header table, we are using it and expecting only numbers
                    if (!TryDecodeDecimal(entryOffset, buffer, 3, ArFile.FieldNameLength - 3, $"BSD Name length following #1/ at offset {entryOffset}", out ulong bsdNameLengthDecoded))
                    {
                        // Don't try to process more entries, the archive might be corrupted
                        return false;
                    }

                    bsdNameLength = bsdNameLengthDecoded;
                }
            }

            // If the last char is `/`
            // Keep file names with / or //
            // But remove trailing `/`for regular file names
            if (!bsdNameLength.HasValue && ArArchiveFile.Kind != ArArchiveKind.Common && idLength > 0 && buffer[idLength - 1] == '/')
            {
                if (!(idLength == 1 || idLength == 2 && buffer[idLength - 2] == '/'))
                {
                    idLength--;
                }
            }

            if (_futureHeaders != null && buffer[0] == (byte)'/')
            {
                // If we have a future header table, we are using it and expecting only numbers
                if (!TryDecodeDecimal(entryOffset, buffer, 1, ArFile.FieldNameLength - 1, $"Name with offset to Future Headers Table at file offset {entryOffset}", out ulong offsetInFutureHeaders))
                {
                    // Don't try to process more entries, the archive might be corrupted
                    return false;
                }

                // If the number is ok, check that we have actually a string for this offset
                if (!_futureHeaders.FileNames.TryGetValue((int)offsetInFutureHeaders, out name))
                {
                    Diagnostics.Error(DiagnosticId.AR_ERR_InvalidReferenceToFutureHeadersTable, $"Invalid reference {offsetInFutureHeaders} found at file offset {entryOffset}. This file is invalid.");
                }
            }

            if (!bsdNameLength.HasValue && name == null)
            {
                name = idLength == 0 ? string.Empty : Encoding.UTF8.GetString(buffer.Slice(0, idLength));
            }
            
            // 16	12	File modification timestamp Decimal
            if (!TryDecodeDecimal(entryOffset, buffer, ArFile.FieldTimestampOffset, ArFile.FieldTimestampLength, "File modification timestamp Decimal", out ulong timestamp))
            {
                return false;
            }

            // 28	6	Owner ID    Decimal
            if (!TryDecodeDecimal(entryOffset, buffer, ArFile.FieldOwnerIdOffset, ArFile.FieldOwnerIdLength, "Owner ID", out ulong ownerId))
            {
                return false;
            }

            // 34	6	Group ID    Decimal
            if (!TryDecodeDecimal(entryOffset, buffer, ArFile.FieldGroupIdOffset, ArFile.FieldGroupIdLength, "Group ID", out ulong groupId))
            {
                return false;
            }

            // 40	8	File mode   Octal
            if (!TryDecodeOctal(entryOffset, buffer, ArFile.FieldFileModeOffset, ArFile.FieldFileModeLength, "File mode", out uint fileMode))
            {
                return false;
            }

            // 48	10	File size in bytes Decimal
            if (!TryDecodeDecimal(entryOffset, buffer, ArFile.FieldFileSizeOffset, ArFile.FieldFileSizeLength, "File size in bytes", out ulong fileSize))
            {
                return false;
            }

            // 58	2	Ending characters   0x60 0x0A
            if (buffer[ArFile.FieldEndCharactersOffset] != 0x60 || buffer[ArFile.FieldEndCharactersOffset + 1] != '\n')
            {
                Diagnostics.Error(DiagnosticId.AR_ERR_InvalidCharacterFoundInFileEntry, $"Invalid ASCII characters found 0x{buffer[ArFile.FieldEndCharactersOffset]:x} 0x{buffer[ArFile.FieldEndCharactersOffset+1]:x} instead of `\\n at the end of file entry at offset {entryOffset + ArFile.FieldEndCharactersOffset}");
                return false;
            }

            entry = CreateFileEntryFromName(name);
            entry.Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)timestamp);
            entry.OwnerId = (uint)ownerId;
            entry.GroupId = (uint)groupId;
            entry.FileMode = fileMode;
            entry.Offset = (ulong)entryOffset;
            entry.Size = fileSize;

            // Read the BSD name if necessary
            if (bsdNameLength.HasValue)
            {
                var nameLength = (int) bsdNameLength.Value;
                var bufferForName = ArrayPool<byte>.Shared.Rent(nameLength);
                var streamPosition = Stream.Position;
                var dataReadCount = Stream.Read(bufferForName, 0, nameLength);
                if (dataReadCount != nameLength)
                {
                    Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unexpected end of file while trying to read the filename from the data section of the file entry at offset of {streamPosition}. Expecting {nameLength} bytes while only {dataReadCount} bytes were read from the stream.");
                    return false;
                }
                name = Encoding.UTF8.GetString(bufferForName, 0, nameLength);
            }

            if (!entry.IsSystem)
            {
                if (name.Contains('/'))
                {
                    Diagnostics.Error(DiagnosticId.AR_ERR_InvalidCharacterInFileEntryName, $"The character `/` was found in the entry `{name}` while it is invalid.");
                    return false;
                }
                entry.Name = name;
            }

            entry.ReadInternal(this);

            // The end of an entry is always aligned
            if ((Stream.Position & 1) != 0)
            {
                long padOffset = Stream.Position;
                int pad = Stream.ReadByte();
                if (pad < 0)
                {
                    Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unexpected end of file while trying to Invalid character 0x{pad:x} found at offset {padOffset} while expecting \\n 0xa");
                    return false;
                }
                if (pad != '\n')
                {
                    Diagnostics.Error(DiagnosticId.AR_ERR_ExpectingNewLineCharacter, $"Invalid character 0x{pad:x} found at offset {padOffset} while expecting \\n 0xa");
                    return false;
                }
            }

            return true;
        }

        private bool TryDecodeDecimal(long entryOffset, Span<byte> buffer, int offset, int length, string fieldName, out ulong value)
        {
            value = 0;
            // == 0, expect number or space
            // == 1, expect space
            int state = 0;
            for (int i = 0; i < length; i++)
            {
                var c = buffer[offset + i];
                if (state == 0 && c >= '0' && c <= '9')
                {
                    value = value * 10 + (ulong) (c - '0');
                }
                else if (state >= 0 && c == ' ')
                {
                    state = 1;
                }
                else 
                {
                    Diagnostics.Error(DiagnosticId.AR_ERR_InvalidCharacterFoundInFileEntry, $"Invalid ASCII character 0x{c:x} found instead of {state switch { 0 => "' '/space or decimal 0-9", _ => "' '/space" }} in file entry at file offset {entryOffset + i} while decoding field entry `{fieldName}`");
                    return false;
                }
            }
            return true;
        }

        private bool TryDecodeOctal(long entryOffset, Span<byte> buffer, int offset, int length, string fieldName, out uint value)
        {
            value = 0;
            // == 0, expect number or space
            // == 1, expect space
            int state = 0;
            for (int i = 0; i < length; i++)
            {
                var c = buffer[offset + i];
                if (state == 0 && c >= '0' && c <= '7')
                {
                    value = value * 8 + (uint)(c - '0');
                }
                else if (state >= 0 && c == ' ')
                {
                    state = 1;
                }
                else
                {
                    Diagnostics.Error(DiagnosticId.AR_ERR_InvalidCharacterFoundInFileEntry, $"Invalid ASCII character 0x{c:x} found instead of {state switch { 0 => "' '/space or octal 0-7", _ => "' '/space" }} in file entry at file offset {entryOffset + i} while decoding field entry `{fieldName}`");
                    return false;
                }
            }
            return true;
        }

        private ArFile CreateFileEntryFromName(string name)
        {
            if (ArArchiveFile.Kind == ArArchiveKind.GNU)
            {
                switch (name)
                {
                    case ArSymbolTable.DefaultGNUSymbolTableName:
                        return new ArSymbolTable();
                    case ArLongNamesTable.DefaultName:
                        return new ArLongNamesTable();
                }
            }
            else if (ArArchiveFile.Kind == ArArchiveKind.BSD)
            {
                if (name == ArSymbolTable.DefaultBSDSymbolTableName)
                {
                    return new ArSymbolTable();
                }
            }

            if (Options.ProcessObjectFiles)
            {
                if (ElfObjectFile.IsElf(Stream))
                {
                    return new ArElfFile();
                }
            }

            return new ArBinaryFile();
        }
    }
}