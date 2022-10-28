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

namespace ILCompiler.ObjectWriter
{
    internal sealed class CodeViewSymbolsBuilder
    {
        private TargetArchitecture _targetArchitecture;
        private SectionWriter _sectionWriter;
        private SubsectionWriter _stringTableWriter;
        private SubsectionWriter _fileTableWriter;
        private Dictionary<string, uint> _fileNameToIndex = new();

        public CodeViewSymbolsBuilder(TargetArchitecture targetArchitecture, SectionWriter sectionWriter)
        {
            _targetArchitecture = targetArchitecture;
            _sectionWriter = sectionWriter;

            // Write CodeView version header
            Span<byte> versionBuffer = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(versionBuffer, 4);
            sectionWriter.Stream.Write(versionBuffer);
        }

        // Maps an ICorDebugInfo register number to the corresponding CodeView
        // register number
        private ushort GetCVRegNum(uint regNum)
        {
            switch (_targetArchitecture)
            {
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

                //case TargetArchitecture.ARM64:
                //    ...
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
            using var symbolSubsection = GetSubsection(0xf1);

            using (var recordWriter = symbolSubsection.StartRecord(0x1147 /* S_GPROC32_ID */))
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
                using (var recordWriter = symbolSubsection.StartRecord(0x113e /* S_LOCAL */))
                {
                    recordWriter.Write(typeIndex);
                    recordWriter.Write((ushort)(debugVar.IsParameter ? 1 : 0)); // TODO: Flags
                    recordWriter.Write(debugVar.Name); // TODO: Names (this, etc.)
                }

                foreach (var range in debugVar.DebugVarInfo.Ranges)
                {
                    switch (range.VarLoc.LocationType)
                    {
                        case VarLocType.VLT_REG:
                        case VarLocType.VLT_REG_FP:
                            var cvRegNum = GetCVRegNum((uint)range.VarLoc.B);
                            if (cvRegNum != CV_REG_NONE)
                            {
                                using (var recordWriter = symbolSubsection.StartRecord(0x1141 /* S_DEFRANGE_REGISTER */))
                                {
                                    recordWriter.Write((ushort)cvRegNum);
                                    recordWriter.Write((ushort)0); // TODO: Attributes
                                    recordWriter.EmitSymbolReference(RelocType.IMAGE_REL_SECREL, methodName, (int)range.StartOffset);
                                    recordWriter.EmitSymbolReference(RelocType.IMAGE_REL_SECTION, methodName);
                                    recordWriter.Write((ushort)(range.EndOffset - range.StartOffset));
                                }
                            }
                            break;

                        case VarLocType.VLT_STK:
                            // FIXME: REGNUM_AMBIENT_SP
                            cvRegNum = GetCVRegNum((uint)range.VarLoc.B);
                            if (cvRegNum != CV_REG_NONE)
                            {
                                using (var recordWriter = symbolSubsection.StartRecord(0x1145 /* S_DEFRANGE_REGISTER_REL */))
                                {
                                    recordWriter.Write((ushort)cvRegNum);
                                    // TODO: Flags, CV_OFFSET_PARENT_LENGTH_LIMIT
                                    recordWriter.Write((ushort)0);
                                    recordWriter.Write((uint)range.VarLoc.C);
                                    recordWriter.EmitSymbolReference(RelocType.IMAGE_REL_SECREL, methodName, (int)range.StartOffset);
                                    recordWriter.EmitSymbolReference(RelocType.IMAGE_REL_SECTION, methodName);
                                    recordWriter.Write((ushort)(range.EndOffset - range.StartOffset));
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

            using (var recordWriter = symbolSubsection.StartRecord(0x114f /* S_PROC_ID_END */))
            {
            }
        }

        private uint GetFileIndex(string fileName)
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
                _stringTableWriter ??= GetSubsection(0xf3);

                uint stringTableIndex = _stringTableWriter._size;
                using (var stringRecord = _stringTableWriter.StartRecord())
                {
                    if (stringTableIndex == 0)
                    {
                        // Start the table with non-zero indexs
                        stringRecord.Write((ushort)0);
                        stringTableIndex += sizeof(ushort);
                    }
                    stringRecord.Write(fileName);
                }

                _fileTableWriter ??= GetSubsection(0xf4);
                uint fileTableIndex = _fileTableWriter._size;
                using (var fileRecord = _fileTableWriter.StartRecord())
                {
                    fileRecord.Write(stringTableIndex);
                    fileRecord.Write((uint)0);
                }

                _fileNameToIndex.Add(fileName, fileTableIndex);

                return fileTableIndex;
            }
        }

        public void EmitLineInfo(
            string methodName,
            int methodPCLength,
            IEnumerable<NativeSequencePoint> sequencePoints)
        {
            using var lineSubsection = GetSubsection(0xf2);

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
                    if (lastFileName == null || lastFileName != sequencePoint.FileName)
                    {
                        if (codes.Count > 0)
                        {
                            recordWriter.Write(fileIndex);
                            recordWriter.Write((uint)(codes.Count / 2));
                            recordWriter.Write((uint)(12 + 4 * codes.Count));
                            foreach (uint code in codes)
                            {
                                recordWriter.Write((uint)code);
                            }
                            codes.Clear();
                        }

                        fileIndex = GetFileIndex(sequencePoint.FileName);
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
            using var symbolSubsection = GetSubsection(0xf1);
            foreach (var (name, typeIndex) in userDefinedTypes)
            {
                using (var recordWriter = symbolSubsection.StartRecord(0x1108 /* S_UDT */))
                {
                    recordWriter.Write(typeIndex);
                    recordWriter.Write(name);
                }
            }
        }

        private SubsectionWriter GetSubsection(uint subsectionKind)
        {
            return new SubsectionWriter(subsectionKind, _sectionWriter);
        }

        public void Write()
        {
            _fileTableWriter?.Dispose();
            _stringTableWriter?.Dispose();
            _fileTableWriter = null;
            _stringTableWriter = null;
        }

        private sealed class SubsectionWriter : IDisposable
        {
            private uint _kind;
            private SectionWriter _sectionWriter;
            internal uint _size;
            internal List<byte[]> _data = new();
            internal List<(uint, RelocType, string)> _relocations = new();
            private ArrayBufferWriter<byte> _bufferWriter = new();
            internal bool _needLengthPrefix;

            public SubsectionWriter(uint kind, SectionWriter sectionWriter)
            {
                _kind = kind;
                _sectionWriter = sectionWriter;
            }

            public void Dispose()
            {
                Write(_sectionWriter);
            }

            public RecordWriter StartRecord()
            {
                _needLengthPrefix = false;
                return new RecordWriter(this, _bufferWriter);
            }

            public RecordWriter StartRecord(ushort recordType)
            {
                RecordWriter writer = new RecordWriter(this, _bufferWriter);
                writer.Write(recordType);
                _needLengthPrefix = true;
                return writer;
            }

            internal void CommitRecord()
            {
                if (_needLengthPrefix)
                {
                    byte[] lengthBuffer = new byte[sizeof(ushort)];
                    BinaryPrimitives.WriteUInt16LittleEndian(lengthBuffer, (ushort)(_bufferWriter.WrittenCount));
                    _data.Add(lengthBuffer);
                    _size += sizeof(ushort);
                }

                // Add data
                _data.Add(_bufferWriter.WrittenSpan.ToArray());
                _size += (uint)_bufferWriter.WrittenCount;

                _bufferWriter.Clear();
            }

            internal void Write(SectionWriter sectionWriter)
            {
                Span<byte> subsectionHeader = stackalloc byte[sizeof(uint) + sizeof(uint)];
                BinaryPrimitives.WriteUInt32LittleEndian(subsectionHeader.Slice(4), _size);
                BinaryPrimitives.WriteUInt32LittleEndian(subsectionHeader, _kind);
                sectionWriter.Stream.Write(subsectionHeader);

                foreach (var (offset, relocType, symbolName) in _relocations)
                {
                    sectionWriter.EmitRelocation(
                        (int)offset,
                        default, // NOTE: We know the data are unused for the relocation types used in debug section
                        relocType,
                        symbolName,
                        0);
                }

                foreach (byte[] data in _data)
                {
                    sectionWriter.Stream.Write(data);
                }

                sectionWriter.EmitAlignment(4);
            }
        }

        private ref struct RecordWriter
        {
            private SubsectionWriter _subsectionWriter;
            private ArrayBufferWriter<byte> _bufferWriter;

            public RecordWriter(SubsectionWriter subsectionWriter, ArrayBufferWriter<byte> bufferWriter)
            {
                _subsectionWriter = subsectionWriter;
                _bufferWriter = bufferWriter;
            }

            public void Dispose()
            {
                _subsectionWriter.CommitRecord();
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
                    (uint)(_subsectionWriter._needLengthPrefix ? sizeof(ushort) : 0) +
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
