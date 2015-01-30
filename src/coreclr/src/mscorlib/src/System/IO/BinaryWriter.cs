// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** 
** 
**
** Purpose: Provides a way to write primitives types in 
** binary from a Stream, while also supporting writing Strings
** in a particular encoding.
**
**
===========================================================*/
using System;
using System.Runtime;
using System.Runtime.Serialization;
using System.Text;
using System.Diagnostics.Contracts;

namespace System.IO {
    // This abstract base class represents a writer that can write
    // primitives to an arbitrary stream. A subclass can override methods to
    // give unique encodings.
    //
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public class BinaryWriter : IDisposable
    {
        public static readonly BinaryWriter Null = new BinaryWriter();
        
        protected Stream OutStream;
        private byte[] _buffer;    // temp space for writing primitives to.
        private Encoding _encoding;
        private Encoder _encoder;

        [OptionalField]  // New in .NET FX 4.5.  False is the right default value.
        private bool _leaveOpen;

        // This field should never have been serialized and has not been used since before v2.0.
        // However, this type is serializable, and we need to keep the field name around when deserializing.
        // Also, we'll make .NET FX 4.5 not break if it's missing.
#pragma warning disable 169
        [OptionalField]
        private char[] _tmpOneCharBuffer;
#pragma warning restore 169

        // Perf optimization stuff
        private byte[] _largeByteBuffer;  // temp space for writing chars.
        private int _maxChars;   // max # of chars we can put in _largeByteBuffer
        // Size should be around the max number of chars/string * Encoding's max bytes/char
        private const int LargeByteBufferSize = 256;  

        // Protected default constructor that sets the output stream
        // to a null stream (a bit bucket).
        protected BinaryWriter()
        {
            OutStream = Stream.Null;
            _buffer = new byte[16];
            _encoding = new UTF8Encoding(false, true);
            _encoder = _encoding.GetEncoder();
        }
    
        public BinaryWriter(Stream output) : this(output, new UTF8Encoding(false, true), false)
        {
        }

        public BinaryWriter(Stream output, Encoding encoding) : this(output, encoding, false)
        {
        }

        public BinaryWriter(Stream output, Encoding encoding, bool leaveOpen)
        {
            if (output==null)
                throw new ArgumentNullException("output");
            if (encoding==null)
                throw new ArgumentNullException("encoding");
            if (!output.CanWrite)
                throw new ArgumentException(Environment.GetResourceString("Argument_StreamNotWritable"));
            Contract.EndContractBlock();
    
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
            if (disposing) {
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
    
        /*
         * Returns the stream associate with the writer. It flushes all pending
         * writes before returning. All subclasses should override Flush to
         * ensure that all buffered data is sent to the stream.
         */
        public virtual Stream BaseStream {
            get {
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
        public virtual void Write(bool value) {
            _buffer[0] = (byte) (value ? 1 : 0);
            OutStream.Write(_buffer, 0, 1);
        }
        
        // Writes a byte to this stream. The current position of the stream is
        // advanced by one.
        // 
        public virtual void Write(byte value) 
        {
            OutStream.WriteByte(value);
        }
        
        // Writes a signed byte to this stream. The current position of the stream 
        // is advanced by one.
        // 
        [CLSCompliant(false)]
        public virtual void Write(sbyte value) 
        {
            OutStream.WriteByte((byte) value);
        }

        // Writes a byte array to this stream.
        // 
        // This default implementation calls the Write(Object, int, int)
        // method to write the byte array.
        // 
        public virtual void Write(byte[] buffer) {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            Contract.EndContractBlock();
            OutStream.Write(buffer, 0, buffer.Length);
        }
        
        // Writes a section of a byte array to this stream.
        //
        // This default implementation calls the Write(Object, int, int)
        // method to write the byte array.
        // 
        public virtual void Write(byte[] buffer, int index, int count) {
            OutStream.Write(buffer, index, count);
        }
        
        
        // Writes a character to this stream. The current position of the stream is
        // advanced by two.
        // Note this method cannot handle surrogates properly in UTF-8.
        // 
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe virtual void Write(char ch) {
            if (Char.IsSurrogate(ch))
                throw new ArgumentException(Environment.GetResourceString("Arg_SurrogatesNotAllowedAsSingleChar"));
            Contract.EndContractBlock();

            Contract.Assert(_encoding.GetMaxByteCount(1) <= 16, "_encoding.GetMaxByteCount(1) <= 16)");
            int numBytes = 0;
            fixed(byte * pBytes = _buffer) {
                numBytes = _encoder.GetBytes(&ch, 1, pBytes, 16, true);
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
                throw new ArgumentNullException("chars");
            Contract.EndContractBlock();

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
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe virtual void Write(double value)
        {
            ulong TmpValue = *(ulong *)&value;
            _buffer[0] = (byte) TmpValue;
            _buffer[1] = (byte) (TmpValue >> 8);
            _buffer[2] = (byte) (TmpValue >> 16);
            _buffer[3] = (byte) (TmpValue >> 24);
            _buffer[4] = (byte) (TmpValue >> 32);
            _buffer[5] = (byte) (TmpValue >> 40);
            _buffer[6] = (byte) (TmpValue >> 48);
            _buffer[7] = (byte) (TmpValue >> 56);
            OutStream.Write(_buffer, 0, 8);
        }

        public virtual void Write(decimal value)
        {
            Decimal.GetBytes(value,_buffer);
            OutStream.Write(_buffer, 0, 16);
        }
    
        // Writes a two-byte signed integer to this stream. The current position of
        // the stream is advanced by two.
        // 
        public virtual void Write(short value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            OutStream.Write(_buffer, 0, 2);
        }

        // Writes a two-byte unsigned integer to this stream. The current position
        // of the stream is advanced by two.
        // 
        [CLSCompliant(false)]
        public virtual void Write(ushort value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            OutStream.Write(_buffer, 0, 2);
        }
    
        // Writes a four-byte signed integer to this stream. The current position
        // of the stream is advanced by four.
        // 
        public virtual void Write(int value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            _buffer[2] = (byte) (value >> 16);
            _buffer[3] = (byte) (value >> 24);
            OutStream.Write(_buffer, 0, 4);
        }

        // Writes a four-byte unsigned integer to this stream. The current position
        // of the stream is advanced by four.
        // 
        [CLSCompliant(false)]
        public virtual void Write(uint value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            _buffer[2] = (byte) (value >> 16);
            _buffer[3] = (byte) (value >> 24);
            OutStream.Write(_buffer, 0, 4);
        }
    
        // Writes an eight-byte signed integer to this stream. The current position
        // of the stream is advanced by eight.
        // 
        public virtual void Write(long value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            _buffer[2] = (byte) (value >> 16);
            _buffer[3] = (byte) (value >> 24);
            _buffer[4] = (byte) (value >> 32);
            _buffer[5] = (byte) (value >> 40);
            _buffer[6] = (byte) (value >> 48);
            _buffer[7] = (byte) (value >> 56);
            OutStream.Write(_buffer, 0, 8);
        }

        // Writes an eight-byte unsigned integer to this stream. The current 
        // position of the stream is advanced by eight.
        // 
        [CLSCompliant(false)]
        public virtual void Write(ulong value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            _buffer[2] = (byte) (value >> 16);
            _buffer[3] = (byte) (value >> 24);
            _buffer[4] = (byte) (value >> 32);
            _buffer[5] = (byte) (value >> 40);
            _buffer[6] = (byte) (value >> 48);
            _buffer[7] = (byte) (value >> 56);
            OutStream.Write(_buffer, 0, 8);
        }
    
        // Writes a float to this stream. The current position of the stream is
        // advanced by four.
        // 
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe virtual void Write(float value)
        {
            uint TmpValue = *(uint *)&value;
            _buffer[0] = (byte) TmpValue;
            _buffer[1] = (byte) (TmpValue >> 8);
            _buffer[2] = (byte) (TmpValue >> 16);
            _buffer[3] = (byte) (TmpValue >> 24);
            OutStream.Write(_buffer, 0, 4);
        }
    
    
        // Writes a length-prefixed string to this stream in the BinaryWriter's
        // current Encoding. This method first writes the length of the string as 
        // a four-byte unsigned integer, and then writes that many characters 
        // to the stream.
        // 
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe virtual void Write(String value) 
        {
            if (value==null)
                throw new ArgumentNullException("value");
            Contract.EndContractBlock();

            int len = _encoding.GetByteCount(value);
            Write7BitEncodedInt(len);

            if (_largeByteBuffer == null) {
                _largeByteBuffer = new byte[LargeByteBufferSize];
                _maxChars = LargeByteBufferSize / _encoding.GetMaxByteCount(1);
            }

            if (len <= LargeByteBufferSize) {
                //Contract.Assert(len == _encoding.GetBytes(chars, 0, chars.Length, _largeByteBuffer, 0), "encoding's GetByteCount & GetBytes gave different answers!  encoding type: "+_encoding.GetType().Name);
                _encoding.GetBytes(value, 0, value.Length, _largeByteBuffer, 0);
                OutStream.Write(_largeByteBuffer, 0, len);
            }
            else {
                // Aggressively try to not allocate memory in this loop for
                // runtime performance reasons.  Use an Encoder to write out 
                // the string correctly (handling surrogates crossing buffer
                // boundaries properly).  
                int charStart = 0;
                int numLeft = value.Length;
#if _DEBUG
                int totalBytes = 0;
#endif
                while (numLeft > 0) {
                    // Figure out how many chars to process this round.
                    int charCount = (numLeft > _maxChars) ? _maxChars : numLeft;
                    int byteLen;
                    fixed(char* pChars = value) {
                        fixed(byte* pBytes = _largeByteBuffer) {
                            byteLen = _encoder.GetBytes(pChars + charStart, charCount, pBytes, LargeByteBufferSize, charCount == numLeft);
                        }
                    }
#if _DEBUG
                    totalBytes += byteLen;
                    Contract.Assert (totalBytes <= len && byteLen <= LargeByteBufferSize, "BinaryWriter::Write(String) - More bytes encoded than expected!");
#endif
                    OutStream.Write(_largeByteBuffer, 0, byteLen);
                    charStart += charCount;
                    numLeft -= charCount;
                }
#if _DEBUG
                Contract.Assert(totalBytes == len, "BinaryWriter::Write(String) - Didn't write out all the bytes!");
#endif
            }
        }
        
        protected void Write7BitEncodedInt(int value) {
            // Write out an int 7 bits at a time.  The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            uint v = (uint) value;   // support negative numbers
            while (v >= 0x80) {
                Write((byte) (v | 0x80));
                v >>= 7;
            }
            Write((byte)v);
        }
    }
}
