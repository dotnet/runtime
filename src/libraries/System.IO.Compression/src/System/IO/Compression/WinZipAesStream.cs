// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    internal sealed class WinZipAesStream : Stream
    {
        private const int BlockSize = 16; // AES block size in bytes
        private const int KeystreamBufferSize = 4096; // Pre-generate 4KB of keystream (256 blocks)
        private const int AuthenticationCodeSize = 10;
        private const int PasswordVerifierSize = 2;

        private readonly Stream _baseStream;
        private readonly bool _encrypting;
        private readonly Aes _aes;
        private IncrementalHash? _hmac;
        private UInt128 _counter = 1;
        private readonly byte[] _salt;
        private readonly byte[] _passwordVerifier;
        private bool _headerWritten;
        private bool _disposed;
        // During decryption: set to true after the stored auth code is read and verified.
        // During encryption: set to true after the computed auth code has been written.
        private bool _authCodeFinalized;
        private readonly long _totalStreamSize;
        private readonly bool _leaveOpen;
        private readonly long _encryptedDataSize;
        private long _encryptedDataRemaining;
        // Pre-generated keystream buffer for efficiency
        private readonly byte[] _keystreamBuffer = new byte[KeystreamBufferSize];
        private int _keystreamOffset = KeystreamBufferSize; // Start depleted to force initial generation

        // Reusable work buffer for write operations, lazily allocated on first write
        private byte[]? _writeWorkBuffer;

        internal static int GetSaltSize(int keySizeBits)
        {
            if (OperatingSystem.IsBrowser())
            {
                throw new PlatformNotSupportedException(SR.WinZipEncryptionNotSupportedOnPlatform);
            }

            return WinZipAesKeyMaterial.GetSaltSize(keySizeBits);
        }

        /// <summary>
        /// Derives key material from a password and optional salt.
        /// </summary>
        internal static WinZipAesKeyMaterial CreateKey(ReadOnlySpan<char> password, byte[]? salt, int keySizeBits)
        {
            if (OperatingSystem.IsBrowser())
            {
                throw new PlatformNotSupportedException(SR.WinZipEncryptionNotSupportedOnPlatform);
            }
            return WinZipAesKeyMaterial.Create(password, salt, keySizeBits);
        }

        /// <summary>
        /// Creates a WinZipAesStream synchronously. Reads and validates the header for decryption.
        /// </summary>
        internal static WinZipAesStream Create(Stream baseStream, WinZipAesKeyMaterial keyMaterial, long totalStreamSize, bool encrypting, bool leaveOpen = false)
        {
            if (OperatingSystem.IsBrowser())
            {
                throw new PlatformNotSupportedException(SR.WinZipEncryptionNotSupportedOnPlatform);
            }

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
            if (OperatingSystem.IsBrowser())
            {
                throw new PlatformNotSupportedException(SR.WinZipEncryptionNotSupportedOnPlatform);
            }

            ArgumentNullException.ThrowIfNull(baseStream);

            if (!encrypting)
            {
                await ReadAndValidateHeaderCore(isAsync: true, baseStream, keyMaterial, cancellationToken).ConfigureAwait(false);
            }

            return new WinZipAesStream(baseStream, keyMaterial, totalStreamSize, encrypting, leaveOpen);
        }

        internal static Stream CreateAuthenticatedStream(Stream payloadStream, Stream aesStream)
        {
            Debug.Assert(aesStream is WinZipAesStream);
            return new AuthenticatedReadStream(payloadStream, (WinZipAesStream)aesStream);
        }

        /// <summary>
        /// Reads and validates the WinZip AES header (salt + password verifier) from the stream.
        /// </summary>
        private static async Task ReadAndValidateHeaderCore(bool isAsync, Stream baseStream, WinZipAesKeyMaterial keyMaterial, CancellationToken cancellationToken)
        {
            if (OperatingSystem.IsBrowser())
            {
                throw new PlatformNotSupportedException(SR.WinZipEncryptionNotSupportedOnPlatform);
            }
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
            byte[] verifier = new byte[PasswordVerifierSize];
            if (isAsync)
            {
                await baseStream.ReadExactlyAsync(verifier, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                baseStream.ReadExactly(verifier);
            }

            // Verify the salt matches. In WinZip AES, the salt is stored in the archive
            // header and is not secret; FixedTimeEquals is used here for consistency.
            if (!CryptographicOperations.FixedTimeEquals(fileSalt, keyMaterial.Salt))
            {
                throw new InvalidDataException(SR.LocalFileHeaderCorrupt);
            }

            // Compare the 2-byte password verifier. This is a weak check (only 2 bytes) used to
            // fail fast on an obviously wrong password; it is not a security guarantee.
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
            if (OperatingSystem.IsBrowser())
            {
                throw new PlatformNotSupportedException(SR.WinZipEncryptionNotSupportedOnPlatform);
            }

            _baseStream = baseStream;

            Debug.Assert((totalStreamSize >= 0) == !encrypting, "Total stream size must be known when decrypting");

            _encrypting = encrypting;
            _totalStreamSize = totalStreamSize;
            _leaveOpen = leaveOpen;

            _aes = Aes.Create();

            _salt = keyMaterial.Salt;
            _passwordVerifier = keyMaterial.PasswordVerifier;

            if (encrypting)
            {
                _encryptedDataSize = -1;
                _encryptedDataRemaining = -1;
            }
            else
            {
                int headerSize = checked(keyMaterial.SaltSize + PasswordVerifierSize);

                _encryptedDataSize = _totalStreamSize - headerSize - AuthenticationCodeSize;
                _encryptedDataRemaining = _encryptedDataSize;

                if (_encryptedDataSize < 0)
                {
                    throw new InvalidDataException(SR.InvalidWinZipSize);
                }
            }

            _hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA1, keyMaterial.HmacKey);
            _aes.SetKey(keyMaterial.EncryptionKey);
        }

        // Compute and check the HMAC for the entire stream. This is called at the end of the stream, after all data has been read/written,
        // similarly to how CRC is computed for non-encrypted ZIP entries. The HMAC is stored in the last 10 bytes of the stream.
        private void ValidateHMAC(byte[] storedAuth)
        {
            Debug.Assert(_hmac is not null, "HMAC should have been initialized");

            Span<byte> expectedAuth = stackalloc byte[SHA1.HashSizeInBytes];
            if (!_hmac.TryGetCurrentHash(expectedAuth, out int bytesWritten) || bytesWritten < AuthenticationCodeSize)
            {
                throw new InvalidDataException(SR.WinZipAuthCodeMismatch);
            }

            // Compare the 10 bytes of the expected hash
            Debug.Assert(storedAuth.Length == AuthenticationCodeSize);
            if (!CryptographicOperations.FixedTimeEquals(storedAuth, expectedAuth.Slice(0, storedAuth.Length)))
            {
                throw new InvalidDataException(SR.WinZipAuthCodeMismatch);
            }
        }

        private async Task CompleteAuthenticationCoreAsync(bool isAsync, CancellationToken cancellationToken)
        {
            ThrowIfNotReadable();
            cancellationToken.ThrowIfCancellationRequested();

            if (_authCodeFinalized)
            {
                return;
            }

            const int AuthenticationBufferSize = 4096;
            byte[]? buffer = null;
            try
            {
                if (_encryptedDataRemaining > 0)
                {
                    // Discarded ciphertext needs hashing, but not decryption.
                    Debug.Assert(_hmac is not null);
                    buffer = ArrayPool<byte>.Shared.Rent(AuthenticationBufferSize);
                    while (_encryptedDataRemaining > 0)
                    {
                        int count = (int)Math.Min(_encryptedDataRemaining, AuthenticationBufferSize);
                        int bytesRead = isAsync
                            ? await _baseStream.ReadAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false)
                            : _baseStream.Read(buffer, 0, count);
                        if (bytesRead == 0)
                        {
                            throw new InvalidDataException(SR.UnexpectedEndOfStream);
                        }

                        _hmac.AppendData(buffer.AsSpan(0, bytesRead));
                        _encryptedDataRemaining -= bytesRead;
                    }
                }

                byte[] storedAuthenticationCode = new byte[AuthenticationCodeSize];
                int authenticationCodeBytesRead = 0;
                while (authenticationCodeBytesRead < AuthenticationCodeSize)
                {
                    int count = AuthenticationCodeSize - authenticationCodeBytesRead;
                    int bytesRead = isAsync
                        ? await _baseStream.ReadAsync(storedAuthenticationCode.AsMemory(authenticationCodeBytesRead, count), cancellationToken).ConfigureAwait(false)
                        : _baseStream.Read(storedAuthenticationCode, authenticationCodeBytesRead, count);
                    if (bytesRead == 0)
                    {
                        throw new InvalidDataException(SR.UnexpectedEndOfStream);
                    }

                    authenticationCodeBytesRead += bytesRead;
                }

                ValidateHMAC(storedAuthenticationCode);
                _authCodeFinalized = true;
            }
            finally
            {
                if (buffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        private async Task WriteHeaderAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(!_headerWritten);

            await _baseStream.WriteAsync(_salt, cancellationToken).ConfigureAwait(false);
            await _baseStream.WriteAsync(_passwordVerifier, cancellationToken).ConfigureAwait(false);

            _headerWritten = true;
        }

        private void WriteHeader()
        {
            Debug.Assert(!_headerWritten);

            _baseStream.Write(_salt);
            _baseStream.Write(_passwordVerifier);
            _headerWritten = true;
        }

        private void ProcessBlock(Span<byte> buffer)
        {
            Debug.Assert(_hmac is not null, "HMAC should have been initialized");

            while (!buffer.IsEmpty)
            {
                // Ensure we have enough keystream bytes available
                int keystreamAvailable = KeystreamBufferSize - _keystreamOffset;
                if (keystreamAvailable == 0)
                {
                    GenerateKeystreamBuffer();
                    keystreamAvailable = KeystreamBufferSize;
                }

                // Process as many bytes as possible with the available keystream
                int bytesToProcess = Math.Min(buffer.Length, keystreamAvailable);

                Span<byte> dataSpan = buffer.Slice(0, bytesToProcess);
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
                buffer = buffer.Slice(bytesToProcess);
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

            if (_authCodeFinalized)
            {
                return;
            }

            // WinZip AES spec requires only the first 10 bytes of the HMAC
            const int MacSizeInBytes = 10;

            byte[] authCode = new byte[SHA1.HashSizeInBytes];

            if (!_hmac.TryGetHashAndReset(authCode, out int bytesWritten) || bytesWritten < MacSizeInBytes)
            {
                throw new CryptographicException();
            }
            if (isAsync)
            {
                // WriteAsync requires Memory<byte>, so we must copy to a heap buffer for the async path
                await _baseStream.WriteAsync(authCode.AsMemory(0, MacSizeInBytes), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _baseStream.Write(authCode.AsSpan(0, MacSizeInBytes));
            }

            _authCodeFinalized = true;
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
            if (buffer.IsEmpty)
            {
                return 0;
            }

            int bytesToRead = GetBytesToRead(buffer.Length);
            int bytesRead = 0;
            if (bytesToRead > 0)
            {
                Span<byte> readBuffer = buffer.Slice(0, bytesToRead);
                bytesRead = _baseStream.Read(readBuffer);
                if (bytesRead == 0)
                {
                    throw new InvalidDataException(SR.UnexpectedEndOfStream);
                }

                _encryptedDataRemaining -= bytesRead;
                ProcessBlock(readBuffer.Slice(0, bytesRead));
            }

            if (_encryptedDataRemaining == 0)
            {
                CompleteAuthenticationCoreAsync(isAsync: false, CancellationToken.None).GetAwaiter().GetResult();
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
            if (buffer.IsEmpty)
            {
                return 0;
            }

            int bytesToRead = GetBytesToRead(buffer.Length);
            int bytesRead = 0;
            if (bytesToRead > 0)
            {
                bytesRead = await _baseStream.ReadAsync(buffer.Slice(0, bytesToRead), cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new InvalidDataException(SR.UnexpectedEndOfStream);
                }

                _encryptedDataRemaining -= bytesRead;
                ProcessBlock(buffer.Span.Slice(0, bytesRead));
            }

            if (_encryptedDataRemaining == 0)
            {
                await CompleteAuthenticationCoreAsync(isAsync: true, cancellationToken).ConfigureAwait(false);
            }

            return bytesRead;
        }

        private byte[] GetWriteWorkBuffer() => _writeWorkBuffer ??= new byte[KeystreamBufferSize];

        private void WriteCore(ReadOnlySpan<byte> buffer, byte[] workBuffer)
        {
            while (!buffer.IsEmpty)
            {
                int bytesToProcess = Math.Min(buffer.Length, workBuffer.Length);

                buffer[..bytesToProcess].CopyTo(workBuffer);
                ProcessBlock(workBuffer.AsSpan(0, bytesToProcess));
                _baseStream.Write(workBuffer, 0, bytesToProcess);

                buffer = buffer[bytesToProcess..];
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

            byte[] workBuffer = GetWriteWorkBuffer();

            while (!buffer.IsEmpty)
            {
                int bytesToProcess = Math.Min(buffer.Length, workBuffer.Length);

                buffer[..bytesToProcess].CopyTo(workBuffer);
                ProcessBlock(workBuffer.AsSpan(0, bytesToProcess));
                await _baseStream.WriteAsync(workBuffer.AsMemory(0, bytesToProcess), cancellationToken).ConfigureAwait(false);

                buffer = buffer[bytesToProcess..];
            }
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return WriteAsyncCore(buffer, cancellationToken);
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
                    if (_encrypting && !_authCodeFinalized)
                    {
                        FinishEncryptingAsync(isAsync: false, CancellationToken.None).GetAwaiter().GetResult();
                    }
                }
                finally
                {
                    _disposed = true;
                    _aes.Dispose();
                    _hmac?.Dispose();

                    if (!_leaveOpen)
                    {
                        _baseStream.Dispose();
                    }
                }
            }

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
                if (_encrypting && !_authCodeFinalized)
                {
                    await FinishEncryptingAsync(isAsync: true, CancellationToken.None).ConfigureAwait(false);
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
        }

        /// <summary>
        /// Completes the encryption sequence: ensures the header is written (even for empty entries),
        /// appends the HMAC authentication code, and flushes the base stream.
        /// </summary>
        private async Task FinishEncryptingAsync(bool isAsync, CancellationToken cancellationToken)
        {
            Debug.Assert(_encrypting && !_authCodeFinalized);

            // Ensure header is written even for empty files
            if (!_headerWritten)
            {
                if (isAsync)
                {
                    await WriteHeaderAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    WriteHeader();
                }
            }

            // Write Auth Code
            await WriteAuthCodeCoreAsync(isAsync, cancellationToken).ConfigureAwait(false);

            if (isAsync)
            {
                await _baseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _baseStream.Flush();
            }
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
            _baseStream.Flush();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            await _baseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        // Decompression can finish without consuming all ciphertext. Best effort: when the plaintext stream above
        // decompression reports EOF, drain the remaining ciphertext and validate the HMAC.
        private sealed class AuthenticatedReadStream : Stream
        {
            private readonly Stream _payloadStream;
            private readonly WinZipAesStream _aesStream;
            private bool _disposed;
            private bool _completed;

            internal AuthenticatedReadStream(Stream payloadStream, WinZipAesStream aesStream)
            {
                ArgumentNullException.ThrowIfNull(payloadStream);

                _payloadStream = payloadStream;
                _aesStream = aesStream;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                ValidateBufferArguments(buffer, offset, count);
                return Read(buffer.AsSpan(offset, count));
            }

            public override int Read(Span<byte> buffer)
            {
                ThrowIfDisposed();

                int bytesRead = _payloadStream.Read(buffer);
                if (bytesRead == 0 && !buffer.IsEmpty)
                {
                    CompleteAuthenticationAtPayloadEndAsync(isAsync: false, CancellationToken.None).GetAwaiter().GetResult();
                }

                return bytesRead;
            }

            public override int ReadByte()
            {
                byte value = default;
                return Read(new Span<byte>(ref value)) == 0 ? -1 : value;
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ValidateBufferArguments(buffer, offset, count);
                return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
            }

            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
                TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);

            public override int EndRead(IAsyncResult asyncResult) => TaskToAsyncResult.End<int>(asyncResult);

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfDisposed();

                int bytesRead = await _payloadStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0 && !buffer.IsEmpty)
                {
                    await CompleteAuthenticationAtPayloadEndAsync(isAsync: true, cancellationToken).ConfigureAwait(false);
                }

                return bytesRead;
            }

            private async Task CompleteAuthenticationAtPayloadEndAsync(bool isAsync, CancellationToken cancellationToken)
            {
                if (!_completed)
                {
                    await _aesStream.CompleteAuthenticationCoreAsync(isAsync, cancellationToken).ConfigureAwait(false);
                    _completed = true;
                }
            }

            private void ThrowIfDisposed()
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing && !_disposed)
                {
                    _payloadStream.Dispose();
                    _disposed = true;
                }

                base.Dispose(disposing);
            }

            public override async ValueTask DisposeAsync()
            {
                if (!_disposed)
                {
                    await _payloadStream.DisposeAsync().ConfigureAwait(false);
                    _disposed = true;
                }
            }

            public override bool CanRead => !_disposed && _payloadStream.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
                ThrowIfDisposed();
                _payloadStream.Flush();
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                return _payloadStream.FlushAsync(cancellationToken);
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException(SR.SeekingNotSupported);
            public override void SetLength(long value) => throw new NotSupportedException(SR.SetLengthRequiresSeekingAndWriting);
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException(SR.WritingNotSupported);
        }
    }
}
