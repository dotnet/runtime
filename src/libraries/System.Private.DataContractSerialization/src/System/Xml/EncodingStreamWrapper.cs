// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.Serialization; // For SR
using System.Text;


namespace System.Xml
{
    // This wrapper does not support seek.
    // Constructors consume/emit byte order mark.
    // Supports: UTF-8, Unicode, BigEndianUnicode
    // ASSUMPTION (Microsoft): This class will only be used for EITHER reading OR writing.  It can be done, it would just mean more buffers.
    // ASSUMPTION (Microsoft): The byte buffer is large enough to hold the declaration
    // ASSUMPTION (Microsoft): The buffer manipulation methods (FillBuffer/Compare/etc.) will only be used to parse the declaration
    //                      during construction.
    internal class EncodingStreamWrapper : Stream
    {
        private enum SupportedEncoding { UTF8, UTF16LE, UTF16BE, None }
        private static readonly UTF8Encoding s_safeUTF8 = new UTF8Encoding(false, false);
        private static readonly UnicodeEncoding s_safeUTF16 = new UnicodeEncoding(false, false, false);
        private static readonly UnicodeEncoding s_safeBEUTF16 = new UnicodeEncoding(true, false, false);
        private static readonly UTF8Encoding s_validatingUTF8 = new UTF8Encoding(false, true);
        private static readonly UnicodeEncoding s_validatingUTF16 = new UnicodeEncoding(false, false, true);
        private static readonly UnicodeEncoding s_validatingBEUTF16 = new UnicodeEncoding(true, false, true);
        private const int BufferLength = 128;

        // UTF-8 is fastpath, so that's how these are stored
        // Compare methods adapt to Unicode.
        private static readonly byte[] s_encodingAttr = new byte[] { (byte)'e', (byte)'n', (byte)'c', (byte)'o', (byte)'d', (byte)'i', (byte)'n', (byte)'g' };
        private static readonly byte[] s_encodingUTF8 = new byte[] { (byte)'u', (byte)'t', (byte)'f', (byte)'-', (byte)'8' };
        private static readonly byte[] s_encodingUnicode = new byte[] { (byte)'u', (byte)'t', (byte)'f', (byte)'-', (byte)'1', (byte)'6' };
        private static readonly byte[] s_encodingUnicodeLE = new byte[] { (byte)'u', (byte)'t', (byte)'f', (byte)'-', (byte)'1', (byte)'6', (byte)'l', (byte)'e' };
        private static readonly byte[] s_encodingUnicodeBE = new byte[] { (byte)'u', (byte)'t', (byte)'f', (byte)'-', (byte)'1', (byte)'6', (byte)'b', (byte)'e' };

        private SupportedEncoding _encodingCode;
        private Encoding _encoding;
        private Encoder _enc;
        private Decoder _dec;
        private bool _isReading;

        private Stream _stream;
        private char[] _chars;
        private byte[] _bytes;
        private int _byteOffset;
        private int _byteCount;

        private byte[] _byteBuffer = new byte[1];

