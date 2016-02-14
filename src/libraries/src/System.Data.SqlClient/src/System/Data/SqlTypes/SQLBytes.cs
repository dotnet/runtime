// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------


using System.Diagnostics;
using System.Data.Common;
using System.IO;
using Res = System.SR;


namespace System.Data.SqlTypes
{
    internal enum SqlBytesCharsState
    {
        Null = 0,
        Buffer = 1,
        //IntPtr = 2,
        Stream = 3,
    }

    public sealed class SqlBytes : System.Data.SqlTypes.INullable
    {
        // --------------------------------------------------------------
        //	  Data members
        // --------------------------------------------------------------

        // SqlBytes has five possible states
        // 1) SqlBytes is Null
        //		- m_stream must be null, m_lCuLen must be x_lNull.
        // 2) SqlBytes contains a valid buffer, 
        //		- m_rgbBuf must not be null,m_stream must be null
        // 3) SqlBytes contains a valid pointer
        //		- m_rgbBuf could be null or not,
        //			if not null, content is garbage, should never look into it.
        //      - m_stream must be null.
        // 4) SqlBytes contains a Stream
        //      - m_stream must not be null
        //      - m_rgbBuf could be null or not. if not null, content is garbage, should never look into it.
        //		- m_lCurLen must be x_lNull.
        // 5) SqlBytes contains a Lazy Materialized Blob (ie, StorageState.Delayed)
        //
        internal byte[] m_rgbBuf;   // Data buffer
        private long _lCurLen; // Current data length
        internal Stream m_stream;
        private SqlBytesCharsState _state;

        private byte[] _rgbWorkBuf;    // A 1-byte work buffer.

        // The max data length that we support at this time.
        private const long x_lMaxLen = (long)System.Int32.MaxValue;

        private const long x_lNull = -1L;

        // --------------------------------------------------------------
        //	  Constructor(s)
        // --------------------------------------------------------------

        // Public default constructor used for XML serialization
        public SqlBytes()
        {
            SetNull();
        }

        // Create a SqlBytes with an in-memory buffer
        public SqlBytes(byte[] buffer)
        {
            m_rgbBuf = buffer;
            m_stream = null;
            if (m_rgbBuf == null)
            {
                _state = SqlBytesCharsState.Null;
                _lCurLen = x_lNull;
            }
            else
            {
                _state = SqlBytesCharsState.Buffer;
                _lCurLen = (long)m_rgbBuf.Length;
            }

            _rgbWorkBuf = null;

            AssertValid();
        }

        // Create a SqlBytes from a SqlBinary
        public SqlBytes(SqlBinary value) : this(value.IsNull ? (byte[])null : value.Value)
        {
        }

        public SqlBytes(Stream s)
        {
            // Create a SqlBytes from a Stream
            m_rgbBuf = null;
            _lCurLen = x_lNull;
            m_stream = s;
            _state = (s == null) ? SqlBytesCharsState.Null : SqlBytesCharsState.Stream;

            _rgbWorkBuf = null;

            AssertValid();
        }



        // --------------------------------------------------------------
        //	  Public properties
        // --------------------------------------------------------------

        // INullable
        public bool IsNull
        {
            get
            {
                return _state == SqlBytesCharsState.Null;
            }
        }

        // Property: the in-memory buffer of SqlBytes
        //		Return Buffer even if SqlBytes is Null.
        public byte[] Buffer
        {
            get
            {
                if (FStream())
                {
                    CopyStreamToBuffer();
                }
                return m_rgbBuf;
            }
        }

        // Property: the actual length of the data
        public long Length
        {
            get
            {
                switch (_state)
                {
                    case SqlBytesCharsState.Null:
                        throw new SqlNullValueException();

                    case SqlBytesCharsState.Stream:
                        return m_stream.Length;

                    default:
                        return _lCurLen;
                }
            }
        }

        // Property: the max length of the data
        //		Return MaxLength even if SqlBytes is Null.
        //		When the buffer is also null, return -1.
        //		If containing a Stream, return -1.
        public long MaxLength
        {
            get
            {
                switch (_state)
                {
                    case SqlBytesCharsState.Stream:
                        return -1L;

                    default:
                        return (m_rgbBuf == null) ? -1L : (long)m_rgbBuf.Length;
                }
            }
        }

