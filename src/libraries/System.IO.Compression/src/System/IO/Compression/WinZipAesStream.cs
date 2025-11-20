// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
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
        private readonly long _totalStreamSize;
        private long _bytesReadFromBase;

        public WinZipAesStream(Stream baseStream, ReadOnlyMemory<char> password, bool encrypting, int keySizeBits = 256, bool ae2 = true, uint? crc32 = null, long totalStreamSize = -1)
        {
            ArgumentNullException.ThrowIfNull(baseStream);


            _baseStream = baseStream;
            _password = password;
            _encrypting = encrypting;
            _keySizeBits = keySizeBits;
            _ae2 = ae2;
            _crc32ForHeader = crc32;
            _totalStreamSize = totalStreamSize; // Store the total size
            _bytesReadFromBase = 0;
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

            Array.Clear(_counterBlock, 0, 16);
            _counterBlock[0] = 1;

            if (_encrypting)
            {
                GenerateKeys();
                InitCipher();
            }
        }

        private void DeriveKeysFromPassword()
        {
            Debug.Assert(_salt is not null, "Salt must be initialized before deriving keys");

            byte[] passwordBytes = Encoding.UTF8.GetBytes(_password.ToArray());

            try
            {
                // AES key size + HMAC key size (same as AES key) + password verifier (2 bytes)
                int keySizeInBytes = _keySizeBits / 8;
                int totalKeySize = keySizeInBytes + keySizeInBytes + 2;

                // WinZip AES uses SHA1 for PBKDF2 with 1000 iterations per spec
                byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(
                    passwordBytes,
                    _salt!,
                    1000,
                    HashAlgorithmName.SHA1,
                    totalKeySize);

                // Split the derived key material
                _key = new byte[keySizeInBytes];
                _hmacKey = new byte[keySizeInBytes];
                _passwordVerifier = new byte[2];

                // Copy the key material in the correct order
                int offset = 0;

                // First: AES encryption key
                Buffer.BlockCopy(derivedKey, offset, _key, 0, _key.Length);
                offset += _key.Length;

                // Second: HMAC authentication key (same size as encryption key)
                Buffer.BlockCopy(derivedKey, offset, _hmacKey, 0, _hmacKey.Length);
                offset += _hmacKey.Length;

                // Third: Password verification value (2 bytes)
                Buffer.BlockCopy(derivedKey, offset, _passwordVerifier, 0, _passwordVerifier.Length);

                // Clear the derived key from memory
                Array.Clear(derivedKey, 0, derivedKey.Length);
            }
            finally
            {
                // Clear the password bytes from memory
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
            }
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
                // Read the 10-byte stored authentication code from the stream
                byte[] storedAuth = new byte[10];
                _baseStream.ReadExactly(storedAuth);

                // Compare the first 10 bytes of the expected hash
                if (!storedAuth.AsSpan().SequenceEqual(expectedAuth.AsSpan(0, 10)))
                    throw new InvalidDataException("Authentication code mismatch.");
            }

            _authCodeValidated = true;
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

            // Salt size depends on AES strength: 8 for AES-128, 12 for AES-192, 16 for AES-256
            int saltSize = _keySizeBits / 16;
            _salt = new byte[saltSize];
            _baseStream.ReadExactly(_salt);

            // Debug: Log the salt
            Debug.WriteLine($"Salt ({saltSize} bytes): {BitConverter.ToString(_salt)}");

            // Read the 2-byte password verifier
            byte[] verifier = new byte[2];
            _baseStream.ReadExactly(verifier);

            // Debug: Log the verifier
            Debug.WriteLine($"Password verifier: {BitConverter.ToString(verifier)}");

            // Derive keys from password and salt
            DeriveKeysFromPassword();

            // Verify the password
            Debug.Assert(_passwordVerifier is not null, "Password verifier should be derived");

            // Debug: Log derived verifier
            Debug.WriteLine($"Derived verifier: {BitConverter.ToString(_passwordVerifier!)}");

            if (!verifier.AsSpan().SequenceEqual(_passwordVerifier!))
            {
                throw new InvalidDataException($"Invalid password. Expected verifier: {BitConverter.ToString(_passwordVerifier!)}, Got: {BitConverter.ToString(verifier)}");
            }

            Debug.Assert(_hmacKey is not null, "HMAC key should be derived");
            _hmac.Key = _hmacKey!;
            InitCipher();

            int headerSize = saltSize + 2; // Salt + Password Verifier
            _bytesReadFromBase += headerSize;

            Array.Clear(_counterBlock, 0, 16);
            _counterBlock[0] = 1;

            _headerRead = true;
        }

        private void ProcessBlock(byte[] buffer, int offset, int count)
        {
            Debug.Assert(_aesEncryptor is not null, "Cipher should have been initialized before processing blocks");

            int processed = 0;
            byte[] keystream = new byte[16];

            // Log initial counter state
            Debug.WriteLine($"=== ProcessBlock Debug ===");
            Debug.WriteLine($"Processing {count} bytes at offset {offset}");
            Debug.WriteLine($"Initial counter: {BitConverter.ToString(_counterBlock)}");

            while (processed < count)
            {
                _aesEncryptor.TransformBlock(_counterBlock, 0, 16, keystream, 0);

                // Log keystream for first block
                if (processed == 0)
                {
                    Debug.WriteLine($"First keystream block: {BitConverter.ToString(keystream)}");
                }

                IncrementCounter();

                int blockSize = Math.Min(16, count - processed);

                // For decryption: HMAC is computed on ciphertext BEFORE decryption
                if (!_encrypting)
                {
                    _hmac.TransformBlock(buffer, offset + processed, blockSize, null, 0);
                }

                // XOR the data with the keystream
                for (int i = 0; i < blockSize; i++)
                {
                    buffer[offset + processed + i] ^= keystream[i];
                }

                // For encryption: HMAC is computed on ciphertext AFTER encryption
                if (_encrypting)
                {
                    _hmac.TransformBlock(buffer, offset + processed, blockSize, null, 0);
                }

                processed += blockSize;
            }

            Debug.WriteLine($"Final counter after processing: {BitConverter.ToString(_counterBlock)}");
        }

        private void IncrementCounter()
        {
            // WinZip AES treats the entire 16-byte block as a little-endian 128-bit integer
            for (int i = 0; i < 16; i++)
            {
                if (++_counterBlock[i] != 0)
                    break;
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
                // WinZip AES spec requires only the first 10 bytes of the HMAC
                _baseStream.Write(authCode, 0, 10);
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

            int bytesToRead = buffer.Length;

            // If we know the total size, ensure we don't read into the HMAC
            if (_totalStreamSize > 0)
            {
                const int hmacSize = 10; // Correct 10-byte HMAC size
                long remainingData = _totalStreamSize - _bytesReadFromBase - hmacSize;

                Debug.WriteLine($"=== ReadCore Debug ===");
                Debug.WriteLine($"Total stream size: {_totalStreamSize}");
                Debug.WriteLine($"Bytes read from base: {_bytesReadFromBase}");
                Debug.WriteLine($"Remaining data: {remainingData}");
                Debug.WriteLine($"Buffer length requested: {buffer.Length}");

                if (remainingData <= 0)
                {
                    if (!_authCodeValidated)
                    {
                        ValidateAuthCode();
                    }
                    return 0;
                }

                bytesToRead = (int)Math.Min(bytesToRead, remainingData);
            }

            if (bytesToRead == 0)
            {
                if (!_authCodeValidated && _totalStreamSize > 0)
                {
                    ValidateAuthCode();
                }
                return 0;
            }

            int n = _baseStream.Read(buffer.Slice(0, bytesToRead));

            Debug.WriteLine($"Read {n} bytes from base stream");

            if (n > 0)
            {
                _bytesReadFromBase += n;

                // Log the ciphertext before decryption
                Debug.WriteLine($"Ciphertext (hex): {BitConverter.ToString(buffer.Slice(0, n).ToArray())}");

                // The buffer now contains the ciphertext.
                // We need to pass an array to ProcessBlock.
                byte[] temp = buffer.Slice(0, n).ToArray();

                // ProcessBlock will now correctly:
                // 1. Update the HMAC with the ciphertext from `temp`.
                // 2. Decrypt `temp` in-place.
                ProcessBlock(temp, 0, n);

                // Log the plaintext after decryption
                Debug.WriteLine($"Plaintext (hex): {BitConverter.ToString(temp)}");
                Debug.WriteLine($"Plaintext (ASCII): {System.Text.Encoding.ASCII.GetString(temp)}");

                // Copy the decrypted data from `temp` back to the original buffer.
                temp.CopyTo(buffer);

                _position += n;
            }
            else // n == 0, meaning end of stream
            {
                if (!_authCodeValidated)
                {
                    ValidateAuthCode();
                }
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

            // Apply the same boundary logic as ReadCore
            int bytesToRead = buffer.Length;

            if (_totalStreamSize > 0)
            {
                const int hmacSize = 10;
                long remainingData = _totalStreamSize - _bytesReadFromBase - hmacSize;

                if (remainingData <= 0)
                {
                    if (!_authCodeValidated)
                    {
                        ValidateAuthCode();
                    }
                    return 0;
                }

                bytesToRead = (int)Math.Min(bytesToRead, remainingData);
            }

            if (bytesToRead == 0)
            {
                if (!_authCodeValidated && _totalStreamSize > 0)
                {
                    ValidateAuthCode();
                }
                return 0;
            }

            int n = await _baseStream.ReadAsync(buffer.Slice(0, bytesToRead), cancellationToken).ConfigureAwait(false);

            if (n > 0)
            {
                _bytesReadFromBase += n; // This was missing - crucial for boundary tracking!

                // Process the data
                byte[] temp = buffer.Slice(0, n).ToArray();
                ProcessBlock(temp, 0, n);
                temp.CopyTo(buffer.Span);
                _position += n;
            }
            else if (!_authCodeValidated)
            {
                ValidateAuthCode();
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

                    // Only flush if the base stream supports writing
                    // SubReadStream (used for reading compressed data) doesn't support Flush()
                    if (_baseStream.CanWrite)
                    {
                        _baseStream.Flush();
                    }
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

            // Only flush if the base stream supports writing
            if (_baseStream.CanWrite)
            {
                _baseStream.Flush();
            }
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
