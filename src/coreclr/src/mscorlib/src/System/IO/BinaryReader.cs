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

using System;
using System.Runtime;
using System.Text;
using System.Globalization;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Security;

namespace System.IO
{
    public class BinaryReader : IDisposable
    {
        private const int MaxCharBytesSize = 128;

        private Stream _stream;
        private byte[] _buffer;
        private Decoder _decoder;
        private byte[] _charBytes;
        private char[] _singleChar;
        private char[] _charBuffer;
        private int _maxCharsSize;  // From MaxCharBytesSize & Encoding

        // Performance optimization for Read() w/ Unicode.  Speeds us up by ~40% 
        private bool _2BytesPerChar;
        private bool _isMemoryStream; // "do we sit on MemoryStream?" for Read/ReadInt32 perf
        private bool _leaveOpen;

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
                throw new ArgumentException(SR.Argument_StreamNotReadable);
            Contract.EndContractBlock();
            _stream = input;
            _decoder = encoding.GetDecoder();
            _maxCharsSize = encoding.GetMaxCharCount(MaxCharBytesSize);
            int minBufferSize = encoding.GetMaxByteCount(1);  // max bytes per one char
            if (minBufferSize < 16)
                minBufferSize = 16;
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

        public virtual void Close()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stream copyOfStream = _stream;
                _stream = null;
                if (copyOfStream != null && !_leaveOpen)
                    copyOfStream.Close();
            }
            _stream = null;
            _buffer = null;
            _decoder = null;
            _charBytes = null;
            _singleChar = null;
            _charBuffer = null;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public virtual int PeekChar()
        {
            Contract.Ensures(Contract.Result<int>() >= -1);

            if (_stream == null) __Error.FileNotOpen();

            if (!_stream.CanSeek)
                return -1;
            long origPos = _stream.Position;
            int ch = Read();
            _stream.Position = origPos;
            return ch;
        }

        public virtual int Read()
        {
            Contract.Ensures(Contract.Result<int>() >= -1);

            if (_stream == null)
            {
                __Error.FileNotOpen();
            }
            return InternalReadOneChar();
        }

        public virtual bool ReadBoolean()
        {
            FillBuffer(1);
            return (_buffer[0] != 0);
        }

        public virtual byte ReadByte()
        {
            // Inlined to avoid some method call overhead with FillBuffer.
            if (_stream == null) __Error.FileNotOpen();

            int b = _stream.ReadByte();
            if (b == -1)
                __Error.EndOfFile();
            return (byte)b;
        }

        [CLSCompliant(false)]
        public virtual sbyte ReadSByte()
        {
            FillBuffer(1);
            return (sbyte)(_buffer[0]);
        }

        public virtual char ReadChar()
        {
            int value = Read();
            if (value == -1)
            {
                __Error.EndOfFile();
            }
            return (char)value;
        }

        public virtual short ReadInt16()
        {
            FillBuffer(2);
            return (short)(_buffer[0] | _buffer[1] << 8);
        }

        [CLSCompliant(false)]
        public virtual ushort ReadUInt16()
        {
            FillBuffer(2);
            return (ushort)(_buffer[0] | _buffer[1] << 8);
        }

        public virtual int ReadInt32()
        {
            if (_isMemoryStream)
            {
                if (_stream == null) __Error.FileNotOpen();
                // read directly from MemoryStream buffer
                MemoryStream mStream = _stream as MemoryStream;
                Debug.Assert(mStream != null, "_stream as MemoryStream != null");

                return mStream.InternalReadInt32();
            }
            else
            {
                FillBuffer(4);
                return (int)(_buffer[0] | _buffer[1] << 8 | _buffer[2] << 16 | _buffer[3] << 24);
            }
        }

        [CLSCompliant(false)]
        public virtual uint ReadUInt32()
        {
            FillBuffer(4);
            return (uint)(_buffer[0] | _buffer[1] << 8 | _buffer[2] << 16 | _buffer[3] << 24);
        }

        public virtual long ReadInt64()
        {
            FillBuffer(8);
            uint lo = (uint)(_buffer[0] | _buffer[1] << 8 |
                             _buffer[2] << 16 | _buffer[3] << 24);
            uint hi = (uint)(_buffer[4] | _buffer[5] << 8 |
                             _buffer[6] << 16 | _buffer[7] << 24);
            return (long)((ulong)hi) << 32 | lo;
        }

