// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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

    internal class InputBuffer
    {
        private byte[] _buffer;           // byte array to store input
        private int _start;               // start poisition of the buffer
        private int _end;                 // end position of the buffer
        private uint _bitBuffer = 0;      // store the bits here, we can quickly shift in this buffer
        private int _bitsInBuffer = 0;    // number of bits available in bitBuffer

        // Total bits available in the input buffer
        public int AvailableBits
        {
            get
            {
                return _bitsInBuffer;
            }
        }

        // Total bytes available in the input buffer
        public int AvailableBytes
        {
            get
            {
                return (_end - _start) + (_bitsInBuffer / 8);
            }
        }

        // Ensure that count bits are in the bit buffer.
        // Returns false if input is not sufficient to make this true.
        // Count can be up to 16.
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
                _bitBuffer |= (uint)_buffer[_start++] << _bitsInBuffer;
                _bitsInBuffer += 8;

                if (_bitsInBuffer < count)
                {
                    if (NeedsInput())
                    {
                        return false;
                    }
                    // insert a byte to bitbuffer
                    _bitBuffer |= (uint)_buffer[_start++] << _bitsInBuffer;
                    _bitsInBuffer += 8;
                }
            }

            return true;
        }

        // This function will try to load 16 or more bits into bitBuffer.
        // It returns whatever is contained in bitBuffer after loading.
        // The main difference between this and GetBits is that this will
        // never return -1. So the caller needs to check AvailableBits to 
        // see how many bits are available. 
        public uint TryLoad16Bits()
        {
            if (_bitsInBuffer < 8)
            {
                if (_start < _end)
                {
                    _bitBuffer |= (uint)_buffer[_start++] << _bitsInBuffer;
                    _bitsInBuffer += 8;
                }

                if (_start < _end)
                {
                    _bitBuffer |= (uint)_buffer[_start++] << _bitsInBuffer;
                    _bitsInBuffer += 8;
                }
            }
            else if (_bitsInBuffer < 16)
            {
                if (_start < _end)
                {
                    _bitBuffer |= (uint)_buffer[_start++] << _bitsInBuffer;
                    _bitsInBuffer += 8;
                }
            }

            return _bitBuffer;
        }

        private uint GetBitMask(int count)
        {
            return ((uint)1 << count) - 1;
        }

        // Gets count bits from the input buffer. Returns -1 if not enough bits available.
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

        /// Copies length bytes from input buffer to output buffer starting
        /// at output[offset].  You have to make sure, that the buffer is
        /// byte aligned.  If not enough bytes are available, copies fewer
        /// bytes.
        /// Returns the number of bytes copied, 0 if no byte is available.
        public int CopyTo(byte[] output, int offset, int length)
        {
            Debug.Assert(output != null, "");
            Debug.Assert(offset >= 0, "");
            Debug.Assert(length >= 0, "");
            Debug.Assert(offset <= output.Length - length, "");
            Debug.Assert((_bitsInBuffer % 8) == 0, "");

            // Copy the bytes in bitBuffer first.
            int bytesFromBitBuffer = 0;
            while (_bitsInBuffer > 0 && length > 0)
            {
                output[offset++] = (byte)_bitBuffer;
                _bitBuffer >>= 8;
                _bitsInBuffer -= 8;
                length--;
                bytesFromBitBuffer++;
            }

            if (length == 0)
            {
                return bytesFromBitBuffer;
            }

            int avail = _end - _start;
            if (length > avail)
            {
                length = avail;
            }

            Array.Copy(_buffer, _start, output, offset, length);
            _start += length;
            return bytesFromBitBuffer + length;
        }

        // Return true is all input bytes are used. 
        // This means the caller can call SetInput to add more input.        
        public bool NeedsInput()
        {
            return _start == _end;
        }

        // Set the byte array to be processed.
        // All the bits remained in bitBuffer will be processed before the new bytes.
        // We don't clone the byte array here since it is expensive.
        // The caller should make sure after a buffer is passed in. 
        // It will not be changed before calling this function again.

        public void SetInput(byte[] buffer, int offset, int length)
        {
            Debug.Assert(buffer != null, "");
            Debug.Assert(offset >= 0, "");
            Debug.Assert(length >= 0, "");
            Debug.Assert(offset <= buffer.Length - length, "");
            Debug.Assert(_start == _end, "");

            _buffer = buffer;
            _start = offset;
            _end = offset + length;
        }

        // Skip n bits in the buffer
        public void SkipBits(int n)
        {
            Debug.Assert(_bitsInBuffer >= n, "No enough bits in the buffer, Did you call EnsureBitsAvailable?");
            _bitBuffer >>= n;
            _bitsInBuffer -= n;
        }

        // Skips to the next byte boundary.
        public void SkipToByteBoundary()
        {
            _bitBuffer >>= (_bitsInBuffer % 8);
            _bitsInBuffer = _bitsInBuffer - (_bitsInBuffer % 8);
        }
    }
}
