// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem;

namespace ILCompiler.ObjectWriter
{
    internal sealed class DwarfInfoWriter : IDisposable
    {
        private sealed record InfoReference(uint TypeIndex, int Position, byte[] Data);

        private readonly SectionWriter _infoSectionWriter;
        private readonly SectionWriter _stringTableWriter;
        private readonly SectionWriter _abbrevSectionWriter;
        private readonly SectionWriter _locSectionWriter;
        private readonly SectionWriter _rangeSectionWriter;
        private readonly DwarfBuilder _builder;
        private readonly RelocType _codeRelocType;
        private readonly List<InfoReference> _lateBoundReferences = new();
        private readonly Stack<DwarfAbbrev> _dieStack = new();
        private readonly Dictionary<DwarfAbbrev, int> _usedAbbrevs = new();
        private readonly ArrayBufferWriter<byte> _expressionBufferWriter = new();

        public DwarfInfoWriter(
            SectionWriter infoSectionWriter,
            SectionWriter stringTableWriter,
            SectionWriter abbrevSectionWriter,
            SectionWriter locSectionWriter,
            SectionWriter rangeSectionWriter,
            DwarfBuilder builder,
            RelocType codeRelocType)
        {
            _infoSectionWriter = infoSectionWriter;
            _stringTableWriter = stringTableWriter;
            _abbrevSectionWriter = abbrevSectionWriter;
            _locSectionWriter = locSectionWriter;
            _rangeSectionWriter = rangeSectionWriter;
            _builder = builder;
            _codeRelocType = codeRelocType;
        }

        public TargetArchitecture TargetArchitecture => _builder.TargetArchitecture;
        public int FrameRegister => _builder.FrameRegister;
        public byte TargetPointerSize => _builder.TargetPointerSize;
        public long Position => _infoSectionWriter.Position;

        public void WriteStartDIE(DwarfAbbrev abbrev)
        {
            if (_dieStack.Count > 0 && !_dieStack.Peek().HasChildren)
            {
                throw new InvalidOperationException($"Trying to write a children into DIE (Tag {_dieStack.Peek().Tag}) with DW_CHILDREN_no");
            }

            if (!_usedAbbrevs.TryGetValue(abbrev, out int abbreviationCode))
            {
                abbreviationCode = _usedAbbrevs.Count + 1;
                _usedAbbrevs.Add(abbrev, abbreviationCode);
            }


            _dieStack.Push(abbrev);
            WriteULEB128((ulong)abbreviationCode);
        }

        public void WriteEndDIE()
        {
            var abbrev = _dieStack.Pop();
            if (abbrev.HasChildren)
            {
                // End children list
                WriteUInt8(0);
            }
        }

        public void Write(ReadOnlySpan<byte> buffer) => _infoSectionWriter.Write(buffer);
        public void WriteULEB128(ulong value) => _infoSectionWriter.WriteULEB128(value);
        public void WriteUInt8(byte value) => _infoSectionWriter.WriteLittleEndian<byte>(value);
        public void WriteUInt16(ushort value) => _infoSectionWriter.WriteLittleEndian<ushort>(value);
        public void WriteUInt32(uint value) => _infoSectionWriter.WriteLittleEndian<uint>(value);
        public void WriteUInt64(ulong value) => _infoSectionWriter.WriteLittleEndian<ulong>(value);

        public void WriteAddressSize(ulong value)
        {
            switch (TargetPointerSize)
            {
                case 4: WriteUInt32((uint)value); break;
                case 8: WriteUInt64(value); break;
                default: throw new NotSupportedException();
            }
        }

        public void WriteStringReference(string value)
        {
            long stringsOffset = _stringTableWriter.Position;
            _stringTableWriter.WriteUtf8String(value);

            Debug.Assert(stringsOffset < uint.MaxValue);
            _infoSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_HIGHLOW, ".debug_str", stringsOffset);
        }

