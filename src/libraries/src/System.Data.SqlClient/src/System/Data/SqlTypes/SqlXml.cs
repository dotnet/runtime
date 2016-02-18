// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------


using System.IO;
using System.Xml;
using System.Data.Common;
using System.Diagnostics;
using System.Text;


namespace System.Data.SqlTypes
{
    public sealed class SqlXml : System.Data.SqlTypes.INullable
    {
        private bool _fNotNull; // false if null, the default ctor (plain 0) will make it Null
        private Stream _stream;
        private bool _firstCreateReader;

        private readonly static XmlReaderSettings s_defaultXmlReaderSettings = new XmlReaderSettings() { ConformanceLevel = ConformanceLevel.Fragment };
        private readonly static XmlReaderSettings s_defaultXmlReaderSettingsCloseInput = new XmlReaderSettings() { ConformanceLevel = ConformanceLevel.Fragment, CloseInput = true };
        private readonly static XmlReaderSettings s_defaultXmlReaderSettingsAsyncCloseInput = new XmlReaderSettings() { Async = true, ConformanceLevel = ConformanceLevel.Fragment, CloseInput = true };

        public SqlXml()
        {
            SetNull();
        }

        // constructor
        // construct a Null
        private SqlXml(bool fNull)
        {
            SetNull();
        }

        public SqlXml(XmlReader value)
        {
            // whoever pass in the XmlReader is responsible for closing it			  
            if (value == null)
            {
                SetNull();
            }
            else
            {
                _fNotNull = true;
                _firstCreateReader = true;
                _stream = CreateMemoryStreamFromXmlReader(value);
            }
        }

        public SqlXml(Stream value)
        {
            // whoever pass in the stream is responsible for closing it
            // similar to SqlBytes implementation
            if (value == null)
            {
                SetNull();
            }
            else
            {
                _firstCreateReader = true;
                _fNotNull = true;
                _stream = value;
            }
        }

        public XmlReader CreateReader()
        {
            if (IsNull)
            {
                throw new SqlNullValueException();
            }

            SqlXmlStreamWrapper stream = new SqlXmlStreamWrapper(_stream);

            // if it is the first time we create reader and stream does not support CanSeek, no need to reset position
            if ((!_firstCreateReader || stream.CanSeek) && stream.Position != 0)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }


            XmlReader r = CreateSqlXmlReader(stream);
            _firstCreateReader = false;
            return r;
        }

        internal static XmlReader CreateSqlXmlReader(Stream stream, bool closeInput = false, bool async = false)
        {
            XmlReaderSettings settingsToUse;
            if (closeInput)
            {
                settingsToUse = async ? s_defaultXmlReaderSettingsAsyncCloseInput : s_defaultXmlReaderSettingsCloseInput;
            }
            else
            {
                Debug.Assert(!async, "Currently we do not have pre-created settings for !closeInput+async");
                settingsToUse = s_defaultXmlReaderSettings;
            }

            return XmlReader.Create(stream, settingsToUse);
        }

        // NOTE: ReflectionPermission required here for accessing the non-public internal method CreateSqlReader() of System.Xml regardless of its grant set.

        // INullable
        public bool IsNull
        {
            get { return !_fNotNull; }
        }

        public string Value
        {
            get
            {
                if (IsNull)
                    throw new SqlNullValueException();

                StringWriter sw = new StringWriter((System.IFormatProvider)null);
                XmlWriterSettings writerSettings = new XmlWriterSettings();
                writerSettings.CloseOutput = false;     // don't close the memory stream
                writerSettings.ConformanceLevel = ConformanceLevel.Fragment;
                XmlWriter ww = XmlWriter.Create(sw, writerSettings);

                XmlReader reader = this.CreateReader();

                if (reader.ReadState == ReadState.Initial)
                    reader.Read();

                while (!reader.EOF)
                {
                    ww.WriteNode(reader, true);
                }
                ww.Flush();

                return sw.ToString();
            }
        }

        public static SqlXml Null
        {
            get
            {
                return new SqlXml(true);
            }
        }

        private void SetNull()
        {
            _fNotNull = false;
            _stream = null;
            _firstCreateReader = true;
        }

