// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.IO
{
    /// <summary>
    /// Reads primitive data types as binary values in a specific encoding.
    /// </summary>
    public class BinaryReader : IDisposable
    {
        private const int MaxCharBytesSize = 128;

        private readonly Stream _stream;
        private readonly Encoding _encoding;
        private Decoder? _decoder;
        private char[]? _charBuffer;
        private readonly int _maxCharsSize;  // From MaxCharBytesSize & Encoding

        // Performance optimization for Read() w/ Unicode.  Speeds us up by ~40%
        private readonly bool _2BytesPerChar;
        private readonly bool _isMemoryStream; // "do we sit on MemoryStream?" for Read/ReadInt32 perf
        private readonly bool _leaveOpen;
        private bool _disposed;

        public BinaryReader(Stream input) : this(input, Encoding.UTF8, false)
        {
        }

        public BinaryReader(Stream input, Encoding encoding) : this(input, encoding, false)
        {
        }

        public BinaryReader(Stream input, Encoding encoding, bool leaveOpen)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(encoding);

            if (!input.CanRead)
            {
                throw new ArgumentException(SR.Argument_StreamNotReadable);
            }

            _stream = input;
            _encoding = encoding;
            _maxCharsSize = encoding.GetMaxCharCount(MaxCharBytesSize);

            // For Encodings that always use 2 bytes per char (or more),
            // special case them here to make Read() & Peek() faster.
            _2BytesPerChar = encoding is UnicodeEncoding;
            // check if BinaryReader is based on MemoryStream, and keep this for it's life
            // we cannot use "as" operator, since derived classes are not allowed
            _isMemoryStream = _stream.GetType() == typeof(MemoryStream);
            _leaveOpen = leaveOpen;
        }

        public virtual Stream BaseStream => _stream;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && !_leaveOpen)
                {
                    _stream.Close();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        /// <remarks>
        /// Override Dispose(bool) instead of Close(). This API exists for compatibility purposes.
        /// </remarks>
        public virtual void Close()
        {
            Dispose(true);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }
        }

        public virtual int PeekChar()
        {
            ThrowIfDisposed();

            if (!_stream.CanSeek)
            {
                return -1;
            }

            long origPos = _stream.Position;
            int ch = Read();
            _stream.Position = origPos;
            return ch;
        }

        public virtual int Read()
        {
            ThrowIfDisposed();

            int charsRead = 0;
            int numBytes;
            long posSav = 0;

            if (_stream.CanSeek)
            {
                posSav = _stream.Position;
            }

            _decoder ??= _encoding.GetDecoder();
            Span<byte> charBytes = stackalloc byte[MaxCharBytesSize];

            char singleChar = '\0';

            while (charsRead == 0)
            {
                // We really want to know what the minimum number of bytes per char
                // is for our encoding.  Otherwise for UnicodeEncoding we'd have to
                // do ~1+log(n) reads to read n characters.
                // Assume 1 byte can be 1 char unless _2BytesPerChar is true.
                numBytes = _2BytesPerChar ? 2 : 1;

                int r = _stream.ReadByte();
                charBytes[0] = (byte)r;
                if (r == -1)
                {
                    numBytes = 0;
                }
                if (numBytes == 2)
                {
                    r = _stream.ReadByte();
                    charBytes[1] = (byte)r;
                    if (r == -1)
                    {
                        numBytes = 1;
                    }
                }

                if (numBytes == 0)
                {
                    return -1;
                }

                Debug.Assert(numBytes is 1 or 2, "BinaryReader::ReadOneChar assumes it's reading one or two bytes only.");

                try
                {
                    charsRead = _decoder.GetChars(charBytes[..numBytes], new Span<char>(ref singleChar), flush: false);
                }
                catch
                {
                    // Handle surrogate char

                    if (_stream.CanSeek)
                    {
                        _stream.Seek(posSav - _stream.Position, SeekOrigin.Current);
                    }
                    // else - we can't do much here

                    throw;
                }

                Debug.Assert(charsRead < 2, "BinaryReader::ReadOneChar - assuming we only got 0 or 1 char, not 2!");
            }
            Debug.Assert(charsRead > 0);
            return singleChar;
        }

        public virtual byte ReadByte() => InternalReadByte();

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Inlined to avoid some method call overhead with InternalRead.
        private byte InternalReadByte()
        {
            ThrowIfDisposed();

            int b = _stream.ReadByte();
            if (b == -1)
            {
                ThrowHelper.ThrowEndOfFileException();
            }

            return (byte)b;
        }

        [CLSCompliant(false)]
        public virtual sbyte ReadSByte() => (sbyte)InternalReadByte();
        public virtual bool ReadBoolean() => InternalReadByte() != 0;

        public virtual char ReadChar()
        {
            int value = Read();
            if (value == -1)
            {
                ThrowHelper.ThrowEndOfFileException();
            }
            return (char)value;
        }

        public virtual short ReadInt16() => BinaryPrimitives.ReadInt16LittleEndian(InternalRead(stackalloc byte[sizeof(short)]));

        [CLSCompliant(false)]
        public virtual ushort ReadUInt16() => BinaryPrimitives.ReadUInt16LittleEndian(InternalRead(stackalloc byte[sizeof(ushort)]));

        public virtual int ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(InternalRead(stackalloc byte[sizeof(int)]));
        [CLSCompliant(false)]
        public virtual uint ReadUInt32() => BinaryPrimitives.ReadUInt32LittleEndian(InternalRead(stackalloc byte[sizeof(uint)]));
        public virtual long ReadInt64() => BinaryPrimitives.ReadInt64LittleEndian(InternalRead(stackalloc byte[sizeof(long)]));
        [CLSCompliant(false)]
        public virtual ulong ReadUInt64() => BinaryPrimitives.ReadUInt64LittleEndian(InternalRead(stackalloc byte[sizeof(ulong)]));
        public virtual unsafe Half ReadHalf() => BinaryPrimitives.ReadHalfLittleEndian(InternalRead(stackalloc byte[sizeof(Half)]));
        public virtual unsafe float ReadSingle() => BinaryPrimitives.ReadSingleLittleEndian(InternalRead(stackalloc byte[sizeof(float)]));
        public virtual unsafe double ReadDouble() => BinaryPrimitives.ReadDoubleLittleEndian(InternalRead(stackalloc byte[sizeof(double)]));

        public virtual decimal ReadDecimal()
        {
            ReadOnlySpan<byte> span = InternalRead(stackalloc byte[sizeof(decimal)]);
            try
            {
                return decimal.ToDecimal(span);
            }
            catch (ArgumentException e)
            {
                // ReadDecimal cannot leak out ArgumentException
                throw new IOException(SR.Arg_DecBitCtor, e);
            }
        }

        public virtual string ReadString()
        {
            ThrowIfDisposed();

            // Length of the string in bytes, not chars
            int stringLength = Read7BitEncodedInt();
            if (stringLength < 0)
            {
                throw new IOException(SR.Format(SR.IO_InvalidStringLen_Len, stringLength));
            }

            if (stringLength == 0)
            {
                return string.Empty;
            }

            Span<byte> charBytes = stackalloc byte[MaxCharBytesSize];

            int currPos = 0;
            StringBuilder? sb = null;
            do
            {
                int readLength = Math.Min(MaxCharBytesSize, stringLength - currPos);

                int n = _stream.Read(charBytes[..readLength]);
                if (n == 0)
                {
                    ThrowHelper.ThrowEndOfFileException();
                }

                if (currPos == 0 && n == stringLength)
                {
                    return _encoding.GetString(charBytes[..n]);
                }

                _decoder ??= _encoding.GetDecoder();
                _charBuffer ??= new char[_maxCharsSize];

                int charsRead = _decoder.GetChars(charBytes[..n], _charBuffer, flush: false);

                // Since we could be reading from an untrusted data source, limit the initial size of the
                // StringBuilder instance we're about to get or create. It'll expand automatically as needed.

                sb ??= StringBuilderCache.Acquire(Math.Min(stringLength, StringBuilderCache.MaxBuilderSize)); // Actual string length in chars may be smaller.
                sb.Append(_charBuffer, 0, charsRead);
                currPos += n;
            } while (currPos < stringLength);

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public virtual int Read(char[] buffer, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }
            ThrowIfDisposed();

            return InternalReadChars(new Span<char>(buffer, index, count));
        }

        public virtual int Read(Span<char> buffer)
        {
            ThrowIfDisposed();
            return InternalReadChars(buffer);
        }

        private int InternalReadChars(Span<char> buffer)
        {
            Debug.Assert(!_disposed);

            _decoder ??= _encoding.GetDecoder();

            int totalCharsRead = 0;
            Span<byte> charBytes = stackalloc byte[MaxCharBytesSize];

            while (!buffer.IsEmpty)
            {
                int numBytes = buffer.Length;

                // We really want to know what the minimum number of bytes per char
                // is for our encoding.  Otherwise for UnicodeEncoding we'd have to
                // do ~1+log(n) reads to read n characters.
                if (_2BytesPerChar)
                {
                    numBytes <<= 1;
                }

                // We do not want to read even a single byte more than necessary.
                //
                // Subtract pending bytes that the decoder may be holding onto. This assumes that each
                // decoded char corresponds to one or more bytes. Note that custom encodings or encodings with
                // a custom replacement sequence may violate this assumption.
                if (numBytes > 1)
                {
                    // For internal decoders, we can check whether the decoder has any pending state.
                    // For custom decoders, assume that the decoder has pending state.
                    if (_decoder is not DecoderNLS decoder || decoder.HasState)
                    {
                        numBytes--;

                        // The worst case is charsRemaining = 2 and UTF32Decoder holding onto 3 pending bytes. We need to read just
                        // one byte in this case.
                        if (_2BytesPerChar && numBytes > 2)
                            numBytes -= 2;
                    }
                }

                scoped ReadOnlySpan<byte> byteBuffer;
                if (_isMemoryStream)
                {
                    Debug.Assert(_stream is MemoryStream);
                    MemoryStream mStream = Unsafe.As<MemoryStream>(_stream);

                    int position = mStream.InternalGetPosition();
                    numBytes = mStream.InternalEmulateRead(numBytes);
                    byteBuffer = new ReadOnlySpan<byte>(mStream.InternalGetBuffer(), position, numBytes);
                }
                else
                {
                    if (numBytes > MaxCharBytesSize)
                    {
                        numBytes = MaxCharBytesSize;
                    }

                    numBytes = _stream.Read(charBytes[..numBytes]);
                    byteBuffer = charBytes[..numBytes];
                }

                if (byteBuffer.IsEmpty)
                {
                    break;
                }

                int charsRead = _decoder.GetChars(byteBuffer, buffer, flush: false);
                buffer = buffer.Slice(charsRead);

                totalCharsRead += charsRead;
            }

            // we may have read fewer than the number of characters requested if end of stream reached
            // or if the encoding makes the char count too big for the buffer (e.g. fallback sequence)
            return totalCharsRead;
        }

        public virtual char[] ReadChars(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            ThrowIfDisposed();

            if (count == 0)
            {
                return Array.Empty<char>();
            }

            char[] chars = new char[count];
            int n = InternalReadChars(new Span<char>(chars));
            if (n != count)
            {
                chars = chars[..n];
            }

            return chars;
        }

        public virtual int Read(byte[] buffer, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }
            ThrowIfDisposed();

            return _stream.Read(buffer, index, count);
        }

        public virtual int Read(Span<byte> buffer)
        {
            ThrowIfDisposed();
            return _stream.Read(buffer);
        }

        public virtual byte[] ReadBytes(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            ThrowIfDisposed();

            if (count == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] result = new byte[count];
            int numRead = _stream.ReadAtLeast(result, result.Length, throwOnEndOfStream: false);

            if (numRead != result.Length)
            {
                // Trim array. This should happen on EOF & possibly net streams.
                result = result[..numRead];
            }

            return result;
        }

        private ReadOnlySpan<byte> InternalRead(Span<byte> buffer)
        {
            Debug.Assert(buffer.Length != 1, "length of 1 should use ReadByte.");

            if (_isMemoryStream)
            {
                // read directly from MemoryStream buffer
                Debug.Assert(_stream is MemoryStream);
                return Unsafe.As<MemoryStream>(_stream).InternalReadSpan(buffer.Length);
            }
            else
            {
                ThrowIfDisposed();

                _stream.ReadExactly(buffer);

                return buffer;
            }
        }

        // FillBuffer is not performing well when reading from MemoryStreams as it is using the public Stream interface.
        // We introduced new function InternalRead which can work directly on the MemoryStream internal buffer or using the public Stream
        // interface when working with all other streams. This function is not needed anymore but we decided not to delete it for compatibility
        // reasons. More about the subject in: https://github.com/dotnet/coreclr/pull/22102
        protected virtual void FillBuffer(int numBytes)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(numBytes);

            ThrowIfDisposed();

            switch (numBytes)
            {
                case 0:
                    // ReadExactly no-ops for empty buffers, so special case numBytes == 0 to preserve existing behavior.
                    int n = _stream.Read(Array.Empty<byte>(), 0, 0);
                    if (n == 0)
                    {
                        ThrowHelper.ThrowEndOfFileException();
                    }
                    break;
                case 1:
                    n = _stream.ReadByte();
                    if (n == -1)
                    {
                        ThrowHelper.ThrowEndOfFileException();
                    }
                    break;
                default:
                    if (_stream.CanSeek)
                    {
                        _stream.Seek(numBytes, SeekOrigin.Current);
                        return;
                    }
                    byte[] buffer = ArrayPool<byte>.Shared.Rent(numBytes);
                    _stream.ReadExactly(buffer.AsSpan(0, numBytes));
                    ArrayPool<byte>.Shared.Return(buffer);
                    break;
            }
        }

        public int Read7BitEncodedInt()
        {
            // Unlike writing, we can't delegate to the 64-bit read on
            // 64-bit platforms. The reason for this is that we want to
            // stop consuming bytes if we encounter an integer overflow.

            uint result = 0;
            byte byteReadJustNow;

            // Read the integer 7 bits at a time. The high bit
            // of the byte when on means to continue reading more bytes.
            //
            // There are two failure cases: we've read more than 5 bytes,
            // or the fifth byte is about to cause integer overflow.
            // This means that we can read the first 4 bytes without
            // worrying about integer overflow.

            const int MaxBytesWithoutOverflow = 4;
            for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
            {
                // ReadByte handles end of stream cases for us.
                byteReadJustNow = ReadByte();
                result |= (byteReadJustNow & 0x7Fu) << shift;

                if (byteReadJustNow <= 0x7Fu)
                {
                    return (int)result; // early exit
                }
            }

            // Read the 5th byte. Since we already read 28 bits,
            // the value of this byte must fit within 4 bits (32 - 28),
            // and it must not have the high bit set.

            byteReadJustNow = ReadByte();
            if (byteReadJustNow > 0b_1111u)
            {
                throw new FormatException(SR.Format_Bad7BitInt);
            }

            result |= (uint)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
            return (int)result;
        }

        public long Read7BitEncodedInt64()
        {
            ulong result = 0;
            byte byteReadJustNow;

            // Read the integer 7 bits at a time. The high bit
            // of the byte when on means to continue reading more bytes.
            //
            // There are two failure cases: we've read more than 10 bytes,
            // or the tenth byte is about to cause integer overflow.
            // This means that we can read the first 9 bytes without
            // worrying about integer overflow.

            const int MaxBytesWithoutOverflow = 9;
            for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
            {
                // ReadByte handles end of stream cases for us.
                byteReadJustNow = ReadByte();
                result |= (byteReadJustNow & 0x7Ful) << shift;

                if (byteReadJustNow <= 0x7Fu)
                {
                    return (long)result; // early exit
                }
            }

            // Read the 10th byte. Since we already read 63 bits,
            // the value of this byte must fit within 1 bit (64 - 63),
            // and it must not have the high bit set.

            byteReadJustNow = ReadByte();
            if (byteReadJustNow > 0b_1u)
            {
                throw new FormatException(SR.Format_Bad7BitInt);
            }

            result |= (ulong)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
            return (long)result;
        }
    }
}
