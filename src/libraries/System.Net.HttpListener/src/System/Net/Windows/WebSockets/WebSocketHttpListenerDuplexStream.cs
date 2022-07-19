// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1844 // Memory-based Read/WriteAsync

namespace System.Net.WebSockets
{
    internal sealed class WebSocketHttpListenerDuplexStream : Stream, WebSocketBase.IWebSocketStream
    {
        private static readonly EventHandler<HttpListenerAsyncEventArgs> s_OnReadCompleted =
            new EventHandler<HttpListenerAsyncEventArgs>(OnReadCompleted);
        private static readonly EventHandler<HttpListenerAsyncEventArgs> s_OnWriteCompleted =
            new EventHandler<HttpListenerAsyncEventArgs>(OnWriteCompleted);
        private static readonly Func<Exception, bool> s_CanHandleException = new Func<Exception, bool>(CanHandleException);
        private static readonly Action<object?> s_OnCancel = new Action<object?>(OnCancel);
        private readonly HttpRequestStream _inputStream;
        private readonly HttpResponseStream _outputStream;
        private readonly HttpListenerContext _context;
        private bool _inOpaqueMode;
        private WebSocketBase? _webSocket;
        private HttpListenerAsyncEventArgs? _writeEventArgs;
        private HttpListenerAsyncEventArgs? _readEventArgs;
        private TaskCompletionSource? _writeTaskCompletionSource;
        private TaskCompletionSource<int>? _readTaskCompletionSource;
        private int _cleanedUp;

#if DEBUG
        private sealed class OutstandingOperations
        {
            internal int _reads;
            internal int _writes;
        }

        private readonly OutstandingOperations _outstandingOperations = new OutstandingOperations();
#endif //DEBUG

        public WebSocketHttpListenerDuplexStream(HttpRequestStream inputStream,
            HttpResponseStream outputStream,
            HttpListenerContext context)
        {
            Debug.Assert(inputStream != null, "'inputStream' MUST NOT be NULL.");
            Debug.Assert(outputStream != null, "'outputStream' MUST NOT be NULL.");
            Debug.Assert(context != null, "'context' MUST NOT be NULL.");
            Debug.Assert(inputStream.CanRead, "'inputStream' MUST support read operations.");
            Debug.Assert(outputStream.CanWrite, "'outputStream' MUST support write operations.");

            _inputStream = inputStream;
            _outputStream = outputStream;
            _context = context;

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Associate(inputStream, this);
                NetEventSource.Associate(outputStream, this);
            }
        }