        // Property: get a copy of the data in a new byte[] array.
        public byte[] Value
        {
            get
            {
                byte[] buffer;

                switch (_state)
                {
                    case SqlBytesCharsState.Null:
                        throw new SqlNullValueException();

                    case SqlBytesCharsState.Stream:
                        if (m_stream.Length > x_lMaxLen)
                            throw new SqlTypeException(Res.GetString(Res.SqlMisc_BufferInsufficientMessage));
                        buffer = new byte[m_stream.Length];
                        if (m_stream.Position != 0)
                            m_stream.Seek(0, SeekOrigin.Begin);
                        m_stream.Read(buffer, 0, checked((int)m_stream.Length));
                        break;

                    default:
                        buffer = new byte[_lCurLen];
                        System.Buffer.BlockCopy(m_rgbBuf, 0, buffer, 0, (int)_lCurLen);
                        break;
                }

                return buffer;
            }
        }

        // class indexer
        public byte this[long offset]
        {
            get
            {
                if (offset < 0 || offset >= this.Length)
                    throw new ArgumentOutOfRangeException("offset");

                if (_rgbWorkBuf == null)
                    _rgbWorkBuf = new byte[1];

                Read(offset, _rgbWorkBuf, 0, 1);
                return _rgbWorkBuf[0];
            }
            set
            {
                if (_rgbWorkBuf == null)
                    _rgbWorkBuf = new byte[1];
                _rgbWorkBuf[0] = value;
                Write(offset, _rgbWorkBuf, 0, 1);
            }
        }

        public StorageState Storage
        {
            get
            {
                switch (_state)
                {
                    case SqlBytesCharsState.Null:
                        throw new SqlNullValueException();
                    case SqlBytesCharsState.Stream:
                        return StorageState.Stream;

                    case SqlBytesCharsState.Buffer:
                        return StorageState.Buffer;

                    default:
                        return StorageState.UnmanagedBuffer;
                }
            }
        }

        public Stream Stream
        {
            get
            {
                return FStream() ? m_stream : new StreamOnSqlBytes(this);
            }
            set
            {
                _lCurLen = x_lNull;
                m_stream = value;
                _state = (value == null) ? SqlBytesCharsState.Null : SqlBytesCharsState.Stream;
                AssertValid();
            }
        }

        // --------------------------------------------------------------
        //	  Public methods
        // --------------------------------------------------------------

        public void SetNull()
        {
            _lCurLen = x_lNull;
            m_stream = null;
            _state = SqlBytesCharsState.Null;

            AssertValid();
        }

        // Set the current length of the data
        // If the SqlBytes is Null, setLength will make it non-Null.
        public void SetLength(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException("value");

            if (FStream())
            {
                m_stream.SetLength(value);
            }
            else
            {
                // If there is a buffer, even the value of SqlBytes is Null,
                // still allow setting length to zero, which will make it not Null.
                // If the buffer is null, raise exception
                //
                if (null == m_rgbBuf)
                    throw new SqlTypeException(Res.GetString(Res.SqlMisc_NoBufferMessage));

                if (value > (long)m_rgbBuf.Length)
                    throw new ArgumentOutOfRangeException("value");

                else if (IsNull)
                    // At this point we know that value is small enough
                    // Go back in buffer mode
                    _state = SqlBytesCharsState.Buffer;

                _lCurLen = value;
            }

            AssertValid();
        }

        // Read data of specified length from specified offset into a buffer
        public long Read(long offset, byte[] buffer, int offsetInBuffer, int count)
        {
            if (IsNull)
                throw new SqlNullValueException();

            // Validate the arguments
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset > this.Length || offset < 0)
                throw new ArgumentOutOfRangeException("offset");

            if (offsetInBuffer > buffer.Length || offsetInBuffer < 0)
                throw new ArgumentOutOfRangeException("offsetInBuffer");

            if (count < 0 || count > buffer.Length - offsetInBuffer)
                throw new ArgumentOutOfRangeException("count");

            // Adjust count based on data length
            if (count > this.Length - offset)
                count = (int)(this.Length - offset);

            if (count != 0)
            {
                switch (_state)
                {
                    case SqlBytesCharsState.Stream:
                        if (m_stream.Position != offset)
                            m_stream.Seek(offset, SeekOrigin.Begin);
                        m_stream.Read(buffer, offsetInBuffer, count);
                        break;

                    default:
                        // ProjectK\Core doesn't support long-typed array indexers
                        Debug.Assert(offset < int.MaxValue);
                        System.Buffer.BlockCopy(m_rgbBuf, checked((int)offset), buffer, offsetInBuffer, count);
                        break;
                }
            }
            return count;
        }

