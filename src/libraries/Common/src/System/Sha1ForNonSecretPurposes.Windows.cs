// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

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
        private SafeBCryptHashHandle? _hashHandle;

        /// <summary>
        /// Call Start() to initialize the hash object.
        /// </summary>
        public void Start()
        {
            _hashHandle?.Dispose();
            
            Interop.BCrypt.NTSTATUS ntStatus = Interop.BCrypt.BCryptCreateHash(
                (nuint)Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA1_ALG_HANDLE,
                out SafeBCryptHashHandle hHash,
                IntPtr.Zero,
                0,
                ReadOnlySpan<byte>.Empty,
                0,
                Interop.BCrypt.BCryptCreateHashFlags.None);

            if (ntStatus != Interop.BCrypt.NTSTATUS.STATUS_SUCCESS)
            {
                hHash.Dispose();
                int hr = unchecked((int)ntStatus) | 0x01000000;
                throw new CryptographicException(hr);
            }

            _hashHandle = hHash;
        }

        /// <summary>
        /// Adds an input byte to the hash.
        /// </summary>
        /// <param name="input">Data to include in the hash.</param>
        public void Append(byte input)
        {
            Append(new ReadOnlySpan<byte>(in input));
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

            if (_hashHandle is null)
            {
                throw new InvalidOperationException();
            }

            Interop.BCrypt.NTSTATUS ntStatus = Interop.BCrypt.BCryptHashData(
                _hashHandle,
                input,
                input.Length,
                0);

            if (ntStatus != Interop.BCrypt.NTSTATUS.STATUS_SUCCESS)
            {
                int hr = unchecked((int)ntStatus) | 0x01000000;
                throw new CryptographicException(hr);
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
            if (_hashHandle is null)
            {
                throw new InvalidOperationException();
            }

            Interop.BCrypt.NTSTATUS ntStatus = Interop.BCrypt.BCryptFinishHash(
                _hashHandle,
                output,
                Math.Min(output.Length, 20),
                0);

            if (ntStatus != Interop.BCrypt.NTSTATUS.STATUS_SUCCESS)
            {
                int hr = unchecked((int)ntStatus) | 0x01000000;
                throw new CryptographicException(hr);
            }

            _hashHandle.Dispose();
            _hashHandle = null;
        }
    }
}
