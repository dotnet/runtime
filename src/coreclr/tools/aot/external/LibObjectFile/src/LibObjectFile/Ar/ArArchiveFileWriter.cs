// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace LibObjectFile.Ar
{
    /// <summary>
    /// Class for writing an <see cref="ArArchiveFile"/> to a <see cref="Stream"/>.
    /// </summary>
    public class ArArchiveFileWriter: ObjectFileReaderWriter
    {
        private long _startStreamOffset;

        internal ArArchiveFileWriter(ArArchiveFile archiveFile, Stream stream) : base(stream)
        {
            ArArchiveFile = archiveFile;
            IsReadOnly = false;
        }

        private ArArchiveFile ArArchiveFile { get; }

        public override bool IsReadOnly { get; }

        internal void Write()
        {
            var localDiagnostics = new DiagnosticBag();
            ArArchiveFile.UpdateLayout(localDiagnostics);
            if (localDiagnostics.HasErrors)
            {
                throw new ObjectFileException("Invalid ar file", localDiagnostics);
            }
            // Copy for warnings
            localDiagnostics.CopyTo(Diagnostics);

            _startStreamOffset = Stream.Position;
            
            Stream.Write(ArArchiveFile.Magic);
            Span<byte> entryBuffer = stackalloc byte[ArFile.FileEntrySizeInBytes];
            
            var headers = ArArchiveFile.LongNamesTable;

            // Serialize all file entries
            for (var i = 0; i < ArArchiveFile.Files.Count; i++)
            {
                var file = ArArchiveFile.Files[i];

                // Serialize the headers at the correct position only if they are required
                if (headers != null && headers.Index == i && headers.Size > 0)
                {
                    WriteFileEntry(entryBuffer, headers);
                    if (Diagnostics.HasErrors) break;
                }

                WriteFileEntry(entryBuffer, file);
                if (Diagnostics.HasErrors) break;
            }

            if (Diagnostics.HasErrors)
            {
                throw new ObjectFileException("Unexpected error while writing ar file", Diagnostics);
            }
        }

        private void WriteFileEntry(Span<byte> buffer, ArFile file)
        {
            Debug.Assert((ulong)(Stream.Position - _startStreamOffset) == file.Offset);
            buffer.Fill((byte)' ');

            var name = file.InternalName;

            bool postFixSlash = false;

            if (name == null)
            {
                name = file.Name;
                if (ArArchiveFile.Kind != ArArchiveKind.Common && !name.EndsWith("/"))
                {
                    postFixSlash = true;
                }
            }

            uint? bsdNameLength = null;

            if (ArArchiveFile.Kind == ArArchiveKind.BSD)
            {
                var nameLength = Encoding.UTF8.GetByteCount(name);
                if (nameLength > ArFile.FieldNameLength)
                {
                    name = $"#1/{nameLength}";
                    bsdNameLength = (uint)nameLength;
                    postFixSlash = false;
                }
            }

            // Encode Length
            int length = Encoding.UTF8.GetBytes(name, buffer.Slice(0, ArFile.FieldNameLength));
            if (postFixSlash)
            {
                buffer[length] = (byte) '/';
            }

            if (!(file is ArLongNamesTable))
            {
                // 16	12	File modification timestamp Decimal
                EncodeDecimal(buffer, ArFile.FieldTimestampOffset, ArFile.FieldTimestampLength, (ulong)file.Timestamp.ToUnixTimeSeconds());
                // 28	6	Owner ID    Decimal
                EncodeDecimal(buffer, ArFile.FieldOwnerIdOffset, ArFile.FieldOwnerIdLength, file.OwnerId);
                // 34	6	Group ID    Decimal
                EncodeDecimal(buffer, ArFile.FieldGroupIdOffset, ArFile.FieldGroupIdLength, file.GroupId);
                // 40	8	File mode   Octal
                EncodeOctal(buffer, ArFile.FieldFileModeOffset, ArFile.FieldFileModeLength, file.FileMode);
            }
            // 48	10	File size in bytes Decimal
            EncodeDecimal(buffer, ArFile.FieldFileSizeOffset, ArFile.FieldFileSizeLength, file.Size);

            buffer[ArFile.FieldEndCharactersOffset] = 0x60;
            buffer[ArFile.FieldEndCharactersOffset + 1] = (byte) '\n';

            // Write the entry
            Stream.Write(buffer);

            // Handle BSD file name by serializing the name before the data if it is required
            if (bsdNameLength.HasValue)
            {
                uint nameLength = bsdNameLength.Value;
                var bufferName = ArrayPool<byte>.Shared.Rent((int) nameLength);
                Encoding.UTF8.GetBytes(file.Name, 0, file.Name.Length, bufferName, 0);
                try
                {
                    Stream.Write(bufferName, 0, (int)nameLength);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(bufferName);
                }
            }

            // Write the content following the entry
            file.WriteInternal(this);

            // Align to even byte
            if ((Stream.Position & 1) != 0)
            {
                Stream.WriteByte((byte)'\n');
            }
        }

        private void EncodeDecimal(in Span<byte> buffer, int offset, int size, ulong value)
        {
            int count = value == 0 ? 1 : 0;
            var check = value;
            while (check > 0)
            {
                check /= 10;
                count++;
            }

            if (count > size)
            {
                Diagnostics.Error(DiagnosticId.AR_ERR_ExpectingNewLineCharacter, $"Cannot encode decimal `{value}` as the size is exceeding the available size {size}");
                return;
            }

            check = value;
            for (int i = 0; i < count; i++)
            {
                var dec = check % 10;
                buffer[offset + count - i - 1] = (byte)((byte) '0' + dec);
                check = check / 10;
            }
        }

        private void EncodeOctal(in Span<byte> buffer, int offset, int size, ulong value)
        {
            int count = value == 0 ? 1 : 0;
            var check = value;
            while (check > 0)
            {
                check /= 8;
                count++;
            }

            if (count > size)
            {
                Diagnostics.Error(DiagnosticId.AR_ERR_ExpectingNewLineCharacter, $"Cannot encode octal `{value}` as the size is exceeding the available size {size}");
            }

            check = value;
            for (int i = 0; i < count; i++)
            {
                var dec = check % 8;
                buffer[offset + count - i - 1] = (byte)((byte)'0' + dec);
                check = check / 8;
            }
        }
    }
}