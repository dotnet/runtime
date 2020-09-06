// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Diagnostics;
using System.Buffers;
using System.Threading.Tasks;
using System.Buffers.Binary;

namespace System.IO
{
    // This abstract base class represents a writer that can write
    // primitives to an arbitrary stream. A subclass can override methods to
    // give unique encodings.
    //
    public class BinaryWriter : IDisposable, IAsyncDisposable
    {
        public static readonly BinaryWriter Null = new BinaryWriter();

        protected Stream OutStream;
        private readonly byte[] _buffer;    // temp space for writing primitives to.
        private readonly Encoding _encoding;
        private readonly Encoder _encoder;

        private readonly bool _leaveOpen;

        // Perf optimization stuff
        private byte[]? _largeByteBuffer;  // temp space for writing chars.
        private int _maxChars;   // max # of chars we can put in _largeByteBuffer
        // Size should be around the max number of chars/string * Encoding's max bytes/char
        private const int LargeByteBufferSize = 256;

        // Protected default constructor that sets the output stream
        // to a null stream (a bit bucket).
        protected BinaryWriter()
        {
            OutStream = Stream.Null;
            _buffer = new byte[16];
            _encoding = EncodingCache.UTF8NoBOM;
            _encoder = _encoding.GetEncoder();
        }

        public BinaryWriter(Stream output) : this(output, EncodingCache.UTF8NoBOM, false)
        {
        }

        public BinaryWriter(Stream output, Encoding encoding) : this(output, encoding, false)
        {
        }

        public BinaryWriter(Stream output, Encoding encoding, bool leaveOpen)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (!output.CanWrite)
                throw new ArgumentException(SR.Argument_StreamNotWritable);

            OutStream = output;
            _buffer = new byte[16];
            _encoding = encoding;
            _encoder = _encoding.GetEncoder();
            _leaveOpen = leaveOpen;
        }

