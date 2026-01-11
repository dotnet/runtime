// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    internal sealed class ZipCryptoStream : Stream
    {
        internal const int KeySize = 12; // 3 * sizeof(uint)

        private readonly bool _encrypting;
        private readonly Stream _base;
        private readonly bool _leaveOpen;
        private bool _headerWritten;
        private readonly ushort _verifierLow2Bytes;       // (DOS time low word when streaming)
        private readonly uint? _crc32ForHeader;           // (CRC-based header when not streaming)

        private uint _key0;
        private uint _key1;
        private uint _key2;
        private static readonly uint[] crc2Table = CreateCrc32Table();

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

        // Decryption constructor using persisted key bytes.
        public ZipCryptoStream(Stream baseStream, byte[] keyBytes, byte expectedCheckByte)
        {
            _base = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            ArgumentNullException.ThrowIfNull(keyBytes);
            if (keyBytes.Length != KeySize)
                throw new ArgumentException($"Key bytes must be exactly {KeySize} bytes.", nameof(keyBytes));

            InitKeysFromKeyBytes(keyBytes);
            _encrypting = false;
            ValidateHeader(expectedCheckByte); // reads & consumes 12 bytes
        }

        // Encryption constructor
        public ZipCryptoStream(Stream baseStream,
                               ReadOnlyMemory<char> password,
                               ushort passwordVerifierLow2Bytes,
                               uint? crc32 = null,
                               bool leaveOpen = false)
        {
            _base = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _encrypting = true;
            _leaveOpen = leaveOpen;
            _verifierLow2Bytes = passwordVerifierLow2Bytes;
            _crc32ForHeader = crc32;
            InitKeysFromBytes(password.Span);
        }

        // Encryption constructor using persisted key bytes.
        public ZipCryptoStream(Stream baseStream,
                               byte[] keyBytes,
                               ushort passwordVerifierLow2Bytes,
                               uint? crc32 = null,
                               bool leaveOpen = false)
        {
            _base = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            ArgumentNullException.ThrowIfNull(keyBytes);
            if (keyBytes.Length != KeySize)
                throw new ArgumentException($"Key bytes must be exactly {KeySize} bytes.", nameof(keyBytes));

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
            var bytes = password.Span.ToArray();
            foreach (byte b in bytes)
            {
                key0 = Crc32Update(key0, b);
                key1 += (key0 & 0xFF);
                key1 = key1 * 134775813 + 1;
                key2 = Crc32Update(key2, (byte)(key1 >> 24));
            }

            // Serialize the 3 keys to bytes in little-endian format
            byte[] keyBytes = new byte[KeySize];
            BinaryPrimitives.WriteUInt32LittleEndian(keyBytes.AsSpan(0, 4), key0);
            BinaryPrimitives.WriteUInt32LittleEndian(keyBytes.AsSpan(4, 4), key1);
            BinaryPrimitives.WriteUInt32LittleEndian(keyBytes.AsSpan(8, 4), key2);

            return keyBytes;
        }

        // Gets the current key state as a 12-byte array.
        // This can be used to persist keys after header validation for update mode.
        internal byte[] GetKeyBytes()
        {
            byte[] keyBytes = new byte[KeySize];
            BinaryPrimitives.WriteUInt32LittleEndian(keyBytes.AsSpan(0, 4), _key0);
            BinaryPrimitives.WriteUInt32LittleEndian(keyBytes.AsSpan(4, 4), _key1);
            BinaryPrimitives.WriteUInt32LittleEndian(keyBytes.AsSpan(8, 4), _key2);
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
                byte ks = Decrypt();
                byte p = hdrPlain[i];
                hdrCiph[i] = (byte)(p ^ ks);
                UpdateKeys(p);
            }

            return hdrCiph;
        }

        private async ValueTask WriteHeaderCore(bool isAsync, CancellationToken cancellationToken = default)
        {
            if (!_encrypting || _headerWritten) return;

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

        private void InitKeysFromBytes(ReadOnlySpan<char> password)
        {
            _key0 = 305419896;
            _key1 = 591751049;
            _key2 = 878082192;

            // ZipCrypto uses raw bytes; ASCII is the most interoperable (UTF8 also acceptable).
            var bytes = password.ToArray();
            foreach (byte b in bytes)
                UpdateKeys(b);
        }

        private void ValidateHeader(byte expectedCheckByte)
        {
            byte[] hdr = new byte[12];
            try
            {
                _base.ReadExactly(hdr);
            }
            catch (EndOfStreamException)
            {
                throw new InvalidDataException(SR.TruncatedZipCryptoHeader);
            }

            for (int i = 0; i < hdr.Length; i++)
                hdr[i] = DecryptByte(hdr[i]);

            if (hdr[11] != expectedCheckByte)
                throw new InvalidDataException(SR.InvalidPassword);
        }

        private void UpdateKeys(byte b)
        {
            _key0 = Crc32Update(_key0, b);
            _key1 += (_key0 & 0xFF);
            _key1 = _key1 * 134775813 + 1;
            _key2 = Crc32Update(_key2, (byte)(_key1 >> 24));
        }

        private byte Decrypt()
        {
            uint temp = _key2 | 2; // use uint to avoid narrowing issues
            return (byte)((temp * (temp ^ 1)) >> 8);
        }

        private byte DecryptByte(byte ciph)
        {
            byte m = Decrypt();
            byte plain = (byte)(ciph ^ m);
            UpdateKeys(plain);
            return plain;
        }

        public override bool CanRead => !_encrypting;
        public override bool CanSeek => false;
        public override bool CanWrite => _encrypting;
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
            if (!_encrypting)
            {
                int n = _base.Read(destination);
                for (int i = 0; i < n; i++)
                    destination[i] = DecryptByte(destination[i]);
                return n;
            }
            throw new NotSupportedException(SR.ReadingNotSupported);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            Write(buffer.AsSpan(offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (!_encrypting) throw new NotSupportedException(SR.WritingNotSupported);

            EnsureHeader();

            byte[] tmp = new byte[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
            {
                byte ks = Decrypt();
                byte p = buffer[i];
                tmp[i] = (byte)(p ^ ks);
                UpdateKeys(p);
            }
            _base.Write(tmp, 0, tmp.Length);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // If encrypted empty entry (no payload written), still must emit 12-byte header:
                if (_encrypting && !_headerWritten)
                    EnsureHeader();

                if (!_leaveOpen)
                    _base.Dispose();
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            // If encrypted empty entry (no payload written), still must emit 12-byte header:
            if (_encrypting && !_headerWritten)
                await EnsureHeaderAsync(CancellationToken.None).ConfigureAwait(false);
            if (!_leaveOpen)
                await _base.DisposeAsync().ConfigureAwait(false);
            await base.DisposeAsync().ConfigureAwait(false);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_encrypting)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int n = await _base.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                Span<byte> span = buffer.Span;
                for (int i = 0; i < n; i++)
                    span[i] = DecryptByte(span[i]);
                return n;
            }
            throw new NotSupportedException(SR.ReadingNotSupported);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_encrypting) throw new NotSupportedException(SR.WritingNotSupported);

            cancellationToken.ThrowIfCancellationRequested();

            await EnsureHeaderAsync(cancellationToken).ConfigureAwait(false);

            byte[] tmp = new byte[buffer.Length];
            ReadOnlySpan<byte> span = buffer.Span;
            for (int i = 0; i < buffer.Length; i++)
            {
                byte ks = Decrypt();
                byte p = span[i];
                tmp[i] = (byte)(p ^ ks);
                UpdateKeys(p);
            }

            await _base.WriteAsync(tmp, cancellationToken).ConfigureAwait(false);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _base.FlushAsync(cancellationToken);
        }

        private static uint Crc32Update(uint crc, byte b)
            => crc2Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
    }
}
