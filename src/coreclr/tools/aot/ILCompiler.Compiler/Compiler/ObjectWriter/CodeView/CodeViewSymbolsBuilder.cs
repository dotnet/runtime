// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using ILCompiler.DependencyAnalysis;
using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.TypeSystem.TypesDebugInfo;

using static ILCompiler.ObjectWriter.CodeViewNative;
using static ILCompiler.ObjectWriter.CodeViewNative.CodeViewRegister;
using static ILCompiler.ObjectWriter.CodeViewNative.CodeViewSymbolDefinition;

namespace ILCompiler.ObjectWriter
{
    /// <summary>Builder for the CodeView .debug$S section.<summary>
    /// <remarks>
    /// The .debug$S section in CodeView contains information about methods,
    /// their parameters, stack layout, and line mapping.
    ///
    /// The section is divided into logical chunks known as subsections for
    /// different kind of information (eg. Symbols, Lines). Unlike the type
    /// section (<see cref="CodeViewTypesBuilder" />) this section does need
    /// relocations.
    ///
    /// The builder emits the subsections linearly into the section writer as
    /// the records are produced. Similarly to MSVC we output separate symbol
    /// subsection and line subsection for each method.
    ///
    /// File table (and related string table) are constructed through <see
    /// cref="CodeViewFileTableBuilder" /> and appended at the very end of
    /// the section.
    /// </remarks>
    internal sealed class CodeViewSymbolsBuilder
    {
        private readonly TargetArchitecture _targetArchitecture;
        private readonly SectionWriter _sectionWriter;

        public CodeViewSymbolsBuilder(TargetArchitecture targetArchitecture, SectionWriter sectionWriter)
        {
            _targetArchitecture = targetArchitecture;
            _sectionWriter = sectionWriter;

            // Write CodeView version header
            Span<byte> versionBuffer = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(versionBuffer, 4);
            sectionWriter.Write(versionBuffer);
        }

        // Maps an ICorDebugInfo register number to the corresponding CodeView
        // register number
        private CodeViewRegister GetCVRegNum(uint regNum)
        {
            switch (_targetArchitecture)
            {
                case TargetArchitecture.X86:
                    return regNum switch
                    {
                        0u => CV_REG_EAX,
                        1u => CV_REG_ECX,
                        2u => CV_REG_EDX,
                        3u => CV_REG_EBX,
                        4u => CV_REG_ESP,
                        5u => CV_REG_EBP,
                        6u => CV_REG_ESI,
                        7u => CV_REG_EDI,
                        // TODO: Floating point
                        _ => CV_REG_NONE,
                    };

                case TargetArchitecture.X64:
                    return regNum switch
                    {
                        0u => CV_AMD64_RAX,
                        1u => CV_AMD64_RCX,
                        2u => CV_AMD64_RDX,
                        3u => CV_AMD64_RBX,
                        4u => CV_AMD64_RSP,
                        5u => CV_AMD64_RBP,
                        6u => CV_AMD64_RSI,
                        7u => CV_AMD64_RDI,
                        8u => CV_AMD64_R8,
                        9u => CV_AMD64_R9,
                        10u => CV_AMD64_R10,
                        11u => CV_AMD64_R11,
                        12u => CV_AMD64_R12,
                        13u => CV_AMD64_R13,
                        14u => CV_AMD64_R14,
                        15u => CV_AMD64_R15,
                        // TODO: Floating point
                        _ => CV_REG_NONE,
                    };

                case TargetArchitecture.ARM64:
                    // X0-X28, FP, LR, SP have same order
                    if (regNum <= 32)
                        return (CodeViewRegister)(regNum + (uint)CV_ARM64_X0);
                    // TODO: Floating point
                    return CV_REG_NONE;

                default:
                    return CV_REG_NONE;
            }
        }