        // Write data of specified length into the SqlBytes from specified offset
        public void Write(long offset, byte[] buffer, int offsetInBuffer, int count)
        {
            if (FStream())
            {
                if (m_stream.Position != offset)
                    m_stream.Seek(offset, SeekOrigin.Begin);
                m_stream.Write(buffer, offsetInBuffer, count);
            }
            else
            {
                // Validate the arguments
                if (buffer == null)
                    throw new ArgumentNullException("buffer");

                if (m_rgbBuf == null)
                    throw new SqlTypeException(Res.GetString(Res.SqlMisc_NoBufferMessage));

                if (offset < 0)
                    throw new ArgumentOutOfRangeException("offset");
                if (offset > m_rgbBuf.Length)
                    throw new SqlTypeException(Res.GetString(Res.SqlMisc_BufferInsufficientMessage));

                if (offsetInBuffer < 0 || offsetInBuffer > buffer.Length)
                    throw new ArgumentOutOfRangeException("offsetInBuffer");

                if (count < 0 || count > buffer.Length - offsetInBuffer)
                    throw new ArgumentOutOfRangeException("count");

                if (count > m_rgbBuf.Length - offset)
                    throw new SqlTypeException(Res.GetString(Res.SqlMisc_BufferInsufficientMessage));

                if (IsNull)
                {
                    // If NULL and there is buffer inside, we only allow writing from 
                    // offset zero.
                    //
                    if (offset != 0)
                        throw new SqlTypeException(Res.GetString(Res.SqlMisc_WriteNonZeroOffsetOnNullMessage));

                    // treat as if our current length is zero.
                    // Note this has to be done after all inputs are validated, so that
                    // we won't throw exception after this point.
                    //
                    _lCurLen = 0;
                    _state = SqlBytesCharsState.Buffer;
                }
                else if (offset > _lCurLen)
                {
                    // Don't allow writing from an offset that this larger than current length.
                    // It would leave uninitialized data in the buffer.
                    //
                    throw new SqlTypeException(Res.GetString(Res.SqlMisc_WriteOffsetLargerThanLenMessage));
                }

                if (count != 0)
                {
                    // ProjectK\Core doesn't support long-typed array indexers
                    Debug.Assert(offset < int.MaxValue);
                    System.Buffer.BlockCopy(buffer, offsetInBuffer, m_rgbBuf, checked((int)offset), count);

                    // If the last position that has been written is after
                    // the current data length, reset the length
                    if (_lCurLen < offset + count)
                        _lCurLen = offset + count;
                }
            }

            AssertValid();
        }

        public SqlBinary ToSqlBinary()
        {
            return IsNull ? SqlBinary.Null : new SqlBinary(Value);
        }

        // --------------------------------------------------------------
        //	  Conversion operators
        // --------------------------------------------------------------

        // Alternative method: ToSqlBinary()
        public static explicit operator SqlBinary(SqlBytes value)
        {
            return value.ToSqlBinary();
        }

        // Alternative method: constructor SqlBytes(SqlBinary)
        public static explicit operator SqlBytes(SqlBinary value)
        {
            return new SqlBytes(value);
        }

        // --------------------------------------------------------------
        //	  Private utility functions
        // --------------------------------------------------------------

        [System.Diagnostics.Conditional("DEBUG")]
        private void AssertValid()
        {
            Debug.Assert(_state >= SqlBytesCharsState.Null && _state <= SqlBytesCharsState.Stream);

            if (IsNull)
            {
            }
            else
            {
                Debug.Assert((_lCurLen >= 0 && _lCurLen <= x_lMaxLen) || FStream());
                Debug.Assert(FStream() || (m_rgbBuf != null && _lCurLen <= m_rgbBuf.Length));
                Debug.Assert(!FStream() || (_lCurLen == x_lNull));
            }
            Debug.Assert(_rgbWorkBuf == null || _rgbWorkBuf.Length == 1);
        }

        // Copy the data from the Stream to the array buffer.
        // If the SqlBytes doesn't hold a buffer or the buffer
        // is not big enough, allocate new byte array.
        private void CopyStreamToBuffer()
        {
            Debug.Assert(FStream());

            long lStreamLen = m_stream.Length;
            if (lStreamLen >= x_lMaxLen)
                throw new SqlTypeException(Res.GetString(Res.SqlMisc_WriteOffsetLargerThanLenMessage));

            if (m_rgbBuf == null || m_rgbBuf.Length < lStreamLen)
                m_rgbBuf = new byte[lStreamLen];

            if (m_stream.Position != 0)
                m_stream.Seek(0, SeekOrigin.Begin);

            m_stream.Read(m_rgbBuf, 0, (int)lStreamLen);
            m_stream = null;
            _lCurLen = lStreamLen;
            _state = SqlBytesCharsState.Buffer;

            AssertValid();
        }

        // whether the SqlBytes contains a pointer
        // whether the SqlBytes contains a Stream
        internal bool FStream()
        {
            return _state == SqlBytesCharsState.Stream;
        }

        // --------------------------------------------------------------
        //	  Static fields, properties
        // --------------------------------------------------------------

