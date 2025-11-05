// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

namespace System.IO.Compression
{
    internal sealed class AesStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly bool _encrypting;
        private readonly int _keySizeBits;
        private readonly bool _ae2;
        private readonly uint? _crc32ForHeader;
        private readonly Aes _aes;
        private ICryptoTransform? _aesEncryptor;
#pragma warning disable CA1416 // HMACSHA1 is available on all platforms
        private readonly HMACSHA1 _hmac;
#pragma warning restore CA1416
        private readonly byte[] _counterBlock = new byte[16];
        private byte[]? _key;
        private byte[]? _hmacKey;
        private byte[]? _salt;
        private byte[]? _passwordVerifier;
        private bool _headerWritten;
        private bool _headerRead;
        private long _position;
        private readonly ReadOnlyMemory<char> _password;
        private bool _disposed;

        public AesStream(Stream baseStream, ReadOnlyMemory<char> password, bool encrypting, int keySizeBits = 256, bool ae2 = true, uint? crc32 = null)
        {
            ArgumentNullException.ThrowIfNull(baseStream);


            _baseStream = baseStream;
            _password = password;
            _encrypting = encrypting;
            _keySizeBits = keySizeBits;
            _ae2 = ae2;
            _crc32ForHeader = crc32;
#pragma warning disable CA1416 // HMACSHA1 is available on all platforms
            _aes = Aes.Create();
#pragma warning restore CA1416
            _aes.Mode = CipherMode.ECB;
            _aes.Padding = PaddingMode.None;

#pragma warning disable CA1416 // HMACSHA1 available on all platforms ?
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms ?
            _hmac = new HMACSHA1();
#pragma warning restore CA5350 // Do Not Use Weak Cryptographic Algorithms
#pragma warning restore CA1416

            if (_encrypting)
            {
                GenerateKeys();
                InitCipher();
            }
        }

        private void GenerateKeys()
        {
            int saltSize = _keySizeBits / 16; // 8 for AES-128, 12 for AES-192, 16 for AES-256
            _salt = new byte[saltSize];
            RandomNumberGenerator.Fill(_salt);

            // WinZip AES uses SHA1 for PBKDF2
            byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(_password.Span, _salt, 1000, HashAlgorithmName.SHA1, (_keySizeBits / 8) + 32 + 2);

            _key = new byte[_keySizeBits / 8];
            _hmacKey = new byte[32];
            _passwordVerifier = new byte[2];

            Buffer.BlockCopy(derivedKey, 0, _key, 0, _key.Length);
            Buffer.BlockCopy(derivedKey, _key.Length, _hmacKey, 0, _hmacKey.Length);
            Buffer.BlockCopy(derivedKey, _key.Length + _hmacKey.Length, _passwordVerifier, 0, _passwordVerifier.Length);

            _hmac.Key = _hmacKey;
        }

        private void InitCipher()
        {
            if (_key is null)
                throw new InvalidOperationException("Keys have not been generated.");

            _aes.Key = _key;
            _aesEncryptor = _aes.CreateEncryptor();
        }

        private void WriteHeader()
        {
            if (_headerWritten) return;

            if (_salt is null || _passwordVerifier is null)
                throw new InvalidOperationException("Keys have not been generated.");

            _baseStream.Write(_salt);
            _baseStream.Write(_passwordVerifier);

            if (_ae2 && _crc32ForHeader.HasValue)
            {
                Span<byte> crcBytes = stackalloc byte[4];
                BitConverter.TryWriteBytes(crcBytes, _crc32ForHeader.Value);
                _baseStream.Write(crcBytes);
            }

            _headerWritten = true;
        }

