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
        private Dictionary<uint, SubsectionWriter> _subsectionWriters = new();

        public CodeViewSymbolsBuilder(TargetArchitecture targetArchitecture)
        {
            _targetArchitecture = targetArchitecture;
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
            var symbolSubsection = GetSubsection(0xf1);

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

/*
  // We have an assembler directive that takes care of the whole line table.
  // We also increase function id for the next function.
  Streamer->emitCVLinetableDirective(FuncId++, Fn, FnEnd);
*/
        }

        public void WriteUserDefinedTypes(IList<(string, uint)> userDefinedTypes)
        {
            var symbolSubsection = GetSubsection(0xf1);
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
            if (_subsectionWriters.TryGetValue(subsectionKind, out var subsectionWriter))
            {
                return subsectionWriter;
            }
            else
            {
                subsectionWriter = new SubsectionWriter();
                _subsectionWriters.Add(subsectionKind, subsectionWriter);
                return subsectionWriter;
            }
        }

        public void Write(SectionWriter sectionWriter)
        {
            // Write CodeView version header
            Span<byte> versionBuffer = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(versionBuffer, 4);
            sectionWriter.Stream.Write(versionBuffer);

            Span<byte> subsectionHeader = stackalloc byte[sizeof(uint) + sizeof(uint)];
            foreach (var (subsectionKind, subsectionWriter) in _subsectionWriters)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(subsectionHeader, subsectionWriter._size);
                BinaryPrimitives.WriteUInt32LittleEndian(subsectionHeader, subsectionKind);
                sectionWriter.Stream.Write(subsectionHeader);

                foreach (var (offset, relocType, symbolName) in subsectionWriter._relocations)
                {
                    sectionWriter.EmitRelocation(
                        (int)offset,
                        default, // NOTE: We know the data are unused for the relocation types used in debug section
                        relocType,
                        symbolName,
                        0);
                }

                foreach (byte[] data in subsectionWriter._data)
                {
                    sectionWriter.Stream.Write(data);
                }

                sectionWriter.EmitAlignment(4);
            }
        }

        private sealed class SubsectionWriter
        {
            internal uint _size;
            internal List<byte[]> _data = new();
            internal List<(uint, RelocType, string)> _relocations = new();
            private ArrayBufferWriter<byte> _bufferWriter = new();

            public RecordWriter StartRecord(ushort recordType)
            {
                RecordWriter writer = new RecordWriter(this, _bufferWriter);
                writer.Write(recordType);
                return writer;
            }

            internal void CommitRecord()
            {
                byte[] lengthBuffer = new byte[sizeof(ushort)];
                BinaryPrimitives.WriteUInt16LittleEndian(lengthBuffer, (ushort)(_bufferWriter.WrittenCount));
                _data.Add(lengthBuffer);
                _size += sizeof(ushort);

                // Add data
                _data.Add(_bufferWriter.WrittenSpan.ToArray());
                _size += (uint)_bufferWriter.WrittenCount;

                _bufferWriter.Clear();
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
                    _subsectionWriter._size + sizeof(ushort) + (uint)_bufferWriter.WrittenCount,
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
