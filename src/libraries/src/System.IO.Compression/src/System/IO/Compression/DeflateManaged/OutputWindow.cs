// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.IO.Compression
{
    /// <summary>
    /// This class maintains a window for decompressed output.
    /// We need to keep this because the decompressed information can be
    /// a literal or a length/distance pair. For length/distance pair,
    /// we need to look back in the output window and copy bytes from there.
    /// We use a byte array of WindowSize circularly.
    /// </summary>
    internal sealed class OutputWindow
    {
        private const int WindowSize = 65536;
        private const int WindowMask = 65535;

        private readonly byte[] _window = new byte[WindowSize]; // The window is 2^15 bytes
        private int _end;       // this is the position to where we should write next byte
        private int _bytesUsed; // The number of bytes in the output window which is not consumed.

        /// <summary>Add a byte to output window.</summary>
        public void Write(byte b)
        {
            Debug.Assert(_bytesUsed < WindowSize, "Can't add byte when window is full!");
            _window[_end++] = b;
            _end &= WindowMask;
            ++_bytesUsed;
        }

        public void WriteLengthDistance(int length, int distance)
        {
            Debug.Assert((_bytesUsed + length) <= WindowSize, "No Enough space");

            // move backwards distance bytes in the output stream,
            // and copy length bytes from this position to the output stream.
            _bytesUsed += length;
            int copyStart = (_end - distance) & WindowMask; // start position for coping.

            int border = WindowSize - length;
            if (copyStart <= border && _end < border)
            {
                if (length <= distance)
                {
                    Array.Copy(_window, copyStart, _window, _end, length);
                    _end += length;
                }
                else
                {
                    // The referenced string may overlap the current
                    // position; for example, if the last 2 bytes decoded have values
                    // X and Y, a string reference with <length = 5, distance = 2>
                    // adds X,Y,X,Y,X to the output stream.
                    while (length-- > 0)
                    {
                        _window[_end++] = _window[copyStart++];
                    }
                }
            }
            else
            {
                // copy byte by byte
                while (length-- > 0)
                {
                    _window[_end++] = _window[copyStart++];
                    _end &= WindowMask;
                    copyStart &= WindowMask;
                }
            }
        }

        /// <summary>
        /// Copy up to length of bytes from input directly.
        /// This is used for uncompressed block.
        /// </summary>
        public int CopyFrom(InputBuffer input, int length)
        {
            length = Math.Min(Math.Min(length, WindowSize - _bytesUsed), input.AvailableBytes);
            int copied;

            // We might need wrap around to copy all bytes.
            int tailLen = WindowSize - _end;
            if (length > tailLen)
            {
                // copy the first part
                copied = input.CopyTo(_window, _end, tailLen);
                if (copied == tailLen)
                {
                    // only try to copy the second part if we have enough bytes in input
                    copied += input.CopyTo(_window, 0, length - tailLen);
                }
            }
            else
            {
                // only one copy is needed if there is no wrap around.
                copied = input.CopyTo(_window, _end, length);
            }

            _end = (_end + copied) & WindowMask;
            _bytesUsed += copied;
            return copied;
        }

        /// <summary>Free space in output window.</summary>
        public int FreeBytes => WindowSize - _bytesUsed;

        /// <summary>Bytes not consumed in output window.</summary>
        public int AvailableBytes => _bytesUsed;

        /// <summary>Copy the decompressed bytes to output array.</summary>
        public int CopyTo(byte[] output, int offset, int length)
        {
            int copy_end;

            if (length > _bytesUsed)
            {
                // we can copy all the decompressed bytes out
                copy_end = _end;
                length = _bytesUsed;
            }
            else
            {
                copy_end = (_end - _bytesUsed + length) & WindowMask; // copy length of bytes
            }

            int copied = length;

            int tailLen = length - copy_end;
            if (tailLen > 0)
            {
                // this means we need to copy two parts separately
                // copy tailLen bytes from the end of output window
                Array.Copy(_window, WindowSize - tailLen,
                                  output, offset, tailLen);
                offset += tailLen;
                length = copy_end;
            }
            Array.Copy(_window, copy_end - length, output, offset, length);
            _bytesUsed -= copied;
            Debug.Assert(_bytesUsed >= 0, "check this function and find why we copied more bytes than we have");
            return copied;
        }
    }
}