        [CLSCompliant(false)]
        public virtual ulong ReadUInt64()
        {
            FillBuffer(8);
            uint lo = (uint)(_buffer[0] | _buffer[1] << 8 |
                             _buffer[2] << 16 | _buffer[3] << 24);
            uint hi = (uint)(_buffer[4] | _buffer[5] << 8 |
                             _buffer[6] << 16 | _buffer[7] << 24);
            return ((ulong)hi) << 32 | lo;
        }

        public virtual unsafe float ReadSingle()
        {
            FillBuffer(4);
            uint tmpBuffer = (uint)(_buffer[0] | _buffer[1] << 8 | _buffer[2] << 16 | _buffer[3] << 24);
            return *((float*)&tmpBuffer);
        }

        public virtual unsafe double ReadDouble()
        {
            FillBuffer(8);
            uint lo = (uint)(_buffer[0] | _buffer[1] << 8 |
                _buffer[2] << 16 | _buffer[3] << 24);
            uint hi = (uint)(_buffer[4] | _buffer[5] << 8 |
                _buffer[6] << 16 | _buffer[7] << 24);

            ulong tmpBuffer = ((ulong)hi) << 32 | lo;
            return *((double*)&tmpBuffer);
        }

        public virtual decimal ReadDecimal()
        {
            FillBuffer(16);
            try
            {
                return Decimal.ToDecimal(_buffer);
            }
            catch (ArgumentException e)
            {
                // ReadDecimal cannot leak out ArgumentException
                throw new IOException(SR.Arg_DecBitCtor, e);
            }
        }

        public virtual String ReadString()
        {
            Contract.Ensures(Contract.Result<String>() != null);

            if (_stream == null)
                __Error.FileNotOpen();

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
                return String.Empty;
            }

            if (_charBytes == null)
            {
                _charBytes = new byte[MaxCharBytesSize];
            }

            if (_charBuffer == null)
            {
                _charBuffer = new char[_maxCharsSize];
            }

            StringBuilder sb = null;
            do
            {
                readLength = ((stringLength - currPos) > MaxCharBytesSize) ? MaxCharBytesSize : (stringLength - currPos);

                n = _stream.Read(_charBytes, 0, readLength);
                if (n == 0)
                {
                    __Error.EndOfFile();
                }

                charsRead = _decoder.GetChars(_charBytes, 0, n, _charBuffer, 0);

                if (currPos == 0 && n == stringLength)
                    return new String(_charBuffer, 0, charsRead);

                if (sb == null)
                    sb = StringBuilderCache.Acquire(stringLength); // Actual string length in chars may be smaller.
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
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= count);
            Contract.EndContractBlock();

            if (_stream == null)
                __Error.FileNotOpen();