        // Closes this writer and releases any system resources associated with the
        // writer. Following a call to Close, any operations on the writer
        // may raise exceptions.
        public virtual void Close()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_leaveOpen)
                    OutStream.Flush();
                else
                    OutStream.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public virtual ValueTask DisposeAsync()
        {
            try
            {
                if (GetType() == typeof(BinaryWriter))
                {
                    if (_leaveOpen)
                    {
                        return new ValueTask(OutStream.FlushAsync());
                    }

                    OutStream.Close();
                }
                else
                {
                    // Since this is a derived BinaryWriter, delegate to whatever logic
                    // the derived implementation already has in Dispose.
                    Dispose();
                }

                return default;
            }
            catch (Exception exc)
            {
                return ValueTask.FromException(exc);
            }
        }

        // Returns the stream associated with the writer. It flushes all pending
        // writes before returning. All subclasses should override Flush to
        // ensure that all buffered data is sent to the stream.
        public virtual Stream BaseStream
        {
            get
            {
                Flush();
                return OutStream;
            }
        }

        // Clears all buffers for this writer and causes any buffered data to be
        // written to the underlying device.
        public virtual void Flush()
        {
            OutStream.Flush();
        }

        public virtual long Seek(int offset, SeekOrigin origin)
        {
            return OutStream.Seek(offset, origin);
        }

        // Writes a boolean to this stream. A single byte is written to the stream
        // with the value 0 representing false or the value 1 representing true.
        //
        public virtual void Write(bool value) => OutStream.WriteByte((byte)(value ? 1 : 0));

        // Writes a byte to this stream. The current position of the stream is
        // advanced by one.
        //
        public virtual void Write(byte value) => OutStream.WriteByte(value);

        // Writes a signed byte to this stream. The current position of the stream
        // is advanced by one.
        //
        [CLSCompliant(false)]
        public virtual void Write(sbyte value) => OutStream.WriteByte((byte)value);

        // Writes a byte array to this stream.
        //
        // This default implementation calls the Write(Object, int, int)
        // method to write the byte array.
        //
        public virtual void Write(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            OutStream.Write(buffer, 0, buffer.Length);
        }

        // Writes a section of a byte array to this stream.
        //
        // This default implementation calls the Write(Object, int, int)
        // method to write the byte array.
        //
        public virtual void Write(byte[] buffer, int index, int count)
        {
            OutStream.Write(buffer, index, count);
        }

        // Writes a character to this stream. The current position of the stream is
        // advanced by two.
        // Note this method cannot handle surrogates properly in UTF-8.
        //
        public virtual unsafe void Write(char ch)
        {
            if (char.IsSurrogate(ch))
                throw new ArgumentException(SR.Arg_SurrogatesNotAllowedAsSingleChar);

            Debug.Assert(_encoding.GetMaxByteCount(1) <= 16, "_encoding.GetMaxByteCount(1) <= 16)");
            int numBytes = 0;
            fixed (byte* pBytes = &_buffer[0])
            {
                numBytes = _encoder.GetBytes(&ch, 1, pBytes, _buffer.Length, flush: true);
            }
            OutStream.Write(_buffer, 0, numBytes);
        }

        // Writes a character array to this stream.
        //
        // This default implementation calls the Write(Object, int, int)
        // method to write the character array.
        //
        public virtual void Write(char[] chars)
        {
            if (chars == null)
                throw new ArgumentNullException(nameof(chars));

            byte[] bytes = _encoding.GetBytes(chars, 0, chars.Length);
            OutStream.Write(bytes, 0, bytes.Length);
        }

        // Writes a section of a character array to this stream.
        //
        // This default implementation calls the Write(Object, int, int)
        // method to write the character array.
        //
        public virtual void Write(char[] chars, int index, int count)
        {
            byte[] bytes = _encoding.GetBytes(chars, index, count);
            OutStream.Write(bytes, 0, bytes.Length);
        }

        // Writes a double to this stream. The current position of the stream is
        // advanced by eight.
        //
        public virtual unsafe void Write(double value)
        {
            BinaryPrimitives.WriteDoubleLittleEndian(_buffer, value);
            OutStream.Write(_buffer, 0, 8);
        }

        public virtual void Write(decimal value)
        {
            decimal.GetBytes(value, _buffer);
            OutStream.Write(_buffer, 0, 16);
        }

        // Writes a two-byte signed integer to this stream. The current position of
        // the stream is advanced by two.
        //
        public virtual void Write(short value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            OutStream.Write(_buffer, 0, 2);
        }

        // Writes a two-byte unsigned integer to this stream. The current position
        // of the stream is advanced by two.
        //
        [CLSCompliant(false)]
        public virtual void Write(ushort value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            OutStream.Write(_buffer, 0, 2);
        }

        // Writes a four-byte signed integer to this stream. The current position
        // of the stream is advanced by four.
        //
        public virtual void Write(int value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            OutStream.Write(_buffer, 0, 4);
        }

        // Writes a four-byte unsigned integer to this stream. The current position
        // of the stream is advanced by four.
        //
        [CLSCompliant(false)]
        public virtual void Write(uint value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            OutStream.Write(_buffer, 0, 4);
        }

        // Writes an eight-byte signed integer to this stream. The current position
        // of the stream is advanced by eight.
        //
        public virtual void Write(long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(_buffer, value);
            OutStream.Write(_buffer, 0, 8);
        }

        // Writes an eight-byte unsigned integer to this stream. The current
        // position of the stream is advanced by eight.
        //
        [CLSCompliant(false)]
        public virtual void Write(ulong value)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(_buffer, value);
            OutStream.Write(_buffer, 0, 8);
        }

        // Writes a float to this stream. The current position of the stream is
        // advanced by four.
        //
        public virtual void Write(float value)
        {
            uint tmpValue = (uint)BitConverter.SingleToInt32Bits(value);
            _buffer[0] = (byte)tmpValue;
            _buffer[1] = (byte)(tmpValue >> 8);
            _buffer[2] = (byte)(tmpValue >> 16);
            _buffer[3] = (byte)(tmpValue >> 24);
            OutStream.Write(_buffer, 0, 4);
        }

        // Writes a half to this stream. The current position of the stream is
        // advanced by two.
        //
        public virtual void Write(Half value)
        {
            ushort tmpValue = (ushort)BitConverter.HalfToInt16Bits(value);
            _buffer[0] = (byte)tmpValue;
            _buffer[1] = (byte)(tmpValue >> 8);
            OutStream.Write(_buffer, 0, 2);
        }

        // Writes a length-prefixed string to this stream in the BinaryWriter's
        // current Encoding. This method first writes the length of the string as
        // an encoded unsigned integer with variable length, and then writes that many characters
        // to the stream.
        //
        public virtual unsafe void Write(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            int totalBytes = _encoding.GetByteCount(value);
            Write7BitEncodedInt(totalBytes);

            if (_largeByteBuffer == null)
            {
                _largeByteBuffer = new byte[LargeByteBufferSize];
                _maxChars = _largeByteBuffer.Length / _encoding.GetMaxByteCount(1);
            }

            if (totalBytes <= _largeByteBuffer.Length)
            {
                _encoding.GetBytes(value, _largeByteBuffer);
                OutStream.Write(_largeByteBuffer, 0, totalBytes);
                return;
            }

            int numLeft = value.Length;
            int charStart = 0;
            ReadOnlySpan<char> str = value;

            // The previous implementation had significant issues packing encoded
            // characters efficiently into the byte buffer. This was due to the assumption,
            // that every input character will take up the maximum possible size of a character in any given encoding,
            // thus resulting in a lot of unused space within the byte buffer.
            // However, in scenarios where the number of characters aligns perfectly with the buffer size the new
            // implementation saw some performance regressions, therefore in such scenarios (ASCIIEncoding)
            // work will be delegated to the previous implementation.
            if (_encoding.GetType() == typeof(UTF8Encoding))
            {
                while (numLeft > 0)
                {
                    _encoder.Convert(str.Slice(charStart), _largeByteBuffer, numLeft <= _maxChars, out int charCount, out int byteCount, out bool _);

                    OutStream.Write(_largeByteBuffer, 0, byteCount);
                    charStart += charCount;
                    numLeft -= charCount;
                }
            }

            else
            {
                WriteWhenEncodingIsNotUtf8(value, totalBytes);
            }
        }

        private unsafe void WriteWhenEncodingIsNotUtf8(string value, int len)
        {
            // This method should only be called from BinaryWriter(string), which does a null-check
            Debug.Assert(_largeByteBuffer != null);

            int numLeft = value.Length;
            int charStart = 0;

            // Aggressively try to not allocate memory in this loop for
            // runtime performance reasons.  Use an Encoder to write out
            // the string correctly (handling surrogates crossing buffer
            // boundaries properly).
#if DEBUG
            int totalBytes = 0;
#endif
            while (numLeft > 0)
            {
                // Figure out how many chars to process this round.
                int charCount = (numLeft > _maxChars) ? _maxChars : numLeft;
                int byteLen;

                checked
                {
                    if (charStart < 0 || charCount < 0 || charStart > value.Length - charCount)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value));
                    }
                    fixed (char* pChars = value)
                    {
                        fixed (byte* pBytes = &_largeByteBuffer[0])
                        {
                            byteLen = _encoder.GetBytes(pChars + charStart, charCount, pBytes, _largeByteBuffer.Length, charCount == numLeft);
                        }
                    }
                }