        // Reading constructor
        public EncodingStreamWrapper(Stream stream, Encoding encoding)
        {
            try
            {
                _isReading = true;
                _stream = stream;

                // Decode the expected encoding
                SupportedEncoding expectedEnc = GetSupportedEncoding(encoding);

                // Get the byte order mark so we can determine the encoding
                // May want to try to delay allocating everything until we know the BOM
                SupportedEncoding declEnc = ReadBOMEncoding(encoding == null);

                // Check that the expected encoding matches the decl encoding.
                if (expectedEnc != SupportedEncoding.None && expectedEnc != declEnc)
                    ThrowExpectedEncodingMismatch(expectedEnc, declEnc);

                // Fastpath: UTF-8 BOM
                if (declEnc == SupportedEncoding.UTF8)
                {
                    // Fastpath: UTF-8 BOM, No declaration
                    FillBuffer(2);
                    if (_bytes[_byteOffset + 1] != '?' || _bytes[_byteOffset] != '<')
                    {
                        return;
                    }

                    FillBuffer(BufferLength);
                    CheckUTF8DeclarationEncoding(_bytes, _byteOffset, _byteCount, declEnc, expectedEnc);
                }
                else
                {
                    // Convert to UTF-8
                    EnsureBuffers();
                    FillBuffer((BufferLength - 1) * 2);
                    SetReadDocumentEncoding(declEnc);
                    CleanupCharBreak();
                    int count = _encoding.GetChars(_bytes, _byteOffset, _byteCount, _chars, 0);
                    _byteOffset = 0;
                    _byteCount = s_validatingUTF8.GetBytes(_chars, 0, count, _bytes, 0);

                    // Check for declaration
                    if (_bytes[1] == '?' && _bytes[0] == '<')
                    {
                        CheckUTF8DeclarationEncoding(_bytes, 0, _byteCount, declEnc, expectedEnc);
                    }
                    else
                    {
                        // Declaration required if no out-of-band encoding
                        if (expectedEnc == SupportedEncoding.None)
                            throw new XmlException(SR.XmlDeclarationRequired);
                    }
                }
            }
            catch (DecoderFallbackException ex)
            {
                throw new XmlException(SR.XmlInvalidBytes, ex);
            }
        }

        private void SetReadDocumentEncoding(SupportedEncoding e)
        {
            EnsureBuffers();
            _encodingCode = e;
            _encoding = GetEncoding(e);
        }

        private static Encoding GetEncoding(SupportedEncoding e)
        {
            switch (e)
            {
                case SupportedEncoding.UTF8:
                    return s_validatingUTF8;

                case SupportedEncoding.UTF16LE:
                    return s_validatingUTF16;

                case SupportedEncoding.UTF16BE:
                    return s_validatingBEUTF16;

                default:
                    throw new XmlException(SR.XmlEncodingNotSupported);
            }
        }

        private static Encoding GetSafeEncoding(SupportedEncoding e)
        {
            switch (e)
            {
                case SupportedEncoding.UTF8:
                    return s_safeUTF8;

                case SupportedEncoding.UTF16LE:
                    return s_safeUTF16;

                case SupportedEncoding.UTF16BE:
                    return s_safeBEUTF16;

                default:
                    throw new XmlException(SR.XmlEncodingNotSupported);
            }
        }

        private static string GetEncodingName(SupportedEncoding enc)
        {
            switch (enc)
            {
                case SupportedEncoding.UTF8:
                    return "utf-8";

                case SupportedEncoding.UTF16LE:
                    return "utf-16LE";

                case SupportedEncoding.UTF16BE:
                    return "utf-16BE";

                default:
                    throw new XmlException(SR.XmlEncodingNotSupported);
            }
        }

        private static SupportedEncoding GetSupportedEncoding(Encoding encoding)
        {
            if (encoding == null)
                return SupportedEncoding.None;
            else if (encoding.WebName == s_validatingUTF8.WebName)
                return SupportedEncoding.UTF8;
            else if (encoding.WebName == s_validatingUTF16.WebName)
                return SupportedEncoding.UTF16LE;
            else if (encoding.WebName == s_validatingBEUTF16.WebName)
                return SupportedEncoding.UTF16BE;
            else
                throw new XmlException(SR.XmlEncodingNotSupported);
        }

        // Writing constructor
        public EncodingStreamWrapper(Stream stream, Encoding encoding, bool emitBOM)
        {
            _isReading = false;
            _encoding = encoding;
            _stream = stream;

            // Set the encoding code
            _encodingCode = GetSupportedEncoding(encoding);

            if (_encodingCode != SupportedEncoding.UTF8)
            {
                EnsureBuffers();
                _dec = s_validatingUTF8.GetDecoder();
                _enc = _encoding.GetEncoder();

                // Emit BOM
                if (emitBOM)
                {
                    ReadOnlySpan<byte> bom = _encoding.Preamble;
                    if (bom.Length > 0)
                        _stream.Write(bom);
                }
            }
        }

