// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    // This wrapper does not support seek.
    // Supports: UTF-8, Unicode, BigEndianUnicode
    // ASSUMPTION (Microsoft): This class will only be used for EITHER reading OR writing.  It can be done, it would just mean more buffers.
    internal sealed class JsonEncodingStreamWrapper : Stream
    {
        private const int BufferLength = 128;

        private int _byteCount;
        private int _byteOffset;
        private byte[]? _bytes;
        private char[]? _chars;
        private Decoder? _dec;
        private Encoder? _enc;
        private Encoding? _encoding;

        private SupportedEncoding _encodingCode;
        private readonly bool _isReading;

        private BufferedStream _stream = null!; // initialized in InitForXXX

        public JsonEncodingStreamWrapper(Stream stream, Encoding? encoding, bool isReader)
        {
            _isReading = isReader;
            if (isReader)
            {
                InitForReading(stream, encoding);
            }
            else
            {
                ArgumentNullException.ThrowIfNull(encoding);

                InitForWriting(stream, encoding);
            }
        }

        private enum SupportedEncoding
        {
            UTF8,
            UTF16LE,
            UTF16BE,
            None
        }

        // This stream wrapper does not support duplex
        public override bool CanRead
        {
            get
            {
                if (!_isReading)
                {
                    return false;
                }

                return _stream.CanRead;
            }
        }

        // The encoding conversion and buffering breaks seeking.
        public override bool CanSeek
        {
            get { return false; }
        }

        // Delegate properties
        public override bool CanTimeout
        {
            get { return _stream.CanTimeout; }
        }

        // This stream wrapper does not support duplex
        public override bool CanWrite
        {
            get
            {
                if (_isReading)
                {
                    return false;
                }

                return _stream.CanWrite;
            }
        }

        public override long Length
        {
            get { return _stream.Length; }
        }


        // The encoding conversion and buffering breaks seeking.
        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

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

        public static ArraySegment<byte> ProcessBuffer(byte[] buffer, int offset, int count, Encoding? encoding)
        {
            try
            {
                SupportedEncoding expectedEnc = GetSupportedEncoding(encoding);
                SupportedEncoding dataEnc = DetectEncoding(buffer.AsSpan(offset, count), out int bomLength);

                // Skip past any byte order mark; it is not part of the document.
                offset += bomLength;
                count -= bomLength;

                if ((expectedEnc != SupportedEncoding.None) && (expectedEnc != dataEnc))
                {
                    ThrowExpectedEncodingMismatch(expectedEnc, dataEnc);
                }

                // Fastpath: UTF-8
                if (dataEnc == SupportedEncoding.UTF8)
                {
                    return new ArraySegment<byte>(buffer, offset, count);
                }

                // Convert to UTF-8
                return
                    new ArraySegment<byte>(DataContractSerializer.ValidatingUTF8.GetBytes(GetEncoding(dataEnc).GetChars(buffer, offset, count)));
            }
            catch (DecoderFallbackException e)
            {
                throw new XmlException(SR.JsonInvalidBytes, e);
            }
        }

        protected override void Dispose(bool disposing)
        {
            Flush();
            _stream.Dispose();
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            Read(new Span<byte>(buffer, offset, count));

        public override int Read(Span<byte> buffer)
        {
            try
            {
                if (_byteCount == 0)
                {
                    if (_encodingCode == SupportedEncoding.UTF8)
                    {
                        return _stream.Read(buffer);
                    }

                    Debug.Assert(_bytes != null);
                    Debug.Assert(_chars != null);
                    // No more bytes than can be turned into characters
                    _byteOffset = 0;
                    _byteCount = _stream.Read(_bytes, _byteCount, (_chars.Length - 1) * 2);

                    // Check for end of stream
                    if (_byteCount == 0)
                    {
                        return 0;
                    }

                    // Fix up incomplete chars
                    CleanupCharBreak();

                    // Change encoding
                    int charCount = _encoding!.GetChars(_bytes, 0, _byteCount, _chars, 0);
                    _byteCount = Encoding.UTF8.GetBytes(_chars, 0, charCount, _bytes, 0);
                }

                // Give them bytes
                int count = buffer.Length;
                if (_byteCount < count)
                {
                    count = _byteCount;
                }

                _bytes.AsSpan(_byteOffset, count).CopyTo(buffer);
                _byteOffset += count;
                _byteCount -= count;
                return count;
            }
            catch (DecoderFallbackException ex)
            {
                throw new XmlException(SR.JsonInvalidBytes, ex);
            }
        }

        public override int ReadByte()
        {
            if (_byteCount == 0 && _encodingCode == SupportedEncoding.UTF8)
            {
                return _stream.ReadByte();
            }

            byte b = 0;
            if (Read(new Span<byte>(ref b)) == 0)
            {
                return -1;
            }
            return b;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        // Delegate methods
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count) =>
            Write(new ReadOnlySpan<byte>(buffer, offset, count));

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            // Optimize UTF-8 case
            if (_encodingCode == SupportedEncoding.UTF8)
            {
                _stream.Write(buffer);
                return;
            }

            Debug.Assert(_bytes != null);
            Debug.Assert(_chars != null);

            while (buffer.Length > 0)
            {
                int size = Math.Min(_chars.Length, buffer.Length);
                int charCount = _dec!.GetChars(buffer.Slice(0, size), _chars, false);
                _byteCount = _enc!.GetBytes(_chars, 0, charCount, _bytes, 0, false);
                _stream.Write(_bytes, 0, _byteCount);
                buffer = buffer.Slice(size);
            }
        }

        public override void WriteByte(byte b)
        {
            if (_encodingCode == SupportedEncoding.UTF8)
            {
                _stream.WriteByte(b);
                return;
            }

            Write(new ReadOnlySpan<byte>(in b));
        }

        private static Encoding GetEncoding(SupportedEncoding e) =>
            e switch
            {
                SupportedEncoding.UTF8 => DataContractSerializer.ValidatingUTF8,
                SupportedEncoding.UTF16LE => DataContractSerializer.ValidatingUTF16,
                SupportedEncoding.UTF16BE => DataContractSerializer.ValidatingBEUTF16,
                _ => throw new XmlException(SR.JsonEncodingNotSupported),
            };

        private static string GetEncodingName(SupportedEncoding enc) =>
            enc switch
            {
                SupportedEncoding.UTF8 => "utf-8",
                SupportedEncoding.UTF16LE => "utf-16LE",
                SupportedEncoding.UTF16BE => "utf-16BE",
                _ => throw new XmlException(SR.JsonEncodingNotSupported),
            };

        private static SupportedEncoding GetSupportedEncoding(Encoding? encoding)
        {
            if (encoding == null)
            {
                return SupportedEncoding.None;
            }
            if (encoding.WebName == DataContractSerializer.ValidatingUTF8.WebName)
            {
                return SupportedEncoding.UTF8;
            }
            else if (encoding.WebName == DataContractSerializer.ValidatingUTF16.WebName)
            {
                return SupportedEncoding.UTF16LE;
            }
            else if (encoding.WebName == DataContractSerializer.ValidatingBEUTF16.WebName)
            {
                return SupportedEncoding.UTF16BE;
            }
            else
            {
                throw new XmlException(SR.JsonEncodingNotSupported);
            }
        }

        // Determines the encoding of a JSON document from its leading bytes. A leading byte order
        // mark, when present, authoritatively selects the encoding and its length is reported via
        // bomLength so callers can skip past it. When no BOM is present, the encoding is inferred
        // from the position of the zero byte in the leading (always ASCII) JSON character. Both the
        // stream and the buffer code paths funnel through this single method so the detection logic
        // lives in one place.
        private static SupportedEncoding DetectEncoding(ReadOnlySpan<byte> data, out int bomLength)
        {
            bomLength = 0;

            // Not enough characters for a BOM
            if (data.Length < 2)
            {
                // A single-byte (or empty) JSON document is necessarily UTF-8.
                return SupportedEncoding.UTF8;
            }

            switch ((data[0], data[1]))
            {
                // Detect known BOMs
                case (0xFF, 0xFE):
                    bomLength = 2;
                    return SupportedEncoding.UTF16LE;
                case (0xFE, 0xFF):
                    bomLength = 2;
                    return SupportedEncoding.UTF16BE;
                case (0xEF, 0xBB):
                    if (data.Length >= 3 && data[2] == 0xBF)
                    {
                        bomLength = 3;
                        return SupportedEncoding.UTF8;
                    }
                    break;

                // No byte order mark or inference from the leading ASCII character.
                case (0x00, not 0x00):
                    return SupportedEncoding.UTF16BE;
                case (not 0x00, 0x00):
                    return SupportedEncoding.UTF16LE;

                // UTF-32BE not supported
                case (0x00, 0x00):
                    throw new XmlException(SR.JsonInvalidBytes);
            }

            // No BOM detected or inferred. Assume UTF8
            return SupportedEncoding.UTF8;
        }

        private static void ThrowExpectedEncodingMismatch(SupportedEncoding expEnc, SupportedEncoding actualEnc)
        {
            throw new XmlException(SR.Format(SR.JsonExpectedEncoding, GetEncodingName(expEnc), GetEncodingName(actualEnc)));
        }

        private void CleanupCharBreak()
        {
            Debug.Assert(_bytes != null);

            int max = _byteOffset + _byteCount;

            // Read on 2 byte boundaries
            if ((_byteCount % 2) != 0)
            {
                int b = _stream.ReadByte();
                if (b < 0)
                {
                    throw new XmlException(SR.JsonUnexpectedEndOfFile);
                }

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
            if ((w & 0xDC00) != 0xDC00 && w >= 0xD800 && w <= 0xDBFF) // First 16-bit number of surrogate pair
            {
                int b1 = _stream.ReadByte();
                int b2 = _stream.ReadByte();
                if (b2 < 0)
                {
                    throw new XmlException(SR.JsonUnexpectedEndOfFile);
                }
                _bytes[max++] = (byte)b1;
                _bytes[max++] = (byte)b2;
                _byteCount += 2;
            }
        }

        [MemberNotNull(nameof(_chars))]
        [MemberNotNull(nameof(_bytes))]
        private void EnsureBuffers()
        {
            EnsureByteBuffer();
            _chars ??= new char[BufferLength];
        }

        [MemberNotNull(nameof(_bytes))]
        private void EnsureByteBuffer()
        {
            if (_bytes != null)
            {
                return;
            }

            _bytes = new byte[BufferLength * 4];
            _byteOffset = 0;
            _byteCount = 0;
        }

        private void FillBuffer(int count)
        {
            Debug.Assert(_bytes != null);

            count -= _byteCount;
            if (count > 0)
            {
                _byteCount += _stream.ReadAtLeast(_bytes.AsSpan(_byteOffset + _byteCount, count), count, throwOnEndOfStream: false);
            }
        }

        private void InitForReading(Stream inputStream, Encoding? expectedEncoding)
        {
            try
            {
                _stream = new BufferedStream(inputStream);

                SupportedEncoding expectedEnc = GetSupportedEncoding(expectedEncoding);
                SupportedEncoding dataEnc = ReadEncoding();
                if ((expectedEnc != SupportedEncoding.None) && (expectedEnc != dataEnc))
                {
                    ThrowExpectedEncodingMismatch(expectedEnc, dataEnc);
                }

                // Fastpath: UTF-8 (do nothing)
                if (dataEnc != SupportedEncoding.UTF8)
                {
                    // Convert to UTF-8
                    EnsureBuffers();
                    FillBuffer((BufferLength - 1) * 2);
                    _encodingCode = dataEnc;
                    _encoding = GetEncoding(dataEnc);
                    CleanupCharBreak();
                    int count = _encoding.GetChars(_bytes, _byteOffset, _byteCount, _chars, 0);
                    _byteOffset = 0;
                    _byteCount = DataContractSerializer.ValidatingUTF8.GetBytes(_chars, 0, count, _bytes, 0);
                }
            }
            catch (DecoderFallbackException ex)
            {
                throw new XmlException(SR.JsonInvalidBytes, ex);
            }
        }

        private void InitForWriting(Stream outputStream, Encoding writeEncoding)
        {
            _encoding = writeEncoding;
            _stream = new BufferedStream(outputStream);

            // Set the encoding code
            _encodingCode = GetSupportedEncoding(writeEncoding);

            if (_encodingCode != SupportedEncoding.UTF8)
            {
                EnsureBuffers();
                _dec = DataContractSerializer.ValidatingUTF8.GetDecoder();
                _enc = _encoding.GetEncoder();
            }
        }

        private SupportedEncoding ReadEncoding()
        {
            EnsureByteBuffer();

            // Read whatever bytes are immediately available, up to the three occupied by the longest
            // byte order mark. A single Read is used deliberately instead of ReadAtLeast: Read performs
            // one underlying read and returns however many bytes were available. If it's enough for
            // BOM detection, we will try to determine encoding. If not, we continue BOM-less.
            // `_stream` here is buffered, so `Read()` should be able to return a full BOM if it's there.
            // We need 3 bytes for full ASCII/UTF-8/16 detection.
            Span<byte> leading = stackalloc byte[3];
            int read = _stream.Read(leading);

            SupportedEncoding e = DetectEncoding(leading.Slice(0, read), out int bomLength);

            // Preserve any bytes that follow the byte order mark; they belong to the document.
            int preserve = read - bomLength;
            leading.Slice(bomLength, preserve).CopyTo(_bytes);
            _byteCount = preserve;

            return e;
        }
    }
}