        public void EmitSubprogramInfo(
            string methodName,
            int methodPCLength,
            uint methodTypeIndex,
            IEnumerable<(DebugVarInfoMetadata, uint)> debugVars,
            IEnumerable<DebugEHClauseInfo> debugEHClauseInfos)
        {
            using var symbolSubsection = GetSubsection(DebugSymbolsSubsectionType.Symbols);

            // TODO: Do we need those?
            _ = methodTypeIndex;
            _ = debugEHClauseInfos;

            using (var recordWriter = symbolSubsection.StartRecord(S_GPROC32_ID))
            {
                recordWriter.Write((uint)0); // pointer to the parent
                recordWriter.Write((uint)0); // pointer to this blocks end
                recordWriter.Write((uint)0); // pointer to next symbol
                recordWriter.Write((uint)methodPCLength);
                recordWriter.Write((uint)0); // Debug start offset
                recordWriter.Write((uint)methodPCLength); // Debug end offset
                recordWriter.Write((uint)0); // Type index or ID
                recordWriter.EmitSymbolReference(RelocType.IMAGE_REL_SECREL, methodName);
                recordWriter.EmitSymbolReference(RelocType.IMAGE_REL_SECTION, methodName);
                recordWriter.Write((byte)0); // Proc flags
                recordWriter.Write(methodName);
            }

            foreach (var (debugVar, typeIndex) in debugVars)
            {
                using (var recordWriter = symbolSubsection.StartRecord(S_LOCAL))
                {
                    recordWriter.Write(typeIndex);
                    recordWriter.Write((ushort)(debugVar.IsParameter ? 1 : 0)); // TODO: Flags
                    recordWriter.Write(debugVar.Name); // TODO: Names (this, etc.)
                }

                foreach (var range in debugVar.DebugVarInfo.Ranges)
                {
                    uint rangeLength = range.EndOffset - range.StartOffset;

                    // Limit the range length to the maximum range expressible in CodeView.
                    // If this proves to be a problem we can emit additional records to
                    // describe the continued range of the variable.
                    if (rangeLength > 0xF000)
                    {
                        rangeLength = 0xF000;
                    }

                    switch (range.VarLoc.LocationType)
                    {
                        case VarLocType.VLT_REG:
                        case VarLocType.VLT_REG_FP:
                            CodeViewRegister cvRegNum = GetCVRegNum((uint)range.VarLoc.B);
                            if (cvRegNum != CV_REG_NONE)
                            {
                                using (var recordWriter = symbolSubsection.StartRecord(S_DEFRANGE_REGISTER))
                                {
                                    recordWriter.Write((ushort)cvRegNum);
                                    recordWriter.Write((ushort)0); // TODO: Attributes
                                    recordWriter.EmitSymbolReference(RelocType.IMAGE_REL_SECREL, methodName, (int)range.StartOffset);
                                    recordWriter.EmitSymbolReference(RelocType.IMAGE_REL_SECTION, methodName);
                                    recordWriter.Write((ushort)rangeLength);
                                }
                            }
                            break;

                        case VarLocType.VLT_STK:
                            // FIXME: Handle REGNUM_AMBIENT_SP
                            cvRegNum = GetCVRegNum((uint)range.VarLoc.B);
                            if (cvRegNum != CV_REG_NONE)
                            {
                                using (var recordWriter = symbolSubsection.StartRecord(S_DEFRANGE_REGISTER_REL))
                                {
                                    recordWriter.Write((ushort)cvRegNum);
                                    // TODO: Flags, CV_OFFSET_PARENT_LENGTH_LIMIT
                                    recordWriter.Write((ushort)0);
                                    recordWriter.Write((uint)range.VarLoc.C);
                                    recordWriter.EmitSymbolReference(RelocType.IMAGE_REL_SECREL, methodName, (int)range.StartOffset);
                                    recordWriter.EmitSymbolReference(RelocType.IMAGE_REL_SECTION, methodName);
                                    recordWriter.Write((ushort)rangeLength);
                                }
                            }
                            break;

                        case VarLocType.VLT_REG_BYREF:
                        case VarLocType.VLT_STK_BYREF:
                        case VarLocType.VLT_REG_REG:
                        case VarLocType.VLT_REG_STK:
                        case VarLocType.VLT_STK_REG:
                        case VarLocType.VLT_STK2:
                        case VarLocType.VLT_FPSTK:
                        case VarLocType.VLT_FIXED_VA:
                            break;

                        default:
                            Debug.Fail("Unknown variable location type");
                            break;
                    }
                }
            }

            using (var recordWriter = symbolSubsection.StartRecord(S_PROC_ID_END))
            {
            }
        }

