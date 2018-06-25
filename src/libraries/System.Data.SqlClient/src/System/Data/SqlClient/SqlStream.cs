// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

namespace System.Data.SqlClient
{
    sealed internal class SqlStream : Stream
    {
        private SqlDataReader _reader; // reader we will stream off
        private int _columnOrdinal;
        private long _bytesCol;
        private int _bom;
        private byte[] _bufferedData;
        private bool _processAllRows;
        private bool _advanceReader;
        private bool _readFirstRow = false;
        private bool _endOfColumn = false;

        internal SqlStream(SqlDataReader reader, bool addByteOrderMark, bool processAllRows) :
            this(0, reader, addByteOrderMark, processAllRows, true)
        {
        }

        internal SqlStream(int columnOrdinal, SqlDataReader reader, bool addByteOrderMark, bool processAllRows, bool advanceReader)
        {
            _columnOrdinal = columnOrdinal;
            _reader = reader;
            _bom = addByteOrderMark ? 0xfeff : 0;
            _processAllRows = processAllRows;
            _advanceReader = advanceReader;
        }

        override public bool CanRead
        {
            get
            {
                return true;
            }
        }

        override public bool CanSeek
        {
            get
            {
                return false;
            }
        }

        override public bool CanWrite
        {
            get
            {
                return false;
            }
        }

        override public long Length
        {
            get
            {
                throw ADP.NotSupported();
            }
        }

        override public long Position
        {
            get
            {
                throw ADP.NotSupported();
            }
            set
            {
                throw ADP.NotSupported();
            }
        }

