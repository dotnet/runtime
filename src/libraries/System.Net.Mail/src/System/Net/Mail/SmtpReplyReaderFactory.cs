// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.IO;
using System.Text;

namespace System.Net.Mail
{
    //Streams created are read only and return 0 once a full server reply has been read
    //To get the next server reply, call GetNextReplyReader
    internal class SmtpReplyReaderFactory
    {
        private enum ReadState
        {
            Status0,
            Status1,
            Status2,
            ContinueFlag,
            ContinueCR,
            ContinueLF,
            LastCR,
            LastLF,
            Done
        }

        private BufferedReadStream _bufferedStream;
        private byte[] _byteBuffer;
        private SmtpReplyReader _currentReader;
        private const int DefaultBufferSize = 256;
        private ReadState _readState = ReadState.Status0;
        private SmtpStatusCode _statusCode;

        internal SmtpReplyReaderFactory(Stream stream)
        {
            _bufferedStream = new BufferedReadStream(stream);
        }

        internal SmtpReplyReader CurrentReader
        {
            get
            {
                return _currentReader;
            }
        }

        internal SmtpStatusCode StatusCode
        {
            get
            {
                return _statusCode;
            }
        }

        internal IAsyncResult BeginReadLines(SmtpReplyReader caller, AsyncCallback callback, object state)
        {
            ReadLinesAsyncResult result = new ReadLinesAsyncResult(this, callback, state);
            result.Read(caller);
            return result;
        }

        internal IAsyncResult BeginReadLine(SmtpReplyReader caller, AsyncCallback callback, object state)
        {
            ReadLinesAsyncResult result = new ReadLinesAsyncResult(this, callback, state, true);
            result.Read(caller);
            return result;
        }

        internal void Close(SmtpReplyReader caller)
        {
            if (_currentReader == caller)
            {
                if (_readState != ReadState.Done)
                {
                    if (_byteBuffer == null)
                    {
                        _byteBuffer = new byte[SmtpReplyReaderFactory.DefaultBufferSize];
                    }

                    while (0 != Read(caller, _byteBuffer, 0, _byteBuffer.Length)) ;
                }

                _currentReader = null;
            }
        }

        internal LineInfo[] EndReadLines(IAsyncResult result)
        {
            return ReadLinesAsyncResult.End(result);
        }

        internal LineInfo EndReadLine(IAsyncResult result)
        {
            LineInfo[] info = ReadLinesAsyncResult.End(result);
            if (info != null && info.Length > 0)
            {
                return info[0];
            }
            return new LineInfo();
        }

        internal SmtpReplyReader GetNextReplyReader()
        {
            if (_currentReader != null)
            {
                _currentReader.Close();
            }

            _readState = ReadState.Status0;
            _currentReader = new SmtpReplyReader(this);
            return _currentReader;
        }

