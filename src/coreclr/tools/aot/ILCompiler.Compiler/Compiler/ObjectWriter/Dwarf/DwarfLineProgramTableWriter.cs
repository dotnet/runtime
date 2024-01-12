// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using ILCompiler.DependencyAnalysis;

namespace ILCompiler.ObjectWriter
{
    internal sealed class DwarfLineProgramTableWriter : IDisposable
    {
        private readonly SectionWriter _lineSectionWriter;
        private readonly byte _targetPointerSize;
        private readonly RelocType _codeRelocType;
        private readonly Dictionary<string, uint> _directoryNameToIndex = new();
        private readonly Dictionary<DwarfFileName, uint> _fileNameToIndex = new();
        private readonly byte[] _sizeBuffer;

        public const byte MaximumOperationsPerInstruction = 1;
        public const sbyte LineBase = -5;
        public const byte LineRange = 14;
        public const byte OpCodeBase = 13;

        private static ReadOnlySpan<byte> StandardOpCodeLengths =>
        [
            0, // DW_LNS_copy
            1, // DW_LNS_advance_pc
            1, // DW_LNS_advance_line
            1, // DW_LNS_set_file
            1, // DW_LNS_set_column
            0, // DW_LNS_negate_stmt
            0, // DW_LNS_set_basic_block
            0, // DW_LNS_const_add_pc
            1, // DW_LNS_fixed_advance_pc
            0, // DW_LNS_set_prologue_end
            0, // DW_LNS_set_epilogue_begin
            1, // DW_LNS_set_isa
        ];

        public DwarfLineProgramTableWriter(
            SectionWriter lineSectionWriter,
            IReadOnlyList<DwarfFileName> fileNames,
            byte targetPointerSize,
            byte minimumInstructionLength,
            RelocType codeRelocType)
        {
            _lineSectionWriter = lineSectionWriter;
            _targetPointerSize = targetPointerSize;
            _codeRelocType = codeRelocType;

            // Length
            _sizeBuffer = new byte[sizeof(uint)];
            lineSectionWriter.EmitData(_sizeBuffer);
            // Version
            lineSectionWriter.WriteLittleEndian<ushort>(4);
            // Header Length
            var headerSizeBuffer = new byte[sizeof(uint)];
            lineSectionWriter.EmitData(headerSizeBuffer);
            var headerStart = lineSectionWriter.Position;
            lineSectionWriter.WriteByte(minimumInstructionLength);
            lineSectionWriter.WriteByte(MaximumOperationsPerInstruction);
            // default_is_stmt
            lineSectionWriter.WriteByte(1);
            // line_base
            lineSectionWriter.WriteByte(unchecked((byte)LineBase));
            // line_range
            lineSectionWriter.WriteByte(LineRange);
            // opcode_base
            lineSectionWriter.WriteByte(OpCodeBase);
            // standard_opcode_lengths
            foreach (var opcodeLength in StandardOpCodeLengths)
            {
                lineSectionWriter.WriteULEB128(opcodeLength);
            }

            // Directory names
            uint directoryIndex = 1;
            foreach (var fileName in fileNames)
            {
                if (fileName.Directory is string directoryName &&
                    !string.IsNullOrEmpty(directoryName) &&
                    !_directoryNameToIndex.ContainsKey(directoryName))
                {
                    lineSectionWriter.WriteUtf8String(directoryName);
                    _directoryNameToIndex.Add(directoryName, directoryIndex);
                    directoryIndex++;
                }
            }
            // Terminate directory list (empty string)
            lineSectionWriter.WriteByte(0);

            // File names
            uint fileNameIndex = 1;
            foreach (var fileName in fileNames)
            {
                directoryIndex = fileName.Directory is string directoryName && !string.IsNullOrEmpty(directoryName) ? _directoryNameToIndex[directoryName] : 0;

                lineSectionWriter.WriteUtf8String(fileName.Name);
                lineSectionWriter.WriteULEB128(directoryIndex);
                lineSectionWriter.WriteULEB128(fileName.Time);
                lineSectionWriter.WriteULEB128(fileName.Size);

                _fileNameToIndex[fileName] = fileNameIndex;
                fileNameIndex++;
            }
            // Terminate file name list (empty string)
            lineSectionWriter.WriteByte(0);

            // Update header size
            BinaryPrimitives.WriteInt32LittleEndian(headerSizeBuffer, (int)(lineSectionWriter.Position - headerStart));
        }

        public void Dispose()
        {
            // Update size
            BinaryPrimitives.WriteInt32LittleEndian(_sizeBuffer, (int)(_lineSectionWriter.Position - sizeof(uint)));
        }

        public void WriteLineSequence(DwarfLineSequenceWriter lineSequenceWriter)
        {
            lineSequenceWriter.Write(_lineSectionWriter, _targetPointerSize, _codeRelocType);
        }
    }
}