        private SupportedEncoding ReadBOMEncoding(bool notOutOfBand)
        {
            int b1 = _stream.ReadByte();
            int b2 = _stream.ReadByte();
            int b3 = _stream.ReadByte();
            int b4 = _stream.ReadByte();

            // Premature end of stream
            if (b4 == -1)
                throw new XmlException(SR.UnexpectedEndOfFile);

            int preserve;
            SupportedEncoding e = ReadBOMEncoding((byte)b1, (byte)b2, (byte)b3, (byte)b4, notOutOfBand, out preserve);

            EnsureByteBuffer();
            switch (preserve)
            {
                case 1:
                    _bytes[0] = (byte)b4;
                    break;

                case 2:
                    _bytes[0] = (byte)b3;
                    _bytes[1] = (byte)b4;
                    break;

                case 4:
                    _bytes[0] = (byte)b1;
                    _bytes[1] = (byte)b2;
                    _bytes[2] = (byte)b3;
                    _bytes[3] = (byte)b4;
                    break;
            }
            _byteCount = preserve;

            return e;
        }

        private static SupportedEncoding ReadBOMEncoding(byte b1, byte b2, byte b3, byte b4, bool notOutOfBand, out int preserve)
        {
            SupportedEncoding e = SupportedEncoding.UTF8; // Default

            preserve = 0;
            if (b1 == '<' && b2 != 0x00) // UTF-8, no BOM
            {
                e = SupportedEncoding.UTF8;
                preserve = 4;
            }
            else if (b1 == 0xFF && b2 == 0xFE) // UTF-16 little endian
            {
                e = SupportedEncoding.UTF16LE;
                preserve = 2;
            }
            else if (b1 == 0xFE && b2 == 0xFF) // UTF-16 big endian
            {
                e = SupportedEncoding.UTF16BE;
                preserve = 2;
            }
            else if (b1 == 0x00 && b2 == '<') // UTF-16 big endian, no BOM
            {
                e = SupportedEncoding.UTF16BE;

                if (notOutOfBand && (b3 != 0x00 || b4 != '?'))
                    throw new XmlException(SR.XmlDeclMissing);
                preserve = 4;
            }
            else if (b1 == '<' && b2 == 0x00) // UTF-16 little endian, no BOM
            {
                e = SupportedEncoding.UTF16LE;

                if (notOutOfBand && (b3 != '?' || b4 != 0x00))
                    throw new XmlException(SR.XmlDeclMissing);
                preserve = 4;
            }
            else if (b1 == 0xEF && b2 == 0xBB) // UTF8 with BOM
            {
                // Encoding error
                if (notOutOfBand && b3 != 0xBF)
                    throw new XmlException(SR.XmlBadBOM);
                preserve = 1;
            }
            else  // Assume UTF8
            {
                preserve = 4;
            }

            return e;
        }

        private void FillBuffer(int count)
        {
            count -= _byteCount;
            while (count > 0)
            {
                int read = _stream.Read(_bytes, _byteOffset + _byteCount, count);
                if (read == 0)
                    break;

                _byteCount += read;
                count -= read;
            }
        }

        private void EnsureBuffers()
        {
            EnsureByteBuffer();
            if (_chars == null)
                _chars = new char[BufferLength];
        }

        private void EnsureByteBuffer()
        {
            if (_bytes != null)
                return;

            _bytes = new byte[BufferLength * 4];
            _byteOffset = 0;
            _byteCount = 0;
        }

