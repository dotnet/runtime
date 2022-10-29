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
        private MemoryStream _stringTableWriter = new();
        private MemoryStream _fileTableWriter = new();
        private Dictionary<string, uint> _fileNameToIndex = new();

        public CodeViewFileTableBuilder()
        {
            // Insert empty entry at the beginning of string table
            _stringTableWriter.Write(stackalloc byte[2]);
        }

        public uint GetFileIndex(string fileName)
        {
            if (fileName == "")
            {
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

        public void Write(Stream sectionStream)
        {
            Span<byte> subsectionHeader = stackalloc byte[sizeof(uint) + sizeof(uint)];

            BinaryPrimitives.WriteUInt32LittleEndian(subsectionHeader, (uint)DebugSymbolsSubsectionType.FileChecksums);
            BinaryPrimitives.WriteUInt32LittleEndian(subsectionHeader.Slice(4), (uint)_fileTableWriter.Length);
            sectionStream.Write(subsectionHeader);
            _fileTableWriter.Position = 0;
            _fileTableWriter.CopyTo(sectionStream);

            BinaryPrimitives.WriteUInt32LittleEndian(subsectionHeader, (uint)DebugSymbolsSubsectionType.StringTable);
            BinaryPrimitives.WriteUInt32LittleEndian(subsectionHeader.Slice(4), (uint)_stringTableWriter.Length);
            sectionStream.Write(subsectionHeader);
            _stringTableWriter.Position = 0;
            _stringTableWriter.CopyTo(sectionStream);
        }
    }
}
