// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System
{
    /// <summary>
    /// Implements the SHA1 hashing algorithm. Note that
    /// implementation is for hashing public information. Do not
    /// use code to hash private data, as implementation does
    /// not take any steps to avoid information disclosure.
    /// </summary>
    internal struct Sha1ForNonSecretPurposes
    {
        private long _length; // Total message length in bits
        private uint[] _w; // Workspace
        private int _pos; // Length of current chunk in bytes

        /// <summary>
        /// Call Start() to initialize the hash object.
        /// </summary>
        public void Start()
        {
            _w ??= new uint[85];

            _length = 0;
            _pos = 0;
            _w[80] = 0x67452301;
            _w[81] = 0xEFCDAB89;
            _w[82] = 0x98BADCFE;
            _w[83] = 0x10325476;
            _w[84] = 0xC3D2E1F0;
        }

        /// <summary>
        /// Adds an input byte to the hash.
        /// </summary>
        /// <param name="input">Data to include in the hash.</param>
        public void Append(byte input)
        {
            int idx = _pos >> 2;
            _w[idx] = (_w[idx] << 8) | input;
            if (64 == ++_pos)
            {
                Drain();
            }
        }

        /// <summary>
        /// Adds input bytes to the hash.
        /// </summary>
        /// <param name="input">
        /// Data to include in the hash. Must not be null.
        /// </param>
        public void Append(ReadOnlySpan<byte> input)
        {
            foreach (byte b in input)
            {
                Append(b);
            }
        }

        /// <summary>
        /// Retrieves the hash value.
        /// Note that after calling function, the hash object should
        /// be considered uninitialized. Subsequent calls to Append or
        /// Finish will produce useless results. Call Start() to
        /// reinitialize.
        /// </summary>
        /// <param name="output">
        /// Buffer to receive the hash value. Must not be null.
        /// Up to 20 bytes of hash will be written to the output buffer.
        /// If the buffer is smaller than 20 bytes, the remaining hash
        /// bytes will be lost. If the buffer is larger than 20 bytes, the
        /// rest of the buffer is left unmodified.
        /// </param>
        public void Finish(Span<byte> output)
        {
            long l = _length + 8 * _pos;
            Append(0x80);
            while (_pos != 56)
            {
                Append(0x00);
            }

            Append((byte)(l >> 56));
            Append((byte)(l >> 48));
            Append((byte)(l >> 40));
            Append((byte)(l >> 32));
            Append((byte)(l >> 24));
            Append((byte)(l >> 16));
            Append((byte)(l >> 8));
            Append((byte)l);

            int end = output.Length < 20 ? output.Length : 20;
            for (int i = 0; i != end; i++)
            {
                uint temp = _w[80 + i / 4];
                output[i] = (byte)(temp >> 24);
                _w[80 + i / 4] = temp << 8;
            }
        }

        /// <summary>
        /// Called when pos reaches 64.
        /// </summary>
        private void Drain()
        {
            for (int i = 16; i != 80; i++)
            {
                _w[i] = BitOperations.RotateLeft(_w[i - 3] ^ _w[i - 8] ^ _w[i - 14] ^ _w[i - 16], 1);
            }

            uint a = _w[80];
            uint b = _w[81];
            uint c = _w[82];
            uint d = _w[83];
            uint e = _w[84];

            for (int i = 0; i != 20; i++)
            {
                const uint k = 0x5A827999;
                uint f = (b & c) | ((~b) & d);
                uint temp = BitOperations.RotateLeft(a, 5) + f + e + k + _w[i]; e = d; d = c; c = BitOperations.RotateLeft(b, 30); b = a; a = temp;
            }

            for (int i = 20; i != 40; i++)
            {
                uint f = b ^ c ^ d;
                const uint k = 0x6ED9EBA1;
                uint temp = BitOperations.RotateLeft(a, 5) + f + e + k + _w[i]; e = d; d = c; c = BitOperations.RotateLeft(b, 30); b = a; a = temp;
            }

            for (int i = 40; i != 60; i++)
            {
                uint f = (b & c) | (b & d) | (c & d);
                const uint k = 0x8F1BBCDC;
                uint temp = BitOperations.RotateLeft(a, 5) + f + e + k + _w[i]; e = d; d = c; c = BitOperations.RotateLeft(b, 30); b = a; a = temp;
            }

            for (int i = 60; i != 80; i++)
            {
                uint f = b ^ c ^ d;
                const uint k = 0xCA62C1D6;
                uint temp = BitOperations.RotateLeft(a, 5) + f + e + k + _w[i]; e = d; d = c; c = BitOperations.RotateLeft(b, 30); b = a; a = temp;
            }

            _w[80] += a;
            _w[81] += b;
            _w[82] += c;
            _w[83] += d;
            _w[84] += e;

            _length += 512; // 64 bytes == 512 bits
            _pos = 0;
        }
    }
}
