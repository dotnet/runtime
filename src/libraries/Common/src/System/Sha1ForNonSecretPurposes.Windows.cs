// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography;

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
        private byte[]? _buffer;
        private int _bufferPos;

        /// <summary>
        /// Call Start() to initialize the hash object.
        /// </summary>
        public void Start()
        {
            _buffer = null;
            _bufferPos = 0;
        }

        /// <summary>
        /// Adds an input byte to the hash.
        /// </summary>
        /// <param name="input">Data to include in the hash.</param>
        public void Append(byte input)
        {
            _buffer ??= new byte[256];

            if (_bufferPos == _buffer.Length)
            {
                byte[] newBuffer = new byte[_buffer.Length * 2];
                _buffer.CopyTo(newBuffer, 0);
                _buffer = newBuffer;
            }

            _buffer[_bufferPos++] = input;
        }

        /// <summary>
        /// Adds input bytes to the hash.
        /// </summary>
        /// <param name="input">
        /// Data to include in the hash. Must not be null.
        /// </param>
        public void Append(ReadOnlySpan<byte> input)
        {
            if (input.IsEmpty)
            {
                return;
            }

            _buffer ??= new byte[256];

            int requiredSize = _bufferPos + input.Length;
            if (requiredSize > _buffer.Length)
            {
                int newSize = _buffer.Length;
                while (newSize < requiredSize)
                {
                    newSize *= 2;
                }
                byte[] newBuffer = new byte[newSize];
                _buffer.AsSpan(0, _bufferPos).CopyTo(newBuffer);
                _buffer = newBuffer;
            }

            input.CopyTo(_buffer.AsSpan(_bufferPos));
            _bufferPos += input.Length;
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
            ReadOnlySpan<byte> source = _buffer is null ? ReadOnlySpan<byte>.Empty : new ReadOnlySpan<byte>(_buffer, 0, _bufferPos);
            
            unsafe
            {
                fixed (byte* pSrc = &MemoryMarshal.GetReference(source))
                fixed (byte* pDest = &MemoryMarshal.GetReference(output))
                {
                    Interop.BCrypt.NTSTATUS ntStatus = Interop.BCrypt.BCryptHash(
                        (uint)Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA1_ALG_HANDLE,
                        null,
                        0,
                        pSrc,
                        source.Length,
                        pDest,
                        Math.Min(output.Length, 20));

                    if (ntStatus != Interop.BCrypt.NTSTATUS.STATUS_SUCCESS)
                    {
                        int hr = unchecked((int)ntStatus) | 0x01000000;
                        throw new CryptographicException(hr);
                    }
                }
            }

            _buffer = null;
            _bufferPos = 0;
        }
    }
}
