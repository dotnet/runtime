// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace LibObjectFile.Ar
{
    /// <summary>
    /// Internal class used for loading long file names for GNU `ar` and Windows `lib` archives.
    /// </summary>
    internal class ArLongNamesTable : ArFile
    {
        public const string DefaultName = "//";

        public ArLongNamesTable()
        {
        }

        public override string Name
        {
            get => DefaultName;
            set => base.Name = value;
        }

        public Dictionary<int, string> FileNames { get; private set; }

        public override bool IsSystem => true;

        protected override void Read(ArArchiveFileReader reader)
        {
            FileNames = new Dictionary<int, string>();

            var buffer = ArrayPool<byte>.Shared.Rent((int)Size);
            int readCount = reader.Stream.Read(buffer, 0, (int)Size);
            int startFileIndex = 0;
            for (int i = 0; i < readCount; i++)
            {
                if (buffer[i] == '\n')
                {
                    var fileNameLength = i - startFileIndex;
                    if (fileNameLength > 0)
                    {
                        // Discard trailing `/`
                        if (buffer[startFileIndex + fileNameLength - 1] == '/')
                        {
                            fileNameLength--;
                        }

                        // TODO: Is it UTF8 or ASCII?
                        FileNames.Add(startFileIndex, Encoding.UTF8.GetString(buffer, startFileIndex, fileNameLength));
                    }
                    startFileIndex = i + 1;
                }
            }
            ArrayPool<byte>.Shared.Return(buffer);
        }

        protected override void Write(ArArchiveFileWriter writer)
        {
            var buffer = ArrayPool<byte>.Shared.Rent((int)Size);
            uint offset = 0;
            for (var i = (int)Index; i < Parent.Files.Count; i++)
            {
                var file = Parent.Files[i];

                if (file is ArLongNamesTable) break;

                var fileName = file.Name;
                if (fileName == null || fileName.StartsWith("/"))
                {
                    continue;
                }

                // byte count + `/`
                var fileNameLength = Encoding.UTF8.GetByteCount(fileName) + 1;
                if (fileNameLength <= FieldNameLength)
                {
                    file.InternalName = null;
                    continue;
                }

                // Add `\n`
                fileNameLength++;
                
                if (fileNameLength > buffer.Length)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = ArrayPool<byte>.Shared.Rent(fileNameLength);
                }

                file.InternalName = $"/{offset}";
                
                Encoding.UTF8.GetBytes(fileName, 0, fileName.Length, buffer, 0);
                buffer[fileNameLength - 2] = (byte)'/';
                buffer[fileNameLength - 1] = (byte)'\n';

                writer.Write(buffer, 0, fileNameLength);
                offset += (uint)fileNameLength;
            }

            if ((offset & 1) != 0)
            {
                writer.Stream.WriteByte((byte)'\n');
            }
            ArrayPool<byte>.Shared.Return(buffer);
        }

        public override void UpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            Size = 0;

            if (Parent == null) return;

            ulong size = 0;
            for (var i = (int)Index; i < Parent.Files.Count; i++)
            {
                var file = Parent.Files[i];
                if (file is ArLongNamesTable) break;

                if (file.Name == null || file.Name.StartsWith("/"))
                {
                    continue;
                }

                var byteCount = Encoding.UTF8.GetByteCount(file.Name);
                // byte count + `/` 
                if (byteCount + 1 > FieldNameLength)
                {
                    // byte count + `/` + `\n`
                    size += (ulong)byteCount + 2;
                }
            }

            if ((size & 1) != 0)
            {
                size++;
            }

            // Once it is calculated freeze it
            Size = size;
        }
    }
}