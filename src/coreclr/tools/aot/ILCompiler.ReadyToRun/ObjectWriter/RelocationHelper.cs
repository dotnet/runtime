// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

using ILCompiler.DependencyAnalysis;

namespace ILCompiler.PEWriter
{
    /// <summary>
    /// Helper used to copy the produced PE file from a BlobBuilder to an output stream,
    /// applying relocations along the way. It's mostly a linear copier that occasionally stops,
    /// patches a few bytes and then continues.
    /// </summary>
    class RelocationHelper
    {
        /// <summary>
        /// Maximum number of bytes to process for any relocation type.
        /// </summary>
        const int LongestRelocationBytes = 8;
        
        /// <summary>
        /// Enumerator of blobs within the blob builder.
        /// </summary>
        private BlobBuilder.Blobs _peFileBlobs;

        /// <summary>
        /// Blob length is used at the end to verify that the relocated file hasn't changed length.
        /// </summary>
        private int _peFileLength;

        /// <summary>
        /// Backing array for the ArraySegment of the current blob.
        /// </summary>
        private byte[] _currentBlob;

        /// <summary>
        /// Current offset within the active blob.
        /// </summary>
        private int _blobOffset;

        /// <summary>
        /// Remaining number of bytes unprocessed in the active blob.
        /// </summary>
        private int _remainingLength;

        /// <summary>
        /// Preferred image load address is needed to properly fix up absolute relocation types.
        /// </summary>
        private ulong _defaultImageBase;

        /// <summary>
        /// Output stream to receive the relocated file
        /// </summary>
        private Stream _outputStream;

        /// <summary>
        /// Current position in the output file
        /// </summary>
        private int _outputFilePos;

        /// <summary>
        /// Buffer to hold data for the currently processed relocation.
        /// </summary>
        private byte[] _relocationBuffer = new byte[LongestRelocationBytes];

        /// <summary>
        /// Relocation helper stores the output stream and initializes the PE blob builder enumerator.
        /// </summary>
        /// <param name="outputStream">Output stream for the relocated PE file</param>
        /// <param name="peFileBuilder">PE file blob builder</param>
        public RelocationHelper(Stream outputStream, ulong defaultImageBase, BlobBuilder peFileBuilder)
        {
            _outputStream = outputStream;
            _outputFilePos = 0;
            
            _defaultImageBase = defaultImageBase;

            _peFileLength = peFileBuilder.Count;
            _peFileBlobs = peFileBuilder.GetBlobs();
            FetchNextBlob();
        }

        /// <summary>
        /// Copy data from the PE file builder to the output stream, stopping at given file position.
        /// </summary>
        /// <param name="filePos">Output PE file position to stop at</param>
        public void CopyToFilePosition(int filePos)
        {
            CopyBytesToOutput(filePos - _outputFilePos);
        }

        /// <summary>
        /// Advance output position in case of external writes to the output stream.
        /// </summary>
        /// <param name="delta">Number of bytes advance output by</param>
        public void AdvanceOutputPos(int delta)
        {
            _outputFilePos += delta;
        }

        /// <summary>
        /// Copy all unprocessed data (after the last relocation) into the output file
        /// without any further modifications.
        /// </summary>
        public void CopyRestOfFile()
        {
            do
            {
                CopyBytesToOutput(_remainingLength);
            }
            while (TryFetchNextBlob());
            
            if (_outputFilePos != _peFileLength)
            {
                // Input / output PE file length mismatch - internal error in the relocator
                throw new BadImageFormatException();
            }
        }

