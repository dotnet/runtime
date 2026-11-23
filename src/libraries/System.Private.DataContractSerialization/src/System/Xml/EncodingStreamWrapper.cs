// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization; // For SR
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace System.Xml
{
    // This wrapper does not support seek.
    // Constructors consume/emit byte order mark.
    // Supports: UTF-8, Unicode, BigEndianUnicode
    // ASSUMPTION (Microsoft): This class will only be used for EITHER reading OR writing.  It can be done, it would just mean more buffers.
    // ASSUMPTION (Microsoft): The byte buffer is large enough to hold the declaration
    // ASSUMPTION (Microsoft): The buffer manipulation methods (FillBuffer/Compare/etc.) will only be used to parse the declaration
    //                      during construction.
    internal sealed partial class EncodingStreamWrapper : Stream, IXmlAsyncStreamWriter
    {
        private enum SupportedEncoding
        {
            UTF8,
            UTF16LE,
            UTF16BE,
            None
        }
        private const int BufferLength = 128;

        // UTF-8 is fastpath, so that's how these are stored
        // Compare methods adapt to Unicode.
        private static readonly byte[] s_encodingUTF8 = "utf-8"u8.ToArray();
        private static readonly byte[] s_encodingUnicode = "utf-16"u8.ToArray();
        private static readonly byte[] s_encodingUnicodeLE = "utf-16le"u8.ToArray();
        private static readonly byte[] s_encodingUnicodeBE = "utf-16be"u8.ToArray();

        private SupportedEncoding _encodingCode;
        private Encoding? _encoding;
        private readonly Encoder? _enc;
        private readonly Decoder? _dec;
        private readonly bool _isReading;

        private readonly Stream _stream;
        private char[]? _chars;
        private byte[]? _bytes;
        private int _byteOffset;
        private int _byteCount;

        private readonly byte[] _byteBuffer = new byte[1];
        private CancellationToken _writeCancellationToken;

        CancellationToken IXmlAsyncStreamWriter.WriteCancellationToken
        {
            get => _writeCancellationToken;
            set => _writeCancellationToken = value;
        }

        // Reading constructor
        public EncodingStreamWrapper(Stream stream, Encoding? encoding)
            : this(stream)
        {
            InitializeReaderSync(encoding);
        }

        private EncodingStreamWrapper(Stream stream)
        {
            _isReading = true;
            _stream = stream;
        }

        internal static async ValueTask<Stream> CreateReaderAsync(Stream stream, Encoding? encoding, CancellationToken cancellationToken)
        {
            EncodingStreamWrapper wrapper = new EncodingStreamWrapper(stream);
            await wrapper.InitializeReader<AsyncXmlStreamIO>(encoding, cancellationToken).ConfigureAwait(false);
            return wrapper;
        }

        private async ValueTask InitializeReader<TMode>(Encoding? encoding, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            try
            {
                // Decode the expected encoding
                SupportedEncoding expectedEnc = GetSupportedEncoding(encoding);

                // Get the byte order mark so we can determine the encoding
                // May want to try to delay allocating everything until we know the BOM
                SupportedEncoding declEnc = await ReadBOMEncoding<TMode>(encoding is null, cancellationToken).ConfigureAwait(false);
                EnsureByteBuffer();

                // Check that the expected encoding matches the decl encoding.
                if (expectedEnc != SupportedEncoding.None && expectedEnc != declEnc)
                    ThrowExpectedEncodingMismatch(expectedEnc, declEnc);

                // Fastpath: UTF-8 BOM
                if (declEnc == SupportedEncoding.UTF8)
                {
                    // Fastpath: UTF-8 BOM, No declaration
                    await FillBuffer<TMode>(2, cancellationToken).ConfigureAwait(false);
                    if (_bytes[_byteOffset + 1] != '?' || _bytes[_byteOffset] != '<')
                    {
                        return;
                    }

                    await FillBuffer<TMode>(BufferLength, cancellationToken).ConfigureAwait(false);
                    CheckUTF8DeclarationEncoding(_bytes, _byteOffset, _byteCount, declEnc, expectedEnc);
                }
                else
                {
                    // Convert to UTF-8
                    EnsureBuffers();
                    await FillBuffer<TMode>((BufferLength - 1) * 2, cancellationToken).ConfigureAwait(false);
                    SetReadDocumentEncoding(declEnc);
                    await CleanupCharBreak<TMode>(cancellationToken).ConfigureAwait(false);
                    int count = _encoding.GetChars(_bytes, _byteOffset, _byteCount, _chars, 0);
                    _byteOffset = 0;
                    _byteCount = DataContractSerializer.ValidatingUTF8.GetBytes(_chars, 0, count, _bytes, 0);

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

        [MemberNotNull(nameof(_encoding))]
        private void SetReadDocumentEncoding(SupportedEncoding e)
        {
            EnsureBuffers();
            _encodingCode = e;
            _encoding = GetEncoding(e);
        }

        private static Encoding GetEncoding(SupportedEncoding e) =>
            e switch
            {
                SupportedEncoding.UTF8 => DataContractSerializer.ValidatingUTF8,
                SupportedEncoding.UTF16LE => DataContractSerializer.ValidatingUTF16,
                SupportedEncoding.UTF16BE => DataContractSerializer.ValidatingBEUTF16,
                _ => throw new XmlException(SR.XmlEncodingNotSupported),
            };

        private static Encoding GetSafeEncoding(SupportedEncoding e) =>
            e switch
            {
                SupportedEncoding.UTF8 => DataContractSerializer.UTF8NoBom,
                SupportedEncoding.UTF16LE => DataContractSerializer.UTF16NoBom,
                SupportedEncoding.UTF16BE => DataContractSerializer.BEUTF16NoBom,
                _ => throw new XmlException(SR.XmlEncodingNotSupported),
            };

        private static string GetEncodingName(SupportedEncoding enc) =>
            enc switch
            {
                SupportedEncoding.UTF8 => "utf-8",
                SupportedEncoding.UTF16LE => "utf-16LE",
                SupportedEncoding.UTF16BE => "utf-16BE",
                _ => throw new XmlException(SR.XmlEncodingNotSupported),
            };

        private static SupportedEncoding GetSupportedEncoding(Encoding? encoding)
        {
            if (encoding == null)
                return SupportedEncoding.None;
            else if (encoding.WebName == DataContractSerializer.ValidatingUTF8.WebName)
                return SupportedEncoding.UTF8;
            else if (encoding.WebName == DataContractSerializer.ValidatingUTF16.WebName)
                return SupportedEncoding.UTF16LE;
            else if (encoding.WebName == DataContractSerializer.ValidatingBEUTF16.WebName)
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
                _dec = DataContractSerializer.ValidatingUTF8.GetDecoder();
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

        internal static async ValueTask<Stream> CreateWriterAsync(Stream stream, Encoding encoding, bool emitBOM, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EncodingStreamWrapper wrapper = new EncodingStreamWrapper(stream, encoding, emitBOM: false);
            if (emitBOM && wrapper._encodingCode != SupportedEncoding.UTF8)
            {
                byte[] preamble = encoding.GetPreamble();
                if (preamble.Length != 0)
                {
                    await stream.WriteAsync(preamble, 0, preamble.Length, cancellationToken).ConfigureAwait(false);
                }
            }
            return wrapper;
        }

        private async ValueTask<SupportedEncoding> ReadBOMEncoding<TMode>(bool notOutOfBand, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            EnsureByteBuffer();
            int count = await TMode.ReadAtLeast(_stream, _bytes.AsMemory(0, 4), cancellationToken).ConfigureAwait(false);

            // Premature end of stream
            if (count < 4)
                throw new XmlException(SR.UnexpectedEndOfFile);

            int preserve;
            SupportedEncoding e = ReadBOMEncoding(_bytes[0], _bytes[1], _bytes[2], _bytes[3], notOutOfBand, out preserve);
            _bytes.AsSpan(4 - preserve, preserve).CopyTo(_bytes);
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

        private async ValueTask FillBuffer<TMode>(int count, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            count -= _byteCount;
            if (count > 0)
            {
                _byteCount += await TMode.ReadAtLeast(_stream, _bytes.AsMemory(_byteOffset + _byteCount, count), cancellationToken).ConfigureAwait(false);
            }
        }

        [MemberNotNull(nameof(_bytes))]
        [MemberNotNull(nameof(_chars))]
        private void EnsureBuffers()
        {
            EnsureByteBuffer();
            _chars ??= new char[BufferLength];
        }

        [MemberNotNull(nameof(_bytes))]
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
            int i;
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
            if (!buffer.AsSpan(0, i + 1).EndsWith("encoding"u8))
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
                    ThrowEncodingMismatch(DataContractSerializer.UTF8NoBom.GetString(buffer, encStart, encCount), DataContractSerializer.UTF8NoBom.GetString(s_encodingUTF8, 0, s_encodingUTF8.Length));
            }
            else
            {
                ThrowEncodingMismatch(DataContractSerializer.UTF8NoBom.GetString(buffer, encStart, encCount), e);
            }

            if (e != declEnc)
                ThrowEncodingMismatch(DataContractSerializer.UTF8NoBom.GetString(buffer, encStart, encCount), e);
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

        private static bool IsWhitespace(byte ch)
        {
            return ch == (byte)' ' || ch == (byte)'\n' || ch == (byte)'\t' || ch == (byte)'\r';
        }

        internal static ArraySegment<byte> ProcessBuffer(byte[] buffer, int offset, int count, Encoding? encoding)
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
                bytes = new byte[DataContractSerializer.ValidatingUTF8.GetMaxByteCount(ccount)];
                int bcount = DataContractSerializer.ValidatingUTF8.GetBytes(chars, 0, ccount, bytes, 0);

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

                seg = new ArraySegment<byte>(DataContractSerializer.ValidatingUTF8.GetBytes(GetEncoding(declEnc).GetChars(buffer, offset, count)));
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

        public override void Close()
        {
            if (_stream.CanWrite)
            {
                Flush();
            }

            base.Close();
            _stream.Dispose();
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken) => _stream.FlushAsync(cancellationToken);

        public override async ValueTask DisposeAsync()
        {
            try
            {
                if (_stream.CanWrite)
                {
                    await _stream.FlushAsync(_writeCancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                await _stream.DisposeAsync().ConfigureAwait(false);
                GC.SuppressFinalize(this);
            }
        }

        public override int ReadByte()
        {
            if (_byteCount == 0 && _encodingCode == SupportedEncoding.UTF8)
                return _stream.ReadByte();
            if (Read(_byteBuffer, 0, 1) == 0)
                return -1;
            return _byteBuffer[0];
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
                        return _stream.Read(buffer);

                    Debug.Assert(_bytes != null);
                    Debug.Assert(_chars != null);

                    // No more bytes than can be turned into characters
                    _byteOffset = 0;
                    _byteCount = _stream.Read(_bytes, _byteCount, (_chars.Length - 1) * 2);

                    // Check for end of stream
                    if (_byteCount == 0)
                        return 0;

                    // Fix up incomplete chars
                    CleanupCharBreakSync();

                    // Change encoding
                    int charCount = _encoding!.GetChars(_bytes, 0, _byteCount, _chars, 0);
                    _byteCount = Encoding.UTF8.GetBytes(_chars, 0, charCount, _bytes, 0);
                }

                // Give them bytes
                int count = buffer.Length;
                if (_byteCount < count)
                    count = _byteCount;

                _bytes.AsSpan(_byteOffset, count).CopyTo(buffer);
                _byteOffset += count;
                _byteCount -= count;
                return count;
            }
            catch (DecoderFallbackException ex)
            {
                throw new XmlException(SR.XmlInvalidBytes, ex);
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (buffer.IsEmpty)
            {
                return 0;
            }

            try
            {
                if (_byteCount == 0)
                {
                    if (_encodingCode == SupportedEncoding.UTF8)
                    {
                        return await _stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    }
                    Debug.Assert(_bytes is not null && _chars is not null);
                    _byteOffset = 0;
                    _byteCount = await _stream.ReadAsync(_bytes.AsMemory(0, (_chars.Length - 1) * 2), cancellationToken).ConfigureAwait(false);
                    if (_byteCount == 0)
                    {
                        return 0;
                    }
                    await CleanupCharBreak<AsyncXmlStreamIO>(cancellationToken).ConfigureAwait(false);
                    int charCount = _encoding!.GetChars(_bytes, 0, _byteCount, _chars, 0);
                    _byteCount = Encoding.UTF8.GetBytes(_chars, 0, charCount, _bytes, 0);
                }

                int count = Math.Min(buffer.Length, _byteCount);
                _bytes.AsMemory(_byteOffset, count).CopyTo(buffer);
                _byteOffset += count;
                _byteCount -= count;
                return count;
            }
            catch (DecoderFallbackException ex)
            {
                throw new XmlException(SR.XmlInvalidBytes, ex);
            }
        }

        private async ValueTask CleanupCharBreak<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            Debug.Assert(_bytes != null);

            int max = _byteOffset + _byteCount;

            // Read on 2 byte boundaries
            if ((_byteCount % 2) != 0)
            {
                int read = await TMode.ReadAtLeast(_stream, _bytes.AsMemory(max, 1), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    throw new XmlException(SR.UnexpectedEndOfFile);

                max++;
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
                int read = await TMode.ReadAtLeast(_stream, _bytes.AsMemory(max, 2), cancellationToken).ConfigureAwait(false);
                if (read < 2)
                    throw new XmlException(SR.UnexpectedEndOfFile);
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

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_encodingCode == SupportedEncoding.UTF8)
            {
                await _stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                return;
            }

            Debug.Assert(_bytes is not null && _chars is not null);
            while (!buffer.IsEmpty)
            {
                int size = Math.Min(_chars.Length, buffer.Length);
                int charCount = _dec!.GetChars(buffer.Span.Slice(0, size), _chars, false);
                int byteCount = _enc!.GetBytes(_chars, 0, charCount, _bytes, 0, false);
                await _stream.WriteAsync(_bytes.AsMemory(0, byteCount), cancellationToken).ConfigureAwait(false);
                buffer = buffer.Slice(size);
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
}
