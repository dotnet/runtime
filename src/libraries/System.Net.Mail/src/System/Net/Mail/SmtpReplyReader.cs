// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
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

        internal LineInfo[] ReadLines()
        {
            Task<LineInfo[]> task = ReadLinesAsync<SyncReadWriteAdapter>();

            Debug.Assert(task.IsCompleted, "ReadLinesAsync should be completed synchronously.");
            return task.GetAwaiter().GetResult();
        }

        internal IAsyncResult BeginReadLines(AsyncCallback callback, object? state)
        {
            return TaskToAsyncResult.Begin(ReadLinesAsync<AsyncReadWriteAdapter>(), callback, state);
        }

        internal static LineInfo[] EndReadLines(IAsyncResult asyncResult)
        {
            return TaskToAsyncResult.End<LineInfo[]>(asyncResult);
        }

        internal LineInfo ReadLine()
        {
            Task<LineInfo> task = ReadLineAsync<SyncReadWriteAdapter>();

            Debug.Assert(task.IsCompleted, "ReadLineAsync should be completed synchronously.");
            return task.GetAwaiter().GetResult();
        }

        internal IAsyncResult BeginReadLine(AsyncCallback callback, object? state)
        {
            return TaskToAsyncResult.Begin(ReadLineAsync<AsyncReadWriteAdapter>(), callback, state);
        }

        internal static LineInfo EndReadLine(IAsyncResult asyncResult)
        {
            return TaskToAsyncResult.End<LineInfo>(asyncResult);
        }

        internal Task<LineInfo[]> ReadLinesAsync()
        {
            return ReadLinesAsync<AsyncReadWriteAdapter>();
        }

        internal Task<LineInfo> ReadLineAsync()
        {
            return ReadLineAsync<AsyncReadWriteAdapter>();
        }

        internal Task<LineInfo[]> ReadLinesAsync<TIOAdapter>(CancellationToken cancellationToken = default) where TIOAdapter : IReadWriteAdapter
        {
            return _reader.ReadLinesAsync<TIOAdapter>(this, false, cancellationToken);
        }

        internal Task<LineInfo> ReadLineAsync<TIOAdapter>(CancellationToken cancellationToken = default) where TIOAdapter : IReadWriteAdapter
        {
            return _reader.ReadLineAsync<TIOAdapter>(this, cancellationToken);
        }
    }
}
