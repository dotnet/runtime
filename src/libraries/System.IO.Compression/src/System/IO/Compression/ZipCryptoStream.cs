// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    // Internal read-only, non-seekable stream that:
    //  - Initializes ZipCrypto keys from the password
    //  - Reads & decrypts the 12-byte header and validates the check byte
    //  - Decrypts subsequent bytes on Read(...)
    internal sealed class ZipCryptoStream : Stream
    {
        private readonly bool _encrypting;
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

        // decryption constructor
        public ZipCryptoStream(Stream baseStream, ReadOnlyMemory<char> password, byte expectedCheckByte)
        {
            _base = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            InitKeys(password.Span);
            ValidateHeader(expectedCheckByte); // reads & consumes 12 bytes
        }

        public ZipCryptoStream(Stream baseStream, ReadOnlyMemory<char> password, ushort passwordVerifierLow2Bytes, uint? crc32 = null)
        {
            _base = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _encrypting = true;

            InitKeys(password.Span);
            CreateAndWriteHeader(passwordVerifierLow2Bytes, crc32);
        }

        private void CreateAndWriteHeader(ushort verifierLow2Bytes, uint? crc32)
        {
            byte[] hdrPlain = new byte[12];

            // 0..9: random
            for (int i = 0; i < 10; i++)
                hdrPlain[i] = 0;


            // 10..11: check bytes
            if (crc32.HasValue)
            {
                uint crc = crc32.Value;
                hdrPlain[10] = (byte)((crc >> 16) & 0xFF);
                hdrPlain[11] = (byte)((crc >> 24) & 0xFF);
            }
            else
            {
                // Fallback when CRC32 is not yet known
                hdrPlain[10] = (byte)(verifierLow2Bytes & 0xFF);
                hdrPlain[11] = (byte)((verifierLow2Bytes >> 8) & 0xFF);
            }

            // Encrypt header and write
            byte[] hdrCiph = new byte[12];
            for (int i = 0; i < 12; i++)
            {
                hdrCiph[i] = EncryptByte(hdrPlain[i]); // EncryptByte updates keys with PLAINTEXT
            }

            _base.Write(hdrCiph, 0, hdrCiph.Length);
        }


        private byte EncryptByte(byte plain)
        {
            byte ks = DecipherByte();
            byte ciph = (byte)(plain ^ ks);
            UpdateKeys(plain);
            return ciph;
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

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_encrypting)
            {
                ArgumentNullException.ThrowIfNull(buffer);
                if ((uint)offset > (uint)buffer.Length || (uint)count > (uint)(buffer.Length - offset))
                    throw new ArgumentOutOfRangeException();

                // Simple temporary buffer; no ArrayPool, no async
                byte[] tmp = new byte[count];
                for (int i = 0; i < count; i++)
                {
                    tmp[i] = EncryptByte(buffer[offset + i]);
                }
                _base.Write(tmp, 0, count);
                return;
            }
            throw new NotSupportedException("Stream is in decryption (read-only) mode.");
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (_encrypting)
            {
                // Simple temporary buffer; no ArrayPool, no async
                byte[] tmp = new byte[buffer.Length];
                for (int i = 0; i < buffer.Length; i++)
                {
                    tmp[i] = EncryptByte(buffer[i]);
                }
                _base.Write(tmp, 0, tmp.Length);
                return;
            }
            throw new NotSupportedException("Stream is in decryption (read-only) mode.");
        }


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