        /// <summary>
        /// Process a single relocation by copying the required number of bytes into a
        /// buffer, applying the relocation and writing it to the output file.
        /// </summary>
        /// <param name="relocationType">Relocation type to process</param>
        /// <param name="sourceRVA">RVA representing the address to relocate</param>
        /// <param name="targetRVA">RVA representing the relocation target</param>
        public void ProcessRelocation(RelocType relocationType, int sourceRVA, int targetRVA, int filePosWhenPlaced)
        {
            int relocationLength = 0;
            long delta = 0;

            switch (relocationType)
            {
                case RelocType.IMAGE_REL_BASED_ABSOLUTE:
                    // No relocation
                    return;
                    
                case RelocType.IMAGE_REL_BASED_HIGHLOW:
                    {
                        relocationLength = 4;
                        delta = unchecked(targetRVA + (int)_defaultImageBase);
                        break;
                    }

                case RelocType.IMAGE_REL_BASED_ADDR32NB:
                case RelocType.IMAGE_REL_SYMBOL_SIZE:
                    {
                        relocationLength = 4;
                        delta = targetRVA;
                        break;
                    }
                
                case RelocType.IMAGE_REL_BASED_REL32:
                    {
                        relocationLength = 4;
                        delta = targetRVA - sourceRVA - 4;
                        break;
                    }
                    
                case RelocType.IMAGE_REL_BASED_DIR64:
                    {
                        relocationLength = 8;
                        delta = unchecked(targetRVA + (long)_defaultImageBase);
                        break;
                    }
                    
                case RelocType.IMAGE_REL_BASED_THUMB_MOV32:
                    {
                        relocationLength = 8;
                        delta = unchecked(targetRVA + (int)_defaultImageBase);
                        break;
                    }

                case RelocType.IMAGE_REL_BASED_THUMB_MOV32_PCREL:
                    {
                        relocationLength = 8;
                        const uint offsetCorrection = 12;
                        delta = unchecked(targetRVA - (sourceRVA + offsetCorrection));
                        break;
                    }
                    
                case RelocType.IMAGE_REL_BASED_THUMB_BRANCH24:
                    {
                        relocationLength = 4;
                        delta = targetRVA - sourceRVA - 4;
                        break;
                    }

                case RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21:
                    {
                        relocationLength = 4;
                        int sourcePageRVA = sourceRVA & ~0xfff;
                        // Page delta always fits in 21 bits as long as we use 4-byte RVAs
                        delta = ((targetRVA - sourcePageRVA) >> 12) & 0x1f_ffff;
                        break;
                    }

                case RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A:
                    {
                        relocationLength = 4;
                        delta = targetRVA & 0xfff;
                        break;
                    }

                case RelocType.IMAGE_REL_FILE_ABSOLUTE:
                    {
                        relocationLength = 4;
                        delta = filePosWhenPlaced;
                        break;
                    }

                case RelocType.IMAGE_REL_BASED_LOONGARCH64_PC:
                case RelocType.IMAGE_REL_BASED_LOONGARCH64_JIR:
                    {
                        relocationLength = 8;
                        delta = targetRVA - sourceRVA;
                        break;
                    }

                case RelocType.IMAGE_REL_BASED_RISCV64_PC:
                    {
                        relocationLength = 8;
                        delta = targetRVA - sourceRVA;
                        break;
                    }

                default:
                    throw new NotSupportedException();
            }
            
            if (relocationLength > 0)
            {
                CopyBytesToBuffer(_relocationBuffer, relocationLength);
                unsafe
                {
                    fixed (byte *bufferContent = _relocationBuffer)
                    {
                        long value = Relocation.ReadValue(relocationType, bufferContent);
                        // Supporting non-zero values for ARM64 would require refactoring this function
                        if (((relocationType == RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21) ||
                             (relocationType == RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A) ||
                             (relocationType == RelocType.IMAGE_REL_BASED_LOONGARCH64_PC) ||
                             (relocationType == RelocType.IMAGE_REL_BASED_LOONGARCH64_JIR) ||
                             (relocationType == RelocType.IMAGE_REL_BASED_RISCV64_PC)
                             ) && (value != 0))
                        {
                            throw new NotSupportedException();
                        }

                        Relocation.WriteValue(relocationType, bufferContent, unchecked(value + delta));
                    }
                }

                // Write the relocated bytes to the output file
                _outputStream.Write(_relocationBuffer, 0, relocationLength);
                _outputFilePos += relocationLength;
            }
        }

        /// <summary>
        /// Read next blob from the PE blob builder. Throw exception of no more data is available
        /// (indicates an inconsistent PE file).
        /// </summary>
        private void FetchNextBlob()
        {
            if (!TryFetchNextBlob())
            {
                throw new BadImageFormatException();
            }
        }

        /// <summary>
        /// Try to fetch next blob from the PE blob builder, return false on EOF.
        /// </summary>
        /// <returns>True when another blob was successfully fetched, false on EOF</returns>
        private bool TryFetchNextBlob()
        {
            if (!_peFileBlobs.MoveNext())
            {
                return false;
            }

            ArraySegment<byte> blobContent = _peFileBlobs.Current.GetBytes();
            _currentBlob = blobContent.Array;
            _blobOffset = blobContent.Offset;
            _remainingLength = blobContent.Count;
            return true;
        }

        /// <summary>
        /// Copy a given number of bytes from the PE blob builder to the output stream.
        /// </summary>
        /// <param name="length">Number of bytes to copy</param>
        private void CopyBytesToOutput(int length)
        {
            Debug.Assert(length >= 0);

            while (length > 0)
            {
                if (_remainingLength == 0)
                {
                    FetchNextBlob();
                }

                int part = Math.Min(length, _remainingLength);
                _outputStream.Write(_currentBlob, _blobOffset, part);
                _outputFilePos += part;
                _blobOffset += part;
                _remainingLength -= part;
                length -= part;
            }
        }

