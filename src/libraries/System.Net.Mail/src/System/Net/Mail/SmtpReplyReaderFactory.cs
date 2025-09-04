// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mail
{
    //Streams created are read only and return 0 once a full server reply has been read
    //To get the next server reply, call GetNextReplyReader
    internal sealed class SmtpReplyReaderFactory
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

        private readonly BufferedReadStream _bufferedStream;
        private byte[]? _byteBuffer;
        private SmtpReplyReader? _currentReader;
        private const int DefaultBufferSize = 256;
        private ReadState _readState = ReadState.Status0;
        private SmtpStatusCode _statusCode;

        internal SmtpReplyReaderFactory(Stream stream)
        {
            _bufferedStream = new BufferedReadStream(stream);
        }

        internal SmtpReplyReader? CurrentReader
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

        internal void Close(SmtpReplyReader caller)
        {
            if (_currentReader == caller)
            {
                if (_readState != ReadState.Done)
                {
                    _byteBuffer ??= new byte[SmtpReplyReaderFactory.DefaultBufferSize];

                    while (0 != Read(caller, _byteBuffer)) ;
                }

                _currentReader = null;
            }
        }

        internal SmtpReplyReader GetNextReplyReader()
        {
            _currentReader?.Close();

            _readState = ReadState.Status0;
            _currentReader = new SmtpReplyReader(this);
            return _currentReader;
        }

        private int ProcessRead(ReadOnlySpan<byte> buffer, bool readLine)
        {
            // if 0 bytes were read,there was a failure
            if (buffer.Length == 0)
            {
                throw new IOException(SR.Format(SR.net_io_readfailure, SR.net_io_connectionclosed));
            }

            unsafe
            {
                fixed (byte* pBuffer = buffer)
                {
                    byte* start = pBuffer;
                    byte* ptr = start;
                    byte* end = ptr + buffer.Length;

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

        internal int Read(SmtpReplyReader caller, Span<byte> buffer)
        {
            // if we've already found the delimiter, then return 0 indicating
            // end of stream.
            if (buffer.Length == 0 || _currentReader != caller || _readState == ReadState.Done)
            {
                return 0;
            }

            int read = _bufferedStream.Read(buffer);
            int actual = ProcessRead(buffer.Slice(0, read), false);
            if (actual < read)
            {
                _bufferedStream.Push(buffer.Slice(actual, read - actual));
            }

            return actual;
        }

        internal async Task<LineInfo[]> ReadLinesAsync<TIOAdapter>(SmtpReplyReader caller, bool oneLine = false, CancellationToken cancellationToken = default) where TIOAdapter : IReadWriteAdapter
        {
            if (caller != _currentReader || _readState == ReadState.Done)
            {
                return Array.Empty<LineInfo>();
            }

            _byteBuffer ??= new byte[DefaultBufferSize];
            System.Diagnostics.Debug.Assert(_readState == ReadState.Status0);

            var builder = new StringBuilder();
            var lines = new List<LineInfo>();
            int statusRead = 0;

            int start = 0;
            int read = 0;

            while (true)
            {
                if (start == read)
                {
                    start = 0;
                    read = await TIOAdapter.ReadAsync(_bufferedStream, _byteBuffer, cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                    {
                        throw new IOException(SR.Format(SR.net_io_readfailure, SR.net_io_connectionclosed));
                    }
                }

                int actual = ProcessRead(_byteBuffer!.AsSpan(start, read - start), true);

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
                    lines.Add(new LineInfo(_statusCode, builder.ToString(0, builder.Length - 2))); // Exclude CRLF

                    if (oneLine)
                    {
                        _bufferedStream.Push(_byteBuffer!.AsSpan(start, read - start));
                        return lines.ToArray();
                    }

                    builder.Clear();
                }
                else if (_readState == ReadState.Done)
                {
                    lines!.Add(new LineInfo(_statusCode, builder.ToString(0, builder.Length - 2))); // return everything except CRLF
                    _bufferedStream.Push(_byteBuffer!.AsSpan(start, read - start));
                    return lines.ToArray();
                }
            }

        }

        internal async Task<LineInfo> ReadLineAsync<TIOAdapter>(SmtpReplyReader caller, CancellationToken cancellationToken) where TIOAdapter : IReadWriteAdapter
        {
            LineInfo[] lines = await ReadLinesAsync<TIOAdapter>(caller, oneLine: true, cancellationToken).ConfigureAwait(false);
            return lines.Length > 0 ? lines[0] : default;
        }
    }
}