        // Get a Null instance. 
        // Since SqlBytes is mutable, have to be property and create a new one each time.
        public static SqlBytes Null
        {
            get
            {
                return new SqlBytes((byte[])null);
            }
        }
    } // class SqlBytes

    // StreamOnSqlBytes is a stream build on top of SqlBytes, and
    // provides the Stream interface. The purpose is to help users
    // to read/write SqlBytes object. After getting the stream from
    // SqlBytes, users could create a BinaryReader/BinaryWriter object
    // to easily read and write primitive types.
    internal sealed class StreamOnSqlBytes : Stream
    {
        // --------------------------------------------------------------
        //	  Data members
        // --------------------------------------------------------------

        private SqlBytes _sb;      // the SqlBytes object 
        private long _lPosition;

        // --------------------------------------------------------------
        //	  Constructor(s)
        // --------------------------------------------------------------

        internal StreamOnSqlBytes(SqlBytes sb)
        {
            _sb = sb;
            _lPosition = 0;
        }

        // --------------------------------------------------------------
        //	  Public properties
        // --------------------------------------------------------------

        // Always can read/write/seek, unless sb is null, 
        // which means the stream has been closed.

        public override bool CanRead
        {
            get
            {
                return _sb != null && !_sb.IsNull;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return _sb != null;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return _sb != null && (!_sb.IsNull || _sb.m_rgbBuf != null);
            }
        }

        public override long Length
        {
            get
            {
                CheckIfStreamClosed("get_Length");
                return _sb.Length;
            }
        }

        public override long Position
        {
            get
            {
                CheckIfStreamClosed("get_Position");
                return _lPosition;
            }
            set
            {
                CheckIfStreamClosed("set_Position");
                if (value < 0 || value > _sb.Length)
                    throw new ArgumentOutOfRangeException("value");
                else
                    _lPosition = value;
            }
        }

        // --------------------------------------------------------------
        //	  Public methods
        // --------------------------------------------------------------

        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckIfStreamClosed("Seek");

            long lPosition = 0;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (offset < 0 || offset > _sb.Length)
                        throw new ArgumentOutOfRangeException("offset");
                    _lPosition = offset;
                    break;

                case SeekOrigin.Current:
                    lPosition = _lPosition + offset;
                    if (lPosition < 0 || lPosition > _sb.Length)
                        throw new ArgumentOutOfRangeException("offset");
                    _lPosition = lPosition;
                    break;

                case SeekOrigin.End:
                    lPosition = _sb.Length + offset;
                    if (lPosition < 0 || lPosition > _sb.Length)
                        throw new ArgumentOutOfRangeException("offset");
                    _lPosition = lPosition;
                    break;

                default:
                    throw ADP.InvalidSeekOrigin("offset");
            }

            return _lPosition;
        }

        // The Read/Write/ReadByte/WriteByte simply delegates to SqlBytes
        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckIfStreamClosed("Read");

            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException("count");

            int iBytesRead = (int)_sb.Read(_lPosition, buffer, offset, count);
            _lPosition += iBytesRead;

            return iBytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckIfStreamClosed("Write");

            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException("count");

            _sb.Write(_lPosition, buffer, offset, count);
            _lPosition += count;
        }

        public override int ReadByte()
        {
            CheckIfStreamClosed("ReadByte");

            // If at the end of stream, return -1, rather than call SqlBytes.ReadByte,
            // which will throw exception. This is the behavior for Stream.
            //
            if (_lPosition >= _sb.Length)
                return -1;

            int ret = _sb[_lPosition];
            _lPosition++;
            return ret;
        }

        public override void WriteByte(byte value)
        {
            CheckIfStreamClosed("WriteByte");

            _sb[_lPosition] = value;
            _lPosition++;
        }

        public override void SetLength(long value)
        {
            CheckIfStreamClosed("SetLength");

            _sb.SetLength(value);
            if (_lPosition > value)
                _lPosition = value;
        }

        // Flush is a no-op for stream on SqlBytes, because they are all in memory
        public override void Flush()
        {
            if (_sb.FStream())
                _sb.m_stream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            // When m_sb is null, it means the stream has been closed, and
            // any opearation in the future should fail.
            // This is the only case that m_sb is null.
            try
            {
                _sb = null;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        // --------------------------------------------------------------
        //	  Private utility functions
        // --------------------------------------------------------------

        private bool FClosed()
        {
            return _sb == null;
        }

        private void CheckIfStreamClosed(string methodname)
        {
            if (FClosed())
                throw ADP.StreamClosed(methodname);
        }
    } // class StreamOnSqlBytes
} // namespace System.Data.SqlTypes
