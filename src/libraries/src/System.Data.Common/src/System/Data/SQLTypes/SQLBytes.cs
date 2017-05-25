// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Data.Common;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Runtime.CompilerServices;

namespace System.Data.SqlTypes
{
    internal enum SqlBytesCharsState
    {
        Null = 0,
        Buffer = 1,
        //IntPtr = 2,
        Stream = 3,
    }

    [XmlSchemaProvider("GetXsdType")]
    public sealed class SqlBytes : INullable, IXmlSerializable, ISerializable
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
        internal byte[] _rgbBuf;   // Data buffer
        private long _lCurLen; // Current data length
        internal Stream _stream;
        private SqlBytesCharsState _state;

        private byte[] _rgbWorkBuf;    // A 1-byte work buffer.

        // The max data length that we support at this time.
        private const long x_lMaxLen = System.Int32.MaxValue;

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
            _rgbBuf = buffer;
            _stream = null;
            if (_rgbBuf == null)
            {
                _state = SqlBytesCharsState.Null;
                _lCurLen = x_lNull;
            }
            else
            {
                _state = SqlBytesCharsState.Buffer;
                _lCurLen = _rgbBuf.Length;
            }

            _rgbWorkBuf = null;

            AssertValid();
        }

        // Create a SqlBytes from a SqlBinary
        public SqlBytes(SqlBinary value) : this(value.IsNull ? null : value.Value)
        {
        }

        public SqlBytes(Stream s)
        {
            // Create a SqlBytes from a Stream
            _rgbBuf = null;
            _lCurLen = x_lNull;
            _stream = s;
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
                return _rgbBuf;
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
                        return _stream.Length;

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
                        return (_rgbBuf == null) ? -1L : _rgbBuf.Length;
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
                        if (_stream.Length > x_lMaxLen)
                            throw new SqlTypeException(SR.SqlMisc_BufferInsufficientMessage);
                        buffer = new byte[_stream.Length];
                        if (_stream.Position != 0)
                            _stream.Seek(0, SeekOrigin.Begin);
                        _stream.Read(buffer, 0, checked((int)_stream.Length));
                        break;

                    default:
                        buffer = new byte[_lCurLen];
                        Array.Copy(_rgbBuf, 0, buffer, 0, (int)_lCurLen);
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
                if (offset < 0 || offset >= Length)
                    throw new ArgumentOutOfRangeException(nameof(offset));

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
                return FStream() ? _stream : new StreamOnSqlBytes(this);
            }
            set
            {
                _lCurLen = x_lNull;
                _stream = value;
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
            _stream = null;
            _state = SqlBytesCharsState.Null;

            AssertValid();
        }

        // Set the current length of the data
        // If the SqlBytes is Null, setLength will make it non-Null.
        public void SetLength(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            if (FStream())
            {
                _stream.SetLength(value);
            }
            else
            {
                // If there is a buffer, even the value of SqlBytes is Null,
                // still allow setting length to zero, which will make it not Null.
                // If the buffer is null, raise exception
                //
                if (null == _rgbBuf)
                    throw new SqlTypeException(SR.SqlMisc_NoBufferMessage);

                if (value > _rgbBuf.Length)
                    throw new ArgumentOutOfRangeException(nameof(value));

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
                throw new ArgumentNullException(nameof(buffer));

            if (offset > Length || offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (offsetInBuffer > buffer.Length || offsetInBuffer < 0)
                throw new ArgumentOutOfRangeException(nameof(offsetInBuffer));

            if (count < 0 || count > buffer.Length - offsetInBuffer)
                throw new ArgumentOutOfRangeException(nameof(count));

            // Adjust count based on data length
            if (count > Length - offset)
                count = (int)(Length - offset);

            if (count != 0)
            {
                switch (_state)
                {
                    case SqlBytesCharsState.Stream:
                        if (_stream.Position != offset)
                            _stream.Seek(offset, SeekOrigin.Begin);
                        _stream.Read(buffer, offsetInBuffer, count);
                        break;

                    default:
                        Array.Copy(_rgbBuf, offset, buffer, offsetInBuffer, count);
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
                if (_stream.Position != offset)
                    _stream.Seek(offset, SeekOrigin.Begin);
                _stream.Write(buffer, offsetInBuffer, count);
            }
            else
            {
                // Validate the arguments
                if (buffer == null)
                    throw new ArgumentNullException(nameof(buffer));

                if (_rgbBuf == null)
                    throw new SqlTypeException(SR.SqlMisc_NoBufferMessage);

                if (offset < 0)
                    throw new ArgumentOutOfRangeException(nameof(offset));
                if (offset > _rgbBuf.Length)
                    throw new SqlTypeException(SR.SqlMisc_BufferInsufficientMessage);

                if (offsetInBuffer < 0 || offsetInBuffer > buffer.Length)
                    throw new ArgumentOutOfRangeException(nameof(offsetInBuffer));

                if (count < 0 || count > buffer.Length - offsetInBuffer)
                    throw new ArgumentOutOfRangeException(nameof(count));

                if (count > _rgbBuf.Length - offset)
                    throw new SqlTypeException(SR.SqlMisc_BufferInsufficientMessage);

                if (IsNull)
                {
                    // If NULL and there is buffer inside, we only allow writing from 
                    // offset zero.
                    //
                    if (offset != 0)
                        throw new SqlTypeException(SR.SqlMisc_WriteNonZeroOffsetOnNullMessage);

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
                    throw new SqlTypeException(SR.SqlMisc_WriteOffsetLargerThanLenMessage);
                }

                if (count != 0)
                {
                    Array.Copy(buffer, offsetInBuffer, _rgbBuf, offset, count);

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

        [Conditional("DEBUG")]
        private void AssertValid()
        {
            Debug.Assert(_state >= SqlBytesCharsState.Null && _state <= SqlBytesCharsState.Stream);

            if (IsNull)
            {
            }
            else
            {
                Debug.Assert((_lCurLen >= 0 && _lCurLen <= x_lMaxLen) || FStream());
                Debug.Assert(FStream() || (_rgbBuf != null && _lCurLen <= _rgbBuf.Length));
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

            long lStreamLen = _stream.Length;
            if (lStreamLen >= x_lMaxLen)
                throw new SqlTypeException(SR.SqlMisc_WriteOffsetLargerThanLenMessage);

            if (_rgbBuf == null || _rgbBuf.Length < lStreamLen)
                _rgbBuf = new byte[lStreamLen];

            if (_stream.Position != 0)
                _stream.Seek(0, SeekOrigin.Begin);

            _stream.Read(_rgbBuf, 0, (int)lStreamLen);
            _stream = null;
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

        private void SetBuffer(byte[] buffer)
        {
            _rgbBuf = buffer;
            _lCurLen = (_rgbBuf == null) ? x_lNull : _rgbBuf.Length;
            _stream = null;
            _state = (_rgbBuf == null) ? SqlBytesCharsState.Null : SqlBytesCharsState.Buffer;

            AssertValid();
        }

        // --------------------------------------------------------------
        // 		XML Serialization
        // --------------------------------------------------------------

        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader r)
        {
            byte[] value = null;

            string isNull = r.GetAttribute("nil", XmlSchema.InstanceNamespace);

            if (isNull != null && XmlConvert.ToBoolean(isNull))
            {
                // Read the next value.
                r.ReadElementString();
                SetNull();
            }
            else
            {
                string base64 = r.ReadElementString();
                if (base64 == null)
                {
                    value = Array.Empty<byte>();
                }
                else
                {
                    base64 = base64.Trim();
                    if (base64.Length == 0)
                        value = Array.Empty<byte>();
                    else
                        value = Convert.FromBase64String(base64);
                }
            }

            SetBuffer(value);
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            if (IsNull)
            {
                writer.WriteAttributeString("xsi", "nil", XmlSchema.InstanceNamespace, "true");
            }
            else
            {
                byte[] value = Buffer;
                writer.WriteString(Convert.ToBase64String(value, 0, (int)(Length)));
            }
        }

        public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
        {
            return new XmlQualifiedName("base64Binary", XmlSchema.Namespace);
        }


        // --------------------------------------------------------------
        // 		Serialization using ISerializable
        // --------------------------------------------------------------

        // State information is not saved. The current state is converted to Buffer and only the underlying
        // array is serialized, except for Null, in which case this state is kept.
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
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
                return _sb != null && (!_sb.IsNull || _sb._rgbBuf != null);
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
                    throw new ArgumentOutOfRangeException(nameof(value));
                else
                    _lPosition = value;
            }
        }

        // --------------------------------------------------------------
        //	  Public methods
        // --------------------------------------------------------------

        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckIfStreamClosed();

            long lPosition = 0;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (offset < 0 || offset > _sb.Length)
                        throw new ArgumentOutOfRangeException(nameof(offset));
                    _lPosition = offset;
                    break;

                case SeekOrigin.Current:
                    lPosition = _lPosition + offset;
                    if (lPosition < 0 || lPosition > _sb.Length)
                        throw new ArgumentOutOfRangeException(nameof(offset));
                    _lPosition = lPosition;
                    break;

                case SeekOrigin.End:
                    lPosition = _sb.Length + offset;
                    if (lPosition < 0 || lPosition > _sb.Length)
                        throw new ArgumentOutOfRangeException(nameof(offset));
                    _lPosition = lPosition;
                    break;

                default:
                    throw ADP.InvalidSeekOrigin(nameof(offset));
            }

            return _lPosition;
        }

        // The Read/Write/ReadByte/WriteByte simply delegates to SqlBytes
        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckIfStreamClosed();

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));

            int iBytesRead = (int)_sb.Read(_lPosition, buffer, offset, count);
            _lPosition += iBytesRead;

            return iBytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckIfStreamClosed();

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));

            _sb.Write(_lPosition, buffer, offset, count);
            _lPosition += count;
        }

        public override int ReadByte()
        {
            CheckIfStreamClosed();

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
            CheckIfStreamClosed();

            _sb[_lPosition] = value;
            _lPosition++;
        }

        public override void SetLength(long value)
        {
            CheckIfStreamClosed();

            _sb.SetLength(value);
            if (_lPosition > value)
                _lPosition = value;
        }

        // Flush is a no-op for stream on SqlBytes, because they are all in memory
        public override void Flush()
        {
            if (_sb.FStream())
                _sb._stream.Flush();
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

        private void CheckIfStreamClosed([CallerMemberName] string methodname = "")
        {
            if (FClosed())
                throw ADP.StreamClosed(methodname);
        }
    } // class StreamOnSqlBytes
} // namespace System.Data.SqlTypes
