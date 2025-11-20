// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    internal sealed class ZipCryptoStream : Stream
    {
        private readonly bool _encrypting;
        private readonly Stream _base;
        private readonly bool _leaveOpen;
        private bool _headerWritten;
        private readonly ushort _verifierLow2Bytes;       // (DOS time low word when streaming)
        private readonly uint? _crc32ForHeader;           // (CRC-based header when not streaming)
        private int _position;

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

        // Private constructor for async factory method
        private ZipCryptoStream(Stream baseStream, bool encrypting, bool leaveOpen = false, ushort verifierLow2Bytes = 0, uint? crc32ForHeader = null)
        {
            _base = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _encrypting = encrypting;
            _leaveOpen = leaveOpen;
            _verifierLow2Bytes = verifierLow2Bytes;
            _crc32ForHeader = crc32ForHeader;
            _position = 0;
        }

        // Synchronous decryption constructor (existing)
        public ZipCryptoStream(Stream baseStream, ReadOnlyMemory<char> password, byte expectedCheckByte)
        {
            _base = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            InitKeysFromBytes(password.Span);
            _encrypting = false;
            ValidateHeader(expectedCheckByte); // reads & consumes 12 bytes
            _position = 0;
        }

        // Synchronous encryption constructor (existing)
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
            _position = 0;
            InitKeysFromBytes(password.Span);
        }

        // Async factory method for decryption
        public static async Task<ZipCryptoStream> CreateForDecryptionAsync(
            Stream baseStream,
            ReadOnlyMemory<char> password,
            byte expectedCheckByte,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(baseStream);

            var stream = new ZipCryptoStream(baseStream, encrypting: false);
            stream.InitKeysFromBytes(password.Span);
            await stream.ValidateHeaderAsync(expectedCheckByte, cancellationToken).ConfigureAwait(false);
            return stream;
        }

        // Async factory method for encryption
        public static Task<ZipCryptoStream> CreateForEncryptionAsync(
            Stream baseStream,
            ReadOnlyMemory<char> password,
            ushort passwordVerifierLow2Bytes,
            uint? crc32 = null,
            bool leaveOpen = false,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(baseStream);
            cancellationToken.ThrowIfCancellationRequested();

            var stream = new ZipCryptoStream(baseStream, encrypting: true, leaveOpen, passwordVerifierLow2Bytes, crc32);
            stream.InitKeysFromBytes(password.Span);

            // No async work needed for encryption constructor
            return Task.FromResult(stream);
        }

        private void EnsureHeader()
        {
            if (!_encrypting || _headerWritten) return;

            Span<byte> hdrPlain = stackalloc byte[12];

            // bytes 0..9 are random
            RandomNumberGenerator.Fill(hdrPlain.Slice(0, 10));

            // bytes 10..11: check bytes (CRC-based if crc32 provided; else DOS time low word)
            if (_crc32ForHeader.HasValue)
            {
                uint crc = _crc32ForHeader.Value;
                hdrPlain[10] = (byte)((crc >> 16) & 0xFF);
                hdrPlain[11] = (byte)((crc >> 24) & 0xFF);
            }
            else
            {
                hdrPlain[10] = (byte)(_verifierLow2Bytes & 0xFF);
                hdrPlain[11] = (byte)((_verifierLow2Bytes >> 8) & 0xFF);
            }

            // Encrypt & write; update keys with PLAINTEXT per spec
            byte[] hdrCiph = new byte[12];
            for (int i = 0; i < 12; i++)
            {
                byte ks = DecipherByte();
                byte p = hdrPlain[i];
                hdrCiph[i] = (byte)(p ^ ks);
                UpdateKeys(p);
            }

            _base.Write(hdrCiph, 0, 12);
            _headerWritten = true;
            _position += 12;
        }

        private async ValueTask EnsureHeaderAsync(CancellationToken cancellationToken)
        {
            if (!_encrypting || _headerWritten) return;

            byte[] hdrPlain = new byte[12];

            // bytes 0..9 are random
            RandomNumberGenerator.Fill(hdrPlain.AsSpan(0, 10));

            // bytes 10..11: check bytes (CRC-based if crc32 provided; else DOS time low word)
            if (_crc32ForHeader.HasValue)
            {
                uint crc = _crc32ForHeader.Value;
                hdrPlain[10] = (byte)((crc >> 16) & 0xFF);
                hdrPlain[11] = (byte)((crc >> 24) & 0xFF);
            }
            else
            {
                hdrPlain[10] = (byte)(_verifierLow2Bytes & 0xFF);
                hdrPlain[11] = (byte)((_verifierLow2Bytes >> 8) & 0xFF);
            }

            // Encrypt & write; update keys with PLAINTEXT per spec
            byte[] hdrCiph = new byte[12];
            for (int i = 0; i < 12; i++)
            {
                byte ks = DecipherByte();
                byte p = hdrPlain[i];
                hdrCiph[i] = (byte)(p ^ ks);
                UpdateKeys(p);
            }

            await _base.WriteAsync(hdrCiph.AsMemory(0, 12), cancellationToken).ConfigureAwait(false);
            _headerWritten = true;
            _position += 12;
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
            int read = 0;
            while (read < hdr.Length)
            {
                int n = _base.Read(hdr, read, hdr.Length - read);
                if (n <= 0) throw new InvalidDataException("Truncated ZipCrypto header.");
                read += n;
            }

            for (int i = 0; i < hdr.Length; i++)
                hdr[i] = DecryptByte(hdr[i]);

            if (hdr[11] != expectedCheckByte)
                throw new InvalidDataException("Invalid password for encrypted ZIP entry.");
        }

        private async ValueTask ValidateHeaderAsync(byte expectedCheckByte, CancellationToken cancellationToken)
        {
            byte[] hdr = new byte[12];
            int read = 0;
            while (read < hdr.Length)
            {
                int n = await _base.ReadAsync(hdr.AsMemory(read, hdr.Length - read), cancellationToken).ConfigureAwait(false);
                if (n <= 0) throw new InvalidDataException("Truncated ZipCrypto header.");
                read += n;
            }

            for (int i = 0; i < hdr.Length; i++)
                hdr[i] = DecryptByte(hdr[i]);

            if (hdr[11] != expectedCheckByte)
                throw new InvalidDataException("Invalid password for encrypted ZIP entry.");
        }

        private void UpdateKeys(byte b)
        {
            _key0 = Crc32Update(_key0, b);
            _key1 += (_key0 & 0xFF);
            _key1 = _key1 * 134775813 + 1;
            _key2 = Crc32Update(_key2, (byte)(_key1 >> 24));
        }

        private byte DecipherByte()
        {
            uint temp = _key2 | 2; // use uint to avoid narrowing issues
            return (byte)((temp * (temp ^ 1)) >> 8);
        }

        private byte DecryptByte(byte ciph)
        {
            byte m = DecipherByte();
            byte plain = (byte)(ciph ^ m);
            UpdateKeys(plain);
            return plain;
        }

        // ---- Stream overrides ----

        public override bool CanRead => !_encrypting;
        public override bool CanSeek => false;
        public override bool CanWrite => _encrypting;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException("ZipCryptoStream does not support seeking.");
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
                _position += n;
                return n;
            }
            throw new NotSupportedException("Stream is in encryption (write-only) mode.");
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            Write(buffer.AsSpan(offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (!_encrypting) throw new NotSupportedException("Stream is in decryption (read-only) mode.");

            EnsureHeader();

            byte[] tmp = new byte[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
            {
                byte ks = DecipherByte();
                byte p = buffer[i];
                tmp[i] = (byte)(p ^ ks);
                UpdateKeys(p);
            }
            _base.Write(tmp, 0, tmp.Length);
            _position += tmp.Length;
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
                _position += n;
                return n;
            }
            throw new NotSupportedException("Stream is in encryption (write-only) mode.");
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_encrypting) throw new NotSupportedException("Stream is in decryption (read-only) mode.");

            cancellationToken.ThrowIfCancellationRequested();

            EnsureHeader();

            byte[] tmp = new byte[buffer.Length];
            ReadOnlySpan<byte> span = buffer.Span;
            for (int i = 0; i < buffer.Length; i++)
            {
                byte ks = DecipherByte();
                byte p = span[i];
                tmp[i] = (byte)(p ^ ks);
                UpdateKeys(p);
            }

            await _base.WriteAsync(tmp, cancellationToken).ConfigureAwait(false);
            _position += tmp.Length;
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _base.FlushAsync(cancellationToken);
        }

        public override async ValueTask DisposeAsync()
        {
            // If encrypted empty entry (no payload written), still must emit 12-byte header:
            if (_encrypting && !_headerWritten)
                EnsureHeader();

            if (!_leaveOpen)
                await _base.DisposeAsync().ConfigureAwait(false);

            await base.DisposeAsync().ConfigureAwait(false);
        }

        private static uint Crc32Update(uint crc, byte b)
            => crc2Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
    }
}
