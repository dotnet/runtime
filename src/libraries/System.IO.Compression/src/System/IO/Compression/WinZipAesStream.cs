// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
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
        private readonly Aes _aes;
        private IncrementalHash? _hmac;
        private UInt128 _counter = 1;
        private readonly byte[] _salt;
        private readonly byte[] _passwordVerifier;
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

        // Reusable work buffer for write operations, lazily allocated on first write
        private byte[]? _writeWorkBuffer;

        internal static int GetSaltSize(int keySizeBits) => WinZipAesKeyMaterial.GetSaltSize(keySizeBits);

        /// <summary>
        /// Derives key material from a password and optional salt.
        /// </summary>
        internal static WinZipAesKeyMaterial CreateKey(ReadOnlySpan<char> password, byte[]? salt, int keySizeBits)
            => WinZipAesKeyMaterial.Create(password, salt, keySizeBits);

        /// <summary>
        /// Creates a WinZipAesStream synchronously. Reads and validates the header for decryption.
        /// </summary>
        internal static WinZipAesStream Create(Stream baseStream, WinZipAesKeyMaterial keyMaterial, long totalStreamSize, bool encrypting, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(baseStream);

            if (!encrypting)
            {
                ReadAndValidateHeaderCore(isAsync: false, baseStream, keyMaterial, CancellationToken.None).GetAwaiter().GetResult();
            }

            return new WinZipAesStream(baseStream, keyMaterial, totalStreamSize, encrypting, leaveOpen);
        }

        /// <summary>
        /// Creates a WinZipAesStream asynchronously. Reads and validates the header for decryption.
        /// </summary>
        internal static async Task<WinZipAesStream> CreateAsync(Stream baseStream, WinZipAesKeyMaterial keyMaterial, long totalStreamSize, bool encrypting, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(baseStream);

            if (!encrypting)
            {
                await ReadAndValidateHeaderCore(isAsync: true, baseStream, keyMaterial, cancellationToken).ConfigureAwait(false);
            }

            return new WinZipAesStream(baseStream, keyMaterial, totalStreamSize, encrypting, leaveOpen);
        }

        /// <summary>
        /// Reads and validates the WinZip AES header (salt + password verifier) from the stream.
        /// </summary>
        private static async Task ReadAndValidateHeaderCore(bool isAsync, Stream baseStream, WinZipAesKeyMaterial keyMaterial, CancellationToken cancellationToken)
        {
            int saltSize = keyMaterial.SaltSize;

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

            // Verify the salt matches — use constant-time comparison because the salt is
            // derived from secret key material and a timing side-channel could leak information.
            if (!CryptographicOperations.FixedTimeEquals(fileSalt, keyMaterial.Salt))
            {
                throw new InvalidDataException(SR.LocalFileHeaderCorrupt);
            }

            // Verify the password verifier using constant-time comparison to prevent
            // timing attacks that could distinguish a wrong password from a corrupt archive.
            if (!CryptographicOperations.FixedTimeEquals(verifier, keyMaterial.PasswordVerifier))
            {
                throw new InvalidDataException(SR.InvalidPassword);
            }
        }

        /// <summary>
        /// Private constructor — used by Create/CreateAsync.
        /// For decryption, the header must already be validated before calling this constructor.
        /// </summary>
        private WinZipAesStream(Stream baseStream, WinZipAesKeyMaterial keyMaterial, long totalStreamSize, bool encrypting, bool leaveOpen)
        {
            _baseStream = baseStream;
            _encrypting = encrypting;
            _totalStreamSize = totalStreamSize;
            _leaveOpen = leaveOpen;

            _aes = Aes.Create();
            _aes.Mode = CipherMode.ECB;
            _aes.Padding = PaddingMode.None;

            _salt = keyMaterial.Salt;
            _passwordVerifier = keyMaterial.PasswordVerifier;

            if (encrypting)
            {
                _encryptedDataSize = -1;
                _encryptedDataRemaining = -1;
            }
            else
            {
                int headerSize = checked(keyMaterial.SaltSize + 2); // Salt + Password Verifier
                const int hmacSize = 10; // 10-byte HMAC

                _encryptedDataSize = _totalStreamSize - headerSize - hmacSize;
                _encryptedDataRemaining = _encryptedDataSize;

                if (_encryptedDataSize < 0)
                {
                    throw new InvalidDataException(SR.InvalidWinZipSize);
                }
            }

            _hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA1, keyMaterial.HmacKey);
            _aes.SetKey(keyMaterial.EncryptionKey);
        }

        private async Task ValidateAuthCodeCoreAsync(bool isAsync, CancellationToken cancellationToken)
        {
            Debug.Assert(!_encrypting, "ValidateAuthCode should only be called during decryption.");
            Debug.Assert(_hmac is not null, "HMAC should have been initialized");

            if (_authCodeValidated)
            {
                return;
            }

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

            // Finalize HMAC computation after reading, so we can use stackalloc
            Span<byte> expectedAuth = stackalloc byte[20]; // SHA1 hash size
            if (!_hmac.TryGetHashAndReset(expectedAuth, out int bytesWritten) || bytesWritten < 10)
            {
                throw new InvalidDataException(SR.WinZipAuthCodeMismatch);
            }

            // Compare the first 10 bytes of the expected hash
            if (!CryptographicOperations.FixedTimeEquals(storedAuth, expectedAuth.Slice(0, 10)))
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

        private async Task WriteHeaderCoreAsync(bool isAsync, CancellationToken cancellationToken)
        {
            if (_headerWritten)
            {
                return;
            }

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

                if (_encrypting)
                {
                    // For encryption: XOR first, then HMAC the ciphertext
                    XorBytes(dataSpan, keystreamSpan);
                    _hmac.AppendData(dataSpan);
                }
                else
                {
                    // For decryption: HMAC first (on ciphertext), then XOR
                    _hmac.AppendData(dataSpan);
                    XorBytes(dataSpan, keystreamSpan);
                }

                _keystreamOffset += bytesToProcess;
                processed += bytesToProcess;
            }
        }

        private void GenerateKeystreamBuffer()
        {
            // Fill the buffer with all counter values first
            for (int i = 0; i < KeystreamBufferSize; i += BlockSize)
            {
                BinaryPrimitives.WriteUInt128LittleEndian(_keystreamBuffer.AsSpan(i, BlockSize), _counter);
                _counter++;
            }

            // Encrypt all 256 counter blocks in a single call
            _aes.EncryptEcb(_keystreamBuffer, _keystreamBuffer, PaddingMode.None);

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

        private async Task WriteAuthCodeCoreAsync(bool isAsync, CancellationToken cancellationToken)
        {
            Debug.Assert(_encrypting, "WriteAuthCode should only be called during encryption.");
            Debug.Assert(_hmac is not null, "HMAC should have been initialized");

            if (_authCodeValidated)
            {
                return;
            }

            // Finalize HMAC computation using stackalloc to avoid heap allocation
            Span<byte> authCode = stackalloc byte[20]; // SHA1 hash size
            if (!_hmac.TryGetHashAndReset(authCode, out int bytesWritten) || bytesWritten < 10)
            {
                throw new CryptographicException();
            }

            // WinZip AES spec requires only the first 10 bytes of the HMAC
            if (isAsync)
            {
                // WriteAsync requires Memory<byte>, so we must copy to a heap buffer for the async path
                byte[] authCodeArray = authCode.Slice(0, 10).ToArray();
                await _baseStream.WriteAsync(authCodeArray.AsMemory(0, 10), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _baseStream.Write(authCode.Slice(0, 10));
            }

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

        private byte[] GetWriteWorkBuffer() => _writeWorkBuffer ??= new byte[KeystreamBufferSize];

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

            WriteCore(buffer, GetWriteWorkBuffer());
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
            byte[] workBuffer = GetWriteWorkBuffer();

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
                _aes.Dispose();
                _hmac?.Dispose();

                if (!_leaveOpen)
                {
                    await _baseStream.DisposeAsync().ConfigureAwait(false);
                }
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

            if (_baseStream.CanWrite)
            {
                await _baseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