        private static void CheckUTF8DeclarationEncoding(byte[] buffer, int offset, int count, SupportedEncoding e, SupportedEncoding expectedEnc)
        {
            byte quot = 0;
            int encEq = -1;
            int max = offset + Math.Min(count, BufferLength);

            // Encoding should be second "=", abort at first "?"
            int i = 0;
            int eq = 0;
            for (i = offset + 2; i < max; i++)  // Skip the "<?" so we don't get caught by the first "?"
            {
                if (quot != 0)
                {
                    if (buffer[i] == quot)
                    {
                        quot = 0;
                    }
                    continue;
                }

                if (buffer[i] == (byte)'\'' || buffer[i] == (byte)'"')
                {
                    quot = buffer[i];
                }
                else if (buffer[i] == (byte)'=')
                {
                    if (eq == 1)
                    {
                        encEq = i;
                        break;
                    }
                    eq++;
                }
                else if (buffer[i] == (byte)'?')  // Not legal character in a decl before second "="
                {
                    break;
                }
            }

            // No encoding found
            if (encEq == -1)
            {
                if (e != SupportedEncoding.UTF8 && expectedEnc == SupportedEncoding.None)
                    throw new XmlException(SR.XmlDeclarationRequired);
                return;
            }

            if (encEq < 28) // Earliest second "=" can appear
                throw new XmlException(SR.XmlMalformedDecl);

            // Back off whitespace
            for (i = encEq - 1; IsWhitespace(buffer[i]); i--) ;

            // Check for encoding attribute
            if (!Compare(s_encodingAttr, buffer, i - s_encodingAttr.Length + 1))
            {
                if (e != SupportedEncoding.UTF8 && expectedEnc == SupportedEncoding.None)
                    throw new XmlException(SR.XmlDeclarationRequired);
                return;
            }

            // Move ahead of whitespace
            for (i = encEq + 1; i < max && IsWhitespace(buffer[i]); i++) ;

            // Find the quotes
            if (buffer[i] != '\'' && buffer[i] != '"')
                throw new XmlException(SR.XmlMalformedDecl);
            quot = buffer[i];

            int q = i;
            for (i = q + 1; buffer[i] != quot && i < max; ++i) ;

            if (buffer[i] != quot)
                throw new XmlException(SR.XmlMalformedDecl);

            int encStart = q + 1;
            int encCount = i - encStart;

            // lookup the encoding
            SupportedEncoding declEnc = e;
            if (encCount == s_encodingUTF8.Length && CompareCaseInsensitive(s_encodingUTF8, buffer, encStart))
            {
                declEnc = SupportedEncoding.UTF8;
            }
            else if (encCount == s_encodingUnicodeLE.Length && CompareCaseInsensitive(s_encodingUnicodeLE, buffer, encStart))
            {
                declEnc = SupportedEncoding.UTF16LE;
            }
            else if (encCount == s_encodingUnicodeBE.Length && CompareCaseInsensitive(s_encodingUnicodeBE, buffer, encStart))
            {
                declEnc = SupportedEncoding.UTF16BE;
            }
            else if (encCount == s_encodingUnicode.Length && CompareCaseInsensitive(s_encodingUnicode, buffer, encStart))
            {
                if (e == SupportedEncoding.UTF8)
                    ThrowEncodingMismatch(s_safeUTF8.GetString(buffer, encStart, encCount), s_safeUTF8.GetString(s_encodingUTF8, 0, s_encodingUTF8.Length));
            }
            else
            {
                ThrowEncodingMismatch(s_safeUTF8.GetString(buffer, encStart, encCount), e);
            }

            if (e != declEnc)
                ThrowEncodingMismatch(s_safeUTF8.GetString(buffer, encStart, encCount), e);
        }

        private static bool CompareCaseInsensitive(byte[] key, byte[] buffer, int offset)
        {
            for (int i = 0; i < key.Length; i++)
            {
                if (key[i] == buffer[offset + i])
                    continue;

                if (key[i] != char.ToLowerInvariant((char)buffer[offset + i]))
                    return false;
            }
            return true;
        }

        private static bool Compare(byte[] key, byte[] buffer, int offset)
        {
            for (int i = 0; i < key.Length; i++)
            {
                if (key[i] != buffer[offset + i])
                    return false;
            }
            return true;
        }

