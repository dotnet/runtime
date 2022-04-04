// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace System.Net.Mail
{
    //streams are read only; return of 0 means end of server's reply
    internal sealed class SmtpReplyReader
    {
        private readonly SmtpReplyReaderFactory _reader;

        internal SmtpReplyReader(SmtpReplyReaderFactory reader)
        {
            _reader = reader;
        }

        internal IAsyncResult BeginReadLines(AsyncCallback? callback, object? state)
        {
            return _reader.BeginReadLines(this, callback, state);
        }

        internal IAsyncResult BeginReadLine(AsyncCallback? callback, object? state)
        {
            return _reader.BeginReadLine(this, callback, state);
        }

        public void Close()
        {
            _reader.Close(this);
        }

        internal static LineInfo[] EndReadLines(IAsyncResult result)
        {
            return SmtpReplyReaderFactory.EndReadLines(result);
        }

        internal static LineInfo EndReadLine(IAsyncResult result)
        {
            return SmtpReplyReaderFactory.EndReadLine(result);
        }

        internal LineInfo[] ReadLines()
        {
            return _reader.ReadLines(this);
        }

        internal LineInfo ReadLine()
        {
            return _reader.ReadLine(this);
        }
    }
}
