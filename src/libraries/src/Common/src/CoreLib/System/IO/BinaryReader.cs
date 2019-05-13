// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Wraps a stream and provides convenient read functionality
** for strings and primitive types.
**
**
============================================================*/

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    public class BinaryReader : IDisposable
    {
        private const int MaxCharBytesSize = 128;

        private readonly Stream _stream;
        private readonly byte[] _buffer;
        private readonly Decoder _decoder;
        private byte[]? _charBytes;
        private char[]? _charBuffer;
        private int _maxCharsSize;  // From MaxCharBytesSize & Encoding

        // Performance optimization for Read() w/ Unicode.  Speeds us up by ~40% 
        private bool _2BytesPerChar;
        private bool _isMemoryStream; // "do we sit on MemoryStream?" for Read/ReadInt32 perf
        private bool _leaveOpen;
        private bool _disposed;

        public BinaryReader(Stream input) : this(input, Encoding.UTF8, false)
        {
        }

        public BinaryReader(Stream input, Encoding encoding) : this(input, encoding, false)
        {
        }

        public BinaryReader(Stream input, Encoding encoding, bool leaveOpen)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }
            if (!input.CanRead)
            {
                throw new ArgumentException(SR.Argument_StreamNotReadable);
            }

            _stream = input;
            _decoder = encoding.GetDecoder();
            _maxCharsSize = encoding.GetMaxCharCount(MaxCharBytesSize);
            int minBufferSize = encoding.GetMaxByteCount(1);  // max bytes per one char
            if (minBufferSize < 16)
            {
                minBufferSize = 16;
            }

            _buffer = new byte[minBufferSize];
            // _charBuffer and _charBytes will be left null.

            // For Encodings that always use 2 bytes per char (or more), 
            // special case them here to make Read() & Peek() faster.
            _2BytesPerChar = encoding is UnicodeEncoding;
            // check if BinaryReader is based on MemoryStream, and keep this for it's life
            // we cannot use "as" operator, since derived classes are not allowed
            _isMemoryStream = (_stream.GetType() == typeof(MemoryStream));
            _leaveOpen = leaveOpen;

            Debug.Assert(_decoder != null, "[BinaryReader.ctor]_decoder!=null");
        }

        public virtual Stream BaseStream
        {
            get
            {
                return _stream;
            }
        }

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
                throw Error.GetFileNotOpen();
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

            if (_charBytes == null)
            {
                _charBytes = new byte[MaxCharBytesSize]; //REVIEW: We need at most 2 bytes/char here? 
            }

            Span<char> singleChar = stackalloc char[1];

            while (charsRead == 0)
            {
                // We really want to know what the minimum number of bytes per char
                // is for our encoding.  Otherwise for UnicodeEncoding we'd have to
                // do ~1+log(n) reads to read n characters.
                // Assume 1 byte can be 1 char unless _2BytesPerChar is true.
                numBytes = _2BytesPerChar ? 2 : 1;

                int r = _stream.ReadByte();
                _charBytes[0] = (byte)r;
                if (r == -1)
                {
                    numBytes = 0;
                }
                if (numBytes == 2)
                {
                    r = _stream.ReadByte();
                    _charBytes[1] = (byte)r;
                    if (r == -1)
                    {
                        numBytes = 1;
                    }
                }

                if (numBytes == 0)
                {
                    return -1;
                }

                Debug.Assert(numBytes == 1 || numBytes == 2, "BinaryReader::ReadOneChar assumes it's reading one or 2 bytes only.");

                try
                {
                    charsRead = _decoder.GetChars(new ReadOnlySpan<byte>(_charBytes, 0, numBytes), singleChar, flush: false);
                }
                catch
                {
                    // Handle surrogate char 

                    if (_stream.CanSeek)
                    {
                        _stream.Seek((posSav - _stream.Position), SeekOrigin.Current);
                    }
                    // else - we can't do much here

                    throw;
                }

                Debug.Assert(charsRead < 2, "BinaryReader::ReadOneChar - assuming we only got 0 or 1 char, not 2!");
            }
            Debug.Assert(charsRead > 0);
            return singleChar[0];
        }

        public virtual byte ReadByte() => InternalReadByte();

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Inlined to avoid some method call overhead with InternalRead.
        private byte InternalReadByte()
        {
            ThrowIfDisposed();

            int b = _stream.ReadByte();
            if (b == -1)
            {
                throw Error.GetEndOfFile();
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
                throw Error.GetEndOfFile();
            }
            return (char)value;
        }

        public virtual short ReadInt16() => BinaryPrimitives.ReadInt16LittleEndian(InternalRead(2));

        [CLSCompliant(false)]
        public virtual ushort ReadUInt16() => BinaryPrimitives.ReadUInt16LittleEndian(InternalRead(2));

        public virtual int ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(InternalRead(4));
        [CLSCompliant(false)]
        public virtual uint ReadUInt32() => BinaryPrimitives.ReadUInt32LittleEndian(InternalRead(4));
        public virtual long ReadInt64() => BinaryPrimitives.ReadInt64LittleEndian(InternalRead(8));
        [CLSCompliant(false)]
        public virtual ulong ReadUInt64() => BinaryPrimitives.ReadUInt64LittleEndian(InternalRead(8));
        public virtual unsafe float ReadSingle() => BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(InternalRead(4)));
        public virtual unsafe double ReadDouble() => BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(InternalRead(8)));

        public virtual decimal ReadDecimal()
        {
            ReadOnlySpan<byte> span = InternalRead(16);
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

            int currPos = 0;
            int n;
            int stringLength;
            int readLength;
            int charsRead;

            // Length of the string in bytes, not chars
            stringLength = Read7BitEncodedInt();
            if (stringLength < 0)
            {
                throw new IOException(SR.Format(SR.IO_InvalidStringLen_Len, stringLength));
            }

            if (stringLength == 0)
            {
                return string.Empty;
            }

            if (_charBytes == null)
            {
                _charBytes = new byte[MaxCharBytesSize];
            }

            if (_charBuffer == null)
            {
                _charBuffer = new char[_maxCharsSize];
            }

            StringBuilder? sb = null;
            do
            {
                readLength = ((stringLength - currPos) > MaxCharBytesSize) ? MaxCharBytesSize : (stringLength - currPos);

                n = _stream.Read(_charBytes, 0, readLength);
                if (n == 0)
                {
                    throw Error.GetEndOfFile();
                }

                charsRead = _decoder.GetChars(_charBytes, 0, n, _charBuffer, 0);

                if (currPos == 0 && n == stringLength)
                {
                    return new string(_charBuffer, 0, charsRead);
                }

                if (sb == null)
                {
                    sb = StringBuilderCache.Acquire(stringLength); // Actual string length in chars may be smaller.
                }

                sb.Append(_charBuffer, 0, charsRead);
                currPos += n;
            } while (currPos < stringLength);

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public virtual int Read(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }
            ThrowIfDisposed();

            // SafeCritical: index and count have already been verified to be a valid range for the buffer
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

            int numBytes = 0;
            int index = 0;
            int charsRemaining = buffer.Length;

            if (_charBytes == null)
            {
                _charBytes = new byte[MaxCharBytesSize];
            }

            while (charsRemaining > 0)
            {
                int charsRead = 0;
                // We really want to know what the minimum number of bytes per char
                // is for our encoding.  Otherwise for UnicodeEncoding we'd have to
                // do ~1+log(n) reads to read n characters.
                numBytes = charsRemaining;

                if (_2BytesPerChar)
                {
                    numBytes <<= 1;
                }
                if (numBytes > MaxCharBytesSize)
                {
                    numBytes = MaxCharBytesSize;
                }

                int position = 0;
                byte[]? byteBuffer = null;
                if (_isMemoryStream)
                {
                    Debug.Assert(_stream is MemoryStream);
                    MemoryStream mStream = (MemoryStream)_stream;

                    position = mStream.InternalGetPosition();
                    numBytes = mStream.InternalEmulateRead(numBytes);
                    byteBuffer = mStream.InternalGetBuffer();
                }
                else
                {
                    numBytes = _stream.Read(_charBytes, 0, numBytes);
                    byteBuffer = _charBytes;
                }

                if (numBytes == 0)
                {
                    return (buffer.Length - charsRemaining);
                }

                Debug.Assert(byteBuffer != null, "expected byteBuffer to be non-null");
                checked
                {
                    if (position < 0 || numBytes < 0 || position > byteBuffer.Length - numBytes)
                    {
                        throw new ArgumentOutOfRangeException(nameof(numBytes));
                    }
                    if (index < 0 || charsRemaining < 0 || index > buffer.Length - charsRemaining)
                    {
                        throw new ArgumentOutOfRangeException(nameof(charsRemaining));
                    }
                    unsafe
                    {
                        fixed (byte* pBytes = byteBuffer)
                        fixed (char* pChars = &MemoryMarshal.GetReference(buffer))
                        {
                            charsRead = _decoder.GetChars(pBytes + position, numBytes, pChars + index, charsRemaining, flush: false);
                        }
                    }
                }

                charsRemaining -= charsRead;
                index += charsRead;
            }

            // this should never fail
            Debug.Assert(charsRemaining >= 0, "We read too many characters.");

            // we may have read fewer than the number of characters requested if end of stream reached 
            // or if the encoding makes the char count too big for the buffer (e.g. fallback sequence)
            return (buffer.Length - charsRemaining);
        }

        public virtual char[] ReadChars(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            ThrowIfDisposed();

            if (count == 0)
            {
                return Array.Empty<char>();
            }

            // SafeCritical: we own the chars buffer, and therefore can guarantee that the index and count are valid
            char[] chars = new char[count];
            int n = InternalReadChars(new Span<char>(chars));
            if (n != count)
            {
                char[] copy = new char[n];
                Buffer.BlockCopy(chars, 0, copy, 0, 2 * n); // sizeof(char)
                chars = copy;
            }

            return chars;
        }

        public virtual int Read(byte[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
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
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            ThrowIfDisposed();

            if (count == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] result = new byte[count];
            int numRead = 0;
            do
            {
                int n = _stream.Read(result, numRead, count);
                if (n == 0)
                {
                    break;
                }

                numRead += n;
                count -= n;
            } while (count > 0);

            if (numRead != result.Length)
            {
                // Trim array.  This should happen on EOF & possibly net streams.
                byte[] copy = new byte[numRead];
                Buffer.BlockCopy(result, 0, copy, 0, numRead);
                result = copy;
            }

            return result;
        }

        private ReadOnlySpan<byte> InternalRead(int numBytes)
        {
            Debug.Assert(numBytes >= 2 && numBytes <= 16, "value of 1 should use ReadByte. value > 16 requires to change the minimal _buffer size");

            if (_isMemoryStream)
            {
                // read directly from MemoryStream buffer
                Debug.Assert(_stream is MemoryStream);
                return ((MemoryStream)_stream).InternalReadSpan(numBytes);
            }
            else
            {
                ThrowIfDisposed();

                int bytesRead = 0;
                int n = 0;

                do
                {
                    n = _stream.Read(_buffer, bytesRead, numBytes - bytesRead);
                    if (n == 0)
                    {
                        throw Error.GetEndOfFile();
                    }
                    bytesRead += n;
                } while (bytesRead < numBytes);

                return _buffer;
            }
        }

        // FillBuffer is not performing well when reading from MemoryStreams as it is using the public Stream interface.
        // We introduced new function InternalRead which can work directly on the MemoryStream internal buffer or using the public Stream
        // interface when working with all other streams. This function is not needed anymore but we decided not to delete it for compatibility
        // reasons. More about the subject in: https://github.com/dotnet/coreclr/pull/22102
        protected virtual void FillBuffer(int numBytes)
        {
            if (numBytes < 0 || numBytes > _buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(numBytes), SR.ArgumentOutOfRange_BinaryReaderFillBuffer);
            }

            int bytesRead = 0;
            int n = 0;

            ThrowIfDisposed();

            // Need to find a good threshold for calling ReadByte() repeatedly
            // vs. calling Read(byte[], int, int) for both buffered & unbuffered
            // streams.
            if (numBytes == 1)
            {
                n = _stream.ReadByte();
                if (n == -1)
                {
                    throw Error.GetEndOfFile();
                }

                _buffer[0] = (byte)n;
                return;
            }

            do
            {
                n = _stream.Read(_buffer, bytesRead, numBytes - bytesRead);
                if (n == 0)
                {
                    throw Error.GetEndOfFile();
                }
                bytesRead += n;
            } while (bytesRead < numBytes);
        }

        protected internal int Read7BitEncodedInt()
        {
            // Read out an Int32 7 bits at a time.  The high bit
            // of the byte when on means to continue reading more bytes.
            int count = 0;
            int shift = 0;
            byte b;
            do
            {
                // Check for a corrupted stream.  Read a max of 5 bytes.
                // In a future version, add a DataFormatException.
                if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
                {
                    throw new FormatException(SR.Format_Bad7BitInt32);
                }

                // ReadByte handles end of stream cases for us.
                b = ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }
    }
}
