// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.IO.Compression
{
    // This class can be used to read bits from an byte array quickly.
    // Normally we get bits from 'bitBuffer' field and bitsInBuffer stores
    // the number of bits available in 'BitBuffer'.
    // When we used up the bits in bitBuffer, we will try to get byte from
    // the byte array and copy the byte to appropiate position in bitBuffer.
    //
    // The byte array is not reused. We will go from 'start' to 'end'.
    // When we reach the end, most read operations will return -1,
    // which means we are running out of input.

    internal sealed class InputBuffer
    {
        private Memory<byte> _buffer;     // memory to store input
        private uint _bitBuffer;      // store the bits here, we can quickly shift in this buffer
        private int _bitsInBuffer;    // number of bits available in bitBuffer

        /// <summary>Total bits available in the input buffer.</summary>
        public int AvailableBits => _bitsInBuffer;

        /// <summary>Total bytes available in the input buffer.</summary>
        public int AvailableBytes => _buffer.Length + (_bitsInBuffer / 8);

        /// <summary>Ensure that count bits are in the bit buffer.</summary>
        /// <param name="count">Can be up to 16.</param>
        /// <returns>Returns false if input is not sufficient to make this true.</returns>
        public bool EnsureBitsAvailable(int count)
        {
            Debug.Assert(0 < count && count <= 16, "count is invalid.");

            // manual inlining to improve perf
            if (_bitsInBuffer < count)
            {
                if (NeedsInput())
                {
                    return false;
                }

                // insert a byte to bitbuffer
                _bitBuffer |= (uint)_buffer.Span[0] << _bitsInBuffer;
                _buffer = _buffer.Slice(1);
                _bitsInBuffer += 8;

                if (_bitsInBuffer < count)
                {
                    if (NeedsInput())
                    {
                        return false;
                    }
                    // insert a byte to bitbuffer
                    _bitBuffer |= (uint)_buffer.Span[0] << _bitsInBuffer;
                    _buffer = _buffer.Slice(1);
                    _bitsInBuffer += 8;
                }
            }

            return true;
        }

        /// <summary>
        /// This function will try to load 16 or more bits into bitBuffer.
        /// It returns whatever is contained in bitBuffer after loading.
        /// The main difference between this and GetBits is that this will
        /// never return -1. So the caller needs to check AvailableBits to
        /// see how many bits are available.
        /// </summary>
        public uint TryLoad16Bits()
        {
            if (_bitsInBuffer < 8)
            {
                if (_buffer.Length > 1)
                {
                    Span<byte> span = _buffer.Span;
                    _bitBuffer |= (uint)span[0] << _bitsInBuffer;
                    _bitBuffer |= (uint)span[1] << (_bitsInBuffer + 8);
                    _buffer = _buffer.Slice(2);
                    _bitsInBuffer += 16;
                }
                else if (_buffer.Length != 0)
                {
                    _bitBuffer |= (uint)_buffer.Span[0] << _bitsInBuffer;
                    _buffer = _buffer.Slice(1);
                    _bitsInBuffer += 8;
                }
            }
            else if (_bitsInBuffer < 16)
            {
                if (!_buffer.IsEmpty)
                {
                    _bitBuffer |= (uint)_buffer.Span[0] << _bitsInBuffer;
                    _buffer = _buffer.Slice(1);
                    _bitsInBuffer += 8;
                }
            }

            return _bitBuffer;
        }

        private uint GetBitMask(int count) => ((uint)1 << count) - 1;

        /// <summary>Gets count bits from the input buffer. Returns -1 if not enough bits available.</summary>
        public int GetBits(int count)
        {
            Debug.Assert(0 < count && count <= 16, "count is invalid.");

            if (!EnsureBitsAvailable(count))
            {
                return -1;
            }

            int result = (int)(_bitBuffer & GetBitMask(count));
            _bitBuffer >>= count;
            _bitsInBuffer -= count;
            return result;
        }

        /// <summary>
        /// Copies bytes from input buffer to output buffer.
        /// You have to make sure, that the buffer is byte aligned. If not enough bytes are
        /// available, copies fewer bytes.
        /// </summary>
        /// <returns>Returns the number of bytes copied, 0 if no byte is available.</returns>
        public int CopyTo(Memory<byte> output)
        {
            Debug.Assert(_bitsInBuffer % 8 == 0);

            // Copy the bytes in bitBuffer first.
            int bytesFromBitBuffer = 0;
            while (_bitsInBuffer > 0 && !output.IsEmpty)
            {
                output.Span[0] = (byte)_bitBuffer;
                output = output.Slice(1);
                _bitBuffer >>= 8;
                _bitsInBuffer -= 8;
                bytesFromBitBuffer++;
            }

            if (output.IsEmpty)
            {
                return bytesFromBitBuffer;
            }

            int length = Math.Min(output.Length, _buffer.Length);
            _buffer.Slice(0, length).CopyTo(output);
            _buffer = _buffer.Slice(length);
            return bytesFromBitBuffer + length;
        }

        /// <summary>
        /// Copies length bytes from input buffer to output buffer starting at output[offset].
        /// You have to make sure, that the buffer is byte aligned. If not enough bytes are
        /// available, copies fewer bytes.
        /// </summary>
        /// <returns>Returns the number of bytes copied, 0 if no byte is available.</returns>
        public int CopyTo(byte[] output, int offset, int length)
        {
            Debug.Assert(output != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(length >= 0);
            Debug.Assert(offset <= output.Length - length);
            Debug.Assert((_bitsInBuffer % 8) == 0);

            return CopyTo(output.AsMemory(offset, length));
        }

        /// <summary>
        /// Return true is all input bytes are used.
        /// This means the caller can call SetInput to add more input.
        /// </summary>
        public bool NeedsInput() => _buffer.IsEmpty;

        /// <summary>
        /// Set the byte buffer to be processed.
        /// All the bits remained in bitbuffer will be processed before the new bytes.
        /// We don't clone the byte buffer here since it is expensive.
        /// The caller should make sure after a buffer is passed in, that
        /// it will not be changed before calling this function again.
        /// </summary>
        public void SetInput(Memory<byte> buffer)
        {
            if (_buffer.IsEmpty)
            {
                _buffer = buffer;
            }
        }

        /// <summary>
        /// Set the byte array to be processed.
        /// All the bits remained in bitBuffer will be processed before the new bytes.
        /// We don't clone the byte array here since it is expensive.
        /// The caller should make sure after a buffer is passed in.
        /// It will not be changed before calling this function again.
        /// </summary>
        public void SetInput(byte[] buffer, int offset, int length)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(length >= 0);
            Debug.Assert(offset <= buffer.Length - length);

            SetInput(buffer.AsMemory(offset, length));
        }

        /// <summary>Skip n bits in the buffer.</summary>
        public void SkipBits(int n)
        {
            Debug.Assert(_bitsInBuffer >= n, "No enough bits in the buffer, Did you call EnsureBitsAvailable?");
            _bitBuffer >>= n;
            _bitsInBuffer -= n;
        }

        /// <summary>Skips to the next byte boundary.</summary>
        public void SkipToByteBoundary()
        {
            _bitBuffer >>= (_bitsInBuffer % 8);
            _bitsInBuffer = _bitsInBuffer - (_bitsInBuffer % 8);
        }
    }
}
