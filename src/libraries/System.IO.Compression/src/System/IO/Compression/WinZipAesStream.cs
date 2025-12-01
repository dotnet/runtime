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
        private const int BLOCK_SIZE = 16; // AES block size in bytes
        private const int KEYSTREAM_BUFFER_SIZE = 4096; // Pre-generate 4KB of keystream (256 blocks)
        private readonly Stream _baseStream;
        private readonly bool _encrypting;
        private readonly int _keySizeBits;
        private readonly Aes _aes;
        private ICryptoTransform? _aesEncryptor;
#pragma warning disable CA1416 // HMACSHA1 is available on all platforms
        private readonly HMACSHA1 _hmac;
#pragma warning restore CA1416
        private readonly byte[] _counterBlock = new byte[BLOCK_SIZE];
        private byte[]? _key;
        private byte[]? _hmacKey;
        private byte[]? _salt;
        private byte[]? _passwordVerifier;
        private bool _headerWritten;
        private bool _headerRead;
        private long _position;
        private bool _disposed;
        private bool _authCodeValidated;
        private readonly long _totalStreamSize;
        private readonly bool _leaveOpen;
        private readonly long _encryptedDataSize;
        private long _encryptedDataRemaining;
        private readonly byte[] _partialBlock = new byte[BLOCK_SIZE];
        private int _partialBlockBytes;

        // Pre-generated keystream buffer for efficiency
        private readonly byte[] _keystreamBuffer = new byte[KEYSTREAM_BUFFER_SIZE];
        private int _keystreamOffset = KEYSTREAM_BUFFER_SIZE; // Start depleted to force initial generation

        public WinZipAesStream(Stream baseStream, ReadOnlyMemory<char> password, bool encrypting, int keySizeBits = 256, long totalStreamSize = -1, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(baseStream);

            _baseStream = baseStream;
            _encrypting = encrypting;
            _keySizeBits = keySizeBits;
            _totalStreamSize = totalStreamSize; // Store the total size
            _leaveOpen = leaveOpen;
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

            if (_totalStreamSize > 0)
            {
                int saltSize = _keySizeBits / 16;
                int headerSize = saltSize + 2; // Salt + Password Verifier
                const int hmacSize = 10; // 10-byte HMAC

                _encryptedDataSize = _totalStreamSize - headerSize - hmacSize;
                _encryptedDataRemaining = _encryptedDataSize;
            }
            else
            {
                _encryptedDataSize = -1;
                _encryptedDataRemaining = -1;
            }

            if (_encrypting)
            {
                // 8 for AES-128, 12 for AES-192, 16 for AES-256
                int saltSize = _keySizeBits / 16;
                _salt = new byte[saltSize];
                RandomNumberGenerator.Fill(_salt);

                DeriveKeysFromPassword(password, _salt);

                Debug.Assert(_hmacKey is not null, "HMAC key should be derived");
                _hmac.Key = _hmacKey!;
                InitCipher();
            }
            else
            {
                ReadHeader(password);
            }
        }

        private void DeriveKeysFromPassword(ReadOnlyMemory<char> password, byte[] salt)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password.ToArray());

            try
            {
                // AES key size + HMAC key size (same as AES key) + password verifier (2 bytes)
                int keySizeInBytes = _keySizeBits / 8;
                int totalKeySize = keySizeInBytes + keySizeInBytes + 2;

                // WinZip AES uses SHA1 for PBKDF2 with 1000 iterations per spec
                byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes,
                salt,
                1000,
                HashAlgorithmName.SHA1,
                totalKeySize);

                // Split the derived key material
                _key = new byte[keySizeInBytes];
                _hmacKey = new byte[keySizeInBytes];
                _passwordVerifier = new byte[2];

                // First: AES encryption key
                derivedKey.AsSpan(0, _key.Length).CopyTo(_key);
                // Second: HMAC authentication key (same size as encryption key)
                derivedKey.AsSpan(_key.Length, _hmacKey.Length).CopyTo(_hmacKey);
                // Third: Password verification value (2 bytes)
                derivedKey.AsSpan(_key.Length + _hmacKey.Length).CopyTo(_passwordVerifier);
                // Clear the derived key from memory
                Array.Clear(derivedKey, 0, derivedKey.Length);
            }
            finally
            {
                // Clear the password bytes from memory
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
            }
        }

        private void ReadHeader(ReadOnlyMemory<char> password)
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

            // Derive keys from password and salt
            DeriveKeysFromPassword(password, _salt);

            // Verify the password
            Debug.Assert(_passwordVerifier is not null, "Password verifier should be derived");

            if (!verifier.AsSpan().SequenceEqual(_passwordVerifier!))
            {
                throw new InvalidDataException($"Invalid password. Expected verifier: {BitConverter.ToString(_passwordVerifier!)}, Got: {BitConverter.ToString(verifier)}");
            }

            Debug.Assert(_hmacKey is not null, "HMAC key should be derived");
            _hmac.Key = _hmacKey!;
            InitCipher();

            Array.Clear(_counterBlock, 0, 16);
            _counterBlock[0] = 1;

            _headerRead = true;
        }

        private async Task ValidateAuthCodeCoreAsync(bool isAsync, CancellationToken cancellationToken)
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

                if (isAsync)
                {
                    await _baseStream.ReadExactlyAsync(storedAuth, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _baseStream.ReadExactly(storedAuth);
                }

                // Compare the first 10 bytes of the expected hash
                if (!storedAuth.AsSpan().SequenceEqual(expectedAuth.AsSpan(0, 10)))
                    throw new InvalidDataException("Authentication code mismatch.");
            }

            _authCodeValidated = true;
        }

        private void ValidateAuthCode()
        {
            ValidateAuthCodeCoreAsync(isAsync: false, CancellationToken.None).GetAwaiter().GetResult();
        }

        private Task ValidateAuthCodeAsync(CancellationToken cancellationToken)
        {
            return ValidateAuthCodeCoreAsync(isAsync: true, cancellationToken);
        }

        private void InitCipher()
        {
            Debug.Assert(_key is not null, "_key is not null");

            _aes.Key = _key!;
            _aesEncryptor = _aes.CreateEncryptor();
        }

        private async Task WriteHeaderCoreAsync(bool isAsync, CancellationToken cancellationToken)
        {
            if (_headerWritten) return;

            Debug.Assert(_salt is not null && _passwordVerifier is not null, "Keys should have been generated before writing header");

            if (isAsync)
            {
                await _baseStream.WriteAsync(_salt, cancellationToken).ConfigureAwait(false);
                await _baseStream.WriteAsync(_passwordVerifier, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _baseStream.Write(_salt);
                _baseStream.Write(_passwordVerifier);
            }

            // output to debug log
            Debug.WriteLine($"Wrote salt: {BitConverter.ToString(_salt)}");
            Debug.WriteLine($"Wrote password verifier: {BitConverter.ToString(_passwordVerifier)}");

            _headerWritten = true;
        }

        private void WriteHeader()
        {
            WriteHeaderCoreAsync(isAsync: false, CancellationToken.None).GetAwaiter().GetResult();
        }

        private Task WriteHeaderAsync(CancellationToken cancellationToken)
        {
            return WriteHeaderCoreAsync(isAsync: true, cancellationToken);
        }

        private void ProcessBlock(byte[] buffer, int offset, int count)
        {
            Debug.Assert(_aesEncryptor is not null, "Cipher should have been initialized before processing blocks");

            int processed = 0;

            while (processed < count)
            {
                // Ensure we have enough keystream bytes available
                int keystreamAvailable = KEYSTREAM_BUFFER_SIZE - _keystreamOffset;
                if (keystreamAvailable == 0)
                {
                    GenerateKeystreamBuffer();
                    keystreamAvailable = KEYSTREAM_BUFFER_SIZE;
                }

                // Process as many bytes as possible with the available keystream
                int bytesToProcess = Math.Min(count - processed, keystreamAvailable);

                // For encryption: XOR first, then HMAC the ciphertext
                if (_encrypting)
                {
                    // XOR the data with the keystream to create ciphertext
                    XorBytes(buffer, offset + processed, _keystreamBuffer, _keystreamOffset, bytesToProcess);
                    // HMAC is computed on the ciphertext (after XOR)
                    _hmac.TransformBlock(buffer, offset + processed, bytesToProcess, null, 0);
                }
                // For decryption: HMAC first (on ciphertext), then XOR
                else
                {
                    // HMAC is computed on the ciphertext (before XOR)
                    _hmac.TransformBlock(buffer, offset + processed, bytesToProcess, null, 0);
                    // XOR the ciphertext with the keystream to recover plaintext
                    XorBytes(buffer, offset + processed, _keystreamBuffer, _keystreamOffset, bytesToProcess);
                }

                _keystreamOffset += bytesToProcess;
                processed += bytesToProcess;
            }
        }

        private void GenerateKeystreamBuffer()
        {
            Debug.Assert(_aesEncryptor is not null, "Cipher should have been initialized");

            // Generate KEYSTREAM_BUFFER_SIZE bytes of keystream (256 blocks of 16 bytes each)
            for (int i = 0; i < KEYSTREAM_BUFFER_SIZE; i += BLOCK_SIZE)
            {
                _aesEncryptor.TransformBlock(_counterBlock, 0, BLOCK_SIZE, _keystreamBuffer, i);
                IncrementCounter();
            }

            _keystreamOffset = 0;
        }

        private static void XorBytes(byte[] dest, int destOffset, byte[] src, int srcOffset, int count)
        {
            Span<byte> destSpan = dest.AsSpan(destOffset, count);
            ReadOnlySpan<byte> srcSpan = src.AsSpan(srcOffset, count);

            // Process 8 bytes at a time when possible for better performance
            int i = 0;
            while (i + 8 <= count)
            {
                long destVal = System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(destSpan.Slice(i));
                long srcVal = System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(srcSpan.Slice(i));
                System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(destSpan.Slice(i), destVal ^ srcVal);
                i += 8;
            }

            // Handle remaining bytes
            while (i < count)
            {
                destSpan[i] ^= srcSpan[i];
                i++;
            }
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

        private async Task WriteAuthCodeCoreAsync(bool isAsync, CancellationToken cancellationToken)
        {
            if (!_encrypting || _authCodeValidated)
                return;

            _hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            byte[]? authCode = _hmac.Hash;

            if (authCode is not null)
            {
                // WinZip AES spec requires only the first 10 bytes of the HMAC
                if (isAsync)
                {
                    await _baseStream.WriteAsync(authCode.AsMemory(0, 10), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _baseStream.Write(authCode, 0, 10);
                }

                Debug.WriteLine($"Wrote authentication code: {BitConverter.ToString(authCode, 0, 10)}");
            }

            _authCodeValidated = true;
        }

        private void WriteAuthCode()
        {
            WriteAuthCodeCoreAsync(isAsync: false, CancellationToken.None).GetAwaiter().GetResult();
        }

        private Task WriteAuthCodeAsync(CancellationToken cancellationToken)
        {
            return WriteAuthCodeCoreAsync(isAsync: true, cancellationToken);
        }

        private async Task<int> ReadCoreShared(Memory<byte> buffer, bool isAsync, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_encrypting)
                throw new NotSupportedException("Stream is in encryption mode.");

            if (!_headerRead)
                throw new InvalidOperationException("Header must be read before reading data.");

            int bytesToRead = buffer.Length;

            // If we know the total size, ensure we don't read into the HMAC
            if (_encryptedDataSize > 0)
            {
                if (_encryptedDataRemaining <= 0)
                {
                    if (!_authCodeValidated)
                    {
                        if (isAsync)
                            await ValidateAuthCodeAsync(cancellationToken).ConfigureAwait(false);
                        else
                            ValidateAuthCode();
                    }
                    return 0;
                }

                bytesToRead = (int)Math.Min(bytesToRead, _encryptedDataRemaining);
            }

            if (bytesToRead == 0)
            {
                if (!_authCodeValidated && _encryptedDataSize > 0)
                {
                    if (isAsync)
                        await ValidateAuthCodeAsync(cancellationToken).ConfigureAwait(false);
                    else
                        ValidateAuthCode();
                }
                return 0;
            }

            int bytesRead;
            if (isAsync)
            {
                bytesRead = await _baseStream.ReadAsync(buffer.Slice(0, bytesToRead), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                bytesRead = _baseStream.Read(buffer.Span.Slice(0, bytesToRead));
            }

            Debug.WriteLine($"Read {bytesRead} bytes from base stream");

            if (bytesRead > 0)
            {
                _encryptedDataRemaining -= bytesRead;

                // Process the data - we need to copy because ProcessBlock modifies in-place
                byte[] temp = buffer.Slice(0, bytesRead).ToArray();
                ProcessBlock(temp, 0, bytesRead);
                temp.CopyTo(buffer.Span);

                _position += bytesRead;
            }
            else // n == 0, meaning end of stream
            {
                if (!_authCodeValidated)
                {
                    if (isAsync)
                        await ValidateAuthCodeAsync(cancellationToken).ConfigureAwait(false);
                    else
                        ValidateAuthCode();
                }
            }

            return bytesRead;
        }

        private int ReadCore(Span<byte> buffer)
        {
            // Convert span to memory and call shared method synchronously
            byte[] tempArray = new byte[buffer.Length];
            Memory<byte> memoryBuffer = tempArray.AsMemory();

            int bytesRead = ReadCoreShared(memoryBuffer, isAsync: false, CancellationToken.None).GetAwaiter().GetResult();

            // Copy the processed data back to the original span
            memoryBuffer.Span.Slice(0, bytesRead).CopyTo(buffer);

            return bytesRead;
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
            return await ReadCoreShared(new Memory<byte>(buffer, offset, count), isAsync: true, cancellationToken).ConfigureAwait(false);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return await ReadCoreShared(buffer, isAsync: true, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteCoreShared(ReadOnlyMemory<byte> buffer, bool isAsync, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!_encrypting) throw new NotSupportedException("Stream is in decryption mode.");

            // Write header if needed
            if (!_headerWritten)
            {
                if (isAsync)
                    await WriteHeaderAsync(cancellationToken).ConfigureAwait(false);
                else
                    WriteHeader();
            }

            int inputOffset = 0;
            int inputCount = buffer.Length;

            // Fill the partial block buffer if it has data
            if (_partialBlockBytes > 0)
            {
                int copyCount = Math.Min(BLOCK_SIZE - _partialBlockBytes, inputCount);
                buffer.Slice(inputOffset, copyCount).CopyTo(_partialBlock.AsMemory(_partialBlockBytes));

                _partialBlockBytes += copyCount;
                inputOffset += copyCount;
                inputCount -= copyCount;

                // If full, encrypt and write immediately
                if (_partialBlockBytes == BLOCK_SIZE)
                {
                    ProcessBlock(_partialBlock, 0, BLOCK_SIZE);

                    if (isAsync)
                        await _baseStream.WriteAsync(_partialBlock.AsMemory(0, BLOCK_SIZE), cancellationToken).ConfigureAwait(false);
                    else
                        _baseStream.Write(_partialBlock, 0, BLOCK_SIZE);

                    _position += BLOCK_SIZE;
                    _partialBlockBytes = 0;
                }
            }

            // Process full blocks directly from the input
            if (inputCount >= BLOCK_SIZE)
            {
                const int ChunkSize = 4096;
                byte[] chunkBuffer = new byte[ChunkSize];

                while (inputCount >= BLOCK_SIZE)
                {
                    // Round down to nearest multiple of 16 for the chunk
                    int bytesToProcess = Math.Min(inputCount, ChunkSize);
                    bytesToProcess = (bytesToProcess / BLOCK_SIZE) * BLOCK_SIZE;

                    // Copy input to local buffer
                    buffer.Slice(inputOffset, bytesToProcess).CopyTo(chunkBuffer);

                    // Encrypt in-place
                    ProcessBlock(chunkBuffer, 0, bytesToProcess);

                    // Write to stream
                    if (isAsync)
                        await _baseStream.WriteAsync(chunkBuffer.AsMemory(0, bytesToProcess), cancellationToken).ConfigureAwait(false);
                    else
                        _baseStream.Write(chunkBuffer, 0, bytesToProcess);

                    _position += bytesToProcess;
                    inputOffset += bytesToProcess;
                    inputCount -= bytesToProcess;
                }
            }

            // Buffer any remaining bytes
            if (inputCount > 0)
            {
                buffer.Slice(inputOffset, inputCount).CopyTo(_partialBlock.AsMemory(_partialBlockBytes));
                _partialBlockBytes += inputCount;
            }
        }

        private void WriteCore(ReadOnlySpan<byte> buffer)
        {
            // Convert span to memory and call shared method synchronously
            byte[] tempArray = buffer.ToArray();
            WriteCoreShared(tempArray, isAsync: false, CancellationToken.None).GetAwaiter().GetResult();
        }

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
            await WriteCoreShared(new ReadOnlyMemory<byte>(buffer, offset, count), isAsync: true, cancellationToken).ConfigureAwait(false);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await WriteCoreShared(buffer, isAsync: true, cancellationToken).ConfigureAwait(false);
        }


        private async Task FinalizeEncryptionAsync(bool isAsync, CancellationToken cancellationToken)
        {
            // Process any bytes remaining in the partial buffer
            if (_partialBlockBytes > 0)
            {
                // Encrypt the partial block (ProcessBlock handles partials by XORing only available bytes)
                ProcessBlock(_partialBlock, 0, _partialBlockBytes);

                if (isAsync)
                {
                    await _baseStream.WriteAsync(_partialBlock.AsMemory(0, _partialBlockBytes), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _baseStream.Write(_partialBlock, 0, _partialBlockBytes);
                }

                _position += _partialBlockBytes;
                _partialBlockBytes = 0;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                try
                {
                    if (_encrypting && !_authCodeValidated && _headerWritten)
                    {
                        // 1. Encrypt remaining partial data
                        FinalizeEncryptionAsync(false, CancellationToken.None).GetAwaiter().GetResult();

                        // 2. Write Auth Code
                        WriteAuthCode();

                        if (_baseStream.CanWrite) _baseStream.Flush();
                    }
                    else if (!_encrypting && !_authCodeValidated && _headerRead)
                    {
                        ValidateAuthCodeCoreAsync(false, CancellationToken.None).GetAwaiter().GetResult();
                    }
                }
                finally
                {
                    _aesEncryptor?.Dispose();
                    _aes.Dispose();
                    _hmac.Dispose();
                    // Removed _encryptionBuffer.Dispose()

                    if (!_leaveOpen) _baseStream.Dispose();
                }
            }

            _disposed = true;
            base.Dispose(disposing);
        }
        public override async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            try
            {
                if (_encrypting && !_authCodeValidated && _headerWritten)
                {
                    await _baseStream.FlushAsync().ConfigureAwait(false);

                    // 1. Encrypt remaining partial data
                    await FinalizeEncryptionAsync(true, CancellationToken.None).ConfigureAwait(false);

                    // 2. Write Auth Code
                    await WriteAuthCodeAsync(CancellationToken.None).ConfigureAwait(false);

                    if (_baseStream.CanWrite) await _baseStream.FlushAsync().ConfigureAwait(false);
                }
                else if (!_encrypting && !_authCodeValidated && _headerRead)
                {
                    await ValidateAuthCodeCoreAsync(true, CancellationToken.None).ConfigureAwait(false);
                }
            }
            finally
            {
                _aesEncryptor?.Dispose();
                _aes.Dispose();
                _hmac.Dispose();

                if (!_leaveOpen) await _baseStream.DisposeAsync().ConfigureAwait(false);
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        public override bool CanRead => !_encrypting && !_disposed;
        public override bool CanSeek => false;
        public override bool CanWrite => _encrypting && !_disposed;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get
            {
                // Calculate the actual position including all metadata
                long position = _position;

                // Add header size if it has been written/read
                if (_headerWritten || _headerRead)
                {
                    int saltSize = _keySizeBits / 16;
                    int headerSize = saltSize + 2; // Salt + Password Verifier
                    position += headerSize;
                }

                // Add auth code size if it has been written/validated
                if (_authCodeValidated)
                {
                    const int authCodeSize = 10;
                    position += authCodeSize;
                }

                return position;
            }
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

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_encrypting)
            {
                // First flush base stream to ensure header is written
                if (_baseStream.CanWrite)
                {
                    await _baseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            // Finally flush base stream to ensure encrypted data is written
            if (_baseStream.CanWrite)
            {
                await _baseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