        /// <summary>
        /// Copy bytes from the PE blob builder to the given byte buffer.
        /// </summary>
        /// <param name="buffer">Buffer to fill in from the blob builder</param>
        /// <param name="count">Number of bytes to copy to the buffer</param>
        public void CopyBytesToBuffer(byte[] buffer, int count)
        {
            int offset = 0;
            while (offset < count)
            {
                if (_remainingLength == 0)
                {
                    FetchNextBlob();
                }

                int part = Math.Min(count - offset, _remainingLength);
                Array.Copy(
                    sourceArray: _currentBlob,
                    sourceIndex: _blobOffset,
                    destinationArray: buffer,
                    destinationIndex: offset,
                    length: part);

                _blobOffset += part;
                _remainingLength -= part;
                offset += part;
            }
        }

        /// <summary>
        /// Extract the 24-bit rel offset from bl instruction
        /// </summary>
        /// <param name="bytes">Byte buffer containing the instruction to analyze</param>
        /// <param name="offset">Offset of the instruction within the buffer</param>
        private static unsafe int GetThumb2BlRel24(byte[] bytes, int offset)
        {
            uint opcode0 = BitConverter.ToUInt16(bytes, offset + 0);
            uint opcode1 = BitConverter.ToUInt16(bytes, offset + 2);

            uint s  = opcode0 >> 10;
            uint j2 = opcode1 >> 11;
            uint j1 = opcode1 >> 13;

            uint ret =
                ((s << 24)              & 0x1000000) |
                (((j1 ^ s ^ 1) << 23)   & 0x0800000) |
                (((j2 ^ s ^ 1) << 22)   & 0x0400000) |
                ((opcode0 << 12)        & 0x03FF000) |
                ((opcode1 <<  1)        & 0x0000FFE);

            // Sign-extend and return
            return (int)((ret << 7) >> 7);
        }

        /// <summary>
        /// Patch a MOVW / MOVT Thumb2 instruction by updating its 16-bit immediate operand to imm16.
        /// </summary>
        /// <param name="int16">Immediate 16-bit operand to inject into the instruction</param>
        /// <param name="bytes">Byte array containing the instruction to patch</param>
        /// <param name="offset">Offset of the MOVW / MOVT instruction</param>
        private static void PutThumb2Imm16(ushort imm16, byte[] bytes, int offset)
        {
            const ushort Mask1 = 0xf000;
            const ushort Val1 = (Mask1 >> 12);
            const ushort Mask2 = 0x0800;
            const ushort Val2 = (Mask2 >> 1);
            const ushort Mask3 = 0x0700;
            const ushort Val3 = (Mask3 << 4);
            const ushort Mask4 = 0x00ff;
            const ushort Val4 = (Mask4 << 0);
            const ushort Val = Val1 | Val2 | Val3 | Val4;

            ushort opcode0 = BitConverter.ToUInt16(bytes, offset);
            ushort opcode1 = BitConverter.ToUInt16(bytes, offset + 2);

            opcode0 &= unchecked((ushort)~Val);
            opcode0 |= unchecked((ushort)(((imm16 & Mask1) >> 12) | ((imm16 & Mask2) >> 1) | ((imm16 & Mask3) << 4) | ((imm16 & Mask4) << 0)));

            WriteUInt16(opcode0, bytes, offset);
            WriteUInt16(opcode1, bytes, offset + 2);
        }

        /// <summary>
        /// Decode the 32-bit immediate operand from a MOVW / MOVT instruction pair (8 bytes total).
        /// </summary>
        /// <param name="bytes">Byte array containing the 8-byte sequence MOVW - MOVT</param>
        private static int GetThumb2Mov32(byte[] bytes)
        {
            Debug.Assert(((uint)BitConverter.ToUInt16(bytes, 0) & 0xFBF0) == 0xF240);
            Debug.Assert(((uint)BitConverter.ToUInt16(bytes, 4) & 0xFBF0) == 0xF2C0);

            return (int)GetThumb2Imm16(bytes, 0) + ((int)(GetThumb2Imm16(bytes, 4) << 16));
        }
        
        /// <summary>
        /// Decode the 16-bit immediate operand from a MOVW / MOVT instruction.
        /// </summary>
        private static ushort GetThumb2Imm16(byte[] bytes, int offset)
        {
            uint opcode0 = BitConverter.ToUInt16(bytes, offset);
            uint opcode1 = BitConverter.ToUInt16(bytes, offset + 2);
            uint result =
                ((opcode0 << 12) & 0xf000) |
                ((opcode0 <<  1) & 0x0800) |
                ((opcode1 >>  4) & 0x0700) |
                ((opcode1 >>  0) & 0x00ff);
            return (ushort)result;
        }

        /// <summary>
        /// Returns whether the offset fits into bl instruction
        /// </summary>
        /// <param name="imm24">Immediate operand to check.</param>
        private static bool FitsInThumb2BlRel24(int imm24)
        {
            return ((imm24 << 7) >> 7) == imm24;
        }

