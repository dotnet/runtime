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

        public CodeViewSymbolsBuilder(TargetArchitecture targetArchitecture, Stream outputStream)
        {
            _targetArchitecture = targetArchitecture;

            // Write CodeView version header
            Span<byte> versionBuffer = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(versionBuffer, 4);
            _outputStream.Write(versionBuffer);
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

        private static 


        /*private ref struct SubsectionWriter
        {

        }*/

        /*private ref struct LeafRecordWriter
        {
            private ArrayBufferWriter<byte> _bufferWriter;
            private Stream _outputStream;

            public LeafRecordWriter(ArrayBufferWriter<byte> bufferWriter, Stream outputStream)
            {
                _bufferWriter = bufferWriter;
                _outputStream = outputStream;
            }

            public void Dispose()
            {
                int length = sizeof(ushort) + _bufferWriter.WrittenCount;
                int padding = ((length + 3) & ~3) - length;
                Span<byte> lengthBuffer = stackalloc byte[sizeof(ushort)];
                BinaryPrimitives.WriteUInt16LittleEndian(lengthBuffer, (ushort)(length + padding - sizeof(ushort)));
                _outputStream.Write(lengthBuffer);
                _outputStream.Write(_bufferWriter.WrittenSpan);
                _outputStream.Write(stackalloc byte[padding]);
                _bufferWriter.Clear();

                // TODO: LF_INDEX for long records
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

            public void WriteEncodedInteger(ulong value)
            {
                if (value < LF_NUMERIC)
                {
                    Write((ushort)value);
                }
                else if (value <= ushort.MaxValue)
                {
                    Write(LF_USHORT);
                    Write((ushort)value);
                }
                else if (value <= uint.MaxValue)
                {
                    Write(LF_ULONG);
                    Write((uint)value);
                }
                else
                {
                    Write(LF_UQUADWORD);
                    Write(value);
                }
            }

            public void WritePadding()
            {
                int paddingLength = ((_bufferWriter.WrittenCount - 2 + 3) & ~3) - (_bufferWriter.WrittenCount - 2);
                Span<byte> padding = _bufferWriter.GetSpan(paddingLength);
                for (int i = 0; i < paddingLength; i++)
                {
                    padding[i] = (byte)(LF_PAD0 + paddingLength - i);
                }
                _bufferWriter.Advance(paddingLength);
            }
        }*/
    }
}
