// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.IO.Compression
{
    internal class OutputBuffer
    {
        private byte[] byteBuffer;  // buffer for storing bytes
        private int pos;            // position
        private uint bitBuf;        // store uncomplete bits 
        private int bitCount;       // number of bits in bitBuffer 

        // set the output buffer we will be using
        internal void UpdateBuffer(byte[] output)
        {
            byteBuffer = output;
            pos = 0;
        }

        internal int BytesWritten
        {
            get
            {
                return pos;
            }
        }

        internal int FreeBytes
        {
            get
            {
                return byteBuffer.Length - pos;
            }
        }

        internal void WriteUInt16(ushort value)
        {
            Debug.Assert(FreeBytes >= 2, "No enough space in output buffer!");

            byteBuffer[pos++] = (byte)value;
            byteBuffer[pos++] = (byte)(value >> 8);
        }

        internal void WriteBits(int n, uint bits)
        {
            Debug.Assert(n <= 16, "length must be larger than 16!");
            bitBuf |= bits << bitCount;
            bitCount += n;
            if (bitCount >= 16)
            {
                Debug.Assert(byteBuffer.Length - pos >= 2, "No enough space in output buffer!");
                byteBuffer[pos++] = unchecked((byte)bitBuf);
                byteBuffer[pos++] = unchecked((byte)(bitBuf >> 8));
                bitCount -= 16;
                bitBuf >>= 16;
            }
        }

        // write the bits left in the output as bytes. 
        internal void FlushBits()
        {
            // flush bits from bit buffer to output buffer
            while (bitCount >= 8)
            {
                byteBuffer[pos++] = unchecked((byte)bitBuf);
                bitCount -= 8;
                bitBuf >>= 8;
            }

            if (bitCount > 0)
            {
                byteBuffer[pos++] = unchecked((byte)bitBuf);
                bitBuf = 0;
                bitCount = 0;
            }
        }

        internal void WriteBytes(byte[] byteArray, int offset, int count)
        {
            Debug.Assert(FreeBytes >= count, "Not enough space in output buffer!");
            // faster 
            if (bitCount == 0)
            {
                Array.Copy(byteArray, offset, byteBuffer, pos, count);
                pos += count;
            }
            else
            {
                WriteBytesUnaligned(byteArray, offset, count);
            }
        }

        private void WriteBytesUnaligned(byte[] byteArray, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                byte b = byteArray[offset + i];
                WriteByteUnaligned(b);
            }
        }

        private void WriteByteUnaligned(byte b)
        {
            WriteBits(8, b);
        }

        internal int BitsInBuffer
        {
            get
            {
                return (bitCount / 8) + 1;
            }
        }

        internal OutputBuffer.BufferState DumpState()
        {
            OutputBuffer.BufferState savedState;
            savedState.pos = pos;
            savedState.bitBuf = bitBuf;
            savedState.bitCount = bitCount;
            return savedState;
        }

        internal void RestoreState(OutputBuffer.BufferState state)
        {
            pos = state.pos;
            bitBuf = state.bitBuf;
            bitCount = state.bitCount;
        }

        internal struct BufferState
        {
            internal int pos;            // position
            internal uint bitBuf;        // store uncomplete bits 
            internal int bitCount;       // number of bits in bitBuffer 
        }
    }
}

