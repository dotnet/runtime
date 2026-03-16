// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;

namespace System.IO.Compression
{
    internal sealed class ZipCryptoStream : Stream
    {
        internal const int KeySize = 12; // 3 * sizeof(uint)

        private const int EncryptionBufferSize = 4096;

        private readonly bool _encrypting;
        private readonly Stream _base;
        private readonly bool _leaveOpen;
        private bool _headerWritten;
        private bool _disposed;
        private readonly ushort _verifierLow2Bytes;       // (DOS time low word when streaming)
        private readonly uint? _crc32ForHeader;           // (CRC-based header when not streaming)

        private uint _key0;
        private uint _key1;
        private uint _key2;
        private static readonly uint[] s_crc2Table = CreateCrc32Table();

        // Reusable work buffer for write operations, lazily allocated on first write
        private byte[]? _writeWorkBuffer;

        private static uint[] CreateCrc32Table()
        {
            var table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int j = 0; j < 8; j++)
                    c = (c & 1) != 0 ? (0xEDB88320u ^ (c >> 1)) : (c >> 1);
                table[i] = c;
            }
            return table;
        }

        private static uint Crc32Update(uint crc, byte b) => s_crc2Table[(crc ^ b) & 0xFF] ^ (crc >> 8);

        // Private decryption constructor - use Create/CreateAsync factory methods instead.
        // Keys must already be validated before calling this constructor.
        private ZipCryptoStream(Stream baseStream, uint key0, uint key1, uint key2, bool leaveOpen = false)
        {
            _base = baseStream;
            _key0 = key0;
            _key1 = key1;
            _key2 = key2;
            _encrypting = false;
            _leaveOpen = leaveOpen;
        }

        /// <summary>
        /// Creates a ZipCryptoStream for decryption. Reads and validates the 12-byte header synchronously.
        /// </summary>
        internal static ZipCryptoStream Create(Stream baseStream, ZipCryptoKeys keys, byte expectedCheckByte, bool encrypting, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(baseStream);
            Debug.Assert(!encrypting, "Use the overload with passwordVerifierLow2Bytes for encryption.");

            (uint key0, uint key1, uint key2) = ReadAndValidateHeaderCore(isAsync: false, baseStream, keys, expectedCheckByte, CancellationToken.None).GetAwaiter().GetResult();
            return new ZipCryptoStream(baseStream, key0, key1, key2, leaveOpen);
        }

        /// <summary>
        /// Creates a ZipCryptoStream for decryption. Reads and validates the 12-byte header asynchronously.
        /// </summary>
        internal static async Task<ZipCryptoStream> CreateAsync(Stream baseStream, ZipCryptoKeys keys, byte expectedCheckByte, bool encrypting, CancellationToken cancellationToken = default, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(baseStream);
            Debug.Assert(!encrypting, "Use the overload with passwordVerifierLow2Bytes for encryption.");

            (uint key0, uint key1, uint key2) = await ReadAndValidateHeaderCore(isAsync: true, baseStream, keys, expectedCheckByte, cancellationToken).ConfigureAwait(false);
            return new ZipCryptoStream(baseStream, key0, key1, key2, leaveOpen);
        }

        /// <summary>
        /// Creates a ZipCryptoStream for encryption. Only synchronous creation is needed since no I/O is performed here.
        /// </summary>
        internal static ZipCryptoStream Create(Stream baseStream,
                                             ZipCryptoKeys keys,
                                             ushort passwordVerifierLow2Bytes,
                                             bool encrypting,
                                             uint? crc32 = null,
                                             bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(baseStream);
            Debug.Assert(encrypting, "Use the overload with expectedCheckByte for decryption.");

            return new ZipCryptoStream(baseStream, keys, passwordVerifierLow2Bytes, crc32, leaveOpen);
        }

        // Encryption constructor
        private ZipCryptoStream(Stream baseStream,
                               ZipCryptoKeys keys,
                               ushort passwordVerifierLow2Bytes,
                               uint? crc32,
                               bool leaveOpen)
        {
            _base = baseStream;
            _encrypting = true;
            _leaveOpen = leaveOpen;
            _verifierLow2Bytes = passwordVerifierLow2Bytes;
            _crc32ForHeader = crc32;
            _key0 = keys.Key0;
            _key1 = keys.Key1;
            _key2 = keys.Key2;
        }

        // Creates the persisted key material from a password.
        // Returns a struct of 3 integers to keep the key off the heap.
        internal static ZipCryptoKeys CreateKey(ReadOnlySpan<char> password)
        {
            // Initialize keys with standard ZipCrypto initial values
            uint key0 = 305419896;
            uint key1 = 591751049;
            uint key2 = 878082192;

            // ASCII produces exactly 1 byte per char, so SegmentSize bytes is sufficient
            // for SegmentSize chars.
            const int SegmentSize = 32;
            Span<byte> buf = stackalloc byte[SegmentSize];

            ReadOnlySpan<char> pwSpan = password;

            while (!pwSpan.IsEmpty)
            {
                ReadOnlySpan<char> segment = pwSpan;

                if (segment.Length > SegmentSize)
                {
                    segment = segment.Slice(0, SegmentSize);
                }

                int byteCount = Encoding.ASCII.GetBytes(segment, buf);

                foreach (byte b in buf.Slice(0, byteCount))
                {
                    UpdateKeys(ref key0, ref key1, ref key2, b);
                }

                pwSpan = pwSpan.Slice(segment.Length);
            }

            return new ZipCryptoKeys(key0, key1, key2);
        }

        private void CalculateHeader(Span<byte> header)
        {
            if (header.Length < 12)
                throw new ArgumentException("Header must be at least 12 bytes.", nameof(header));

            // bytes 0..9 random
            RandomNumberGenerator.Fill(header.Slice(0, 10));

            // bytes 10..11 verifier
            if (_crc32ForHeader.HasValue)
            {
                uint crc = _crc32ForHeader.Value;
                BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(10), (ushort)(crc >> 16));
            }
            else
            {
                BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(10), _verifierLow2Bytes);
            }

            // encrypt in place
            for (int i = 0; i < 12; i++)
            {
                byte p = header[i];
                byte ks = DecryptByte(_key2);
                header[i] = (byte)(p ^ ks);

                // keys updated with PLAINTEXT per ZIP spec
                UpdateKeys(ref _key0, ref _key1, ref _key2, p);
            }
        }

        private void WriteHeader()
        {
            if (!_encrypting || _headerWritten)
                return;

            Span<byte> header = stackalloc byte[12];
            CalculateHeader(header);
            _base.Write(header);
            _headerWritten = true;
        }

        private async ValueTask WriteHeaderAsync(CancellationToken cancellationToken)
        {
            if (!_encrypting || _headerWritten)
                return;

            byte[] header = new byte[12];
            CalculateHeader(header);
            await _base.WriteAsync(header, cancellationToken).ConfigureAwait(false);
            _headerWritten = true;
        }

        private void EnsureHeader()
        {
            WriteHeader();
        }

        private ValueTask EnsureHeaderAsync(CancellationToken cancellationToken)
        {
            return WriteHeaderAsync(cancellationToken);
        }

        private static async Task<(uint key0, uint key1, uint key2)> ReadAndValidateHeaderCore(bool isAsync, Stream baseStream, ZipCryptoKeys keys, byte expectedCheckByte, CancellationToken cancellationToken)
        {
            // Initialize keys from input
            uint key0 = keys.Key0;
            uint key1 = keys.Key1;
            uint key2 = keys.Key2;

            byte[] hdr = new byte[12];
            int bytesRead;

            if (isAsync)
            {
                bytesRead = await baseStream.ReadAtLeastAsync(hdr, hdr.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                bytesRead = baseStream.ReadAtLeast(hdr, hdr.Length, throwOnEndOfStream: false);
            }

            if (bytesRead < hdr.Length)
            {
                throw new InvalidDataException(SR.TruncatedZipCryptoHeader);
            }

            // Decrypt header and update keys
            for (int i = 0; i < hdr.Length; i++)
            {
                byte m = DecryptByte(key2);
                byte plain = (byte)(hdr[i] ^ m);
                UpdateKeys(ref key0, ref key1, ref key2, plain);
                hdr[i] = plain;
            }

            if (hdr[11] != expectedCheckByte)
            {
                throw new InvalidDataException(SR.InvalidPassword);
            }

            return (key0, key1, key2);
        }

        private static void UpdateKeys(ref uint key0, ref uint key1, ref uint key2, byte b)
        {
            key0 = Crc32Update(key0, b);
            key1 += (key0 & 0xFF);
            key1 = key1 * 134775813 + 1;
            key2 = Crc32Update(key2, (byte)(key1 >> 24));
        }

        private static byte DecryptByte(uint key2)
        {
            uint temp = key2 | 2;
            return (byte)((temp * (temp ^ 1)) >> 8);
        }

        private byte DecryptAndUpdateKeys(byte ciph)
        {
            byte m = DecryptByte(_key2);
            byte plain = (byte)(ciph ^ m);
            UpdateKeys(ref _key0, ref _key1, ref _key2, plain);
            return plain;
        }

        public override bool CanRead => !_disposed && !_encrypting;
        public override bool CanSeek => false;
        public override bool CanWrite => !_disposed && _encrypting;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override void Flush() => _base.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            return Read(buffer.AsSpan(offset, count));
        }

        public override int Read(Span<byte> destination)
        {
            if (_encrypting)
            {
                throw new NotSupportedException(SR.ReadingNotSupported);
            }
            int n = _base.Read(destination);
            for (int i = 0; i < n; i++)
                destination[i] = DecryptAndUpdateKeys(destination[i]);
            return n;

        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            Write(buffer.AsSpan(offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (!_encrypting)
            {
                throw new NotSupportedException(SR.WritingNotSupported);
            }

            EnsureHeader();

            byte[] workBuffer = GetWriteWorkBuffer();
            ReadOnlySpan<byte> remaining = buffer;

            while (!remaining.IsEmpty)
            {
                int chunkSize = Math.Min(remaining.Length, workBuffer.Length);

                for (int i = 0; i < chunkSize; i++)
                {
                    byte ks = DecryptByte(_key2);
                    byte p = remaining[i];
                    workBuffer[i] = (byte)(p ^ ks);
                    UpdateKeys(ref _key0, ref _key1, ref _key2, p);
                }

                _base.Write(workBuffer, 0, chunkSize);
                remaining = remaining.Slice(chunkSize);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            if (disposing)
            {
                // If encrypted empty entry (no payload written), still must emit 12-byte header:
                if (_encrypting && !_headerWritten)
                {
                    EnsureHeader();
                }

                if (!_leaveOpen)
                {
                    _base.Dispose();
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
            _disposed = true;

            // If encrypted empty entry (no payload written), still must emit 12-byte header:
            if (_encrypting && !_headerWritten)
            {
                await EnsureHeaderAsync(CancellationToken.None).ConfigureAwait(false);
            }
            if (!_leaveOpen)
            {
                await _base.DisposeAsync().ConfigureAwait(false);
            }

            GC.SuppressFinalize(this);

            // Don't call base.DisposeAsync() as it would call Dispose() synchronously,
            // which could fail on async-only streams. We've already handled all cleanup.
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_encrypting)
            {
                throw new NotSupportedException(SR.ReadingNotSupported);
            }

            cancellationToken.ThrowIfCancellationRequested();
            int n = await _base.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            Span<byte> span = buffer.Span;

            for (int i = 0; i < n; i++)
                span[i] = DecryptAndUpdateKeys(span[i]);

            return n;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_encrypting)
            {
                throw new NotSupportedException(SR.WritingNotSupported);
            }

            cancellationToken.ThrowIfCancellationRequested();

            await EnsureHeaderAsync(cancellationToken).ConfigureAwait(false);

            byte[] workBuffer = GetWriteWorkBuffer();
            int offset = 0;

            while (offset < buffer.Length)
            {
                int chunkSize = Math.Min(buffer.Length - offset, workBuffer.Length);
                ReadOnlySpan<byte> span = buffer.Span;

                for (int i = 0; i < chunkSize; i++)
                {
                    byte ks = DecryptByte(_key2);
                    byte p = span[offset + i];
                    workBuffer[i] = (byte)(p ^ ks);
                    UpdateKeys(ref _key0, ref _key1, ref _key2, p);
                }

                await _base.WriteAsync(workBuffer.AsMemory(0, chunkSize), cancellationToken).ConfigureAwait(false);
                offset += chunkSize;
            }
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _base.FlushAsync(cancellationToken);
        }

        private byte[] GetWriteWorkBuffer() => _writeWorkBuffer ??= new byte[EncryptionBufferSize];
    }
}