        private static bool IsWhitespace(byte ch)
        {
            return ch == (byte)' ' || ch == (byte)'\n' || ch == (byte)'\t' || ch == (byte)'\r';
        }

        internal static ArraySegment<byte> ProcessBuffer(byte[] buffer, int offset, int count, Encoding encoding)
        {
            if (count < 4)
                throw new XmlException(SR.UnexpectedEndOfFile);

            try
            {
                int preserve;
                ArraySegment<byte> seg;

                SupportedEncoding expectedEnc = GetSupportedEncoding(encoding);
                SupportedEncoding declEnc = ReadBOMEncoding(buffer[offset], buffer[offset + 1], buffer[offset + 2], buffer[offset + 3], encoding == null, out preserve);
                if (expectedEnc != SupportedEncoding.None && expectedEnc != declEnc)
                    ThrowExpectedEncodingMismatch(expectedEnc, declEnc);

                offset += 4 - preserve;
                count -= 4 - preserve;

                // Fastpath: UTF-8
                char[] chars;
                byte[] bytes;
                Encoding localEnc;
                if (declEnc == SupportedEncoding.UTF8)
                {
                    // Fastpath: No declaration
                    if (buffer[offset + 1] != '?' || buffer[offset] != '<')
                    {
                        seg = new ArraySegment<byte>(buffer, offset, count);
                        return seg;
                    }

                    CheckUTF8DeclarationEncoding(buffer, offset, count, declEnc, expectedEnc);
                    seg = new ArraySegment<byte>(buffer, offset, count);
                    return seg;
                }

                // Convert to UTF-8
                localEnc = GetSafeEncoding(declEnc);
                int inputCount = Math.Min(count, BufferLength * 2);
                chars = new char[localEnc.GetMaxCharCount(inputCount)];
                int ccount = localEnc.GetChars(buffer, offset, inputCount, chars, 0);
                bytes = new byte[s_validatingUTF8.GetMaxByteCount(ccount)];
                int bcount = s_validatingUTF8.GetBytes(chars, 0, ccount, bytes, 0);

                // Check for declaration
                if (bytes[1] == '?' && bytes[0] == '<')
                {
                    CheckUTF8DeclarationEncoding(bytes, 0, bcount, declEnc, expectedEnc);
                }
                else
                {
                    // Declaration required if no out-of-band encoding
                    if (expectedEnc == SupportedEncoding.None)
                        throw new XmlException(SR.XmlDeclarationRequired);
                }

                seg = new ArraySegment<byte>(s_validatingUTF8.GetBytes(GetEncoding(declEnc).GetChars(buffer, offset, count)));
                return seg;
            }
            catch (DecoderFallbackException e)
            {
                throw new XmlException(SR.XmlInvalidBytes, e);
            }
        }

        private static void ThrowExpectedEncodingMismatch(SupportedEncoding expEnc, SupportedEncoding actualEnc)
        {
            throw new XmlException(SR.Format(SR.XmlExpectedEncoding, GetEncodingName(expEnc), GetEncodingName(actualEnc)));
        }

        private static void ThrowEncodingMismatch(string declEnc, SupportedEncoding enc)
        {
            ThrowEncodingMismatch(declEnc, GetEncodingName(enc));
        }

        private static void ThrowEncodingMismatch(string declEnc, string docEnc)
        {
            throw new XmlException(SR.Format(SR.XmlEncodingMismatch, declEnc, docEnc));
        }

        // This stream wrapper does not support duplex
        public override bool CanRead
        {
            get
            {
                if (!_isReading)
                    return false;

                return _stream.CanRead;
            }
        }

        // The encoding conversion and buffering breaks seeking.
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        // This stream wrapper does not support duplex
        public override bool CanWrite
        {
            get
            {
                if (_isReading)
                    return false;

                return _stream.CanWrite;
            }
        }


        // The encoding conversion and buffering breaks seeking.
        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_stream.CanWrite)
            {
                Flush();
            }

