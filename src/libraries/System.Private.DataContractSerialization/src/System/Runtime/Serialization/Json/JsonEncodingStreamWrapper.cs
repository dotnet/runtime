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
        private static readonly UnicodeEncoding s_validatingBEUTF16 = new UnicodeEncoding(true, false, true);

        private static readonly UnicodeEncoding s_validatingUTF16 = new UnicodeEncoding(false, false, true);

        private static readonly UTF8Encoding s_validatingUTF8 = new UTF8Encoding(false, true);
        private const int BufferLength = 128;

        private readonly byte[] _byteBuffer = new byte[1];
        private int _byteCount;
        private int _byteOffset;
        private byte[]? _bytes;
        private char[]? _chars;
        private Decoder? _dec;
        private Encoder? _enc;
        private Encoding? _encoding;

        private SupportedEncoding _encodingCode;
        private readonly bool _isReading;

        private Stream _stream = null!; // initialized in InitForXXX

        public JsonEncodingStreamWrapper(Stream stream, Encoding? encoding, bool isReader)
        {
            _isReading = isReader;
            if (isReader)
            {
                InitForReading(stream, encoding);
            }
            else
            {
                if (encoding == null)
                {
                    throw new ArgumentNullException(nameof(encoding));
                }

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
            get
            {
#pragma warning suppress 56503 // The contract for non seekable stream is to throw exception
                throw new NotSupportedException();
            }
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
                SupportedEncoding dataEnc;
                if (count < 2)
                {
                    dataEnc = SupportedEncoding.UTF8;
                }
                else
                {
                    dataEnc = ReadEncoding(buffer[offset], buffer[offset + 1]);
                }
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
                    new ArraySegment<byte>(s_validatingUTF8.GetBytes(GetEncoding(dataEnc).GetChars(buffer, offset, count)));
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

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                if (_byteCount == 0)
                {
                    if (_encodingCode == SupportedEncoding.UTF8)
                    {
                        return _stream.Read(buffer, offset, count);
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
                if (_byteCount < count)
                {
                    count = _byteCount;
                }
                Buffer.BlockCopy(_bytes!, _byteOffset, buffer, offset, count);
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
            if (Read(_byteBuffer, 0, 1) == 0)
            {
                return -1;
            }
            return _byteBuffer[0];
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

        public override void Write(byte[] buffer, int offset, int count)
        {
            // Optimize UTF-8 case
            if (_encodingCode == SupportedEncoding.UTF8)
            {
                _stream.Write(buffer, offset, count);
                return;
            }

            Debug.Assert(_bytes != null);
            Debug.Assert(_chars != null);
            while (count > 0)
            {
                int size = _chars.Length < count ? _chars.Length : count;
                int charCount = _dec!.GetChars(buffer, offset, size, _chars, 0, false);
                _byteCount = _enc!.GetBytes(_chars, 0, charCount, _bytes, 0, false);
                _stream.Write(_bytes, 0, _byteCount);
                offset += size;
                count -= size;
            }
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

        private static Encoding GetEncoding(SupportedEncoding e) =>
            e switch
            {
                SupportedEncoding.UTF8 => s_validatingUTF8,
                SupportedEncoding.UTF16LE => s_validatingUTF16,
                SupportedEncoding.UTF16BE => s_validatingBEUTF16,
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
            if (encoding.WebName == s_validatingUTF8.WebName)
            {
                return SupportedEncoding.UTF8;
            }
            else if (encoding.WebName == s_validatingUTF16.WebName)
            {
                return SupportedEncoding.UTF16LE;
            }
            else if (encoding.WebName == s_validatingBEUTF16.WebName)
            {
                return SupportedEncoding.UTF16BE;
            }
            else
            {
                throw new XmlException(SR.JsonEncodingNotSupported);
            }
        }

        private static SupportedEncoding ReadEncoding(byte b1, byte b2)
        {
            if (b1 == 0x00 && b2 != 0x00)
            {
                return SupportedEncoding.UTF16BE;
            }
            else if (b1 != 0x00 && b2 == 0x00)
            {
                // 857 It's possible to misdetect UTF-32LE as UTF-16LE, but that's OK.
                return SupportedEncoding.UTF16LE;
            }
            else if (b1 == 0x00 && b2 == 0x00)
            {
                // UTF-32BE not supported
                throw new XmlException(SR.JsonInvalidBytes);
            }
            else
            {
                return SupportedEncoding.UTF8;
            }
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
            if (_chars == null)
            {
                _chars = new char[BufferLength];
            }
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
            while (count > 0)
            {
                int read = _stream.Read(_bytes, _byteOffset + _byteCount, count);
                if (read == 0)
                {
                    break;
                }

                _byteCount += read;
                count -= read;
            }
        }

        private void InitForReading(Stream inputStream, Encoding? expectedEncoding)
        {
            try
            {
                //this.stream = new BufferedStream(inputStream);
                _stream = inputStream;

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
                    _byteCount = s_validatingUTF8.GetBytes(_chars, 0, count, _bytes, 0);
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
            //this.stream = new BufferedStream(outputStream);
            _stream = outputStream;

            // Set the encoding code
            _encodingCode = GetSupportedEncoding(writeEncoding);

            if (_encodingCode != SupportedEncoding.UTF8)
            {
                EnsureBuffers();
                _dec = s_validatingUTF8.GetDecoder();
                _enc = _encoding.GetEncoder();
            }
        }

        private SupportedEncoding ReadEncoding()
        {
            int b1 = _stream.ReadByte();
            int b2 = _stream.ReadByte();

            EnsureByteBuffer();

            SupportedEncoding e;

            if (b1 == -1)
            {
                e = SupportedEncoding.UTF8;
                _byteCount = 0;
            }
            else if (b2 == -1)
            {
                e = SupportedEncoding.UTF8;
                _bytes[0] = (byte)b1;
                _byteCount = 1;
            }
            else
            {
                e = ReadEncoding((byte)b1, (byte)b2);
                _bytes[0] = (byte)b1;
                _bytes[1] = (byte)b2;
                _byteCount = 2;
            }

            return e;
        }
    }
}