        private int ProcessRead(byte[] buffer, int offset, int read, bool readLine)
        {
            // if 0 bytes were read,there was a failure
            if (read == 0)
            {
                throw new IOException(SR.Format(SR.net_io_readfailure, SR.net_io_connectionclosed));
            }

            unsafe
            {
                fixed (byte* pBuffer = buffer)
                {
                    byte* start = pBuffer + offset;
                    byte* ptr = start;
                    byte* end = ptr + read;

                    switch (_readState)
                    {
                        case ReadState.Status0:
                            {
                                if (ptr < end)
                                {
                                    byte b = *ptr++;
                                    if (b < '0' && b > '9')
                                    {
                                        throw new FormatException(SR.SmtpInvalidResponse);
                                    }

                                    _statusCode = (SmtpStatusCode)(100 * (b - '0'));

                                    goto case ReadState.Status1;
                                }
                                _readState = ReadState.Status0;
                                break;
                            }
                        case ReadState.Status1:
                            {
                                if (ptr < end)
                                {
                                    byte b = *ptr++;
                                    if (b < '0' && b > '9')
                                    {
                                        throw new FormatException(SR.SmtpInvalidResponse);
                                    }

                                    _statusCode += 10 * (b - '0');

                                    goto case ReadState.Status2;
                                }
                                _readState = ReadState.Status1;
                                break;
                            }
                        case ReadState.Status2:
                            {
                                if (ptr < end)
                                {
                                    byte b = *ptr++;
                                    if (b < '0' && b > '9')
                                    {
                                        throw new FormatException(SR.SmtpInvalidResponse);
                                    }

                                    _statusCode += b - '0';

                                    goto case ReadState.ContinueFlag;
                                }
                                _readState = ReadState.Status2;
                                break;
                            }
                        case ReadState.ContinueFlag:
                            {
                                if (ptr < end)
                                {
                                    byte b = *ptr++;
                                    if (b == ' ')       // last line
                                    {
                                        goto case ReadState.LastCR;
                                    }
                                    else if (b == '-')  // more lines coming
                                    {
                                        goto case ReadState.ContinueCR;
                                    }
                                    else                // error
                                    {
                                        throw new FormatException(SR.SmtpInvalidResponse);
                                    }
                                }
                                _readState = ReadState.ContinueFlag;
                                break;
                            }
                        case ReadState.ContinueCR:
                            {
                                while (ptr < end)
                                {
                                    if (*ptr++ == '\r')
                                    {
                                        goto case ReadState.ContinueLF;
                                    }
                                }
                                _readState = ReadState.ContinueCR;
                                break;
                            }
                        case ReadState.ContinueLF:
                            {
                                if (ptr < end)
                                {
                                    if (*ptr++ != '\n')
                                    {
                                        throw new FormatException(SR.SmtpInvalidResponse);
                                    }
                                    if (readLine)
                                    {
                                        _readState = ReadState.Status0;
                                        return (int)(ptr - start);
                                    }
                                    goto case ReadState.Status0;
                                }
                                _readState = ReadState.ContinueLF;
                                break;
                            }
                        case ReadState.LastCR:
                            {
                                while (ptr < end)
                                {
                                    if (*ptr++ == '\r')
                                    {
                                        goto case ReadState.LastLF;
                                    }
                                }
                                _readState = ReadState.LastCR;
                                break;
                            }
                        case ReadState.LastLF:
                            {
                                if (ptr < end)
                                {
                                    if (*ptr++ != '\n')
                                    {
                                        throw new FormatException(SR.SmtpInvalidResponse);
                                    }
                                    goto case ReadState.Done;
                                }
                                _readState = ReadState.LastLF;
                                break;
                            }
                        case ReadState.Done:
                            {
                                int actual = (int)(ptr - start);
                                _readState = ReadState.Done;
                                return actual;
                            }
                    }
                    return (int)(ptr - start);
                }
            }
        }

        internal int Read(SmtpReplyReader caller, byte[] buffer, int offset, int count)
        {
            // if we've already found the delimitter, then return 0 indicating
            // end of stream.
            if (count == 0 || _currentReader != caller || _readState == ReadState.Done)
            {
                return 0;
            }

            int read = _bufferedStream.Read(buffer, offset, count);
            int actual = ProcessRead(buffer, offset, read, false);
            if (actual < read)
            {
                _bufferedStream.Push(buffer, offset + actual, read - actual);
            }

            return actual;
        }

        internal LineInfo ReadLine(SmtpReplyReader caller)
        {
            LineInfo[] info = ReadLines(caller, true);
            if (info != null && info.Length > 0)
            {
                return info[0];
            }
            return new LineInfo();
        }

        internal LineInfo[] ReadLines(SmtpReplyReader caller)
        {
            return ReadLines(caller, false);
        }

        internal LineInfo[] ReadLines(SmtpReplyReader caller, bool oneLine)
        {
            if (caller != _currentReader || _readState == ReadState.Done)
            {
                return Array.Empty<LineInfo>();
            }

            if (_byteBuffer == null)
            {
                _byteBuffer = new byte[SmtpReplyReaderFactory.DefaultBufferSize];
            }

            System.Diagnostics.Debug.Assert(_readState == ReadState.Status0);

            StringBuilder builder = new StringBuilder();
            ArrayList lines = new ArrayList();
            int statusRead = 0;

            for (int start = 0, read = 0; ;)
            {
                if (start == read)
                {
                    read = _bufferedStream.Read(_byteBuffer, 0, _byteBuffer.Length);
                    start = 0;
                }

                int actual = ProcessRead(_byteBuffer, start, read - start, true);

                if (statusRead < 4)
                {
                    int left = Math.Min(4 - statusRead, actual);
                    statusRead += left;
                    start += left;
                    actual -= left;
                    if (actual == 0)
                    {
                        continue;
                    }
                }

                builder.Append(Encoding.UTF8.GetString(_byteBuffer, start, actual));
                start += actual;

                if (_readState == ReadState.Status0)
                {
                    statusRead = 0;
                    lines.Add(new LineInfo(_statusCode, builder.ToString(0, builder.Length - 2))); // return everything except CRLF

                    if (oneLine)
                    {
                        _bufferedStream.Push(_byteBuffer, start, read - start);
                        return (LineInfo[])lines.ToArray(typeof(LineInfo));
                    }
                    builder = new StringBuilder();
                }
                else if (_readState == ReadState.Done)
                {
                    lines.Add(new LineInfo(_statusCode, builder.ToString(0, builder.Length - 2))); // return everything except CRLF
                    _bufferedStream.Push(_byteBuffer, start, read - start);
                    return (LineInfo[])lines.ToArray(typeof(LineInfo));
                }
            }
        }