        override protected void Dispose(bool disposing)
        {
            try
            {
                if (disposing && _advanceReader && _reader != null && !_reader.IsClosed)
                {
                    _reader.Close();
                }
                _reader = null;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        override public void Flush()
        {
            throw ADP.NotSupported();
        }

        override public int Read(byte[] buffer, int offset, int count)
        {
            int intCount = 0;
            int cBufferedData = 0;

            if ((null == _reader))
            {
                throw ADP.StreamClosed();
            }
            if (null == buffer)
            {
                throw ADP.ArgumentNull(nameof(buffer));
            }
            if ((offset < 0) || (count < 0))
            {
                throw ADP.ArgumentOutOfRange(string.Empty, (offset < 0 ? nameof(offset) : nameof(count)));
            }
            if (buffer.Length - offset < count)
            {
                throw ADP.ArgumentOutOfRange(nameof(count));
            }

            // Need to find out if we should add byte order mark or not. 
            // We need to add this if we are getting ntext xml, not if we are getting binary xml
            // Binary Xml always begins with the bytes 0xDF and 0xFF
            // If we aren't getting these, then we are getting unicode xml
            if (_bom > 0)
            {
                // Read and buffer the first two bytes
                _bufferedData = new byte[2];
                cBufferedData = ReadBytes(_bufferedData, 0, 2);
                // Check to se if we should add the byte order mark
                if ((cBufferedData < 2) || ((_bufferedData[0] == 0xDF) && (_bufferedData[1] == 0xFF)))
                {
                    _bom = 0;
                }
                while (count > 0)
                {
                    if (_bom > 0)
                    {
                        buffer[offset] = (byte)_bom;
                        _bom >>= 8;
                        offset++;
                        count--;
                        intCount++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (cBufferedData > 0)
            {
                while (count > 0)
                {
                    buffer[offset++] = _bufferedData[0];
                    intCount++;
                    count--;
                    if ((cBufferedData > 1) && (count > 0))
                    {
                        buffer[offset++] = _bufferedData[1];
                        intCount++;
                        count--;
                        break;
                    }
                }
                _bufferedData = null;
            }

            intCount += ReadBytes(buffer, offset, count);

            return intCount;
        }

        private static bool AdvanceToNextRow(SqlDataReader reader)
        {
            Debug.Assert(reader != null && !reader.IsClosed);

            // this method skips empty result sets
            do
            {
                if (reader.Read())
                {
                    return true;
                }
            } while (reader.NextResult());

            // no more rows
            return false;
        }

        private int ReadBytes(byte[] buffer, int offset, int count)
        {
            bool gotData = true;
            int intCount = 0;
            int cb = 0;

            if (_reader.IsClosed || _endOfColumn)
            {
                return 0;
            }
            try
            {
                while (count > 0)
                {
                    // if no bytes were read, get the next row
                    if (_advanceReader && (0 == _bytesCol))
                    {
                        gotData = false;

                        if (_readFirstRow && !_processAllRows)
                        {
                            // for XML column, stop processing after the first row
                            // no op here - reader is closed after the end of this loop
                        }
                        else if (AdvanceToNextRow(_reader))
                        {
                            _readFirstRow = true;

                            if (_reader.IsDBNull(_columnOrdinal))
                            {
                                // Handle row with DBNULL as empty data
                                // for XML column, processing is stopped on the next loop since _readFirstRow is true
                                continue;
                            }

                            // the value is not null, read it
                            gotData = true;
                        }
                        // else AdvanceToNextRow has returned false - no more rows or result sets remained, stop processing
                    }

                    if (gotData)
                    {
                        cb = (int)_reader.GetBytesInternal(_columnOrdinal, _bytesCol, buffer, offset, count);

                        if (cb < count)
                        {
                            _bytesCol = 0;
                            gotData = false;
                            if (!_advanceReader)
                            {
                                _endOfColumn = true;
                            }
                        }
                        else
                        {
                            Debug.Assert(cb == count);
                            _bytesCol += cb;
                        }

                        // we are guaranteed that cb is < Int32.Max since we always pass in count which is of type Int32 to
                        // our getbytes interface
                        count -= (int)cb;
                        offset += (int)cb;
                        intCount += (int)cb;
                    }
                    else
                    {
                        break; // no more data available, we are done
                    }
                }
                if (!gotData && _advanceReader)
                {
                    _reader.Close();    // Need to close the reader if we are done reading
                }
            }
            catch (Exception e)
            {
                if (_advanceReader && ADP.IsCatchableExceptionType(e))
                {
                    _reader.Close();
                }
                throw;
            }

            return intCount;
        }

        internal XmlReader ToXmlReader(bool async = false)
        {
            return SqlTypeWorkarounds.SqlXmlCreateSqlXmlReader(this, closeInput: true, async: async);
        }

        override public long Seek(long offset, SeekOrigin origin)
        {
            throw ADP.NotSupported();
        }

        override public void SetLength(long value)
        {
            throw ADP.NotSupported();
        }

        override public void Write(byte[] buffer, int offset, int count)
        {
            throw ADP.NotSupported();
        }
    }


    // XmlTextReader does not read all the bytes off the network buffers, so we have to cache it here in the random access
    // case. This causes double buffering and is a perf hit, but this is not the high perf way for accessing this type of data.
    // In the case of sequential access, we do not have to do any buffering since the XmlTextReader we return can become 
    // invalid as soon as we move off the current column.
    sealed internal class SqlCachedStream : Stream
    {
        private int _currentPosition;   // Position within the current array byte
        private int _currentArrayIndex; // Index into the _cachedBytes List
        private List<byte[]> _cachedBytes;
        private long _totalLength;

        // Reads off from the network buffer and caches bytes. Only reads one column value in the current row.
        internal SqlCachedStream(SqlCachedBuffer sqlBuf)
        {
            _cachedBytes = sqlBuf.CachedBytes;
        }

        override public bool CanRead
        {
            get
            {
                return true;
            }
        }

        override public bool CanSeek
        {
            get
            {
                return true;
            }
        }

        override public bool CanWrite
        {
            get
            {
                return false;
            }
        }

        override public long Length
        {
            get
            {
                return TotalLength;
            }
        }

        override public long Position
        {
            get
            {
                long pos = 0;
                if (_currentArrayIndex > 0)
                {
                    for (int ii = 0; ii < _currentArrayIndex; ii++)
                    {
                        pos += _cachedBytes[ii].Length;
                    }
                }
                pos += _currentPosition;
                return pos;
            }
            set
            {
                if (null == _cachedBytes)
                {
                    throw ADP.StreamClosed(ADP.ParameterSetPosition);
                }
                SetInternalPosition(value, ADP.ParameterSetPosition);
            }
        }

        override protected void Dispose(bool disposing)
        {
            try
            {
                if (disposing && _cachedBytes != null)
                    _cachedBytes.Clear();
                _cachedBytes = null;
                _currentPosition = 0;
                _currentArrayIndex = 0;
                _totalLength = 0;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        override public void Flush()
        {
            throw ADP.NotSupported();
        }

        override public int Read(byte[] buffer, int offset, int count)
        {
            int cb;
            int intCount = 0;

            if (null == _cachedBytes)
            {
                throw ADP.StreamClosed();
            }

            if (null == buffer)
            {
                throw ADP.ArgumentNull(nameof(buffer));
            }

            if ((offset < 0) || (count < 0))
            {
                throw ADP.ArgumentOutOfRange(string.Empty, (offset < 0 ? nameof(offset) : nameof(count)));
            }

            if (buffer.Length - offset < count)
            {
                throw ADP.ArgumentOutOfRange(nameof(count));
            }

            if (_cachedBytes.Count <= _currentArrayIndex)
            {
                return 0;       // Everything is read!
            }

            while (count > 0)
            {
                if (_cachedBytes[_currentArrayIndex].Length <= _currentPosition)
                {
                    _currentArrayIndex++;       // We are done reading this chunk, go to next
                    if (_cachedBytes.Count > _currentArrayIndex)
                    {
                        _currentPosition = 0;
                    }
                    else
                    {
                        break;
                    }
                }
                cb = _cachedBytes[_currentArrayIndex].Length - _currentPosition;
                if (cb > count)
                    cb = count;
                Buffer.BlockCopy(_cachedBytes[_currentArrayIndex], _currentPosition, buffer, offset, cb);

                _currentPosition += cb;
                count -= (int)cb;
                offset += (int)cb;
                intCount += (int)cb;
            }

            return intCount;
        }

        override public long Seek(long offset, SeekOrigin origin)
        {
            long pos = 0;

            if (null == _cachedBytes)
            {
                throw ADP.StreamClosed();
            }

            switch (origin)
            {
                case SeekOrigin.Begin:
                    SetInternalPosition(offset, nameof(offset));
                    break;

                case SeekOrigin.Current:
                    pos = offset + Position;
                    SetInternalPosition(pos, nameof(offset));
                    break;

                case SeekOrigin.End:
                    pos = TotalLength + offset;
                    SetInternalPosition(pos, nameof(offset));
                    break;

                default:
                    throw ADP.InvalidSeekOrigin(nameof(offset));
            }
            return pos;
        }

        override public void SetLength(long value)
        {
            throw ADP.NotSupported();
        }

        override public void Write(byte[] buffer, int offset, int count)
        {
            throw ADP.NotSupported();
        }

        private void SetInternalPosition(long lPos, string argumentName)
        {
            long pos = lPos;

            if (pos < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName);
            }
            for (int ii = 0; ii < _cachedBytes.Count; ii++)
            {
                if (pos > _cachedBytes[ii].Length)
                {
                    pos -= _cachedBytes[ii].Length;
                }
                else
                {
                    _currentArrayIndex = ii;
                    _currentPosition = (int)pos;
                    return;
                }
            }
            if (pos > 0)
                throw new ArgumentOutOfRangeException(argumentName);
        }

        private long TotalLength
        {
            get
            {
                if ((_totalLength == 0) && (_cachedBytes != null))
                {
                    long pos = 0;
                    for (int ii = 0; ii < _cachedBytes.Count; ii++)
                    {
                        pos += _cachedBytes[ii].Length;
                    }
                    _totalLength = pos;
                }
                return _totalLength;
            }
        }
    }

    sealed internal class SqlStreamingXml
    {
        private int _columnOrdinal;
        private SqlDataReader _reader;
        private XmlReader _xmlReader;
        private XmlWriter _xmlWriter;
        private StringWriter _strWriter;
        private long _charsRemoved;

        public SqlStreamingXml(int i, SqlDataReader reader)
        {
            _columnOrdinal = i;
            _reader = reader;
        }

        public void Close()
        {
            ((IDisposable)_xmlWriter).Dispose();
            ((IDisposable)_xmlReader).Dispose();
            _reader = null;
            _xmlReader = null;
            _xmlWriter = null;
            _strWriter = null;
        }

        public int ColumnOrdinal
        {
            get
            {
                return _columnOrdinal;
            }
        }

        public long GetChars(long dataIndex, char[] buffer, int bufferIndex, int length)
        {
            if (_xmlReader == null)
            {
                SqlStream sqlStream = new SqlStream(_columnOrdinal, _reader, true /* addByteOrderMark */, false /* processAllRows*/, false /*advanceReader*/);
                _xmlReader = sqlStream.ToXmlReader();
                _strWriter = new StringWriter((System.IFormatProvider)null);
                XmlWriterSettings writerSettings = new XmlWriterSettings();
                writerSettings.CloseOutput = true;      // close the memory stream when done
                writerSettings.ConformanceLevel = ConformanceLevel.Fragment;
                _xmlWriter = XmlWriter.Create(_strWriter, writerSettings);
            }

            int charsToSkip = 0;
            int cnt = 0;
            if (dataIndex < _charsRemoved)
            {
                throw ADP.NonSeqByteAccess(dataIndex, _charsRemoved, nameof(GetChars));
            }
            else if (dataIndex > _charsRemoved)
            {
                charsToSkip = (int)(dataIndex - _charsRemoved);
            }

            // If buffer parameter is null, we have to return -1 since there is no way for us to know the
            // total size up front without reading and converting the XML.
            if (buffer == null)
            {
                return (long)(-1);
            }

            StringBuilder strBldr = _strWriter.GetStringBuilder();
            while (!_xmlReader.EOF)
            {
                if (strBldr.Length >= (length + charsToSkip))
                {
                    break;
                }
                // Can't call _xmlWriter.WriteNode here, since it reads all of the data in before returning the first char.
                // Do own implementation of WriteNode instead that reads just enough data to return the required number of chars
                //_xmlWriter.WriteNode(_xmlReader, true);
                //  _xmlWriter.Flush();
                WriteXmlElement();
                if (charsToSkip > 0)
                {
                    // Aggressively remove the characters we want to skip to avoid growing StringBuilder size too much
                    cnt = strBldr.Length < charsToSkip ? strBldr.Length : charsToSkip;
                    strBldr.Remove(0, cnt);
                    charsToSkip -= cnt;
                    _charsRemoved += (long)cnt;
                }
            }

            if (charsToSkip > 0)
            {
                cnt = strBldr.Length < charsToSkip ? strBldr.Length : charsToSkip;
                strBldr.Remove(0, cnt);
                charsToSkip -= cnt;
                _charsRemoved += (long)cnt;
            }

            if (strBldr.Length == 0)
            {
                return 0;
            }
            // At this point charsToSkip must be 0
            Debug.Assert(charsToSkip == 0);

            cnt = strBldr.Length < length ? strBldr.Length : length;
            for (int i = 0; i < cnt; i++)
            {
                buffer[bufferIndex + i] = strBldr[i];
            }
            // Remove the characters we have already returned
            strBldr.Remove(0, cnt);
            _charsRemoved += (long)cnt;
            return (long)cnt;
        }

        // This method duplicates the work of XmlWriter.WriteNode except that it reads one element at a time 
        // instead of reading the entire node like XmlWriter.
        private void WriteXmlElement()
        {
            if (_xmlReader.EOF)
                return;

            bool canReadChunk = _xmlReader.CanReadValueChunk;
            char[] writeNodeBuffer = null;

            // Constants
            const int WriteNodeBufferSize = 1024;

            _xmlReader.Read();
            switch (_xmlReader.NodeType)
            {
                case XmlNodeType.Element:
                    _xmlWriter.WriteStartElement(_xmlReader.Prefix, _xmlReader.LocalName, _xmlReader.NamespaceURI);
                    _xmlWriter.WriteAttributes(_xmlReader, true);
                    if (_xmlReader.IsEmptyElement)
                    {
                        _xmlWriter.WriteEndElement();
                        break;
                    }
                    break;
                case XmlNodeType.Text:
                    if (canReadChunk)
                    {
                        if (writeNodeBuffer == null)
                        {
                            writeNodeBuffer = new char[WriteNodeBufferSize];
                        }
                        int read;
                        while ((read = _xmlReader.ReadValueChunk(writeNodeBuffer, 0, WriteNodeBufferSize)) > 0)
                        {
                            _xmlWriter.WriteChars(writeNodeBuffer, 0, read);
                        }
                    }
                    else
                    {
                        _xmlWriter.WriteString(_xmlReader.Value);
                    }
                    break;
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    _xmlWriter.WriteWhitespace(_xmlReader.Value);
                    break;
                case XmlNodeType.CDATA:
                    _xmlWriter.WriteCData(_xmlReader.Value);
                    break;
                case XmlNodeType.EntityReference:
                    _xmlWriter.WriteEntityRef(_xmlReader.Name);
                    break;
                case XmlNodeType.XmlDeclaration:
                case XmlNodeType.ProcessingInstruction:
                    _xmlWriter.WriteProcessingInstruction(_xmlReader.Name, _xmlReader.Value);
                    break;
                case XmlNodeType.DocumentType:
                    _xmlWriter.WriteDocType(_xmlReader.Name, _xmlReader.GetAttribute("PUBLIC"), _xmlReader.GetAttribute("SYSTEM"), _xmlReader.Value);
                    break;
                case XmlNodeType.Comment:
                    _xmlWriter.WriteComment(_xmlReader.Value);
                    break;
                case XmlNodeType.EndElement:
                    _xmlWriter.WriteFullEndElement();
                    break;
            }
            _xmlWriter.Flush();
        }
    }
}
