// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    /// <summary>
    /// A custom network stream that stores and reuses a single SocketAsyncEventArgs instance
    /// for reads and a single SocketAsyncEventArgs instance for writes.  This limits it to
    /// supporting a single read and a single write at a time, but with much less per-operation
    /// overhead than with System.Net.Sockets.NetworkStream.
    /// </summary>
    internal sealed class AsyncEventArgsNetworkStream : NetworkStream
    {
        private readonly Socket _socket;
        private readonly SocketAsyncEventArgs _readArgs;
        private readonly SocketAsyncEventArgs _writeArgs;

        private AsyncTaskMethodBuilder<int> _readAtmb;
        private AsyncTaskMethodBuilder _writeAtmb;
        private bool _disposed;

        public AsyncEventArgsNetworkStream(Socket socket) : base(socket, ownsSocket: true)
        {
            _socket = socket;

            _readArgs = new SocketAsyncEventArgs();
            _readArgs.Completed += ReadCompleted;

            _writeArgs = new SocketAsyncEventArgs();
            _writeArgs.Completed += WriteCompleted;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && !_disposed)
            {
                _disposed = true;
                try
                {
                    _readArgs.Dispose();
                    _writeArgs.Dispose();
                }
                catch (ObjectDisposedException) { }
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            _readAtmb = new AsyncTaskMethodBuilder<int>();
            Task<int> t = _readAtmb.Task;

            _readArgs.SetBuffer(buffer, offset, count);
            if (!_socket.ReceiveAsync(_readArgs))
            {
                ReadCompleted(null, _readArgs);
            }

            return t;
        }

        private void ReadCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                _readAtmb.SetResult(e.BytesTransferred);
            }
            else
            {
                _readAtmb.SetException(CreateException(e.SocketError));
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            _writeAtmb = new AsyncTaskMethodBuilder();
            Task t = _writeAtmb.Task;

            _writeArgs.SetBuffer(buffer, offset, count);
            if (!_socket.SendAsync(_writeArgs))
            {
                // TODO: #4900 This path should be hit very frequently (sends should very frequently simply
                // write into the kernel's send buffer), but it's practically never getting hit due to the current
                // System.Net.Sockets.dll implementation that always completing asynchronously on success :(
                // If that doesn't get fixed, we should try to come up with some alternative here.  This is
                // an important path, in part as it means the caller will complete awaits synchronously rather
                // than spending the costs associated with yielding in each async method up the call chain.
                // (This applies to ReadAsync as well, but typically to a much less extent.)
                WriteCompleted(null, _writeArgs);
            }

            return t;
        }

        private void WriteCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                _writeAtmb.SetResult();
            }
            else
            {
                _writeAtmb.SetException(CreateException(e.SocketError));
            }
        }

        private Exception CreateException(SocketError error)
        {
            if (_disposed)
            {
                return new ObjectDisposedException(GetType().Name);
            }
            else if (error == SocketError.OperationAborted)
            {
                return new OperationCanceledException();
            }
            else
            {
                return new IOException(SR.net_WebSockets_Generic, new SocketException((int)error));
            }
        }
    }
}