        private class ReadLinesAsyncResult : LazyAsyncResult
        {
            private StringBuilder _builder;
            private ArrayList _lines;
            private SmtpReplyReaderFactory _parent;
            private static AsyncCallback s_readCallback = new AsyncCallback(ReadCallback);
            private int _read;
            private int _statusRead;
            private bool _oneLine;

            internal ReadLinesAsyncResult(SmtpReplyReaderFactory parent, AsyncCallback callback, object state) : base(null, state, callback)
            {
                _parent = parent;
            }

            internal ReadLinesAsyncResult(SmtpReplyReaderFactory parent, AsyncCallback callback, object state, bool oneLine) : base(null, state, callback)
            {
                _oneLine = oneLine;
                _parent = parent;
            }

            internal void Read(SmtpReplyReader caller)
            {
                // if we've already found the delimitter, then return 0 indicating
                // end of stream.
                if (_parent._currentReader != caller || _parent._readState == ReadState.Done)
                {
                    InvokeCallback();
                    return;
                }

                if (_parent._byteBuffer == null)
                {
                    _parent._byteBuffer = new byte[SmtpReplyReaderFactory.DefaultBufferSize];
                }

                System.Diagnostics.Debug.Assert(_parent._readState == ReadState.Status0);

                _builder = new StringBuilder();
                _lines = new ArrayList();

                Read();
            }

            internal static LineInfo[] End(IAsyncResult result)
            {
                ReadLinesAsyncResult thisPtr = (ReadLinesAsyncResult)result;
                thisPtr.InternalWaitForCompletion();
                return (LineInfo[])thisPtr._lines.ToArray(typeof(LineInfo));
            }

            private void Read()
            {
                do
                {
                    IAsyncResult result = _parent._bufferedStream.BeginRead(_parent._byteBuffer, 0, _parent._byteBuffer.Length, s_readCallback, this);
                    if (!result.CompletedSynchronously)
                    {
                        return;
                    }
                    _read = _parent._bufferedStream.EndRead(result);
                } while (ProcessRead());
            }

            private static void ReadCallback(IAsyncResult result)
            {
                if (!result.CompletedSynchronously)
                {
                    Exception exception = null;
                    ReadLinesAsyncResult thisPtr = (ReadLinesAsyncResult)result.AsyncState;
                    try
                    {
                        thisPtr._read = thisPtr._parent._bufferedStream.EndRead(result);
                        if (thisPtr.ProcessRead())
                        {
                            thisPtr.Read();
                        }
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }

                    if (exception != null)
                    {
                        thisPtr.InvokeCallback(exception);
                    }
                }
            }

            private bool ProcessRead()
            {
                if (_read == 0)
                {
                    throw new IOException(SR.Format(SR.net_io_readfailure, SR.net_io_connectionclosed));
                }

                for (int start = 0; start != _read;)
                {
                    int actual = _parent.ProcessRead(_parent._byteBuffer, start, _read - start, true);

                    if (_statusRead < 4)
                    {
                        int left = Math.Min(4 - _statusRead, actual);
                        _statusRead += left;
                        start += left;
                        actual -= left;
                        if (actual == 0)
                        {
                            continue;
                        }
                    }

                    _builder.Append(Encoding.UTF8.GetString(_parent._byteBuffer, start, actual));
                    start += actual;

                    if (_parent._readState == ReadState.Status0)
                    {
                        _lines.Add(new LineInfo(_parent._statusCode, _builder.ToString(0, _builder.Length - 2))); // return everything except CRLF
                        _builder = new StringBuilder();
                        _statusRead = 0;

                        if (_oneLine)
                        {
                            _parent._bufferedStream.Push(_parent._byteBuffer, start, _read - start);
                            InvokeCallback();
                            return false;
                        }
                    }
                    else if (_parent._readState == ReadState.Done)
                    {
                        _lines.Add(new LineInfo(_parent._statusCode, _builder.ToString(0, _builder.Length - 2))); // return everything except CRLF
                        _parent._bufferedStream.Push(_parent._byteBuffer, start, _read - start);
                        InvokeCallback();
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