        public override bool CanRead
        {
            get
            {
                return _inputStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanTimeout
        {
            get
            {
                return _inputStream.CanTimeout && _outputStream.CanTimeout;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return _outputStream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException(SR.net_noseek);
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException(SR.net_noseek);
            }
            set
            {
                throw new NotSupportedException(SR.net_noseek);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _inputStream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            WebSocketValidate.ValidateBuffer(buffer, offset, count);

            return ReadAsyncCore(buffer, offset, count, cancellationToken);
        }

        private async Task<int> ReadAsyncCore(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CancellationTokenRegistration cancellationTokenRegistration = default;

            int bytesRead = 0;
            try
            {
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationTokenRegistration = cancellationToken.Register(s_OnCancel, this, false);
                }

                if (!_inOpaqueMode)
                {
                    bytesRead = await _inputStream.ReadAsync(buffer, offset, count, cancellationToken).SuppressContextFlow<int>();
                }
                else
                {
#if DEBUG
                    // When using fast path only one outstanding read is permitted. By switching into opaque mode
                    // via IWebSocketStream.SwitchToOpaqueMode (see more detailed comments in interface definition)
                    // caller takes responsibility for enforcing this constraint.
                    Debug.Assert(Interlocked.Increment(ref _outstandingOperations._reads) == 1,
                        "Only one outstanding read allowed at any given time.");
#endif
                    _readTaskCompletionSource = new TaskCompletionSource<int>();
                    _readEventArgs!.SetBuffer(buffer, offset, count);
                    if (!ReadAsyncFast(_readEventArgs))
                    {
                        if (_readEventArgs.Exception != null)
                        {
                            throw _readEventArgs.Exception;
                        }

                        bytesRead = _readEventArgs.BytesTransferred;
                    }
                    else
                    {
                        bytesRead = await _readTaskCompletionSource.Task.SuppressContextFlow<int>();
                    }
                }
            }
            catch (Exception error)
            {
                if (s_CanHandleException(error))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                throw;
            }
            finally
            {
                cancellationTokenRegistration.Dispose();
            }

            return bytesRead;
        }

        // return value indicates sync vs async completion
        // false: sync completion
        // true: async completion or error
        private unsafe bool ReadAsyncFast(HttpListenerAsyncEventArgs eventArgs)
        {
            eventArgs.StartOperationCommon(this, _inputStream.InternalHttpContext.RequestQueueBoundHandle);
            eventArgs.StartOperationReceive();

            bool completedAsynchronouslyOrWithError;
            try
            {
                Debug.Assert(eventArgs.Buffer != null, "'BufferList' is not supported for read operations.");
                if (eventArgs.Count == 0 || _inputStream.Closed)
                {
                    eventArgs.FinishOperationSuccess(0, true);
                    return false;
                }

                uint dataRead = 0;
                int offset = eventArgs.Offset;
                int remainingCount = eventArgs.Count;

                if (_inputStream.BufferedDataChunksAvailable)
                {
                    dataRead = _inputStream.GetChunks(eventArgs.Buffer, eventArgs.Offset, eventArgs.Count);
                    if (_inputStream.BufferedDataChunksAvailable && dataRead == eventArgs.Count)
                    {
                        eventArgs.FinishOperationSuccess(eventArgs.Count, true);
                        return false;
                    }
                }

                Debug.Assert(!_inputStream.BufferedDataChunksAvailable, "'m_InputStream.BufferedDataChunksAvailable' MUST BE 'FALSE' at this point.");
                Debug.Assert(dataRead <= eventArgs.Count, "'dataRead' MUST NOT be bigger than 'eventArgs.Count'.");

                if (dataRead != 0)
                {
                    offset += (int)dataRead;
                    remainingCount -= (int)dataRead;
                    //the http.sys team recommends that we limit the size to 128kb
                    if (remainingCount > HttpRequestStream.MaxReadSize)
                    {
                        remainingCount = HttpRequestStream.MaxReadSize;
                    }

                    eventArgs.SetBuffer(eventArgs.Buffer, offset, remainingCount);
                }
                else if (remainingCount > HttpRequestStream.MaxReadSize)
                {
                    remainingCount = HttpRequestStream.MaxReadSize;
                    eventArgs.SetBuffer(eventArgs.Buffer, offset, remainingCount);
                }

                uint flags = 0;
                uint bytesReturned = 0;
                uint statusCode =
                    Interop.HttpApi.HttpReceiveRequestEntityBody(
                        _inputStream.InternalHttpContext.RequestQueueHandle,
                        _inputStream.InternalHttpContext.RequestId,
                        flags,
                        (byte*)_webSocket!.InternalBuffer.ToIntPtr(eventArgs.Offset),
                        (uint)eventArgs.Count,
                        out bytesReturned,
                        eventArgs.NativeOverlapped);

                if (statusCode != Interop.HttpApi.ERROR_SUCCESS &&
                    statusCode != Interop.HttpApi.ERROR_IO_PENDING &&
                    statusCode != Interop.HttpApi.ERROR_HANDLE_EOF)
                {
                    throw new HttpListenerException((int)statusCode);
                }
                else if (statusCode == Interop.HttpApi.ERROR_SUCCESS &&
                    HttpListener.SkipIOCPCallbackOnSuccess)
                {
                    // IO operation completed synchronously. No IO completion port callback is used because
                    // it was disabled in SwitchToOpaqueMode()
                    eventArgs.FinishOperationSuccess((int)bytesReturned, true);
                    completedAsynchronouslyOrWithError = false;
                }
                else if (statusCode == Interop.HttpApi.ERROR_HANDLE_EOF)
                {
                    eventArgs.FinishOperationSuccess(0, true);
                    completedAsynchronouslyOrWithError = false;
                }
                else
                {
                    completedAsynchronouslyOrWithError = true;
                }
            }
            catch (Exception e)
            {
                _readEventArgs!.FinishOperationFailure(e, true);
                _outputStream.SetClosedFlag();
                _outputStream.InternalHttpContext.Abort();

                completedAsynchronouslyOrWithError = true;
            }

            return completedAsynchronouslyOrWithError;
        }

        public override int ReadByte()
        {
            return _inputStream.ReadByte();
        }

        public bool SupportsMultipleWrite
        {
            get
            {
                return true;
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer,
            int offset,
            int count,
            AsyncCallback? callback,
            object? state)
        {
            return _inputStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _inputStream.EndRead(asyncResult);
        }

        public Task MultipleWriteAsync(IList<ArraySegment<byte>> sendBuffers, CancellationToken cancellationToken)
        {
            Debug.Assert(_inOpaqueMode, "The stream MUST be in opaque mode at this point.");
            Debug.Assert(sendBuffers != null, "'sendBuffers' MUST NOT be NULL.");
            Debug.Assert(sendBuffers.Count == 1 || sendBuffers.Count == 2,
                "'sendBuffers.Count' MUST be either '1' or '2'.");

            if (sendBuffers.Count == 1)
            {
                ArraySegment<byte> buffer = sendBuffers[0];
                return WriteAsync(buffer.Array!, buffer.Offset, buffer.Count, cancellationToken);
            }

            return MultipleWriteAsyncCore(sendBuffers, cancellationToken);
        }

        private async Task MultipleWriteAsyncCore(IList<ArraySegment<byte>> sendBuffers, CancellationToken cancellationToken)
        {
            Debug.Assert(sendBuffers != null, "'sendBuffers' MUST NOT be NULL.");
            Debug.Assert(sendBuffers.Count == 2, "'sendBuffers.Count' MUST be '2' at this point.");

            CancellationTokenRegistration cancellationTokenRegistration = default;

            try
            {
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationTokenRegistration = cancellationToken.Register(s_OnCancel, this, false);
                }
#if DEBUG
                // When using fast path only one outstanding read is permitted. By switching into opaque mode
                // via IWebSocketStream.SwitchToOpaqueMode (see more detailed comments in interface definition)
                // caller takes responsibility for enforcing this constraint.
                Debug.Assert(Interlocked.Increment(ref _outstandingOperations._writes) == 1,
                    "Only one outstanding write allowed at any given time.");
#endif
                _writeTaskCompletionSource = new TaskCompletionSource();
                _writeEventArgs!.SetBuffer(null, 0, 0);
                _writeEventArgs.BufferList = sendBuffers;
                if (WriteAsyncFast(_writeEventArgs))
                {
                    await _writeTaskCompletionSource.Task.SuppressContextFlow();
                }
            }
            catch (Exception error)
            {
                if (s_CanHandleException(error))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                throw;
            }
            finally
            {
                cancellationTokenRegistration.Dispose();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _outputStream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            WebSocketValidate.ValidateBuffer(buffer, offset, count);

            return WriteAsyncCore(buffer, offset, count, cancellationToken);
        }

        private async Task WriteAsyncCore(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CancellationTokenRegistration cancellationTokenRegistration = default;

            try
            {
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationTokenRegistration = cancellationToken.Register(s_OnCancel, this, false);
                }

                if (!_inOpaqueMode)
                {
                    await _outputStream.WriteAsync(buffer, offset, count, cancellationToken).SuppressContextFlow();
                }
                else
                {
#if DEBUG
                    // When using fast path only one outstanding read is permitted. By switching into opaque mode
                    // via IWebSocketStream.SwitchToOpaqueMode (see more detailed comments in interface definition)
                    // caller takes responsibility for enforcing this constraint.
                    Debug.Assert(Interlocked.Increment(ref _outstandingOperations._writes) == 1,
                        "Only one outstanding write allowed at any given time.");
#endif
                    _writeTaskCompletionSource = new TaskCompletionSource();
                    _writeEventArgs!.BufferList = null;
                    _writeEventArgs.SetBuffer(buffer, offset, count);
                    if (WriteAsyncFast(_writeEventArgs))
                    {
                        await _writeTaskCompletionSource.Task.SuppressContextFlow();
                    }
                }
            }
            catch (Exception error)
            {
                if (s_CanHandleException(error))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                throw;
            }
            finally
            {
                cancellationTokenRegistration.Dispose();
            }
        }

        // return value indicates sync vs async completion
        // false: sync completion
        // true: async completion or with error
        private unsafe bool WriteAsyncFast(HttpListenerAsyncEventArgs eventArgs)
        {
            Interop.HttpApi.HTTP_FLAGS flags = Interop.HttpApi.HTTP_FLAGS.NONE;

            eventArgs.StartOperationCommon(this, _outputStream.InternalHttpContext.RequestQueueBoundHandle);
            eventArgs.StartOperationSend();

            uint statusCode;
            bool completedAsynchronouslyOrWithError;
            try
            {
                if (_outputStream.Closed ||
                    (eventArgs.Buffer != null && eventArgs.Count == 0))
                {
                    eventArgs.FinishOperationSuccess(eventArgs.Count, true);
                    return false;
                }

                if (eventArgs.ShouldCloseOutput)
                {
                    flags |= Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_DISCONNECT;
                }
                else
                {
                    flags |= Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA;
                    // When using HTTP_SEND_RESPONSE_FLAG_BUFFER_DATA HTTP.SYS will copy the payload to
                    // kernel memory (Non-Paged Pool). Http.Sys will buffer up to
                    // Math.Min(16 MB, current TCP window size)
                    flags |= Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_BUFFER_DATA;
                }

                uint bytesSent;
                statusCode =
                    Interop.HttpApi.HttpSendResponseEntityBody(
                        _outputStream.InternalHttpContext.RequestQueueHandle,
                        _outputStream.InternalHttpContext.RequestId,
                        (uint)flags,
                        eventArgs.EntityChunkCount,
                        (Interop.HttpApi.HTTP_DATA_CHUNK*)eventArgs.EntityChunks,
                        &bytesSent,
                        SafeLocalAllocHandle.Zero,
                        0,
                        eventArgs.NativeOverlapped,
                        null);

                if (statusCode != Interop.HttpApi.ERROR_SUCCESS &&
                    statusCode != Interop.HttpApi.ERROR_IO_PENDING)
                {
                    throw new HttpListenerException((int)statusCode);
                }
                else if (statusCode == Interop.HttpApi.ERROR_SUCCESS &&
                    HttpListener.SkipIOCPCallbackOnSuccess)
                {
                    // IO operation completed synchronously - callback won't be called to signal completion.
                    eventArgs.FinishOperationSuccess((int)bytesSent, true);
                    completedAsynchronouslyOrWithError = false;
                }
                else
                {
                    completedAsynchronouslyOrWithError = true;
                }
            }
            catch (Exception e)
            {
                _writeEventArgs!.FinishOperationFailure(e, true);
                _outputStream.SetClosedFlag();
                _outputStream.InternalHttpContext.Abort();

                completedAsynchronouslyOrWithError = true;
            }

            return completedAsynchronouslyOrWithError;
        }

        public override void WriteByte(byte value)
        {
            _outputStream.WriteByte(value);
        }

        public override IAsyncResult BeginWrite(byte[] buffer,
            int offset,
            int count,
            AsyncCallback? callback,
            object? state)
        {
            return _outputStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _outputStream.EndWrite(asyncResult);
        }

        public override void Flush()
        {
            _outputStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _outputStream.FlushAsync(cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(SR.net_noseek);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(SR.net_noseek);
        }

        public async Task CloseNetworkConnectionAsync(CancellationToken cancellationToken)
        {
            // need to yield here to make sure that we don't get any exception synchronously
            await Task.Yield();

            CancellationTokenRegistration cancellationTokenRegistration = default;

            try
            {
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationTokenRegistration = cancellationToken.Register(s_OnCancel, this, false);
                }
#if DEBUG
                // When using fast path only one outstanding read is permitted. By switching into opaque mode
                // via IWebSocketStream.SwitchToOpaqueMode (see more detailed comments in interface definition)
                // caller takes responsibility for enforcing this constraint.
                Debug.Assert(Interlocked.Increment(ref _outstandingOperations._writes) == 1,
                    "Only one outstanding write allowed at any given time.");
#endif
                _writeTaskCompletionSource = new TaskCompletionSource();
                _writeEventArgs!.SetShouldCloseOutput();
                if (WriteAsyncFast(_writeEventArgs))
                {
                    await _writeTaskCompletionSource.Task.SuppressContextFlow();
                }
            }
            catch (Exception error)
            {
                if (!s_CanHandleException(error))
                {
                    throw;
                }

                // throw OperationCancelledException when canceled by the caller
                // otherwise swallow the exception
                cancellationToken.ThrowIfCancellationRequested();
            }
            finally
            {
                cancellationTokenRegistration.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && Interlocked.Exchange(ref _cleanedUp, 1) == 0)
            {
                _readTaskCompletionSource?.TrySetCanceled();

                _writeTaskCompletionSource?.TrySetCanceled();

                _readEventArgs?.Dispose();
                _writeEventArgs?.Dispose();

                try
                {
                    _inputStream.Close();
                }
                finally
                {
                    _outputStream.Close();
                }
            }
        }

        public void Abort()
        {
            OnCancel(this);
        }

        private static bool CanHandleException(Exception error)
        {
            return error is HttpListenerException ||
                error is ObjectDisposedException ||
                error is IOException;
        }

        private static void OnCancel(object? state)
        {
            Debug.Assert(state != null, "'state' MUST NOT be NULL.");
            WebSocketHttpListenerDuplexStream thisPtr = (state as WebSocketHttpListenerDuplexStream)!;
            Debug.Assert(thisPtr != null, "'thisPtr' MUST NOT be NULL.");

            try
            {
                thisPtr._outputStream.SetClosedFlag();
                thisPtr._context.Abort();
            }
            catch { }

            thisPtr._readTaskCompletionSource?.TrySetCanceled();
            thisPtr._writeTaskCompletionSource?.TrySetCanceled();
        }

        public void SwitchToOpaqueMode(WebSocketBase webSocket)
        {
            Debug.Assert(webSocket != null, "'webSocket' MUST NOT be NULL.");
            Debug.Assert(_outputStream != null, "'m_OutputStream' MUST NOT be NULL.");
            Debug.Assert(_outputStream.InternalHttpContext != null,
                "'m_OutputStream.InternalHttpContext' MUST NOT be NULL.");
            Debug.Assert(_outputStream.InternalHttpContext.Response != null,
                "'m_OutputStream.InternalHttpContext.Response' MUST NOT be NULL.");
            Debug.Assert(_outputStream.InternalHttpContext.Response.SentHeaders,
                "Headers MUST have been sent at this point.");
            Debug.Assert(!_inOpaqueMode, "SwitchToOpaqueMode MUST NOT be called multiple times.");

            if (_inOpaqueMode)
            {
                throw new InvalidOperationException();
            }

            _webSocket = webSocket;
            _inOpaqueMode = true;
            _readEventArgs = new HttpListenerAsyncEventArgs(webSocket, this);
            _readEventArgs.Completed += s_OnReadCompleted;
            _writeEventArgs = new HttpListenerAsyncEventArgs(webSocket, this);
            _writeEventArgs.Completed += s_OnWriteCompleted;

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Associate(this, webSocket);
            }
        }

        private static void OnWriteCompleted(object? sender, HttpListenerAsyncEventArgs eventArgs)
        {
            Debug.Assert(eventArgs != null, "'eventArgs' MUST NOT be NULL.");
            WebSocketHttpListenerDuplexStream thisPtr = eventArgs.CurrentStream;
            Debug.Assert(thisPtr != null, "'thisPtr' MUST NOT be NULL.");
#if DEBUG
            Debug.Assert(Interlocked.Decrement(ref thisPtr._outstandingOperations._writes) >= 0,
                "'thisPtr.m_OutstandingOperations.m_Writes' MUST NOT be negative.");
#endif

            if (eventArgs.Exception != null)
            {
                thisPtr._writeTaskCompletionSource!.TrySetException(eventArgs.Exception);
            }
            else
            {
                thisPtr._writeTaskCompletionSource!.TrySetResult();
            }
        }

        private static void OnReadCompleted(object? sender, HttpListenerAsyncEventArgs eventArgs)
        {
            Debug.Assert(eventArgs != null, "'eventArgs' MUST NOT be NULL.");
            WebSocketHttpListenerDuplexStream thisPtr = eventArgs.CurrentStream;
            Debug.Assert(thisPtr != null, "'thisPtr' MUST NOT be NULL.");
#if DEBUG
            Debug.Assert(Interlocked.Decrement(ref thisPtr._outstandingOperations._reads) >= 0,
                "'thisPtr.m_OutstandingOperations.m_Reads' MUST NOT be negative.");
#endif

            if (eventArgs.Exception != null)
            {
                thisPtr._readTaskCompletionSource!.TrySetException(eventArgs.Exception);
            }
            else
            {
                thisPtr._readTaskCompletionSource!.TrySetResult(eventArgs.BytesTransferred);
            }
        }

        internal sealed class HttpListenerAsyncEventArgs : EventArgs, IDisposable
        {
            private const int Free = 0;
            private const int InProgress = 1;
            private const int Disposed = 2;
            private int _operating;

            private bool _disposeCalled;
            private unsafe NativeOverlapped* _ptrNativeOverlapped;
            private ThreadPoolBoundHandle? _boundHandle;
            private event EventHandler<HttpListenerAsyncEventArgs>? m_Completed;
            private byte[]? _buffer;
            private IList<ArraySegment<byte>>? _bufferList;
            private int _count;
            private int _offset;
            private int _bytesTransferred;
            private HttpListenerAsyncOperation _completedOperation;
            private Interop.HttpApi.HTTP_DATA_CHUNK[]? _dataChunks;
            private GCHandle _dataChunksGCHandle;
            private ushort _dataChunkCount;
            private Exception? _exception;
            private bool _shouldCloseOutput;
            private readonly WebSocketBase _webSocket;
            private readonly WebSocketHttpListenerDuplexStream _currentStream;

#if DEBUG
            private volatile int _nativeOverlappedCounter;
            private volatile int _nativeOverlappedUsed;

            private void DebugRefCountReleaseNativeOverlapped()
            {
                Debug.Assert(Interlocked.Decrement(ref _nativeOverlappedCounter) == 0, "NativeOverlapped released too many times.");
                Interlocked.Decrement(ref _nativeOverlappedUsed);
            }

            private void DebugRefCountAllocNativeOverlapped()
            {
                Debug.Assert(Interlocked.Increment(ref _nativeOverlappedCounter) == 1, "NativeOverlapped allocated without release.");
            }
#endif

            public HttpListenerAsyncEventArgs(WebSocketBase webSocket, WebSocketHttpListenerDuplexStream stream)
                : base()
            {
                _webSocket = webSocket;
                _currentStream = stream;
            }

            public int BytesTransferred
            {
                get { return _bytesTransferred; }
            }

            public byte[]? Buffer
            {
                get { return _buffer; }
            }

            // BufferList property.
            // Mutually exclusive with Buffer.
            // Setting this property with an existing non-null Buffer will cause an assert.
            public IList<ArraySegment<byte>>? BufferList
            {
                get { return _bufferList; }
                set
                {
                    Debug.Assert(!_shouldCloseOutput, "'m_ShouldCloseOutput' MUST be 'false' at this point.");
                    Debug.Assert(value == null || _buffer == null,
                        "Either 'm_Buffer' or 'm_BufferList' MUST be NULL.");
                    Debug.Assert(_operating == Free,
                        "This property can only be modified if no IO operation is outstanding.");
                    Debug.Assert(value == null || value.Count == 2,
                        "This list can only be 'NULL' or MUST have exactly '2' items.");
                    _bufferList = value;
                }
            }

            public bool ShouldCloseOutput
            {
                get { return _shouldCloseOutput; }
            }

            public int Offset
            {
                get { return _offset; }
            }

            public int Count
            {
                get { return _count; }
            }

            public Exception? Exception
            {
                get { return _exception; }
            }

            public ushort EntityChunkCount
            {
                get
                {
                    if (_dataChunks == null)
                    {
                        return 0;
                    }

                    return _dataChunkCount;
                }
            }

            internal unsafe NativeOverlapped* NativeOverlapped
            {
                get
                {
#if DEBUG
                    Debug.Assert(Interlocked.Increment(ref _nativeOverlappedUsed) == 1, "NativeOverlapped reused.");
#endif
                    return _ptrNativeOverlapped;
                }
            }

            public IntPtr EntityChunks
            {
                get
                {
                    if (_dataChunks == null)
                    {
                        return IntPtr.Zero;
                    }

                    return Marshal.UnsafeAddrOfPinnedArrayElement(_dataChunks, 0);
                }
            }

            public WebSocketHttpListenerDuplexStream CurrentStream
            {
                get { return _currentStream; }
            }

            public event EventHandler<HttpListenerAsyncEventArgs> Completed
            {
                add
                {
                    m_Completed += value;
                }
                remove
                {
                    m_Completed -= value;
                }
            }

            private void OnCompleted(HttpListenerAsyncEventArgs e)
            {
                m_Completed?.Invoke(e._currentStream, e);
            }

            public void SetShouldCloseOutput()
            {
                _bufferList = null;
                _buffer = null;
                _shouldCloseOutput = true;
            }

            public void Dispose()
            {
                // Remember that Dispose was called.
                _disposeCalled = true;

                // Check if this object is in-use for an async socket operation.
                if (Interlocked.CompareExchange(ref _operating, Disposed, Free) != Free)
                {
                    // Either already disposed or will be disposed when current operation completes.
                    return;
                }

                // Don't bother finalizing later.
                GC.SuppressFinalize(this);
            }

            private unsafe void InitializeOverlapped(ThreadPoolBoundHandle boundHandle)
            {
#if DEBUG
                DebugRefCountAllocNativeOverlapped();
#endif
                _boundHandle = boundHandle;
                _ptrNativeOverlapped = boundHandle.AllocateNativeOverlapped(CompletionPortCallback, null, null);
            }

            // Method to clean up any existing Overlapped object and related state variables.
            private unsafe void FreeOverlapped(bool checkForShutdown)
            {
                if (!checkForShutdown || !Environment.HasShutdownStarted)
                {
                    // Free the overlapped object
                    if (_ptrNativeOverlapped != null)
                    {
#if DEBUG
                        DebugRefCountReleaseNativeOverlapped();
#endif
                        _boundHandle!.FreeNativeOverlapped(_ptrNativeOverlapped);
                        _ptrNativeOverlapped = null;
                    }

                    if (_dataChunksGCHandle.IsAllocated)
                    {
                        _dataChunksGCHandle.Free();
                        _dataChunks = null;
                    }
                }
            }

            // Method called to prepare for a native async http.sys call.
            // This method performs the tasks common to all http.sys operations.
            internal void StartOperationCommon(WebSocketHttpListenerDuplexStream currentStream, ThreadPoolBoundHandle boundHandle)
            {
                // Change status to "in-use".
                if (Interlocked.CompareExchange(ref _operating, InProgress, Free) != Free)
                {
                    // If it was already "in-use" check if Dispose was called.
                    ObjectDisposedException.ThrowIf(_disposeCalled, this);

                    Debug.Fail("Only one outstanding async operation is allowed per HttpListenerAsyncEventArgs instance.");
                    // Only one at a time.
                    throw new InvalidOperationException();
                }

                // HttpSendResponseEntityBody can return ERROR_INVALID_PARAMETER if the InternalHigh field of the overlapped
                // is not IntPtr.Zero, so we have to reset this field because we are reusing the Overlapped.
                // When using the IAsyncResult based approach of HttpListenerResponseStream the Overlapped is reinitialized
                // for each operation by the CLR when returned from the OverlappedDataCache.

                InitializeOverlapped(boundHandle);

                _exception = null;
                _bytesTransferred = 0;
            }

            internal void StartOperationReceive()
            {
                // Remember the operation type.
                _completedOperation = HttpListenerAsyncOperation.Receive;
            }

            internal void StartOperationSend()
            {
                UpdateDataChunk();

                // Remember the operation type.
                _completedOperation = HttpListenerAsyncOperation.Send;
            }

            public void SetBuffer(byte[]? buffer, int offset, int count)
            {
                Debug.Assert(!_shouldCloseOutput, "'m_ShouldCloseOutput' MUST be 'false' at this point.");
                Debug.Assert(buffer == null || _bufferList == null, "Either 'm_Buffer' or 'm_BufferList' MUST be NULL.");
                _buffer = buffer;
                _offset = offset;
                _count = count;
            }

            private unsafe void UpdateDataChunk()
            {
                if (_dataChunks == null)
                {
                    _dataChunks = new Interop.HttpApi.HTTP_DATA_CHUNK[2];
                    _dataChunksGCHandle = GCHandle.Alloc(_dataChunks, GCHandleType.Pinned);
                    _dataChunks[0] = default;
                    _dataChunks[0].DataChunkType = Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    _dataChunks[1] = default;
                    _dataChunks[1].DataChunkType = Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                }

                Debug.Assert(_buffer == null || _bufferList == null, "Either 'm_Buffer' or 'm_BufferList' MUST be NULL.");
                Debug.Assert(_shouldCloseOutput || _buffer != null || _bufferList != null, "Either 'm_Buffer' or 'm_BufferList' MUST NOT be NULL.");

                // The underlying byte[] m_Buffer or each m_BufferList[].Array are pinned already
                if (_buffer != null)
                {
                    UpdateDataChunk(0, _buffer, _offset, _count);
                    UpdateDataChunk(1, null, 0, 0);
                    _dataChunkCount = 1;
                }
                else if (_bufferList != null)
                {
                    Debug.Assert(_bufferList != null && _bufferList.Count == 2,
                        "'m_BufferList' MUST NOT be NULL and have exactly '2' items at this point.");
                    UpdateDataChunk(0, _bufferList[0].Array, _bufferList[0].Offset, _bufferList[0].Count);
                    UpdateDataChunk(1, _bufferList[1].Array, _bufferList[1].Offset, _bufferList[1].Count);
                    _dataChunkCount = 2;
                }
                else
                {
                    Debug.Assert(_shouldCloseOutput, "'m_ShouldCloseOutput' MUST be 'true' at this point.");
                    _dataChunks = null;
                }
            }

            private unsafe void UpdateDataChunk(int index, byte[]? buffer, int offset, int count)
            {
                if (buffer == null)
                {
                    _dataChunks![index].pBuffer = null;
                    _dataChunks![index].BufferLength = 0;
                    return;
                }

                if (_webSocket.InternalBuffer.IsInternalBuffer(buffer, offset, count))
                {
                    _dataChunks![index].pBuffer = (byte*)(_webSocket.InternalBuffer.ToIntPtr(offset));
                }
                else
                {
                    _dataChunks![index].pBuffer =
                        (byte*)_webSocket.InternalBuffer.ConvertPinnedSendPayloadToNative(buffer, offset, count);
                }

                _dataChunks![index].BufferLength = (uint)count;
            }

            // Method to mark this object as no longer "in-use".
            // Will also execute a Dispose deferred because I/O was in progress.
            internal void Complete()
            {
                FreeOverlapped(false);
                // Mark as not in-use
                Interlocked.Exchange(ref _operating, Free);

                // Check for deferred Dispose().
                // The deferred Dispose is not guaranteed if Dispose is called while an operation is in progress.
                // The m_DisposeCalled variable is not managed in a thread-safe manner on purpose for performance.
                if (_disposeCalled)
                {
                    Dispose();
                }
            }

            // Method to update internal state after sync or async completion.
            private void SetResults(Exception? exception, int bytesTransferred)
            {
                _exception = exception;
                _bytesTransferred = bytesTransferred;
            }

            internal void FinishOperationFailure(Exception exception, bool syncCompletion)
            {
                SetResults(exception, 0);

                if (NetEventSource.Log.IsEnabled())
                {
                    string methodName = _completedOperation == HttpListenerAsyncOperation.Receive ? nameof(ReadAsyncFast) : nameof(WriteAsyncFast);
                    NetEventSource.Error(_currentStream, $"{methodName} {exception}");
                }

                Complete();
                OnCompleted(this);
            }

            internal void FinishOperationSuccess(int bytesTransferred, bool syncCompletion)
            {
                SetResults(null, bytesTransferred);

                if (NetEventSource.Log.IsEnabled())
                {
                    if (_buffer != null && NetEventSource.Log.IsEnabled())
                    {
                        string methodName = _completedOperation == HttpListenerAsyncOperation.Receive ? nameof(ReadAsyncFast) : nameof(WriteAsyncFast);
                        NetEventSource.DumpBuffer(_currentStream, _buffer, _offset, bytesTransferred, methodName);
                    }
                    else if (_bufferList != null)
                    {
                        Debug.Assert(_completedOperation == HttpListenerAsyncOperation.Send,
                            "'BufferList' is only supported for send operations.");

                        foreach (ArraySegment<byte> buffer in BufferList!)
                        {
                            NetEventSource.DumpBuffer(this, buffer.Array!, buffer.Offset, buffer.Count, nameof(WriteAsyncFast));
                        }
                    }
                }

                if (_shouldCloseOutput)
                {
                    _currentStream._outputStream.SetClosedFlag();
                }

                // Complete the operation and raise completion event.
                Complete();
                OnCompleted(this);
            }

            private unsafe void CompletionPortCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
            {
                if (errorCode == Interop.HttpApi.ERROR_SUCCESS ||
                    errorCode == Interop.HttpApi.ERROR_HANDLE_EOF)
                {
                    FinishOperationSuccess((int)numBytes, false);
                }
                else
                {
                    FinishOperationFailure(new HttpListenerException((int)errorCode), false);
                }
            }

            public enum HttpListenerAsyncOperation
            {
                None,
                Receive,
                Send
            }
        }
    }
}
