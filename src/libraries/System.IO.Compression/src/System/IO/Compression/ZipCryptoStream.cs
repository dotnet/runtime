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
        internal static ZipCryptoStream Create(Stream baseStream, byte[] keyBytes, byte expectedCheckByte, bool encrypting, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(baseStream);
            ArgumentNullException.ThrowIfNull(keyBytes);
            Debug.Assert(keyBytes.Length == KeySize, $"Key bytes must be exactly {KeySize} bytes.");
            Debug.Assert(!encrypting, "Use the overload with passwordVerifierLow2Bytes for encryption.");

            (uint key0, uint key1, uint key2) = ReadAndValidateHeaderCore(isAsync: false, baseStream, keyBytes, expectedCheckByte, CancellationToken.None).GetAwaiter().GetResult();
            return new ZipCryptoStream(baseStream, key0, key1, key2, leaveOpen);
        }

        /// <summary>
        /// Creates a ZipCryptoStream for decryption. Reads and validates the 12-byte header asynchronously.
        /// </summary>
        internal static async Task<ZipCryptoStream> CreateAsync(Stream baseStream, byte[] keyBytes, byte expectedCheckByte, bool encrypting, CancellationToken cancellationToken = default, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(baseStream);
            ArgumentNullException.ThrowIfNull(keyBytes);
            Debug.Assert(keyBytes.Length == KeySize, $"Key bytes must be exactly {KeySize} bytes.");
            Debug.Assert(!encrypting, "Use the overload with passwordVerifierLow2Bytes for encryption.");

            (uint key0, uint key1, uint key2) = await ReadAndValidateHeaderCore(isAsync: true, baseStream, keyBytes, expectedCheckByte, cancellationToken).ConfigureAwait(false);
            return new ZipCryptoStream(baseStream, key0, key1, key2, leaveOpen);
        }

        /// <summary>
        /// Creates a ZipCryptoStream for encryption. Only synchronous creation is needed since no I/O is performed here.
        /// </summary>
        internal static ZipCryptoStream Create(Stream baseStream,
                                             byte[] keyBytes,
                                             ushort passwordVerifierLow2Bytes,
                                             bool encrypting,
                                             uint? crc32 = null,
                                             bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(baseStream);
            ArgumentNullException.ThrowIfNull(keyBytes);
            Debug.Assert(keyBytes.Length == KeySize, $"Key bytes must be exactly {KeySize} bytes.");
            Debug.Assert(encrypting, "Use the overload with expectedCheckByte for decryption.");

            return new ZipCryptoStream(baseStream, keyBytes, passwordVerifierLow2Bytes, crc32, leaveOpen);
        }

        // Encryption constructor
        private ZipCryptoStream(Stream baseStream,
                               byte[] keyBytes,
                               ushort passwordVerifierLow2Bytes,
                               uint? crc32,
                               bool leaveOpen)
        {
            _base = baseStream;
            _encrypting = true;
            _leaveOpen = leaveOpen;
            _verifierLow2Bytes = passwordVerifierLow2Bytes;
            _crc32ForHeader = crc32;
            InitKeysFromKeyBytes(keyBytes);
        }

        // Creates the persisted key bytes from a password.
        // The returned byte array contains the 3 ZipCrypto keys (key0, key1, key2)
        // serialized as 12 bytes in little-endian format.
        public static byte[] CreateKey(ReadOnlyMemory<char> password)
        {
            // Initialize keys with standard ZipCrypto initial values
            uint key0 = 305419896;
            uint key1 = 591751049;
            uint key2 = 878082192;

            // ZipCrypto uses raw bytes; ASCII is the most interoperable
            var bytes = Encoding.ASCII.GetBytes(password.ToArray());
            foreach (byte b in bytes)
            {
                UpdateKeys(ref key0, ref key1, ref key2, b);
            }

            // Serialize the 3 keys to bytes in little-endian format
            byte[] keyBytes = new byte[KeySize];
            BinaryPrimitives.WriteUInt32LittleEndian(keyBytes.AsSpan(0, 4), key0);
            BinaryPrimitives.WriteUInt32LittleEndian(keyBytes.AsSpan(4, 4), key1);
            BinaryPrimitives.WriteUInt32LittleEndian(keyBytes.AsSpan(8, 4), key2);

            return keyBytes;
        }

        // Initializes keys from persisted key bytes.
        private void InitKeysFromKeyBytes(byte[] keyBytes)
        {
            _key0 = BinaryPrimitives.ReadUInt32LittleEndian(keyBytes.AsSpan(0, 4));
            _key1 = BinaryPrimitives.ReadUInt32LittleEndian(keyBytes.AsSpan(4, 4));
            _key2 = BinaryPrimitives.ReadUInt32LittleEndian(keyBytes.AsSpan(8, 4));
        }

        private byte[] CalculateHeader()
        {
            byte[] hdrPlain = new byte[12];

            // bytes 0..9 are random
            RandomNumberGenerator.Fill(hdrPlain.AsSpan(0, 10));

            // bytes 10..11: check bytes (CRC-based if crc32 provided; else DOS time low word)
            if (_crc32ForHeader.HasValue)
            {
                uint crc = _crc32ForHeader.Value;
                BinaryPrimitives.WriteUInt16LittleEndian(hdrPlain.AsSpan(10), (ushort)(crc >> 16));
            }
            else
            {
                BinaryPrimitives.WriteUInt16LittleEndian(hdrPlain.AsSpan(10), _verifierLow2Bytes);
            }

            // Update keys with PLAINTEXT per spec
            byte[] hdrCiph = new byte[12];
            for (int i = 0; i < 12; i++)
            {
                byte ks = DecryptByte(_key2);
                byte p = hdrPlain[i];
                hdrCiph[i] = (byte)(p ^ ks);
                UpdateKeys(ref _key0, ref _key1, ref _key2, p);
            }

            return hdrCiph;
        }

        private async ValueTask WriteHeaderCore(bool isAsync, CancellationToken cancellationToken = default)
        {
            if (!_encrypting || _headerWritten)
            {
                return;
            }

            byte[] hdrCiph = CalculateHeader();

            if (isAsync)
            {
                await _base.WriteAsync(hdrCiph.AsMemory(0, 12), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _base.Write(hdrCiph, 0, 12);
            }

            _headerWritten = true;
        }

        private void EnsureHeader()
        {
            WriteHeaderCore(isAsync: false).AsTask().GetAwaiter().GetResult();
        }

        private ValueTask EnsureHeaderAsync(CancellationToken cancellationToken)
        {
            return WriteHeaderCore(isAsync: true, cancellationToken);
        }

        private static async Task<(uint key0, uint key1, uint key2)> ReadAndValidateHeaderCore(bool isAsync, Stream baseStream, byte[] keyBytes, byte expectedCheckByte, CancellationToken cancellationToken)
        {
            // Initialize keys from input
            uint key0 = BinaryPrimitives.ReadUInt32LittleEndian(keyBytes.AsSpan(0, 4));
            uint key1 = BinaryPrimitives.ReadUInt32LittleEndian(keyBytes.AsSpan(4, 4));
            uint key2 = BinaryPrimitives.ReadUInt32LittleEndian(keyBytes.AsSpan(8, 4));

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

            byte[] tmp = new byte[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
            {
                byte ks = DecryptByte(_key2);
                byte p = buffer[i];
                tmp[i] = (byte)(p ^ ks);
                UpdateKeys(ref _key0, ref _key1, ref _key2, p);
            }
            _base.Write(tmp, 0, tmp.Length);
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

            byte[] tmp = new byte[buffer.Length];
            ReadOnlySpan<byte> span = buffer.Span;
            for (int i = 0; i < buffer.Length; i++)
            {
                byte ks = DecryptByte(_key2);
                byte p = span[i];
                tmp[i] = (byte)(p ^ ks);
                UpdateKeys(ref _key0, ref _key1, ref _key2, p);
            }

            await _base.WriteAsync(tmp, cancellationToken).ConfigureAwait(false);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _base.FlushAsync(cancellationToken);
        }
    }
}