            _stream.Dispose();
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override int ReadByte()
        {
            if (_byteCount == 0 && _encodingCode == SupportedEncoding.UTF8)
                return _stream.ReadByte();
            if (Read(_byteBuffer, 0, 1) == 0)
                return -1;
            return _byteBuffer[0];
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                if (_byteCount == 0)
                {
                    if (_encodingCode == SupportedEncoding.UTF8)
                        return _stream.Read(buffer, offset, count);

                    // No more bytes than can be turned into characters
                    _byteOffset = 0;
                    _byteCount = _stream.Read(_bytes, _byteCount, (_chars.Length - 1) * 2);

                    // Check for end of stream
                    if (_byteCount == 0)
                        return 0;

                    // Fix up incomplete chars
                    CleanupCharBreak();

                    // Change encoding
                    int charCount = _encoding.GetChars(_bytes, 0, _byteCount, _chars, 0);
                    _byteCount = Encoding.UTF8.GetBytes(_chars, 0, charCount, _bytes, 0);
                }

                // Give them bytes
                if (_byteCount < count)
                    count = _byteCount;
                Buffer.BlockCopy(_bytes, _byteOffset, buffer, offset, count);
                _byteOffset += count;
                _byteCount -= count;
                return count;
            }
            catch (DecoderFallbackException ex)
            {
                throw new XmlException(SR.XmlInvalidBytes, ex);
            }
        }

        private void CleanupCharBreak()
        {
            int max = _byteOffset + _byteCount;

            // Read on 2 byte boundaries
            if ((_byteCount % 2) != 0)
            {
                int b = _stream.ReadByte();
                if (b < 0)
                    throw new XmlException(SR.UnexpectedEndOfFile);

                _bytes[max++] = (byte)b;
                _byteCount++;
            }

            // Don't cut off a surrogate character
            int w;
            if (_encodingCode == SupportedEncoding.UTF16LE)
            {
                w = _bytes[max - 2] + (_bytes[max - 1] << 8);
            }
            else
            {
                w = _bytes[max - 1] + (_bytes[max - 2] << 8);
            }
            if ((w & 0xDC00) != 0xDC00 && w >= 0xD800 && w <= 0xDBFF)  // First 16-bit number of surrogate pair
            {
                int b1 = _stream.ReadByte();
                int b2 = _stream.ReadByte();
                if (b2 < 0)
                    throw new XmlException(SR.UnexpectedEndOfFile);
                _bytes[max++] = (byte)b1;
                _bytes[max++] = (byte)b2;
                _byteCount += 2;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void WriteByte(byte b)
        {
            if (_encodingCode == SupportedEncoding.UTF8)
            {
                _stream.WriteByte(b);
                return;
            }
            _byteBuffer[0] = b;
            Write(_byteBuffer, 0, 1);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // Optimize UTF-8 case
            if (_encodingCode == SupportedEncoding.UTF8)
            {
                _stream.Write(buffer, offset, count);
                return;
            }

            while (count > 0)
            {
                int size = _chars.Length < count ? _chars.Length : count;
                int charCount = _dec.GetChars(buffer, offset, size, _chars, 0, false);
                _byteCount = _enc.GetBytes(_chars, 0, charCount, _bytes, 0, false);
                _stream.Write(_bytes, 0, _byteCount);
                offset += size;
                count -= size;
            }
        }

        // Delegate properties
        public override bool CanTimeout { get { return _stream.CanTimeout; } }
        public override long Length { get { return _stream.Length; } }
        public override int ReadTimeout
        {
            get { return _stream.ReadTimeout; }
            set { _stream.ReadTimeout = value; }
        }
        public override int WriteTimeout
        {
            get { return _stream.WriteTimeout; }
            set { _stream.WriteTimeout = value; }
        }

        // Delegate methods
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }

    // Add format exceptions
    // Do we need to modify the stream position/Seek to account for the buffer?
    // ASSUMPTION (Microsoft): This class will only be used for EITHER reading OR writing.
}