        public void WriteInfoAbsReference(long offset)
        {
            Debug.Assert(offset < uint.MaxValue);
            _infoSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_HIGHLOW, ".debug_info", offset);
        }

        public void WriteInfoReference(uint typeIndex)
        {
            uint offset = _builder.ResolveOffset(typeIndex);

            if (offset == 0)
            {
                // Late bound forward reference
                var data = new byte[sizeof(uint)];
                _lateBoundReferences.Add(new InfoReference(typeIndex, (int)_infoSectionWriter.Position, data));
                _infoSectionWriter.EmitData(data);
            }
            else
            {
                WriteUInt32(offset);
            }
        }

        public void WriteCodeReference(string sectionSymbolName, long offset = 0)
        {
            Debug.Assert(offset >= 0);
            _infoSectionWriter.EmitSymbolReference(_codeRelocType, sectionSymbolName, offset);
        }

        public void WriteLineReference(long offset)
        {
            Debug.Assert(offset < uint.MaxValue);
            _infoSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_HIGHLOW, ".debug_line", offset);
        }

        public DwarfExpressionBuilder GetExpressionBuilder()
        {
            _expressionBufferWriter.ResetWrittenCount();
            return new DwarfExpressionBuilder(TargetArchitecture, TargetPointerSize, _expressionBufferWriter);
        }

        public void WriteExpression(DwarfExpressionBuilder expressionBuilder)
        {
            _ = expressionBuilder;
            WriteULEB128((uint)_expressionBufferWriter.WrittenCount);
            Write(_expressionBufferWriter.WrittenSpan);
        }

        public void WriteStartLocationList()
        {
            long offset = _locSectionWriter.Position;
            Debug.Assert(offset < uint.MaxValue);
            _infoSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_HIGHLOW, ".debug_loc", (int)offset);
        }

        public void WriteLocationListExpression(string methodName, long startOffset, long endOffset, DwarfExpressionBuilder expressionBuilder)
        {
            _ = expressionBuilder;
            _locSectionWriter.EmitSymbolReference(_codeRelocType, methodName, startOffset);
            _locSectionWriter.EmitSymbolReference(_codeRelocType, methodName, endOffset);
            _locSectionWriter.WriteLittleEndian<ushort>((ushort)_expressionBufferWriter.WrittenCount);
            _locSectionWriter.Write(_expressionBufferWriter.WrittenSpan);
        }

        public void WriteEndLocationList()
        {
            _locSectionWriter.WritePadding(TargetPointerSize * 2);
        }

        public void WriteStartRangeList()
        {
            long offset = _rangeSectionWriter.Position;
            Debug.Assert(offset < uint.MaxValue);
            _infoSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_HIGHLOW, ".debug_ranges", offset);
        }

        public void WriteRangeListEntry(string symbolName, long startOffset, long endOffset)
        {
            _rangeSectionWriter.EmitSymbolReference(_codeRelocType, symbolName, startOffset);
            _rangeSectionWriter.EmitSymbolReference(_codeRelocType, symbolName, endOffset);
        }

        public void WriteEndRangeList()
        {
            _rangeSectionWriter.WritePadding(TargetPointerSize * 2);
        }

        public void Dispose()
        {
            // Debug.Assert(_dieStack.Count == 0);

            // Flush late bound forward references
            foreach (var lateBoundReference in _lateBoundReferences)
            {
                uint offset = _builder.ResolveOffset(lateBoundReference.TypeIndex);
                BinaryPrimitives.WriteUInt32LittleEndian(lateBoundReference.Data, offset);
            }

            // Write abbreviation section
            foreach ((DwarfAbbrev abbrev, int abbreviationCode) in _usedAbbrevs)
            {
                _abbrevSectionWriter.WriteULEB128((ulong)abbreviationCode);
                abbrev.Write(_abbrevSectionWriter, TargetPointerSize);
            }
            _abbrevSectionWriter.Write([0, 0]);
        }
    }
}
