// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data.SqlTypes
{
    [XmlSchemaProvider("GetXsdType")]
    public sealed class SqlChars : INullable, IXmlSerializable, ISerializable
    {
        // --------------------------------------------------------------
        //      Data members
        // --------------------------------------------------------------

        // SqlChars has five possible states
        // 1) SqlChars is Null
        //        - m_stream must be null, m_lCuLen must be x_lNull
        // 2) SqlChars contains a valid buffer,
        //        - m_rgchBuf must not be null, and m_stream must be null
        // 3) SqlChars contains a valid pointer
        //        - m_rgchBuf could be null or not,
        //            if not null, content is garbage, should never look into it.
        //      - m_stream must be null.
        // 4) SqlChars contains a SqlStreamChars
        //      - m_stream must not be null
        //      - m_rgchBuf could be null or not. if not null, content is garbage, should never look into it.
        //        - m_lCurLen must be x_lNull.
        // 5) SqlChars contains a Lazy Materialized Blob (ie, StorageState.Delayed)
        //
        internal char[]? _rgchBuf;  // Data buffer
        private long _lCurLen; // Current data length
        internal SqlStreamChars? _stream;
        private SqlBytesCharsState _state;

        private char[]? _rgchWorkBuf;   // A 1-char work buffer.

        // The max data length that we support at this time.
        private const long x_lMaxLen = int.MaxValue;

        private const long x_lNull = -1L;

        // --------------------------------------------------------------
        //      Constructor(s)
        // --------------------------------------------------------------

        // Public default constructor used for XML serialization
        public SqlChars()
        {
            SetNull();
        }

        // Create a SqlChars with an in-memory buffer
        public SqlChars(char[]? buffer)
        {
            _rgchBuf = buffer;
            _stream = null;
            if (_rgchBuf == null)
            {
                _state = SqlBytesCharsState.Null;
                _lCurLen = x_lNull;
            }
            else
            {
                _state = SqlBytesCharsState.Buffer;
                _lCurLen = _rgchBuf.Length;
            }

            _rgchWorkBuf = null;

            AssertValid();
        }

        // Create a SqlChars from a SqlString
        public SqlChars(SqlString value) : this(value.IsNull ? null : value.Value.ToCharArray())
        {
        }

        // Create a SqlChars from a SqlStreamChars
        internal SqlChars(SqlStreamChars s)
        {
            _rgchBuf = null;
            _lCurLen = x_lNull;
            _stream = s;
            _state = (s == null) ? SqlBytesCharsState.Null : SqlBytesCharsState.Stream;

            _rgchWorkBuf = null;

            AssertValid();
        }

        // --------------------------------------------------------------
        //      Public properties
        // --------------------------------------------------------------

        // INullable
        public bool IsNull
        {
            get
            {
                return _state == SqlBytesCharsState.Null;
            }
        }

        // Property: the in-memory buffer of SqlChars
        //        Return Buffer even if SqlChars is Null.

        public char[]? Buffer
        {
            get
            {
                if (FStream())
                {
                    CopyStreamToBuffer();
                }
                return _rgchBuf;
            }
        }

        // Property: the actual length of the data
        public long Length
        {
            get
            {
                return _state switch
                {
                    SqlBytesCharsState.Null => throw new SqlNullValueException(),
                    SqlBytesCharsState.Stream => _stream!.Length,
                    _ => _lCurLen,
                };
            }
        }

        // Property: the max length of the data
        //        Return MaxLength even if SqlChars is Null.
        //        When the buffer is also null, return -1.
        //        If containing a Stream, return -1.

        public long MaxLength
        {
            get
            {
                return _state switch
                {
                    SqlBytesCharsState.Stream => -1L,
                    _ => (_rgchBuf == null) ? -1L : _rgchBuf.Length,
                };
            }
        }

        // Property: get a copy of the data in a new char[] array.

        public char[] Value
        {
            get
            {
                char[] buffer;

                switch (_state)
                {
                    case SqlBytesCharsState.Null:
                        throw new SqlNullValueException();

                    case SqlBytesCharsState.Stream:
                        if (_stream!.Length > x_lMaxLen)
                            throw new SqlTypeException(SR.SqlMisc_BufferInsufficientMessage);
                        buffer = new char[_stream.Length];
                        if (_stream.Position != 0)
                            _stream.Seek(0, SeekOrigin.Begin);
                        _stream.Read(buffer, 0, checked((int)_stream.Length));
                        break;

                    default:
                        buffer = new char[_lCurLen];
                        Array.Copy(_rgchBuf!, buffer, (int)_lCurLen);
                        break;
                }

                return buffer;
            }
        }

        // class indexer

        public char this[long offset]
        {
            get
            {
                ArgumentOutOfRangeException.ThrowIfNegative(offset);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(offset, Length);

                _rgchWorkBuf ??= new char[1];

                Read(offset, _rgchWorkBuf, 0, 1);
                return _rgchWorkBuf[0];
            }
            set
            {
                _rgchWorkBuf ??= new char[1];

                _rgchWorkBuf[0] = value;
                Write(offset, _rgchWorkBuf, 0, 1);
            }
        }

        internal SqlStreamChars Stream
        {
            get
            {
                return FStream() ? _stream! : new SqlStreamChars(this);
            }
            set
            {
                _lCurLen = x_lNull;
                _stream = value;
                _state = (value == null) ? SqlBytesCharsState.Null : SqlBytesCharsState.Stream;

                AssertValid();
            }
        }

        public StorageState Storage
        {
            get
            {
                return _state switch
                {
                    SqlBytesCharsState.Null => throw new SqlNullValueException(),
                    SqlBytesCharsState.Stream => StorageState.Stream,
                    SqlBytesCharsState.Buffer => StorageState.Buffer,
                    _ => StorageState.UnmanagedBuffer,
                };
            }
        }

        // --------------------------------------------------------------
        //      Public methods
        // --------------------------------------------------------------

        public void SetNull()
        {
            _lCurLen = x_lNull;
            _stream = null;
            _state = SqlBytesCharsState.Null;

            AssertValid();
        }

        // Set the current length of the data
        // If the SqlChars is Null, setLength will make it non-Null.
        public void SetLength(long value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);

            if (FStream())
            {
                _stream!.SetLength(value);
            }
            else
            {
                // If there is a buffer, even the value of SqlChars is Null,
                // still allow setting length to zero, which will make it not Null.
                // If the buffer is null, raise exception
                //
                if (null == _rgchBuf)
                    throw new SqlTypeException(SR.SqlMisc_NoBufferMessage);

                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, _rgchBuf.Length);

                if (IsNull)
                    // At this point we know that value is small enough
                    // Go back in buffer mode
                    _state = SqlBytesCharsState.Buffer;

                _lCurLen = value;
            }

            AssertValid();
        }

        // Read data of specified length from specified offset into a buffer

        public long Read(long offset, char[] buffer, int offsetInBuffer, int count)
        {
            if (IsNull)
                throw new SqlNullValueException();

            // Validate the arguments
            ArgumentNullException.ThrowIfNull(buffer);

            ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, Length);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);

            ArgumentOutOfRangeException.ThrowIfGreaterThan(offsetInBuffer, buffer.Length);
            ArgumentOutOfRangeException.ThrowIfNegative(offsetInBuffer);

            ArgumentOutOfRangeException.ThrowIfNegative(count);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - offsetInBuffer);

            // Adjust count based on data length
            if (count > Length - offset)
                count = (int)(Length - offset);

            if (count != 0)
            {
                switch (_state)
                {
                    case SqlBytesCharsState.Stream:
                        if (_stream!.Position != offset)
                            _stream.Seek(offset, SeekOrigin.Begin);
                        _stream.Read(buffer, offsetInBuffer, count);
                        break;

                    default:
                        Array.Copy(_rgchBuf!, offset, buffer, offsetInBuffer, count);
                        break;
                }
            }
            return count;
        }

        // Write data of specified length into the SqlChars from specified offset

        public void Write(long offset, char[] buffer, int offsetInBuffer, int count)
        {
            if (FStream())
            {
                if (_stream!.Position != offset)
                    _stream.Seek(offset, SeekOrigin.Begin);
                _stream.Write(buffer, offsetInBuffer, count);
            }
            else
            {
                // Validate the arguments
                ArgumentNullException.ThrowIfNull(buffer);

                if (_rgchBuf == null)
                    throw new SqlTypeException(SR.SqlMisc_NoBufferMessage);

                ArgumentOutOfRangeException.ThrowIfNegative(offset);

                if (offset > _rgchBuf.Length)
                    throw new SqlTypeException(SR.SqlMisc_BufferInsufficientMessage);

                ArgumentOutOfRangeException.ThrowIfNegative(offsetInBuffer);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(offsetInBuffer, buffer.Length);

                ArgumentOutOfRangeException.ThrowIfNegative(count);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - offsetInBuffer);

                if (count > _rgchBuf.Length - offset)
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
                    Array.Copy(buffer, offsetInBuffer, _rgchBuf, offset, count);

                    // If the last position that has been written is after
                    // the current data length, reset the length
                    if (_lCurLen < offset + count)
                        _lCurLen = offset + count;
                }
            }

            AssertValid();
        }

        public SqlString ToSqlString()
        {
            return IsNull ? SqlString.Null : new string(Value);
        }

        // --------------------------------------------------------------
        //      Conversion operators
        // --------------------------------------------------------------

        // Alternative method: ToSqlString()
        public static explicit operator SqlString(SqlChars value)
        {
            return value.ToSqlString();
        }

        // Alternative method: constructor SqlChars(SqlString)
        public static explicit operator SqlChars(SqlString value)
        {
            return new SqlChars(value);
        }

        // --------------------------------------------------------------
        //      Private utility functions
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
                Debug.Assert(FStream() || (_rgchBuf != null && _lCurLen <= _rgchBuf.Length));
                Debug.Assert(!FStream() || (_lCurLen == x_lNull));
            }
            Debug.Assert(_rgchWorkBuf == null || _rgchWorkBuf.Length == 1);
        }

        // whether the SqlChars contains a Stream
        internal bool FStream()
        {
            return _state == SqlBytesCharsState.Stream;
        }

        // Copy the data from the Stream to the array buffer.
        // If the SqlChars doesn't hold a buffer or the buffer
        // is not big enough, allocate new char array.
        private void CopyStreamToBuffer()
        {
            Debug.Assert(FStream());

            long lStreamLen = _stream!.Length;
            if (lStreamLen >= x_lMaxLen)
                throw new SqlTypeException(SR.SqlMisc_BufferInsufficientMessage);

            if (_rgchBuf == null || _rgchBuf.Length < lStreamLen)
                _rgchBuf = new char[lStreamLen];

            if (_stream.Position != 0)
                _stream.Seek(0, SeekOrigin.Begin);

            _stream.Read(_rgchBuf, 0, (int)lStreamLen);
            _stream = null;
            _lCurLen = lStreamLen;
            _state = SqlBytesCharsState.Buffer;

            AssertValid();
        }

        private void SetBuffer(char[] buffer)
        {
            _rgchBuf = buffer;
            _lCurLen = (_rgchBuf == null) ? x_lNull : _rgchBuf.Length;
            _stream = null;
            _state = (_rgchBuf == null) ? SqlBytesCharsState.Null : SqlBytesCharsState.Buffer;

            AssertValid();
        }

        // --------------------------------------------------------------
        //         XML Serialization
        // --------------------------------------------------------------


        XmlSchema? IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader r)
        {
            char[]? value;

            string? isNull = r.GetAttribute("nil", XmlSchema.InstanceNamespace);

            if (isNull != null && XmlConvert.ToBoolean(isNull))
            {
                // Read the next value.
                r.ReadElementString();
                SetNull();
            }
            else
            {
                value = r.ReadElementString().ToCharArray();
                SetBuffer(value);
            }
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            if (IsNull)
            {
                writer.WriteAttributeString("xsi", "nil", XmlSchema.InstanceNamespace, "true");
            }
            else
            {
                char[] value = Buffer!;
                writer.WriteString(new string(value, 0, (int)(Length)));
            }
        }

        public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
        {
            return new XmlQualifiedName("string", XmlSchema.Namespace);
        }

        // --------------------------------------------------------------
        //         Serialization using ISerializable
        // --------------------------------------------------------------

        // State information is not saved. The current state is converted to Buffer and only the underlying
        // array is serialized, except for Null, in which case this state is kept.
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        // --------------------------------------------------------------
        //      Static fields, properties
        // --------------------------------------------------------------

        // Get a Null instance.
        // Since SqlChars is mutable, have to be property and create a new one each time.
        public static SqlChars Null
        {
            get
            {
                return new SqlChars((char[]?)null);
            }
        }
    } // class SqlChars

    // SqlStreamChars is a stream build on top of SqlChars, and
    // provides the Stream interface. The purpose is to help users
    // to read/write SqlChars object.
    internal sealed class SqlStreamChars
    {
        // --------------------------------------------------------------
        //      Data members
        // --------------------------------------------------------------

        private readonly SqlChars _sqlchars;        // the SqlChars object
        private long _lPosition;

        // --------------------------------------------------------------
        //      Constructor(s)
        // --------------------------------------------------------------

        internal SqlStreamChars(SqlChars s)
        {
            _sqlchars = s;
            _lPosition = 0;
        }

        // --------------------------------------------------------------
        //      Public properties
        // --------------------------------------------------------------

        public long Length
        {
            get
            {
                CheckIfStreamClosed("get_Length");
                return _sqlchars.Length;
            }
        }

        public long Position
        {
            get
            {
                CheckIfStreamClosed("get_Position");
                return _lPosition;
            }
            set
            {
                CheckIfStreamClosed("set_Position");
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, _sqlchars.Length);
                _lPosition = value;
            }
        }

        // --------------------------------------------------------------
        //      Public methods
        // --------------------------------------------------------------

        public long Seek(long offset, SeekOrigin origin)
        {
            CheckIfStreamClosed();

            long lPosition;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (offset < 0 || offset > _sqlchars.Length)
                        throw ADP.ArgumentOutOfRange(nameof(offset));
                    _lPosition = offset;
                    break;

                case SeekOrigin.Current:
                    lPosition = _lPosition + offset;
                    if (lPosition < 0 || lPosition > _sqlchars.Length)
                        throw ADP.ArgumentOutOfRange(nameof(offset));
                    _lPosition = lPosition;
                    break;

                case SeekOrigin.End:
                    lPosition = _sqlchars.Length + offset;
                    if (lPosition < 0 || lPosition > _sqlchars.Length)
                        throw ADP.ArgumentOutOfRange(nameof(offset));
                    _lPosition = lPosition;
                    break;

                default:
                    throw ADP.ArgumentOutOfRange(nameof(offset));
            }

            return _lPosition;
        }

        // The Read/Write/Readchar/Writechar simply delegates to SqlChars
        public int Read(char[] buffer, int offset, int count)
        {
            CheckIfStreamClosed();

            ArgumentNullException.ThrowIfNull(buffer);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, buffer.Length);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - offset);

            int icharsRead = (int)_sqlchars.Read(_lPosition, buffer, offset, count);
            _lPosition += icharsRead;

            return icharsRead;
        }

        public void Write(char[] buffer, int offset, int count)
        {
            CheckIfStreamClosed();

            ArgumentNullException.ThrowIfNull(buffer);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, buffer.Length);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - offset);

            _sqlchars.Write(_lPosition, buffer, offset, count);
            _lPosition += count;
        }

        public void SetLength(long value)
        {
            CheckIfStreamClosed();

            _sqlchars.SetLength(value);
            if (_lPosition > value)
                _lPosition = value;
        }

        // --------------------------------------------------------------
        //      Private utility functions
        // --------------------------------------------------------------

        private bool FClosed()
        {
            return _sqlchars == null;
        }

        private void CheckIfStreamClosed([CallerMemberName] string methodname = "")
        {
            if (FClosed())
                throw ADP.StreamClosed(methodname);
        }
    } // class SqlStreamChars
} // namespace System.Data.SqlTypes
