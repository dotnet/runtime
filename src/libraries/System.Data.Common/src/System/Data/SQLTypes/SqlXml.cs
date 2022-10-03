// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.SqlTypes
{
    [XmlSchemaProvider("GetXsdType")]
    public sealed class SqlXml : INullable, IXmlSerializable
    {
        private static readonly Func<Stream, XmlReaderSettings, XmlParserContext?, XmlReader> s_sqlReaderDelegate = CreateSqlReaderDelegate();
        private static readonly XmlReaderSettings s_defaultXmlReaderSettings = new XmlReaderSettings() { ConformanceLevel = ConformanceLevel.Fragment };
        private static readonly XmlReaderSettings s_defaultXmlReaderSettingsCloseInput = new XmlReaderSettings() { ConformanceLevel = ConformanceLevel.Fragment, CloseInput = true };
        private static MethodInfo? s_createSqlReaderMethodInfo;
        private MethodInfo? _createSqlReaderMethodInfo;

        private bool _fNotNull; // false if null, the default ctor (plain 0) will make it Null
        private Stream? _stream;
        private bool _firstCreateReader;

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

        public SqlXml(XmlReader? value)
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

        public SqlXml(Stream? value)
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

            SqlXmlStreamWrapper stream = new SqlXmlStreamWrapper(_stream!);

            // if it is the first time we create reader and stream does not support CanSeek, no need to reset position
            if ((!_firstCreateReader || stream.CanSeek) && stream.Position != 0)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            // NOTE: Maintaining createSqlReaderMethodInfo private field member to preserve the serialization of the class
            _createSqlReaderMethodInfo ??= CreateSqlReaderMethodInfo;
            Debug.Assert(_createSqlReaderMethodInfo != null, "MethodInfo reference for XmlReader.CreateSqlReader should not be null.");

            XmlReader r = CreateSqlXmlReader(stream);
            _firstCreateReader = false;
            return r;
        }

        internal static XmlReader CreateSqlXmlReader(Stream stream, bool closeInput = false, bool throwTargetInvocationExceptions = false)
        {
            // Call the internal delegate
            XmlReaderSettings settingsToUse = closeInput ? s_defaultXmlReaderSettingsCloseInput : s_defaultXmlReaderSettings;
            try
            {
                return s_sqlReaderDelegate(stream, settingsToUse, null);
            }
            // For particular callers, we need to wrap all exceptions inside a TargetInvocationException to simulate calling CreateSqlReader via MethodInfo.Invoke
            catch (Exception ex)
            {
                if ((!throwTargetInvocationExceptions) || (!ADP.IsCatchableExceptionType(ex)))
                {
                    throw;
                }
                else
                {
                    throw new TargetInvocationException(ex);
                }
            }
        }

        private static Func<Stream, XmlReaderSettings, XmlParserContext?, XmlReader> CreateSqlReaderDelegate()
        {
            Debug.Assert(CreateSqlReaderMethodInfo != null, "MethodInfo reference for XmlReader.CreateSqlReader should not be null.");

            return CreateSqlReaderMethodInfo.CreateDelegate<Func<Stream, XmlReaderSettings, XmlParserContext?, XmlReader>>();
        }

        private static MethodInfo CreateSqlReaderMethodInfo =>
            s_createSqlReaderMethodInfo ??= typeof(System.Xml.XmlReader).GetMethod("CreateSqlReader", BindingFlags.Static | BindingFlags.NonPublic)!;

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

                StringWriter sw = new StringWriter((System.IFormatProvider)null!);
                XmlWriterSettings writerSettings = new XmlWriterSettings();
                writerSettings.CloseOutput = false;     // don't close the memory stream
                writerSettings.ConformanceLevel = ConformanceLevel.Fragment;
                XmlWriter ww = XmlWriter.Create(sw, writerSettings);

                XmlReader reader = CreateReader();

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

        private static Stream CreateMemoryStreamFromXmlReader(XmlReader reader)
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

        XmlSchema? IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader r)
        {
            string? isNull = r.GetAttribute("nil", XmlSchema.InstanceNamespace);

            if (isNull != null && XmlConvert.ToBoolean(isNull))
            {
                // Read the next value.
                r.ReadInnerXml();
                SetNull();
            }
            else
            {
                _fNotNull = true;
                _firstCreateReader = true;

                _stream = new MemoryStream();
                StreamWriter sw = new StreamWriter(_stream);
                sw.Write(r.ReadInnerXml());
                sw.Flush();

                if (_stream.CanSeek)
                    _stream.Seek(0, SeekOrigin.Begin);
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
                // Instead of the WriteRaw use the WriteNode. As Tds sends a binary stream - Create a XmlReader to convert
                // get the Xml string value from the binary and call WriteNode to pass that out to the XmlWriter.
                XmlReader reader = CreateReader();
                if (reader.ReadState == ReadState.Initial)
                    reader.Read();

                while (!reader.EOF)
                {
                    writer.WriteNode(reader, true);
                }
            }
            writer.Flush();
        }

        public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
        {
            return new XmlQualifiedName("anyType", XmlSchema.Namespace);
        }
    } // SqlXml

    // two purposes for this class
    // 1) keep its internal position so one reader positions on the original stream
    //    will not interface with the other
    // 2) when xmlreader calls close, do not close the original stream
    //
    internal sealed class SqlXmlStreamWrapper : Stream
    {
        // --------------------------------------------------------------
        //      Data members
        // --------------------------------------------------------------

        private readonly Stream _stream;
        private long _lPosition;
        private bool _isClosed;

        // --------------------------------------------------------------
        //      Constructor(s)
        // --------------------------------------------------------------

        internal SqlXmlStreamWrapper(Stream stream)
        {
            _stream = stream;
            Debug.Assert(_stream != null, "stream can not be null");
            _lPosition = 0;
            _isClosed = false;
        }

        // --------------------------------------------------------------
        //      Public properties
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
        //      Public methods
        // --------------------------------------------------------------

        public override long Seek(long offset, SeekOrigin origin)
        {
            long lPosition = 0;

            ThrowIfStreamClosed(nameof(Seek));
            ThrowIfStreamCannotSeek(nameof(Seek));
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
                    throw ADP.InvalidSeekOrigin(nameof(offset));
            }

            return _lPosition;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfStreamClosed(nameof(Read));
            ThrowIfStreamCannotRead(nameof(Read));

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (_stream.CanSeek && _stream.Position != _lPosition)
                _stream.Seek(_lPosition, SeekOrigin.Begin);

            int iBytesRead = _stream.Read(buffer, offset, count);
            _lPosition += iBytesRead;

            return iBytesRead;
        }

        // Duplicate the Read(byte[]) logic here instead of refactoring both to use Spans
        // in case the backing _stream doesn't override Read(Span).
        public override int Read(Span<byte> buffer)
        {
            ThrowIfStreamClosed(nameof(Read));
            ThrowIfStreamCannotRead(nameof(Read));

            if (_stream.CanSeek && _stream.Position != _lPosition)
                _stream.Seek(_lPosition, SeekOrigin.Begin);

            int iBytesRead = _stream.Read(buffer);
            _lPosition += iBytesRead;

            return iBytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowIfStreamClosed(nameof(Write));
            ThrowIfStreamCannotWrite(nameof(Write));

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
            ThrowIfStreamClosed(nameof(ReadByte));
            ThrowIfStreamCannotRead(nameof(ReadByte));
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
            ThrowIfStreamClosed(nameof(WriteByte));
            ThrowIfStreamCannotWrite(nameof(WriteByte));
            if (_stream.CanSeek && _stream.Position != _lPosition)
                _stream.Seek(_lPosition, SeekOrigin.Begin);
            _stream.WriteByte(value);
            _lPosition++;
        }

        public override void SetLength(long value)
        {
            ThrowIfStreamClosed(nameof(SetLength));
            ThrowIfStreamCannotSeek(nameof(SetLength));

            _stream.SetLength(value);
            if (_lPosition > value)
                _lPosition = value;
        }

        public override void Flush()
        {
            _stream?.Flush();
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