        public void EmitLineInfo(
            CodeViewFileTableBuilder fileTableBuilder,
            string methodName,
            int methodPCLength,
            IEnumerable<NativeSequencePoint> sequencePoints)
        {
            using var lineSubsection = GetSubsection(DebugSymbolsSubsectionType.Lines);

            using (var recordWriter = lineSubsection.StartRecord())
            {
                recordWriter.EmitSymbolReference(RelocType.IMAGE_REL_SECREL, methodName);
                recordWriter.EmitSymbolReference(RelocType.IMAGE_REL_SECTION, methodName);
                recordWriter.Write((ushort)0); // TODO: Flags (eg. have columns)
                recordWriter.Write((uint)methodPCLength);

                string lastFileName = null;
                uint fileIndex = 0;
                List<uint> codes = new();

                foreach (var sequencePoint in sequencePoints)
                {
                    if (lastFileName is null || lastFileName != sequencePoint.FileName)
                    {
                        if (codes.Count > 0)
                        {
                            recordWriter.Write(fileIndex);
                            // Number of code pairs (ie. offset + sequence code)
                            recordWriter.Write((uint)(codes.Count / 2));
                            // Record size including this header
                            recordWriter.Write((uint)(3 * sizeof(uint) + codes.Count * sizeof(uint)));
                            foreach (uint code in codes)
                            {
                                recordWriter.Write((uint)code);
                            }
                            codes.Clear();
                        }

                        fileIndex = fileTableBuilder.GetFileIndex(sequencePoint.FileName);
                        lastFileName = sequencePoint.FileName;
                    }

                    codes.Add((uint)sequencePoint.NativeOffset);
                    codes.Add(0x80000000 | (uint)sequencePoint.LineNumber);
                }

                if (codes.Count > 0)
                {
                    recordWriter.Write(fileIndex);
                    recordWriter.Write((uint)(codes.Count / 2));
                    recordWriter.Write((uint)(12 + 4 * codes.Count));
                    foreach (uint code in codes)
                    {
                        recordWriter.Write((uint)code);
                    }
                }
            }
        }

        public void WriteUserDefinedTypes(IList<(string, uint)> userDefinedTypes)
        {
            using var symbolSubsection = GetSubsection(DebugSymbolsSubsectionType.Symbols);
            foreach (var (name, typeIndex) in userDefinedTypes)
            {
                using (var recordWriter = symbolSubsection.StartRecord(S_UDT))
                {
                    recordWriter.Write(typeIndex);
                    recordWriter.Write(name);
                }
            }
        }

        private SubsectionWriter GetSubsection(DebugSymbolsSubsectionType subsectionKind)
        {
            return new SubsectionWriter(subsectionKind, _sectionWriter);
        }

        private sealed class SubsectionWriter : IDisposable
        {
            private readonly DebugSymbolsSubsectionType _kind;
            private readonly SectionWriter _sectionWriter;
            internal uint _size;
            internal readonly List<byte[]> _data = new();
            internal readonly List<(uint, RelocType, string)> _relocations = new();

            public SubsectionWriter(DebugSymbolsSubsectionType kind, SectionWriter sectionWriter)
            {
                _kind = kind;
                _sectionWriter = sectionWriter;
            }