        /// <summary>
        /// Deposit the 24-bit rel offset into bl instruction
        /// </summary>
        /// <param name="imm24">Immediate operand to inject into the instruction</param>
        /// <param name="bytes">Byte buffer containing the BL instruction to patch</param>
        /// <param name="offset">Offset of the instruction within the buffer</param>
        private static void PutThumb2BlRel24(int imm24, byte[] bytes, int offset)
        {
            // Verify that we got a valid offset
            Debug.Assert(FitsInThumb2BlRel24(imm24));

            // Ensure that the ThumbBit is not set on the offset
            // as it cannot be encoded.
            Debug.Assert((imm24 & 1/*THUMB_CODE*/) == 0);

            ushort opcode0 = BitConverter.ToUInt16(bytes, 0);
            ushort opcode1 = BitConverter.ToUInt16(bytes, 2);
            opcode0 &= 0xF800;
            opcode1 &= 0xD000;

            uint s  =  (unchecked((uint)imm24) & 0x1000000) >> 24;
            uint j1 = ((unchecked((uint)imm24) & 0x0800000) >> 23) ^ s ^ 1;
            uint j2 = ((unchecked((uint)imm24) & 0x0400000) >> 22) ^ s ^ 1;

            opcode0 |= (ushort)(((unchecked((uint)imm24) & 0x03FF000) >> 12) | (s << 10));
            opcode1 |= (ushort)(((unchecked((uint)imm24) & 0x0000FFE) >>  1) | (j1 << 13) | (j2 << 11));

            WriteUInt16(opcode0, bytes, offset + 0);
            WriteUInt16(opcode1, bytes, offset + 2);

            Debug.Assert(GetThumb2BlRel24(bytes, 0) == imm24);
        }

        /// <summary>
        /// Helper to write 16-bit value to a byte array.
        /// </summary>
        /// <param name="value">Value to write to the byte array</param>
        /// <param name="bytes">Target byte array</param>
        /// <param name="offset">Offset in the array</param>
        static void WriteUInt16(ushort value, byte[] bytes, int offset)
        {
            bytes[offset + 0] = unchecked((byte)value);
            bytes[offset + 1] = (byte)(value >> 8);
        }

        /// <summary>
        /// Helper to write 32-bit value to a byte array.
        /// </summary>
        /// <param name="value">Value to write to the byte array</param>
        /// <param name="bytes">Target byte array</param>
        /// <param name="offset">Offset in the array</param>
        static void WriteUInt32(uint value, byte[] bytes, int offset)
        {
            bytes[offset + 0] = unchecked((byte)(value >> 0));
            bytes[offset + 1] = unchecked((byte)(value >> 8));
            bytes[offset + 2] = unchecked((byte)(value >> 16));
            bytes[offset + 3] = unchecked((byte)(value >> 24));
        }

        /// <summary>
        /// We use the same byte encoding for signed and unsigned 32-bit values
        /// so this method just forwards to WriteUInt32.
        /// </summary>
        /// <param name="value">Value to write to the byte array</param>
        /// <param name="bytes">Target byte array</param>
        /// <param name="offset">Offset in the array</param>
        static void WriteInt32(int value, byte[] bytes, int offset)
        {
            WriteUInt32(unchecked((uint)value), bytes, offset);
        }

        /// <summary>
        /// Helper to write 64-bit value to a byte array.
        /// </summary>
        /// <param name="value">Value to write to the byte array</param>
        /// <param name="bytes">Target byte array</param>
        /// <param name="offset">Offset in the array</param>
        static void WriteUInt64(ulong value, byte[] bytes, int offset)
        {
            bytes[offset + 0] = unchecked((byte)(value >> 0));
            bytes[offset + 1] = unchecked((byte)(value >> 8));
            bytes[offset + 2] = unchecked((byte)(value >> 16));
            bytes[offset + 3] = unchecked((byte)(value >> 24));
            bytes[offset + 4] = unchecked((byte)(value >> 32));
            bytes[offset + 5] = unchecked((byte)(value >> 40));
            bytes[offset + 6] = unchecked((byte)(value >> 48));
            bytes[offset + 7] = unchecked((byte)(value >> 56));
        }

        /// <summary>
        /// We use the same byte encoding for signed and unsigned 64-bit values
        /// so this method just forwards to WriteUInt64.
        /// </summary>
        /// <param name="value">Value to write to the byte array</param>
        /// <param name="bytes">Target byte array</param>
        /// <param name="offset">Offset in the array</param>
        static void WriteInt64(long value, byte[] bytes, int offset)
        {
            WriteUInt64(unchecked((ulong)value), bytes, offset);
        }
    }
}