#if DEBUG
                totalBytes += byteLen;
                Debug.Assert(totalBytes <= len && byteLen <= _largeByteBuffer.Length, "BinaryWriter::Write(String) - More bytes encoded than expected!");
#endif
                OutStream.Write(_largeByteBuffer, 0, byteLen);
                charStart += charCount;
                numLeft -= charCount;
            }
#if DEBUG
            Debug.Assert(totalBytes == len, "BinaryWriter::Write(String) - Didn't write out all the bytes!");
#endif
        }

        public virtual void Write(ReadOnlySpan<byte> buffer)
        {
            if (GetType() == typeof(BinaryWriter))
            {
                OutStream.Write(buffer);
            }
            else
            {
                byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length);
                try
                {
                    buffer.CopyTo(array);
                    Write(array, 0, buffer.Length);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(array);
                }
            }
        }

        public virtual void Write(ReadOnlySpan<char> chars)
        {
            byte[] bytes = ArrayPool<byte>.Shared.Rent(_encoding.GetMaxByteCount(chars.Length));
            try
            {
                int bytesWritten = _encoding.GetBytes(chars, bytes);
                Write(bytes, 0, bytesWritten);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        public void Write7BitEncodedInt(int value)
        {
            uint uValue = (uint)value;

            // Write out an int 7 bits at a time. The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            //
            // Using the constants 0x7F and ~0x7F below offers smaller
            // codegen than using the constant 0x80.

            while (uValue > 0x7Fu)
            {
                Write((byte)(uValue | ~0x7Fu));
                uValue >>= 7;
            }

            Write((byte)uValue);
        }

        public void Write7BitEncodedInt64(long value)
        {
            ulong uValue = (ulong)value;

            // Write out an int 7 bits at a time. The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            //
            // Using the constants 0x7F and ~0x7F below offers smaller
            // codegen than using the constant 0x80.

            while (uValue > 0x7Fu)
            {
                Write((byte)((uint)uValue | ~0x7Fu));
                uValue >>= 7;
            }

            Write((byte)uValue);
        }
    }
}