            public void Dispose()
            {
                Span<byte> subsectionHeader = stackalloc byte[sizeof(uint) + sizeof(uint)];
                BinaryPrimitives.WriteUInt32LittleEndian(subsectionHeader, (uint)_kind);
                BinaryPrimitives.WriteUInt32LittleEndian(subsectionHeader.Slice(4), _size);
                _sectionWriter.Write(subsectionHeader);

                foreach (var (offset, relocType, symbolName) in _relocations)
                {
                    _sectionWriter.EmitRelocation(
                        (int)offset,
                        default, // NOTE: We know the data are unused for the relocation types used in debug section
                        relocType,
                        symbolName,
                        0);
                }

                foreach (byte[] data in _data)
                {
                    _sectionWriter.Write(data);
                }

                _sectionWriter.EmitAlignment(4);
            }

            public RecordWriter StartRecord()
            {
                return new RecordWriter(this, false);
            }

            public RecordWriter StartRecord(CodeViewSymbolDefinition recordType)
            {
                RecordWriter writer = new RecordWriter(this, true);
                writer.Write((ushort)recordType);
                return writer;
            }
        }

        private ref struct RecordWriter
        {
            private readonly SubsectionWriter _subsectionWriter;
            private readonly ArrayBufferWriter<byte> _bufferWriter;
            private readonly bool _hasLengthPrefix;

            public RecordWriter(SubsectionWriter subsectionWriter, bool hasLengthPrefix)
            {
                _subsectionWriter = subsectionWriter;
                _bufferWriter = new();
                _hasLengthPrefix = hasLengthPrefix;
            }

            public void Dispose()
            {
                if (_hasLengthPrefix)
                {
                    byte[] lengthBuffer = new byte[sizeof(ushort)];
                    BinaryPrimitives.WriteUInt16LittleEndian(lengthBuffer, (ushort)(_bufferWriter.WrittenCount));
                    _subsectionWriter._data.Add(lengthBuffer);
                    _subsectionWriter._size += sizeof(ushort);
                }

                // Add data
                _subsectionWriter._data.Add(_bufferWriter.WrittenSpan.ToArray());
                _subsectionWriter._size += (uint)_bufferWriter.WrittenCount;

                _bufferWriter.Clear();
            }

            public void Write(byte value)
            {
                _bufferWriter.GetSpan(1)[0] = value;
                _bufferWriter.Advance(1);
            }

            public void Write(ushort value)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(_bufferWriter.GetSpan(sizeof(ushort)), value);
                _bufferWriter.Advance(sizeof(ushort));
            }

            public void Write(uint value)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(_bufferWriter.GetSpan(sizeof(uint)), value);
                _bufferWriter.Advance(sizeof(uint));
            }

            public void Write(ulong value)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(_bufferWriter.GetSpan(sizeof(ulong)), value);
                _bufferWriter.Advance(sizeof(ulong));
            }

            public void Write(string value)
            {
                int byteCount = Encoding.UTF8.GetByteCount(value) + 1;
                Encoding.UTF8.GetBytes(value, _bufferWriter.GetSpan(byteCount));
                _bufferWriter.Advance(byteCount);
            }

            public void EmitSymbolReference(
                RelocType relocType,
                string symbolName,
                int addend = 0)
            {
                _subsectionWriter._relocations.Add((
                    _subsectionWriter._size +
                    (uint)(_hasLengthPrefix ? sizeof(ushort) : 0) +
                    (uint)_bufferWriter.WrittenCount,
                    relocType, symbolName));

                switch (relocType)
                {
                    case RelocType.IMAGE_REL_SECTION:
                        Write((ushort)0);
                        break;
                    case RelocType.IMAGE_REL_SECREL:
                        Write((uint)addend);
                        break;
                    default:
                        throw new NotSupportedException("Unsupported relocation");
                }
            }
        }
    }
}
