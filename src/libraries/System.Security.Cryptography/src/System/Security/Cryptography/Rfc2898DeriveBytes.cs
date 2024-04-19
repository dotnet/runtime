// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Text;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public partial class Rfc2898DeriveBytes : DeriveBytes
    {
        private byte[] _salt;
        private uint _iterations;
        private IncrementalHash _hmac;
        private readonly int _blockSize;

        private byte[] _buffer;
        private uint _block;
        private int _startIndex;
        private int _endIndex;

        /// <summary>
        /// Gets the hash algorithm used for byte derivation.
        /// </summary>
        public HashAlgorithmName HashAlgorithm { get; }

        [Obsolete(Obsoletions.Rfc2898OutdatedCtorMessage, DiagnosticId = Obsoletions.Rfc2898OutdatedCtorDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public Rfc2898DeriveBytes(byte[] password, byte[] salt, int iterations)
            : this(password, salt, iterations, HashAlgorithmName.SHA1)
        {
        }

        public Rfc2898DeriveBytes(byte[] password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm)
            : this(password, salt, iterations, hashAlgorithm, clearPassword: false)
        {
        }

        [Obsolete(Obsoletions.Rfc2898OutdatedCtorMessage, DiagnosticId = Obsoletions.Rfc2898OutdatedCtorDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public Rfc2898DeriveBytes(string password, byte[] salt)
             : this(password, salt, 1000)
        {
        }

        [Obsolete(Obsoletions.Rfc2898OutdatedCtorMessage, DiagnosticId = Obsoletions.Rfc2898OutdatedCtorDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public Rfc2898DeriveBytes(string password, byte[] salt, int iterations)
            : this(password, salt, iterations, HashAlgorithmName.SHA1)
        {
        }

        public Rfc2898DeriveBytes(string password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm)
            : this(
                Encoding.UTF8.GetBytes(password ?? throw new ArgumentNullException(nameof(password))),
                salt,
                iterations,
                hashAlgorithm,
                clearPassword: true)
        {
        }

        [Obsolete(Obsoletions.Rfc2898OutdatedCtorMessage, DiagnosticId = Obsoletions.Rfc2898OutdatedCtorDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public Rfc2898DeriveBytes(string password, int saltSize)
            : this(password, saltSize, 1000)
        {
        }

        [Obsolete(Obsoletions.Rfc2898OutdatedCtorMessage, DiagnosticId = Obsoletions.Rfc2898OutdatedCtorDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public Rfc2898DeriveBytes(string password, int saltSize, int iterations)
            : this(password, saltSize, iterations, HashAlgorithmName.SHA1)
        {
        }

        public Rfc2898DeriveBytes(string password, int saltSize, int iterations, HashAlgorithmName hashAlgorithm)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(saltSize);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);

            _salt = new byte[saltSize + sizeof(uint)];
            RandomNumberGenerator.Fill(_salt.AsSpan(0, saltSize));

            _iterations = (uint)iterations;
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            HashAlgorithm = hashAlgorithm;
            _hmac = OpenHmac(passwordBytes);
            CryptographicOperations.ZeroMemory(passwordBytes);
            _blockSize = _hmac.HashLengthInBytes;

            Initialize();
        }

        internal Rfc2898DeriveBytes(byte[] password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm, bool clearPassword) :
            this(
                new ReadOnlySpan<byte>(password ?? throw new ArgumentNullException(nameof(password))),
                new ReadOnlySpan<byte>(salt ?? throw new ArgumentNullException(nameof(salt))),
                iterations,
                hashAlgorithm)
        {
            if (clearPassword)
            {
                CryptographicOperations.ZeroMemory(password);
            }
        }

        internal Rfc2898DeriveBytes(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterations, HashAlgorithmName hashAlgorithm)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);

            _salt = new byte[salt.Length + sizeof(uint)];
            salt.CopyTo(_salt);
            _iterations = (uint)iterations;
            HashAlgorithm = hashAlgorithm;
            _hmac = OpenHmac(password);

            _blockSize = _hmac.HashLengthInBytes;
            Initialize();
        }

        public int IterationCount
        {
            get
            {
                return (int)_iterations;
            }

            set
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
                _iterations = (uint)value;
                Initialize();
            }
        }

        public byte[] Salt
        {
            get
            {
                return _salt.AsSpan(0, _salt.Length - sizeof(uint)).ToArray();
            }

            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _salt = new byte[value.Length + sizeof(uint)];
                value.AsSpan().CopyTo(_salt);
                Initialize();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_hmac != null)
                {
                    _hmac.Dispose();
                    _hmac = null!;
                }

                if (_buffer != null)
                    Array.Clear(_buffer);
                if (_salt != null)
                    Array.Clear(_salt);
            }
            base.Dispose(disposing);
        }

        public override byte[] GetBytes(int cb)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cb);

            byte[] ret = new byte[cb];
            GetBytes(ret);
            return ret;
        }

        internal void GetBytes(Span<byte> destination)
        {
            Debug.Assert(_blockSize > 0);
            int cb = destination.Length;
            int offset = 0;
            int size = _endIndex - _startIndex;
            ReadOnlySpan<byte> bufferSpan = _buffer;

            if (size > 0)
            {
                if (cb >= size)
                {
                    bufferSpan.Slice(_startIndex, size).CopyTo(destination);
                    _startIndex = _endIndex = 0;
                    offset += size;
                }
                else
                {
                    bufferSpan.Slice(_startIndex, cb).CopyTo(destination);
                    _startIndex += cb;
                    return;
                }
            }

            Debug.Assert(_startIndex == 0 && _endIndex == 0, "Invalid start or end index in the internal buffer.");

            while (offset < cb)
            {
                Func();
                int remainder = cb - offset;
                if (remainder >= _blockSize)
                {
                    bufferSpan.Slice(0, _blockSize).CopyTo(destination.Slice(offset));
                    offset += _blockSize;
                }
                else
                {
                    bufferSpan.Slice(0, remainder).CopyTo(destination.Slice(offset));
                    _startIndex = remainder;
                    _endIndex = _buffer.Length;
                    return;
                }
            }
        }

        [Obsolete(Obsoletions.Rfc2898CryptDeriveKeyMessage, DiagnosticId = Obsoletions.Rfc2898CryptDeriveKeyDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public byte[] CryptDeriveKey(string algname, string alghashname, int keySize, byte[] rgbIV)
        {
            // If this were to be implemented here, CAPI would need to be used (not CNG) because of
            // unfortunate differences between the two. Using CNG would break compatibility. Since this
            // assembly currently doesn't use CAPI it would require non-trivial additions.
            // In addition, if implemented here, only Windows would be supported as it is intended as
            // a thin wrapper over the corresponding native API.
            // Note that this method is implemented in PasswordDeriveBytes (in the Csp assembly) using CAPI.
            throw new PlatformNotSupportedException();
        }

        public override void Reset()
        {
            Initialize();
        }

        private IncrementalHash OpenHmac(ReadOnlySpan<byte> password)
        {
            HashAlgorithmName hashAlgorithm = HashAlgorithm;

            if (string.IsNullOrEmpty(hashAlgorithm.Name))
            {
                throw new CryptographicException(SR.Cryptography_HashAlgorithmNameNullOrEmpty);
            }

            // Restrict the HashAlgorithmName to known hashes, particularly excluding MD5.
            if (hashAlgorithm != HashAlgorithmName.SHA1 &&
                hashAlgorithm != HashAlgorithmName.SHA256 &&
                hashAlgorithm != HashAlgorithmName.SHA384 &&
                hashAlgorithm != HashAlgorithmName.SHA512 &&
                hashAlgorithm != HashAlgorithmName.SHA3_256 &&
                hashAlgorithm != HashAlgorithmName.SHA3_384 &&
                hashAlgorithm != HashAlgorithmName.SHA3_512)
            {
                throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
            }

            return IncrementalHash.CreateHMAC(hashAlgorithm, password);
        }

        [MemberNotNull(nameof(_buffer))]
        private void Initialize()
        {
            if (_buffer != null)
                Array.Clear(_buffer);
            _buffer = new byte[_blockSize];
            _block = 0;
            _startIndex = _endIndex = 0;
        }

        // This function is defined as follows:
        // Func (S, i) = HMAC(S || i) ^ HMAC2(S || i) ^ ... ^ HMAC(iterations) (S || i)
        // where i is the block number.
        private void Func()
        {
            // Block number is going to overflow, exceeding the maximum total possible bytes
            // that can be extracted.
            if (_block == uint.MaxValue)
                throw new CryptographicException(SR.Cryptography_ExceedKdfExtractLimit);

            BinaryPrimitives.WriteUInt32BigEndian(_salt.AsSpan(_salt.Length - sizeof(uint)), _block + 1);
            Debug.Assert(_blockSize == _buffer.Length);

            // The biggest _blockSize we have is from SHA512, which is 64 bytes.
            // Since we have a closed set of supported hash algorithms (OpenHmac())
            // we can know this always fits.
            //
            Span<byte> uiSpan = stackalloc byte[64];
            uiSpan = uiSpan.Slice(0, _blockSize);
            _hmac.AppendData(_salt);
            int bytesWritten = _hmac.GetHashAndReset(uiSpan);
            Debug.Assert(bytesWritten == _blockSize);

            uiSpan.CopyTo(_buffer);

            for (int i = 2; i <= _iterations; i++)
            {
                _hmac.AppendData(uiSpan);
                bytesWritten = _hmac.GetHashAndReset(uiSpan);
                Debug.Assert(bytesWritten == _blockSize);

                for (int j = _buffer.Length - 1; j >= 0; j--)
                {
                    _buffer[j] ^= uiSpan[j];
                }
            }

            // increment the block count.
            _block++;
        }
    }
}
