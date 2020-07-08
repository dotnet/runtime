// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// This helper class is used to emit method fixups which use 4-bit encoding.
    /// </summary>
    public sealed class NibbleWriter
    {
        /// <summary>
        /// Number of output bytes that fit into the small buffer.
        /// </summary>
        private const int ByteCountInSmallBuffer = sizeof(ulong);

        /// <summary>
        /// Number of nibbles that fit into the small buffer.
        /// </summary>
        private const int NibbleCountInSmallBuffer = ByteCountInSmallBuffer * 2;

        /// <summary>
        /// first 16 nibbles are put into this small buffer. When it overflows,
        /// we allocate a full-fledged BlobBuilder.
        /// </summary>
        private ulong _smallBuffer;

        /// <summary>
        /// Number of nibbles that have already been written.
        /// </summary>
        private int _nibbleCount;

        /// <summary>
        /// When emitting into the _largeBuffer, this represents the last unwritten nibble.
        /// </summary>
        private byte _pendingNibble;

        /// <summary>
        /// BlobBuilder is used to accumulate longer nibble streams.
        /// </summary>
        private BlobBuilder _largeBuffer;

        /// <summary>
        /// Initialize an empty nibble writer
        /// </summary>
        public NibbleWriter()
        {
            _smallBuffer = 0;
            _nibbleCount = 0;
        }

        /// <summary>
        /// Add a single nibble to the output stream.
        /// </summary>
        /// <param name="nibble">Nibble to add</param>
        public void WriteNibble(byte nibble)
        {
            Debug.Assert((nibble & ~0xF) == 0);
            if (_nibbleCount < NibbleCountInSmallBuffer)
            {
                _smallBuffer |= ((ulong)nibble) << (4 * _nibbleCount);
            }
            else if ((_nibbleCount & 1) == 0)
            {
                // We're emitting the first half of a new nibble
                _pendingNibble = (byte)(nibble & 0x0F);
            }
            else
            {
                // Flush the pending nibble into output
                if (_largeBuffer == null)
                {
                    _largeBuffer = new BlobBuilder();
                }
                _largeBuffer.WriteByte((byte)(_pendingNibble | (nibble << 4)));
            }

            _nibbleCount++;
        }

        /// <summary>
        /// Write an unsigned int via variable length nibble encoding.
        /// We use the bit scheme:
        /// 0ABC (if 0 <= dw <= 0x7)
        /// 1ABC 0DEF (if 0 <= dw <= 0x7f)
        /// 1ABC 1DEF 0GHI (if 0 <= dw <= 0x7FF)
        /// etc..
        /// </summary>
        /// <param name="value">Unsigned 32-bit value to emit to the nibble stream</param>
        public void WriteUInt(uint value)
        {
            // Fast path for common small inputs
            if (value <= 63)
            {
                if (value > 7)
                {
                    WriteNibble((byte)((value >> 3) | 8));
                }

                WriteNibble((byte)(value & 7));
                return;
            }

            // Note we must write this out with the low terminating nibble (0ABC) last b/c the
            // reader gets nibbles in the same order we write them.
            int i = 0;
            while ((value >> i) > 7)
            {
                i += 3;
            }
            while (i > 0)
            {
                WriteNibble((byte)(((value >> i) & 0x7) | 0x8));
                i -= 3;
            }
            WriteNibble((byte)(value & 0x7));
        }

        /// <summary>
        /// Write a signed 32 bit value into the nibble stream. Signed values use basically the same
        /// encoding as the unsigned values, just left-shifting the absolute value by one bit and
        /// filling in bit #0 with the sign bit.
        /// </summary>
        /// <param name="signedValue">Signed value to encode in the nibble stream</param>
        public void WriteInt(int signedValue)
        {
            uint value = (signedValue < 0) ? (((uint)(-signedValue) << 1) + 1) : ((uint)signedValue << 1);
            WriteUInt(value);
        }

        /// <summary>
        /// Create a byte array representation of the complete fixup blob.
        /// </summary>
        public byte[] ToArray()
        {
            int totalBytes = (_nibbleCount + 1) >> 1;
            byte[] output = new byte[totalBytes];

            int smallBytes = Math.Min(totalBytes, NibbleCountInSmallBuffer);

            for (int byteIndex = 0; byteIndex < smallBytes; byteIndex++)
            {
                output[byteIndex] = unchecked((byte)(_smallBuffer >> (8 * byteIndex)));
            }

            if (_nibbleCount > NibbleCountInSmallBuffer)
            {
                int startOffset = ByteCountInSmallBuffer;
                if (_largeBuffer != null)
                {
                    foreach (Blob blob in _largeBuffer.GetBlobs())
                    {
                        ArraySegment<byte> blobSegment = blob.GetBytes();
                        Array.Copy(blobSegment.Array, blobSegment.Offset, output, startOffset, blob.Length);
                        startOffset += blob.Length;
                    }
                }

                if ((_nibbleCount & 1) != 0)
                {
                    // Emit the last pending half-nibble
                    output[startOffset] = _pendingNibble;
                    startOffset++;
                }

                Debug.Assert(startOffset == totalBytes);
            }
            return output;
        }
    }
}
