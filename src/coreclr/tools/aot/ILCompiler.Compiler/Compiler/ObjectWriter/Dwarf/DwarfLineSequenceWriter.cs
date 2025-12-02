// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using ILCompiler.DependencyAnalysis;
using static ILCompiler.ObjectWriter.DwarfNative;

namespace ILCompiler.ObjectWriter
{
    internal sealed class DwarfLineSequenceWriter
    {
        private readonly ArrayBufferWriter<byte> _writer;
        private readonly string _sectionName;
        private readonly byte _minimumInstructionLength;
        private readonly uint _maxDeltaAddressPerSpecialCode;

        // DWARF state machine
        private ulong _address;
        private int _fileNameIndex;
        private int _line = 1;
        private int _column;

        public DwarfLineSequenceWriter(string sectionName, byte minimumInstructionLength)
        {
            _writer = new ArrayBufferWriter<byte>();
            _sectionName = sectionName;

            // Calculations hard code this
            Debug.Assert(DwarfLineProgramTableWriter.MaximumOperationsPerInstruction == 1);

            byte maxOperationAdvance = (255 - DwarfLineProgramTableWriter.OpCodeBase) / DwarfLineProgramTableWriter.LineRange;
            _maxDeltaAddressPerSpecialCode = (uint)(maxOperationAdvance * minimumInstructionLength);
            _minimumInstructionLength = minimumInstructionLength;
        }

        private void WriteByte(byte value)
        {
            _writer.GetSpan(1)[0] = value;
            _writer.Advance(1);
        }

        private void WriteULEB128(ulong value) => DwarfHelper.WriteULEB128(_writer, value);

        private void WriteSLEB128(long value) => DwarfHelper.WriteSLEB128(_writer, value);

        public void EmitLineInfo(int fileNameIndex, long methodAddress, NativeSequencePoint sequencePoint)
        {
            if (_column != sequencePoint.ColNumber)
            {
                _column = sequencePoint.ColNumber;
                WriteByte(DW_LNS_set_column);
                WriteULEB128((uint)_column);
            }

            if (_fileNameIndex != fileNameIndex)
            {
                _fileNameIndex = fileNameIndex;
                WriteByte(DW_LNS_set_file);
                WriteULEB128((uint)_fileNameIndex);
            }

            int deltaLine = sequencePoint.LineNumber - _line;
            if (deltaLine != 0)
            {
                bool canEncodeLineInSpecialCode =
                    deltaLine >= DwarfLineProgramTableWriter.LineBase &&
                    deltaLine < DwarfLineProgramTableWriter.LineBase + DwarfLineProgramTableWriter.LineRange;

                if (!canEncodeLineInSpecialCode)
                {
                    WriteByte(DW_LNS_advance_line);
                    WriteSLEB128(deltaLine);
                    deltaLine = 0;
                }
            }

            ulong deltaAddress = (ulong)sequencePoint.NativeOffset + (ulong)methodAddress - _address;
            if (deltaAddress > _maxDeltaAddressPerSpecialCode && deltaAddress <= (2U * _maxDeltaAddressPerSpecialCode))
            {
                deltaAddress -= _maxDeltaAddressPerSpecialCode;
                WriteByte(DW_LNS_const_add_pc);
            }

            if (deltaAddress > 0 || deltaLine != 0)
            {
                ulong operationAdvance = deltaAddress / _minimumInstructionLength;
                ulong opcode =
                    operationAdvance * DwarfLineProgramTableWriter.LineRange +
                    DwarfLineProgramTableWriter.OpCodeBase + (ulong)(deltaLine - DwarfLineProgramTableWriter.LineBase);
                if (opcode > 255)
                {
                    WriteByte(DW_LNS_advance_pc);
                    WriteULEB128((uint)operationAdvance);
                    if (deltaLine != 0)
                    {
                        WriteByte((byte)(DwarfLineProgramTableWriter.OpCodeBase + deltaLine - DwarfLineProgramTableWriter.LineBase));
                    }
                    else
                    {
                        WriteByte(DW_LNS_copy);
                    }
                }
                else
                {
                    WriteByte((byte)opcode);
                }
            }

            _line = sequencePoint.LineNumber;
            _address = (ulong)sequencePoint.NativeOffset + (ulong)methodAddress;
        }

        public void Write(SectionWriter lineSection, byte targetPointerSize, RelocType codeRelocType)
        {
            // Set the address to beginning of section
            lineSection.Write([0, (byte)(1u + targetPointerSize), DW_LNE_set_address]);
            lineSection.EmitSymbolReference(codeRelocType, _sectionName, 0);

            lineSection.Write(_writer.WrittenSpan);
            lineSection.Write([0, 1, DW_LNE_end_sequence]);
        }
    }
}
