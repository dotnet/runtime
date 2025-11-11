// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    internal sealed class WinZipAesStream : Stream
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
        private bool _authCodeValidated;
        private readonly byte[] _authCodeBuffer = new byte[20]; // HMACSHA1 is 20 bytes
        private int _authCodeBufferCount;

        public WinZipAesStream(Stream baseStream, ReadOnlyMemory<char> password, bool encrypting, int keySizeBits = 256, bool ae2 = true, uint? crc32 = null)
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

        private void DeriveKeysFromPassword()
        {
            Debug.Assert(_salt is not null, "Salt must be initialized before deriving keys");

            // WinZip AES uses SHA1 for PBKDF2 with 1000 iterations per spec
            byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(_password.Span, _salt!, 1000, HashAlgorithmName.SHA1, (_keySizeBits / 8) + 32 + 2);

            _key = new byte[_keySizeBits / 8];
            _hmacKey = new byte[32];
            _passwordVerifier = new byte[2];

            Buffer.BlockCopy(derivedKey, 0, _key, 0, _key.Length);
            Buffer.BlockCopy(derivedKey, _key.Length, _hmacKey, 0, _hmacKey.Length);
            Buffer.BlockCopy(derivedKey, _key.Length + _hmacKey.Length, _passwordVerifier, 0, _passwordVerifier.Length);
        }

        private void GenerateKeys()
        {
            // 8 for AES-128, 12 for AES-192, 16 for AES-256
            int saltSize = _keySizeBits / 16;
            _salt = new byte[saltSize];
            RandomNumberGenerator.Fill(_salt);

            DeriveKeysFromPassword();

            Debug.Assert(_hmacKey is not null, "HMAC key should be derived");
            _hmac.Key = _hmacKey!;
        }

        private void InitCipher()
        {
            Debug.Assert(_key is not null, "_key is not null");

            _aes.Key = _key!;
            _aesEncryptor = _aes.CreateEncryptor();
        }

        private void WriteHeader()
        {
            if (_headerWritten) return;

            Debug.Assert(_salt is not null && _passwordVerifier is not null, "Keys should have been generated before writing header");

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

            DeriveKeysFromPassword();

            Debug.Assert(_passwordVerifier is not null, "Password verifier should be derived");
            if (!verifier.AsSpan().SequenceEqual(_passwordVerifier!))
                throw new InvalidDataException("Invalid password.");

            Debug.Assert(_hmacKey is not null, "HMAC key should be derived");
            _hmac.Key = _hmacKey!;
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
            Debug.Assert(_aesEncryptor is not null, "Cipher should have been initialized before processing blocks");

            int processed = 0;
            byte[] keystream = new byte[16];
            while (processed < count)
            {
                IncrementCounter();
                _aesEncryptor.TransformBlock(_counterBlock, 0, 16, keystream, 0);

                // For the last block, we may use less than 16 bytes of the keystream
                // This is correct CTR mode behavior - we only use as many bytes as needed
                int blockSize = Math.Min(16, count - processed);

                // XOR the data with the keystream
                // Note: If blockSize < 16, we only use the first 'blockSize' bytes of keystream
                // The unused bytes are discarded, which is the expected
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

        private void WriteAuthCode()
        {
            if (!_encrypting || _authCodeValidated)
                return;

            _hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            byte[]? authCode = _hmac.Hash;

            if (authCode is not null)
            {
                _baseStream.Write(authCode);
            }

            _authCodeValidated = true;
        }

        private void ValidateAuthCode()
        {
            if (_encrypting || _authCodeValidated)
                return;

            // Finalize HMAC computation
            _hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            byte[]? expectedAuth = _hmac.Hash;

            if (expectedAuth is not null)
            {
                // Read the stored authentication code from the stream
                byte[] storedAuth = new byte[expectedAuth.Length];
                _baseStream.ReadExactly(storedAuth);

                if (!storedAuth.AsSpan().SequenceEqual(expectedAuth))
                    throw new InvalidDataException("Authentication code mismatch.");
            }

            _authCodeValidated = true;
        }

        private void WriteCore(ReadOnlySpan<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_encrypting)
                throw new NotSupportedException("Stream is in decryption mode.");

            WriteHeader();

            // We need to copy the data since ProcessBlock modifies it in place
            byte[] tmp = buffer.ToArray();
            ProcessBlock(tmp, 0, tmp.Length);
            _baseStream.Write(tmp);
            _position += buffer.Length;
        }

        private int ReadCore(Span<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_encrypting)
                throw new NotSupportedException("Stream is in encryption mode.");

            if (!_headerRead)
                ReadHeader();

            int n = _baseStream.Read(buffer);

            // Check if we reached the end of the stream
            if (n == 0 && !_authCodeValidated)
            {
                ValidateAuthCode();
                return 0;
            }

            if (n > 0)
            {
                // Process the data in-place for reads (it's already in the buffer)
                // We need to temporarily copy to array for HMAC processing
                byte[] temp = buffer.Slice(0, n).ToArray();
                ProcessBlock(temp, 0, n);
                temp.CopyTo(buffer);
                _position += n;
            }

            return n;
        }

        // All Write overloads redirect to Write(ReadOnlySpan<byte>)
        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            Write(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            WriteCore(buffer);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            await WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).ConfigureAwait(false);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_encrypting)
                throw new NotSupportedException("Stream is in decryption mode.");

            return Core(buffer, cancellationToken);

            async ValueTask Core(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            {
                WriteHeader();

                // We need to copy the data since ProcessBlock modifies it in place
                byte[] tmp = buffer.ToArray();
                ProcessBlock(tmp, 0, tmp.Length);
                await _baseStream.WriteAsync(tmp, cancellationToken).ConfigureAwait(false);
                _position += buffer.Length;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadCore(new Span<byte>(buffer, offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            return ReadCore(buffer);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).ConfigureAwait(false);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_encrypting)
                throw new NotSupportedException("Stream is in encryption mode.");

            if (!_headerRead)
                ReadHeader();

            int n = await _baseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (n > 0)
            {
                // Process the data - work with the Memory span
                byte[] temp = buffer.Slice(0, n).ToArray();
                ProcessBlock(temp, 0, n);
                temp.CopyTo(buffer.Span);
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
                    // For encryption, write the auth code when closing
                    if (_encrypting && _headerWritten && !_authCodeValidated)
                    {
                        WriteAuthCode();
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
