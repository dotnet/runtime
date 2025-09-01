// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mail
{
    //streams are read only; return of 0 means end of server's reply
    internal sealed class SmtpReplyReader : IDisposable
    {
        public void Dispose()
        {
            Close();
        }

        private readonly SmtpReplyReaderFactory _reader;

        internal SmtpReplyReader(SmtpReplyReaderFactory reader)
        {
            _reader = reader;
        }

        public void Close()
        {
            _reader.Close(this);
        }

        internal Task<LineInfo[]> ReadLinesAsync<TIOAdapter>(CancellationToken cancellationToken) where TIOAdapter : IReadWriteAdapter
        {
            return _reader.ReadLinesAsync<TIOAdapter>(this, false, cancellationToken);
        }

        internal Task<LineInfo> ReadLineAsync<TIOAdapter>(CancellationToken cancellationToken) where TIOAdapter : IReadWriteAdapter
        {
            return _reader.ReadLineAsync<TIOAdapter>(this, cancellationToken);
        }
    }
}
