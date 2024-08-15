// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;

namespace System.Net
{
    /// <summary>
    /// <para>
    ///     The FtpDataStream class implements the FTP data connection.
    /// </para>
    /// </summary>
    internal sealed class FtpDataStream : Stream, ICloseEx
    {
        private readonly FtpWebRequest _request;
        private readonly Stream _stream;
        private readonly NetworkStream _originalStream;
        private bool _writeable;
        private bool _readable;
        private bool _isFullyRead;
        private bool _closing;

        private const int DefaultCloseTimeout = -1;

        internal FtpDataStream(Stream stream, NetworkStream originalStream, FtpWebRequest request, TriState writeOnly)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this);

            _readable = true;
            _writeable = true;
            if (writeOnly == TriState.True)
            {
                _readable = false;
            }
            else if (writeOnly == TriState.False)
            {
                _writeable = false;
            }
            _stream = stream;
            _originalStream = originalStream;
            _request = request;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                    ((ICloseEx)this).CloseEx(CloseExState.Normal);
                else
                    ((ICloseEx)this).CloseEx(CloseExState.Abort | CloseExState.Silent);
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        //TODO: Add this to FxCopBaseline.cs once https://github.com/dotnet/roslyn/issues/15728 is fixed
        void ICloseEx.CloseEx(CloseExState closeState)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"state = {closeState}");

            lock (this)
            {
                if (_closing)
                    return;
                _closing = true;
                _writeable = false;
                _readable = false;
            }

            try
            {
                try
                {
                    if ((closeState & CloseExState.Abort) == 0)
                        _originalStream.Close(DefaultCloseTimeout);
                    else
                        _originalStream.Close(0);
                }
                finally
                {
                    _request.DataStreamClosed(closeState);
                }
            }
            catch (Exception exception)
            {
                bool doThrow = true;
                WebException? webException = exception as WebException;
                if (webException != null)
                {
                    FtpWebResponse? response = webException.Response as FtpWebResponse;
                    if (response != null)
                    {
                        if (!_isFullyRead
                            && response.StatusCode == FtpStatusCode.ConnectionClosed)
                            doThrow = false;
                    }
                }

                if (doThrow)
                    if ((closeState & CloseExState.Silent) == 0)
                        throw;
            }
        }

        private void CheckError()
        {
            if (_request.Aborted)
            {
                throw ExceptionHelper.RequestAbortedException;
            }
        }

        public override bool CanRead
        {
            get
            {
                return _readable;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return _stream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return _writeable;
            }
        }

        public override long Length
        {
            get
            {
                return _stream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return _stream.Position;
            }

            set
            {
                _stream.Position = value;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckError();
            try
            {
                return _stream.Seek(offset, origin);
            }
            catch
            {
                CheckError();
                throw;
            }
        }

        public override int Read(byte[] buffer, int offset, int size)
        {
            CheckError();
            int readBytes;
            try
            {
                readBytes = _stream.Read(buffer, offset, size);
            }
            catch
            {
                CheckError();
                throw;
            }
            if (readBytes == 0)
            {
                _isFullyRead = true;
                Close();
            }
            return readBytes;
        }

        public override int Read(Span<byte> buffer)
        {
            CheckError();
            int readBytes;
            try
            {
                readBytes = _stream.Read(buffer);
            }
            catch
            {
                CheckError();
                throw;
            }
            if (readBytes == 0)
            {
                _isFullyRead = true;
                Close();
            }
            return readBytes;
        }

        public override void Write(byte[] buffer, int offset, int size)
        {
            CheckError();
            try
            {
                _stream.Write(buffer, offset, size);
            }
            catch
            {
                CheckError();
                throw;
            }
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            CheckError();
            try
            {
                _stream.Write(buffer);
            }
            catch
            {
                CheckError();
                throw;
            }
        }

        private void AsyncReadCallback(IAsyncResult ar)
        {
            LazyAsyncResult userResult = (LazyAsyncResult)ar.AsyncState!;
            try
            {
                try
                {
                    int readBytes = _stream.EndRead(ar);
                    if (readBytes == 0)
                    {
                        _isFullyRead = true;
                        Close(); // This should block for pipeline completion
                    }
                    userResult.InvokeCallback(readBytes);
                }
                catch (Exception exception)
                {
                    // Complete with error. If already completed rethrow on the worker thread
                    if (!userResult.IsCompleted)
                        userResult.InvokeCallback(exception);
                }
            }
            catch { }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback? callback, object? state)
        {
            CheckError();
            LazyAsyncResult userResult = new LazyAsyncResult(this, state, callback);
            try
            {
                _stream.BeginRead(buffer, offset, size, new AsyncCallback(AsyncReadCallback), userResult);
            }
            catch
            {
                CheckError();
                throw;
            }
            return userResult;
        }

        public override int EndRead(IAsyncResult ar)
        {
            try
            {
                object result = ((LazyAsyncResult)ar).InternalWaitForCompletion()!;

                if (result is Exception e)
                {
                    ExceptionDispatchInfo.Throw(e);
                }

                return (int)result;
            }
            finally
            {
                CheckError();
            }
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback? callback, object? state)
        {
            CheckError();
            try
            {
                return _stream.BeginWrite(buffer, offset, size, callback, state);
            }
            catch
            {
                CheckError();
                throw;
            }
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            try
            {
                _stream.EndWrite(asyncResult);
            }
            finally
            {
                CheckError();
            }
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override bool CanTimeout
        {
            get
            {
                return _stream.CanTimeout;
            }
        }

        public override int ReadTimeout
        {
            get
            {
                return _stream.ReadTimeout;
            }
            set
            {
                _stream.ReadTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get
            {
                return _stream.WriteTimeout;
            }
            set
            {
                _stream.WriteTimeout = value;
            }
        }

        internal void SetSocketTimeoutOption(int timeout)
        {
            _stream.ReadTimeout = timeout;
            _stream.WriteTimeout = timeout;
        }
    }
}
