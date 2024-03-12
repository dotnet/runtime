// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using static ILCompiler.ObjectWriter.CodeViewNative;

namespace ILCompiler.ObjectWriter
{
    internal sealed class CodeViewFileTableBuilder
    {
        private readonly MemoryStream _stringTableWriter = new();
        private readonly MemoryStream _fileTableWriter = new();
        private readonly Dictionary<string, uint> _fileNameToIndex = new();

        public CodeViewFileTableBuilder()
        {
            // Insert empty entry at the beginning of string table
            _stringTableWriter.Write(stackalloc byte[2] { 0, 0 });
        }

        public uint GetFileIndex(string fileName)
        {
            if (fileName == "")
            {
                // Match the placeholder value from LLVM. We need to use a non-empty
                // string to ensure that the null terminator of the UTF-8 representation
                // is not treated as the terminator of the whole file name table.
                fileName = "<stdin>";
            }

            if (_fileNameToIndex.TryGetValue(fileName, out uint fileIndex))
            {
                return fileIndex;
            }
            else
            {
                uint stringTableIndex = (uint)_stringTableWriter.Position;
                _stringTableWriter.Write(Encoding.UTF8.GetBytes(fileName));
                _stringTableWriter.WriteByte(0);

                uint fileTableIndex = (uint)_fileTableWriter.Position;
                Span<byte> fileTableEntry = stackalloc byte[sizeof(uint) + sizeof(uint)];
                BinaryPrimitives.WriteUInt32LittleEndian(fileTableEntry, stringTableIndex);
                BinaryPrimitives.WriteUInt32LittleEndian(fileTableEntry.Slice(4), 0);
                _fileTableWriter.Write(fileTableEntry);

                _fileNameToIndex.Add(fileName, fileTableIndex);

                return fileTableIndex;
            }
        }

        public void Write(SectionWriter sectionWriter)
        {
            sectionWriter.WriteLittleEndian<uint>((uint)DebugSymbolsSubsectionType.FileChecksums);
            sectionWriter.WriteLittleEndian<uint>((uint)_fileTableWriter.Length);
            sectionWriter.EmitData(_fileTableWriter.GetBuffer().AsMemory(0, (int)_fileTableWriter.Length));
            sectionWriter.WriteLittleEndian<uint>((uint)DebugSymbolsSubsectionType.StringTable);
            sectionWriter.WriteLittleEndian<uint>((uint)_stringTableWriter.Length);
            sectionWriter.EmitData(_stringTableWriter.GetBuffer().AsMemory(0, (int)_stringTableWriter.Length));
        }
    }
}