        private void ReadHeader()
        {
            if (_headerRead) return;

            int saltSize = _keySizeBits / 16;
            _salt = new byte[saltSize];
            _baseStream.ReadExactly(_salt);

            byte[] verifier = new byte[2];
            _baseStream.ReadExactly(verifier);

            // WinZip AES uses SHA1 for PBKDF2
            byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(_password.Span, _salt, 1000, HashAlgorithmName.SHA1, (_keySizeBits / 8) + 32 + 2);

            _key = new byte[_keySizeBits / 8];
            _hmacKey = new byte[32];
            _passwordVerifier = new byte[2];

            Buffer.BlockCopy(derivedKey, 0, _key, 0, _key.Length);
            Buffer.BlockCopy(derivedKey, _key.Length, _hmacKey, 0, _hmacKey.Length);
            Buffer.BlockCopy(derivedKey, _key.Length + _hmacKey.Length, _passwordVerifier, 0, _passwordVerifier.Length);

            if (!verifier.AsSpan().SequenceEqual(_passwordVerifier))
                throw new InvalidDataException("Invalid password.");

            _hmac.Key = _hmacKey;
            InitCipher();

            if (_ae2)
            {
                byte[] crcBytes = new byte[4];
                _baseStream.ReadExactly(crcBytes);
                // CRC can be validated later if needed
            }

            _headerRead = true;
        }

        private void ProcessBlock(byte[] buffer, int offset, int count)
        {
            if (_aesEncryptor is null)
                throw new InvalidOperationException("Cipher has not been initialized.");

            int processed = 0;
            while (processed < count)
            {
                IncrementCounter();
                byte[] keystream = new byte[16];
                _aesEncryptor.TransformBlock(_counterBlock, 0, 16, keystream, 0);

                int blockSize = Math.Min(16, count - processed);
                for (int i = 0; i < blockSize; i++)
                {
                    buffer[offset + processed + i] ^= keystream[i];
                }

                _hmac.TransformBlock(buffer, offset + processed, blockSize, null, 0);
                processed += blockSize;
            }
        }

        private void IncrementCounter()
        {
            for (int i = 15; i >= 0; i--)
            {
                if (++_counterBlock[i] != 0) break;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_encrypting)
                throw new NotSupportedException("Stream is in decryption mode.");

            WriteHeader();
            byte[] tmp = new byte[count];
            Buffer.BlockCopy(buffer, offset, tmp, 0, count);
            ProcessBlock(tmp, 0, count);
            _baseStream.Write(tmp, 0, count);
            _position += count;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_encrypting)
                throw new NotSupportedException("Stream is in encryption mode.");

            if (!_headerRead)
                ReadHeader();

            int n = _baseStream.Read(buffer, offset, count);
            if (n > 0)
            {
                ProcessBlock(buffer, offset, n);
                _position += n;
            }

            return n;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_encrypting)
                throw new NotSupportedException("Stream is in decryption mode.");

            WriteHeader();
            byte[] tmp = buffer.ToArray();
            ProcessBlock(tmp, 0, tmp.Length);
            _baseStream.Write(tmp);
            _position += buffer.Length;
        }

        public override int Read(Span<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_encrypting)
                throw new NotSupportedException("Stream is in encryption mode.");

            if (!_headerRead)
                ReadHeader();

            int n = _baseStream.Read(buffer);
            if (n > 0)
            {
                byte[] tmp = buffer[..n].ToArray();
                ProcessBlock(tmp, 0, n);
                tmp.CopyTo(buffer);
                _position += n;
            }

            return n;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                try
                {
                    if (_headerWritten || _headerRead)
                    {
                        _hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                        byte[]? authCode = _hmac.Hash;

                        if (authCode is not null)
                        {
                            if (_encrypting)
                            {
                                _baseStream.Write(authCode);
                            }
                            else
                            {
                                // For decryption, read and validate footer
                                byte[] storedAuth = new byte[authCode.Length];
                                _baseStream.ReadExactly(storedAuth);
                                if (!storedAuth.AsSpan().SequenceEqual(authCode))
                                    throw new InvalidDataException("Authentication code mismatch.");
                            }
                        }
                    }

                    _baseStream.Flush();
                }
                finally
                {
                    _aesEncryptor?.Dispose();
                    _aes.Dispose();
                    _hmac.Dispose();
                }
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        public override bool CanRead => !_encrypting && !_disposed;
        public override bool CanSeek => false;
        public override bool CanWrite => _encrypting && !_disposed;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _baseStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
