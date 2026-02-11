// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    [UnsupportedOSPlatform("browser")]
    internal sealed class WinZipAesStream : Stream
    {
        private const int BlockSize = 16; // AES block size in bytes
        private const int KeystreamBufferSize = 4096; // Pre-generate 4KB of keystream (256 blocks)
        private readonly Stream _baseStream;
        private readonly bool _encrypting;
        private readonly int _keySizeBits;
        private readonly Aes _aes;
        private ICryptoTransform? _aesEncryptor;
        private IncrementalHash? _hmac;
        private readonly byte[] _counterBlock = new byte[BlockSize];
        private byte[]? _key;
        private byte[]? _hmacKey;
        private byte[]? _salt;
        private byte[]? _passwordVerifier;
        private bool _headerWritten;
        private bool _disposed;
        private bool _authCodeValidated;
        private readonly long _totalStreamSize;
        private readonly bool _leaveOpen;
        private readonly long _encryptedDataSize;
        private long _encryptedDataRemaining;
        private readonly byte[] _partialBlock = new byte[BlockSize];
        private int _partialBlockBytes;

        // Pre-generated keystream buffer for efficiency
        private readonly byte[] _keystreamBuffer = new byte[KeystreamBufferSize];
        private int _keystreamOffset = KeystreamBufferSize; // Start depleted to force initial generation

        internal static int GetSaltSize(int keySizeBits) => keySizeBits / 16;

        //A byte array containing salt + derived key material
        internal static byte[] CreateKey(ReadOnlyMemory<char> password, byte[]? salt, int keySizeBits)
        {
            int saltSize = GetSaltSize(keySizeBits);
            int keySizeBytes = keySizeBits / 8;
            int totalKeySize = keySizeBytes + keySizeBytes + 2; // encryption key + HMAC key + verifier

            // Generate or validate salt
            byte[] saltBytes;
            if (salt == null)
            {
                saltBytes = new byte[saltSize];
                RandomNumberGenerator.Fill(saltBytes);
            }
            else
            {
                if (salt.Length != saltSize)
                {
                    throw new ArgumentException($"Salt must be {saltSize} bytes for AES-{keySizeBits}.", nameof(salt));
                }
                saltBytes = salt;
            }

            // Derive keys using PBKDF2
            int maxPasswordByteCount = Encoding.UTF8.GetMaxByteCount(password.Length);
            Span<byte> passwordBytes = stackalloc byte[maxPasswordByteCount];
            int actualByteCount = Encoding.UTF8.GetBytes(password.Span, passwordBytes);
            Span<byte> passwordSpan = passwordBytes[..actualByteCount];

            Span<byte> derivedKey = stackalloc byte[totalKeySize];

            try
            {
                Rfc2898DeriveBytes.Pbkdf2(
                    passwordSpan,
                    saltBytes,
                    derivedKey,
                    1000,
                    HashAlgorithmName.SHA1);

                // Format: [salt][encryption key][HMAC key][password verifier]
                byte[] result = new byte[saltSize + totalKeySize];
                saltBytes.CopyTo(result, 0);
                derivedKey.CopyTo(result.AsSpan(saltSize));

                return result;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(passwordBytes);
                CryptographicOperations.ZeroMemory(derivedKey);
            }
        }

        // Parses persisted key material into its components.
        private static void ParseKeyMaterial(byte[] keyMaterial, int keySizeBits,
            out byte[] salt, out byte[] encryptionKey, out byte[] hmacKey, out byte[] passwordVerifier)
        {
            int saltSize = GetSaltSize(keySizeBits);
            int keySizeBytes = keySizeBits / 8;
            int expectedSize = saltSize + keySizeBytes + keySizeBytes + 2;

            Debug.Assert(keyMaterial.Length == expectedSize, "Key material length does not match expected size.");
            int offset = 0;

            salt = new byte[saltSize];
            Array.Copy(keyMaterial, offset, salt, 0, saltSize);
            offset += saltSize;

            encryptionKey = new byte[keySizeBytes];
            Array.Copy(keyMaterial, offset, encryptionKey, 0, keySizeBytes);
            offset += keySizeBytes;

            hmacKey = new byte[keySizeBytes];
            Array.Copy(keyMaterial, offset, hmacKey, 0, keySizeBytes);
            offset += keySizeBytes;

            passwordVerifier = new byte[2];
            Array.Copy(keyMaterial, offset, passwordVerifier, 0, 2);
        }

        /// <summary>
        /// Creates a WinZipAesStream for decryption. Reads and validates the header synchronously.
        /// </summary>
        internal static WinZipAesStream Create(Stream baseStream, byte[] keyMaterial, int keySizeBits, long totalStreamSize, bool encrypting, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(baseStream);
            ArgumentNullException.ThrowIfNull(keyMaterial);

            if (!encrypting)
            {
                // Read and validate header before creating the stream
                ReadAndValidateHeaderCore(isAsync: false, baseStream, keyMaterial, keySizeBits, CancellationToken.None).GetAwaiter().GetResult();
            }

            return new WinZipAesStream(baseStream, keyMaterial, keySizeBits, totalStreamSize, encrypting, leaveOpen);
        }

        /// <summary>
        /// Creates a WinZipAesStream for decryption. Reads and validates the header asynchronously.
        /// </summary>
        internal static async Task<WinZipAesStream> CreateAsync(Stream baseStream, byte[] keyMaterial, int keySizeBits, long totalStreamSize, bool encrypting, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(baseStream);
            ArgumentNullException.ThrowIfNull(keyMaterial);

            if (!encrypting)
            {
                // Read and validate header before creating the stream
                await ReadAndValidateHeaderCore(isAsync: true, baseStream, keyMaterial, keySizeBits, cancellationToken).ConfigureAwait(false);

            }

            return new WinZipAesStream(baseStream, keyMaterial, keySizeBits, totalStreamSize, encrypting, leaveOpen);
        }

        /// <summary>
        /// Reads and validates the WinZip AES header.
        /// </summary>
        private static async Task ReadAndValidateHeaderCore(bool isAsync, Stream baseStream, byte[] keyMaterial, int keySizeBits, CancellationToken cancellationToken)
        {
            int saltSize = GetSaltSize(keySizeBits);
            int keySizeBytes = keySizeBits / 8;

            // Parse the expected salt and password verifier from key material
            byte[] expectedSalt = new byte[saltSize];
            Array.Copy(keyMaterial, 0, expectedSalt, 0, saltSize);

            byte[] expectedVerifier = new byte[2];
            int verifierOffset = saltSize + keySizeBytes + keySizeBytes; // After salt + encryption key + HMAC key
            Array.Copy(keyMaterial, verifierOffset, expectedVerifier, 0, 2);

            // Read salt from stream
            byte[] fileSalt = new byte[saltSize];
            if (isAsync)
            {
                await baseStream.ReadExactlyAsync(fileSalt, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                baseStream.ReadExactly(fileSalt);
            }

            // Read the 2-byte password verifier from stream
            byte[] verifier = new byte[2];
            if (isAsync)
            {
                await baseStream.ReadExactlyAsync(verifier, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                baseStream.ReadExactly(verifier);
            }

            // Verify the salt matches
            Debug.Assert(fileSalt.AsSpan().SequenceEqual(expectedSalt), "Salt mismatch - key material does not match this entry.");

            // Verify the password verifier
            if (!verifier.AsSpan().SequenceEqual(expectedVerifier))
            {
                throw new InvalidDataException(SR.InvalidPassword);
            }
        }

        /// <summary>
        /// Private constructor - used in Create/CreateAsync
        /// For decryption, header must already be validated before calling this constructor.
        /// </summary>
        private WinZipAesStream(Stream baseStream, byte[] keyMaterial, int keySizeBits, long totalStreamSize, bool encrypting, bool leaveOpen)
        {
            _baseStream = baseStream;
            _encrypting = encrypting;
            _keySizeBits = keySizeBits;
            _totalStreamSize = totalStreamSize;
            _leaveOpen = leaveOpen;

            _aes = Aes.Create();
            _aes.Mode = CipherMode.ECB;
            _aes.Padding = PaddingMode.None;

            Array.Clear(_counterBlock, 0, 16);
            _counterBlock[0] = 1;

            // Parse the persisted key material
            ParseKeyMaterial(keyMaterial, keySizeBits, out _salt!, out _key!, out _hmacKey!, out _passwordVerifier!);

            if (encrypting)
            {
                _encryptedDataSize = -1;
                _encryptedDataRemaining = -1;
            }
            else
            {
                int saltSize = _keySizeBits / 16;
                int headerSize = saltSize + 2; // Salt + Password Verifier
                const int hmacSize = 10; // 10-byte HMAC

                _encryptedDataSize = _totalStreamSize - headerSize - hmacSize;
                _encryptedDataRemaining = _encryptedDataSize;

                if (_encryptedDataSize < 0)
                {
                    throw new InvalidDataException(SR.InvalidWinZipSize);
                }
            }

            _hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA1, _hmacKey!);
            InitCipher();
        }

        private async Task ValidateAuthCodeCoreAsync(bool isAsync, CancellationToken cancellationToken)
        {
            Debug.Assert(!_encrypting, "ValidateAuthCode should only be called during decryption.");
            Debug.Assert(_hmac is not null, "HMAC should have been initialized");

            if (_authCodeValidated)
            {
                return;
            }

            // Finalize HMAC computation
            byte[] expectedAuth = _hmac.GetHashAndReset();

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
            {
                throw new InvalidDataException(SR.WinZipAuthCodeMismatch);
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
            if (_headerWritten)
            {
                return;
            }
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

        private void ProcessBlock(Span<byte> buffer)
        {
            Debug.Assert(_aesEncryptor is not null, "Cipher should have been initialized before processing blocks");
            Debug.Assert(_hmac is not null, "HMAC should have been initialized");

            int processed = 0;

            while (processed < buffer.Length)
            {
                // Ensure we have enough keystream bytes available
                int keystreamAvailable = KeystreamBufferSize - _keystreamOffset;
                if (keystreamAvailable == 0)
                {
                    GenerateKeystreamBuffer();
                    keystreamAvailable = KeystreamBufferSize;
                }

                // Process as many bytes as possible with the available keystream
                int bytesToProcess = Math.Min(buffer.Length - processed, keystreamAvailable);

                Span<byte> dataSpan = buffer.Slice(processed, bytesToProcess);
                ReadOnlySpan<byte> keystreamSpan = _keystreamBuffer.AsSpan(_keystreamOffset, bytesToProcess);

                // For encryption: XOR first, then HMAC the ciphertext
                if (_encrypting)
                {
                    // XOR the data with the keystream to create ciphertext
                    XorBytes(dataSpan, keystreamSpan);
                    // HMAC is computed on the ciphertext (after XOR)
                    _hmac.AppendData(dataSpan);
                }
                // For decryption: HMAC first (on ciphertext), then XOR
                else
                {
                    // HMAC is computed on the ciphertext (before XOR)
                    _hmac.AppendData(dataSpan);
                    // XOR the ciphertext with the keystream to recover plaintext
                    XorBytes(dataSpan, keystreamSpan);
                }

                _keystreamOffset += bytesToProcess;
                processed += bytesToProcess;
            }
        }
        private void GenerateKeystreamBuffer()
        {
            Debug.Assert(_aesEncryptor is not null, "Cipher should have been initialized");

            // Generate KeystreamBufferSize  bytes of keystream (256 blocks of 16 bytes each)
            for (int i = 0; i < KeystreamBufferSize; i += BlockSize)
            {
                _aesEncryptor.TransformBlock(_counterBlock, 0, BlockSize, _keystreamBuffer, i);
                IncrementCounter();
            }

            _keystreamOffset = 0;
        }

        private static void XorBytes(Span<byte> dest, ReadOnlySpan<byte> src)
        {
            Debug.Assert(dest.Length <= src.Length);

            for (int i = 0; i < dest.Length; i++)
            {
                dest[i] ^= src[i];
            }
        }

        private void IncrementCounter()
        {
            // WinZip AES treats the entire 16-byte block as a little-endian 128-bit integer
            for (int i = 0; i < 16; i++)
            {
                if (++_counterBlock[i] != 0)
                {
                    break;
                }
            }
        }

        private async Task WriteAuthCodeCoreAsync(bool isAsync, CancellationToken cancellationToken)
        {
            Debug.Assert(_encrypting, "WriteAuthCode should only be called during encryption.");
            Debug.Assert(_hmac is not null, "HMAC should have been initialized");

            if (_authCodeValidated)
            {
                return;
            }

            byte[] authCode = _hmac.GetHashAndReset();

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

            _authCodeValidated = true;
        }
        private void ThrowIfNotReadable()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_encrypting)
            {
                throw new NotSupportedException(SR.ReadingNotSupported);
            }
        }

        private int GetBytesToRead(int requestedCount)
        {
            if (_encryptedDataRemaining <= 0)
            {
                return 0;
            }

            return (int)Math.Min(requestedCount, _encryptedDataRemaining);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            return Read(buffer.AsSpan(offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            ThrowIfNotReadable();

            int bytesToRead = GetBytesToRead(buffer.Length);
            if (bytesToRead == 0)
            {
                // Only validate auth code when we've actually reached end of encrypted data,
                // not when caller simply requested 0 bytes
                if (_encryptedDataRemaining <= 0)
                {
                    ValidateAuthCode();
                }
                return 0;
            }

            Span<byte> readBuffer = buffer.Slice(0, bytesToRead);
            int bytesRead = _baseStream.Read(readBuffer);

            if (bytesRead > 0)
            {
                _encryptedDataRemaining -= bytesRead;
                ProcessBlock(readBuffer.Slice(0, bytesRead));

                // Validate auth code immediately when we've read all encrypted data
                if (_encryptedDataRemaining <= 0)
                {
                    ValidateAuthCode();
                }
            }
            else if (_encryptedDataRemaining > 0)
            {
                // Base stream returned 0 bytes but we expected more encrypted data - stream is truncated
                throw new InvalidDataException(SR.UnexpectedEndOfStream);
            }

            return bytesRead;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfNotReadable();

            int bytesToRead = GetBytesToRead(buffer.Length);
            if (bytesToRead == 0)
            {
                // Only validate auth code when we've actually reached end of encrypted data,
                // not when caller simply requested 0 bytes
                if (_encryptedDataRemaining <= 0)
                {
                    await ValidateAuthCodeAsync(cancellationToken).ConfigureAwait(false);
                }
                return 0;
            }

            int bytesRead = await _baseStream.ReadAsync(buffer.Slice(0, bytesToRead), cancellationToken).ConfigureAwait(false);

            if (bytesRead > 0)
            {
                _encryptedDataRemaining -= bytesRead;
                ProcessBlock(buffer.Span.Slice(0, bytesRead));

                // Validate auth code immediately when we've read all encrypted data
                if (_encryptedDataRemaining <= 0)
                {
                    await ValidateAuthCodeAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            else if (_encryptedDataRemaining > 0)
            {
                // Base stream returned 0 bytes but we expected more encrypted data - stream is truncated
                throw new InvalidDataException(SR.UnexpectedEndOfStream);
            }

            return bytesRead;
        }

        private void WriteCore(ReadOnlySpan<byte> buffer, byte[] workBuffer)
        {
            int inputOffset = 0;
            int inputCount = buffer.Length;

            // Fill the partial block buffer if it has data
            if (_partialBlockBytes > 0)
            {
                int copyCount = Math.Min(BlockSize - _partialBlockBytes, inputCount);
                buffer.Slice(inputOffset, copyCount).CopyTo(_partialBlock.AsSpan(_partialBlockBytes));

                _partialBlockBytes += copyCount;
                inputOffset += copyCount;
                inputCount -= copyCount;

                // If full, encrypt and write immediately
                if (_partialBlockBytes == BlockSize)
                {
                    ProcessBlock(_partialBlock.AsSpan(0, BlockSize));
                    _baseStream.Write(_partialBlock, 0, BlockSize);
                    _partialBlockBytes = 0;
                }
            }

            // Process full blocks
            while (inputCount >= BlockSize)
            {
                int bytesToProcess = Math.Min(inputCount, workBuffer.Length);
                bytesToProcess = (bytesToProcess / BlockSize) * BlockSize;

                buffer.Slice(inputOffset, bytesToProcess).CopyTo(workBuffer);
                ProcessBlock(workBuffer.AsSpan(0, bytesToProcess));
                _baseStream.Write(workBuffer, 0, bytesToProcess);

                inputOffset += bytesToProcess;
                inputCount -= bytesToProcess;
            }

            // Buffer any remaining bytes
            if (inputCount > 0)
            {
                buffer.Slice(inputOffset, inputCount).CopyTo(_partialBlock.AsSpan(_partialBlockBytes));
                _partialBlockBytes += inputCount;
            }
        }

        private void ThrowIfNotWritable()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_encrypting)
            {
                throw new NotSupportedException(SR.WritingNotSupported);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            Write(buffer.AsSpan(offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            ThrowIfNotWritable();
            if (!_headerWritten)
            {
                WriteHeader();
            }

            byte[] workBuffer = new byte[KeystreamBufferSize];
            WriteCore(buffer, workBuffer);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return WriteAsyncCore(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        private async ValueTask WriteAsyncCore(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfNotWritable();
            if (!_headerWritten)
            {
                await WriteHeaderAsync(cancellationToken).ConfigureAwait(false);
            }

            int inputOffset = 0;
            int inputCount = buffer.Length;
            byte[] workBuffer = new byte[KeystreamBufferSize];

            // Fill the partial block buffer if it has data
            if (_partialBlockBytes > 0)
            {
                int copyCount = Math.Min(BlockSize - _partialBlockBytes, inputCount);
                buffer.Slice(inputOffset, copyCount).CopyTo(_partialBlock.AsMemory(_partialBlockBytes));

                _partialBlockBytes += copyCount;
                inputOffset += copyCount;
                inputCount -= copyCount;

                // If full, encrypt and write immediately
                if (_partialBlockBytes == BlockSize)
                {
                    ProcessBlock(_partialBlock.AsSpan(0, BlockSize));
                    await _baseStream.WriteAsync(_partialBlock.AsMemory(0, BlockSize), cancellationToken).ConfigureAwait(false);
                    _partialBlockBytes = 0;
                }
            }

            // Process full blocks
            while (inputCount >= BlockSize)
            {
                int bytesToProcess = Math.Min(inputCount, workBuffer.Length);
                bytesToProcess = (bytesToProcess / BlockSize) * BlockSize;

                buffer.Slice(inputOffset, bytesToProcess).CopyTo(workBuffer);
                ProcessBlock(workBuffer.AsSpan(0, bytesToProcess));
                await _baseStream.WriteAsync(workBuffer.AsMemory(0, bytesToProcess), cancellationToken).ConfigureAwait(false);

                inputOffset += bytesToProcess;
                inputCount -= bytesToProcess;
            }

            // Buffer any remaining bytes
            if (inputCount > 0)
            {
                buffer.Slice(inputOffset, inputCount).CopyTo(_partialBlock.AsMemory(_partialBlockBytes));
                _partialBlockBytes += inputCount;
            }
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return WriteAsyncCore(buffer, cancellationToken);
        }
        private async Task FinalizeEncryptionAsync(bool isAsync, CancellationToken cancellationToken)
        {
            // Process any bytes remaining in the partial buffer
            if (_partialBlockBytes > 0)
            {
                // Encrypt the partial block (ProcessBlock handles partials by XORing only available bytes)
                ProcessBlock(_partialBlock.AsSpan(0, _partialBlockBytes));

                if (isAsync)
                {
                    await _baseStream.WriteAsync(_partialBlock.AsMemory(0, _partialBlockBytes), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _baseStream.Write(_partialBlock, 0, _partialBlockBytes);
                }

                _partialBlockBytes = 0;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                try
                {
                    if (_encrypting && !_authCodeValidated)
                    {
                        // Ensure header is written even for empty files
                        if (!_headerWritten)
                        {
                            WriteHeader();
                        }

                        // Encrypt remaining partial data
                        FinalizeEncryptionAsync(false, CancellationToken.None).GetAwaiter().GetResult();

                        // Write Auth Code
                        WriteAuthCodeCoreAsync(false, CancellationToken.None).GetAwaiter().GetResult();

                        if (_baseStream.CanWrite)
                        {
                            _baseStream.Flush();
                        }
                    }
                }
                finally
                {
                    _aesEncryptor?.Dispose();
                    _aes.Dispose();
                    _hmac?.Dispose();

                    if (!_leaveOpen)
                    {
                        _baseStream.Dispose();
                    }
                }
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                if (_encrypting && !_authCodeValidated)
                {
                    // Ensure header is written even for empty files
                    if (!_headerWritten)
                    {
                        await WriteHeaderAsync(CancellationToken.None).ConfigureAwait(false);
                    }

                    // Encrypt remaining partial data
                    await FinalizeEncryptionAsync(true, CancellationToken.None).ConfigureAwait(false);

                    // Write Auth Code
                    await WriteAuthCodeCoreAsync(true, CancellationToken.None).ConfigureAwait(false);

                    if (_baseStream.CanWrite)
                    {
                        await _baseStream.FlushAsync().ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                _aesEncryptor?.Dispose();
                _aes.Dispose();
                _hmac?.Dispose();

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
            get => throw new NotSupportedException();
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

            //// Finally flush base stream to ensure encrypted data is written
            //if (_baseStream.CanWrite)
            //{
            //    await _baseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            //}
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