        private Stream CreateMemoryStreamFromXmlReader(XmlReader reader)
        {
            XmlWriterSettings writerSettings = new XmlWriterSettings();
            writerSettings.CloseOutput = false;     // don't close the memory stream
            writerSettings.ConformanceLevel = ConformanceLevel.Fragment;
            writerSettings.Encoding = Encoding.GetEncoding("utf-16");
            writerSettings.OmitXmlDeclaration = true;
            MemoryStream writerStream = new MemoryStream();
            XmlWriter ww = XmlWriter.Create(writerStream, writerSettings);

            if (reader.ReadState == ReadState.Closed)
                throw new InvalidOperationException(SQLResource.ClosedXmlReaderMessage);

            if (reader.ReadState == ReadState.Initial)
                reader.Read();

            while (!reader.EOF)
            {
                ww.WriteNode(reader, true);
            }
            ww.Flush();
            // set the stream to the beginning			
            writerStream.Seek(0, SeekOrigin.Begin);
            return writerStream;
        }
    } // SqlXml 		

    // two purposes for this class
    // 1) keep its internal position so one reader positions on the orginial stream 
    //	  will not interface with the other
    // 2) when xmlreader calls close, do not close the orginial stream
    //
    internal sealed class SqlXmlStreamWrapper : Stream
    {
        // --------------------------------------------------------------
        //	  Data members
        // --------------------------------------------------------------

        private Stream _stream;
        private long _lPosition;
        private bool _isClosed;

        // --------------------------------------------------------------
        //	  Constructor(s)
        // --------------------------------------------------------------

        internal SqlXmlStreamWrapper(Stream stream)
        {
            _stream = stream;
            Debug.Assert(_stream != null, "stream can not be null");
            _lPosition = 0;
            _isClosed = false;
        }

        // --------------------------------------------------------------
        //	  Public properties
        // --------------------------------------------------------------

        // Always can read/write/seek, unless stream is null, 
        // which means the stream has been closed.

        public override bool CanRead
        {
            get
            {
                if (IsStreamClosed())
                    return false;
                return _stream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                if (IsStreamClosed())
                    return false;
                return _stream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                if (IsStreamClosed())
                    return false;
                return _stream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                ThrowIfStreamClosed("get_Length");
                ThrowIfStreamCannotSeek("get_Length");
                return _stream.Length;
            }
        }

        public override long Position
        {
            get
            {
                ThrowIfStreamClosed("get_Position");
                ThrowIfStreamCannotSeek("get_Position");
                return _lPosition;
            }
            set
            {
                ThrowIfStreamClosed("set_Position");
                ThrowIfStreamCannotSeek("set_Position");
                if (value < 0 || value > _stream.Length)
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
            long lPosition = 0;

            ThrowIfStreamClosed("Seek");
            ThrowIfStreamCannotSeek("Seek");
            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (offset < 0 || offset > _stream.Length)
                        throw new ArgumentOutOfRangeException(nameof(offset));
                    _lPosition = offset;
                    break;

                case SeekOrigin.Current:
                    lPosition = _lPosition + offset;
                    if (lPosition < 0 || lPosition > _stream.Length)
                        throw new ArgumentOutOfRangeException(nameof(offset));
                    _lPosition = lPosition;
                    break;

                case SeekOrigin.End:
                    lPosition = _stream.Length + offset;
                    if (lPosition < 0 || lPosition > _stream.Length)
                        throw new ArgumentOutOfRangeException(nameof(offset));
                    _lPosition = lPosition;
                    break;

                default:
                    throw ADP.InvalidSeekOrigin("offset");
            }

            return _lPosition;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfStreamClosed("Read");
            ThrowIfStreamCannotRead("Read");

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (_stream.CanSeek && _stream.Position != _lPosition)
                _stream.Seek(_lPosition, SeekOrigin.Begin);

            int iBytesRead = (int)_stream.Read(buffer, offset, count);
            _lPosition += iBytesRead;

            return iBytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowIfStreamClosed("Write");
            ThrowIfStreamCannotWrite("Write");
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (_stream.CanSeek && _stream.Position != _lPosition)
                _stream.Seek(_lPosition, SeekOrigin.Begin);

            _stream.Write(buffer, offset, count);
            _lPosition += count;
        }

        public override int ReadByte()
        {
            ThrowIfStreamClosed("ReadByte");
            ThrowIfStreamCannotRead("ReadByte");
            // If at the end of stream, return -1, rather than call ReadByte,
            // which will throw exception. This is the behavior for Stream.
            //
            if (_stream.CanSeek && _lPosition >= _stream.Length)
                return -1;

            if (_stream.CanSeek && _stream.Position != _lPosition)
                _stream.Seek(_lPosition, SeekOrigin.Begin);

            int ret = _stream.ReadByte();
            _lPosition++;
            return ret;
        }

        public override void WriteByte(byte value)
        {
            ThrowIfStreamClosed("WriteByte");
            ThrowIfStreamCannotWrite("WriteByte");
            if (_stream.CanSeek && _stream.Position != _lPosition)
                _stream.Seek(_lPosition, SeekOrigin.Begin);
            _stream.WriteByte(value);
            _lPosition++;
        }

        public override void SetLength(long value)
        {
            ThrowIfStreamClosed("SetLength");
            ThrowIfStreamCannotSeek("SetLength");

            _stream.SetLength(value);
            if (_lPosition > value)
                _lPosition = value;
        }

        public override void Flush()
        {
            if (_stream != null)
                _stream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                // does not close the underline stream but mark itself as closed
                _isClosed = true;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private void ThrowIfStreamCannotSeek(string method)
        {
            if (!_stream.CanSeek)
                throw new NotSupportedException(SQLResource.InvalidOpStreamNonSeekable(method));
        }

        private void ThrowIfStreamCannotRead(string method)
        {
            if (!_stream.CanRead)
                throw new NotSupportedException(SQLResource.InvalidOpStreamNonReadable(method));
        }

        private void ThrowIfStreamCannotWrite(string method)
        {
            if (!_stream.CanWrite)
                throw new NotSupportedException(SQLResource.InvalidOpStreamNonWritable(method));
        }

        private void ThrowIfStreamClosed(string method)
        {
            if (IsStreamClosed())
                throw new ObjectDisposedException(SQLResource.InvalidOpStreamClosed(method));
        }

        private bool IsStreamClosed()
        {
            // Check the .CanRead and .CanWrite and .CanSeek properties to make sure stream is really closed

            if (_isClosed || _stream == null || (!_stream.CanRead && !_stream.CanWrite && !_stream.CanSeek))
                return true;
            else
                return false;
        }
    } // class SqlXmlStreamWrapper
}

