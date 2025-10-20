// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    // Internal read-only, non-seekable stream that:
    //  - Initializes ZipCrypto keys from the password
    //  - Reads & decrypts the 12-byte header and validates the check byte
    //  - Decrypts subsequent bytes on Read(...)
    internal sealed class ZipCryptoDecryptionStream : Stream
    {
        private readonly Stream _base;
        private uint _key0;
        private uint _key1;
        private uint _key2;
        private static readonly uint[] crc2Table = CreateCrc32Table();

        private static uint[] CreateCrc32Table() {

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

        public ZipCryptoDecryptionStream(Stream baseStream, ReadOnlySpan<char> password, byte expectedCheckByte)
        {
            _base = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            InitKeys(password);
            ValidateHeader(expectedCheckByte); // reads & consumes 12 bytes
        }

        private void InitKeys(ReadOnlySpan<char> password)
        {
            _key0 = 305419896;
            _key1 = 591751049;
            _key2 = 878082192;

            foreach (char ch in password)
            {
                UpdateKeys((byte)ch);
            }
        }

        private void ValidateHeader(byte expectedCheckByte)
        {
            byte[] hdr = new byte[12];
            int read = 0;
            while (read < hdr.Length)
            {
                int n = _base.Read(hdr.AsSpan(read));
                if (n <= 0) throw new InvalidDataException("Truncated ZipCrypto header.");
                read += n;
            }

            for (int i = 0; i < hdr.Length; i++)
            {
                hdr[i] = DecryptByte(hdr[i]);
            }

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
            ushort temp = (ushort)(_key2 | 2);
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

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            if ((uint)offset > buffer.Length || (uint)count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException();

            int n = _base.Read(buffer, offset, count);
            for (int i = 0; i < n; i++)
            {
                buffer[offset + i] = DecryptByte(buffer[offset + i]);
            }
            return n;
        }

        public override int Read(Span<byte> destination)
        {
            int n = _base.Read(destination);
            for (int i = 0; i < n; i++)
            {
                destination[i] = DecryptByte(destination[i]);
            }
            return n;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) _base.Dispose();
            base.Dispose(disposing);
        }

        // TODO: replace with the runtime's internal CRC32 update routine (fast table-based).
        private static uint Crc32Update(uint crc, byte b)
        {
            return crc2Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }
    }
}