            // SafeCritical: index and count have already been verified to be a valid range for the buffer
            return InternalReadChars(buffer, index, count);
        }

        private int InternalReadChars(char[] buffer, int index, int count)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(index >= 0 && count >= 0);
            Debug.Assert(_stream != null);

            int numBytes = 0;
            int charsRemaining = count;

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

                // special case for DecoderNLS subclasses when there is a hanging byte from the previous loop
                DecoderNLS decoder = _decoder as DecoderNLS;
                if (decoder != null && decoder.HasState && numBytes > 1)
                {
                    numBytes -= 1;
                }

                if (_2BytesPerChar)
                    numBytes <<= 1;
                if (numBytes > MaxCharBytesSize)
                    numBytes = MaxCharBytesSize;

                int position = 0;
                byte[] byteBuffer = null;
                if (_isMemoryStream)
                {
                    MemoryStream mStream = _stream as MemoryStream;
                    Debug.Assert(mStream != null, "_stream as MemoryStream != null");

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
                    return (count - charsRemaining);
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
                        fixed (char* pChars = buffer)
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
            return (count - charsRemaining);
        }

        private int InternalReadOneChar()
        {
            // I know having a separate InternalReadOneChar method seems a little 
            // redundant, but this makes a scenario like the security parser code
            // 20% faster, in addition to the optimizations for UnicodeEncoding I
            // put in InternalReadChars.   
            int charsRead = 0;
            int numBytes = 0;
            long posSav = posSav = 0;

            if (_stream.CanSeek)
                posSav = _stream.Position;

            if (_charBytes == null)
            {
                _charBytes = new byte[MaxCharBytesSize];
            }
            if (_singleChar == null)
            {
                _singleChar = new char[1];
            }

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
                    numBytes = 0;
                if (numBytes == 2)
                {
                    r = _stream.ReadByte();
                    _charBytes[1] = (byte)r;
                    if (r == -1)
                        numBytes = 1;
                }

                if (numBytes == 0)
                {
                    // Console.WriteLine("Found no bytes.  We're outta here.");
                    return -1;
                }

                Debug.Assert(numBytes == 1 || numBytes == 2, "BinaryReader::InternalReadOneChar assumes it's reading one or 2 bytes only.");

                try
                {
                    charsRead = _decoder.GetChars(_charBytes, 0, numBytes, _singleChar, 0);
                }
                catch
                {
                    // Handle surrogate char 

                    if (_stream.CanSeek)
                        _stream.Seek((posSav - _stream.Position), SeekOrigin.Current);
                    // else - we can't do much here

                    throw;
                }

                Debug.Assert(charsRead < 2, "InternalReadOneChar - assuming we only got 0 or 1 char, not 2!");
                //                Console.WriteLine("That became: " + charsRead + " characters.");
            }
            if (charsRead == 0)
                return -1;
            return _singleChar[0];
        }

        public virtual char[] ReadChars(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            Contract.Ensures(Contract.Result<char[]>() != null);
            Contract.Ensures(Contract.Result<char[]>().Length <= count);
            Contract.EndContractBlock();
            if (_stream == null)
            {
                __Error.FileNotOpen();
            }

            if (count == 0)
            {
                return Array.Empty<Char>();
            }

            // SafeCritical: we own the chars buffer, and therefore can guarantee that the index and count are valid
            char[] chars = new char[count];
            int n = InternalReadChars(chars, 0, count);
            if (n != count)
            {
                char[] copy = new char[n];
                Buffer.InternalBlockCopy(chars, 0, copy, 0, 2 * n); // sizeof(char)
                chars = copy;
            }

            return chars;
        }

        public virtual int Read(byte[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (buffer.Length - index < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= count);
            Contract.EndContractBlock();

            if (_stream == null) __Error.FileNotOpen();
            return _stream.Read(buffer, index, count);
        }

        public virtual byte[] ReadBytes(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            Contract.Ensures(Contract.Result<byte[]>() != null);
            Contract.Ensures(Contract.Result<byte[]>().Length <= Contract.OldValue(count));
            Contract.EndContractBlock();
            if (_stream == null) __Error.FileNotOpen();

            if (count == 0)
            {
                return Array.Empty<Byte>();
            }

            byte[] result = new byte[count];

            int numRead = 0;
            do
            {
                int n = _stream.Read(result, numRead, count);
                if (n == 0)
                    break;
                numRead += n;
                count -= n;
            } while (count > 0);

            if (numRead != result.Length)
            {
                // Trim array.  This should happen on EOF & possibly net streams.
                byte[] copy = new byte[numRead];
                Buffer.InternalBlockCopy(result, 0, copy, 0, numRead);
                result = copy;
            }

            return result;
        }

        protected virtual void FillBuffer(int numBytes)
        {
            if (_buffer != null && (numBytes < 0 || numBytes > _buffer.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(numBytes), SR.ArgumentOutOfRange_BinaryReaderFillBuffer);
            }
            int bytesRead = 0;
            int n = 0;

            if (_stream == null) __Error.FileNotOpen();

            // Need to find a good threshold for calling ReadByte() repeatedly
            // vs. calling Read(byte[], int, int) for both buffered & unbuffered
            // streams.
            if (numBytes == 1)
            {
                n = _stream.ReadByte();
                if (n == -1)
                    __Error.EndOfFile();
                _buffer[0] = (byte)n;
                return;
            }

            do
            {
                n = _stream.Read(_buffer, bytesRead, numBytes - bytesRead);
                if (n == 0)
                {
                    __Error.EndOfFile();
                }
                bytesRead += n;
            } while (bytesRead < numBytes);
        }

        internal protected int Read7BitEncodedInt()
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
                    throw new FormatException(SR.Format_Bad7BitInt32);

                // ReadByte handles end of stream cases for us.
                b = ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }
    }
}
