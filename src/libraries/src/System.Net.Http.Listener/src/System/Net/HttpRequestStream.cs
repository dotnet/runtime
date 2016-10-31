// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    unsafe class HttpRequestStream : Stream
    {
        private HttpListenerContext m_HttpContext;
        private uint m_DataChunkOffset;
        private int m_DataChunkIndex;
        private bool m_Closed;
        internal const int MaxReadSize = 0x20000; //http.sys recommends we limit reads to 128k
        private bool m_InOpaqueMode;

        internal HttpRequestStream(HttpListenerContext httpContext)
        {
            //GlobalLog.Print("HttpRequestStream#" + LoggingHash.HashString(this) + "::.ctor() HttpListenerContext#" + LoggingHash.HashString(httpContext));
            m_HttpContext = httpContext;
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        internal bool Closed
        {
            get
            {
                return m_Closed;
            }
        }

        internal bool BufferedDataChunksAvailable
        {
            get
            {
                return m_DataChunkIndex > -1;
            }
        }

        // This low level API should only be consumed if the caller can make sure that the state is not corrupted
        // WebSocketHttpListenerDuplexStream (a duplex wrapper around HttpRequestStream/HttpResponseStream)
        // is currenlty the only consumer of this API
        internal HttpListenerContext InternalHttpContext
        {
            get
            {
                return m_HttpContext;
            }
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
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

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(SR.net_noseek);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(SR.net_noseek);
        }

        public override int Read([In, Out] byte[] buffer, int offset, int size)
        {
            //if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "Read", "");
            //GlobalLog.Print("HttpRequestStream#" + LoggingHash.HashString(this) + "::Read() size:" + size + " offset:" + offset);
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (size < 0 || size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException("size");
            }
            if (size == 0 || m_Closed)
            {
                //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Read", "dataRead:0");
                return 0;
            }

            uint dataRead = 0;

            if (m_DataChunkIndex != -1)
            {
                dataRead = Interop.HttpApi.GetChunks(m_HttpContext.Request.RequestBuffer, m_HttpContext.Request.OriginalBlobAddress, ref m_DataChunkIndex, ref m_DataChunkOffset, buffer, offset, size);
            }

            if (m_DataChunkIndex == -1 && dataRead < size)
            {
                //GlobalLog.Print("HttpRequestStream#" + LoggingHash.HashString(this) + "::Read() size:" + size + " offset:" + offset);
                uint statusCode = 0;
                uint extraDataRead = 0;
                offset += (int)dataRead;
                size -= (int)dataRead;

                //the http.sys team recommends that we limit the size to 128kb
                if (size > MaxReadSize)
                {
                    size = MaxReadSize;
                }

                fixed (byte* pBuffer = buffer)
                {
                    // issue unmanaged blocking call
                    //GlobalLog.Print("HttpRequestStream#" + LoggingHash.HashString(this) + "::Read() calling Interop.HttpApi.HttpReceiveRequestEntityBody");

                    uint flags = 0;

                    if (!m_InOpaqueMode)
                    {
                        flags = (uint)Interop.HttpApi.HTTP_FLAGS.HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY;
                    }

                    statusCode =
                        Interop.HttpApi.HttpReceiveRequestEntityBody(
                            m_HttpContext.RequestQueueHandle,
                            m_HttpContext.RequestId,
                            flags,
                            (void*)(pBuffer + offset),
                            (uint)size,
                            out extraDataRead,
                            null);

                    dataRead += extraDataRead;
                    //GlobalLog.Print("HttpRequestStream#" + LoggingHash.HashString(this) + "::Read() call to Interop.HttpApi.HttpReceiveRequestEntityBody returned:" + statusCode + " dataRead:" + dataRead);
                }
                if (statusCode != Interop.HttpApi.ERROR_SUCCESS && statusCode != Interop.HttpApi.ERROR_HANDLE_EOF)
                {
                    Exception exception = new HttpListenerException((int)statusCode);
                    //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "Read", exception);
                    throw exception;
                }
                UpdateAfterRead(statusCode, dataRead);
            }
            //if (NetEventSource.Log.IsEnabled()) NetEventSource.Dump(NetEventSource.ComponentType.HttpListener, this, "Read", buffer, offset, (int)dataRead);
            //GlobalLog.Print("HttpRequestStream#" + LoggingHash.HashString(this) + "::Read() returning dataRead:" + dataRead);
            //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Read", "dataRead:" + dataRead);
            return (int)dataRead;
        }

        void UpdateAfterRead(uint statusCode, uint dataRead)
        {
            //GlobalLog.Print("HttpRequestStream#" + LoggingHash.HashString(this) + "::UpdateAfterRead() statusCode:" + statusCode + " m_Closed:" + m_Closed);
            if (statusCode == Interop.HttpApi.ERROR_HANDLE_EOF || dataRead == 0)
            {
                Close();
            }
            //GlobalLog.Print("HttpRequestStream#" + LoggingHash.HashString(this) + "::UpdateAfterRead() statusCode:" + statusCode + " m_Closed:" + m_Closed);
        }


        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            //if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "BeginRead", "");
            //GlobalLog.Print("HttpRequestStream#" + LoggingHash.HashString(this) + "::BeginRead() buffer.Length:" + buffer.Length + " size:" + size + " offset:" + offset);
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (size < 0 || size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException("size");
            }
            if (size == 0 || m_Closed)
            {
                //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "BeginRead", "");
                HttpRequestStreamAsyncResult result = new HttpRequestStreamAsyncResult(this, state, callback);
                result.InvokeCallback((uint)0);
                return result;
            }

            HttpRequestStreamAsyncResult asyncResult = null;

            uint dataRead = 0;
            if (m_DataChunkIndex != -1)
            {
                dataRead = Interop.HttpApi.GetChunks(m_HttpContext.Request.RequestBuffer, m_HttpContext.Request.OriginalBlobAddress, ref m_DataChunkIndex, ref m_DataChunkOffset, buffer, offset, size);
                if (m_DataChunkIndex != -1 && dataRead == size)
                {
                    asyncResult = new HttpRequestStreamAsyncResult(m_HttpContext.RequestQueueBoundHandle, this, state, callback, buffer, offset, (uint)size, 0);
                    asyncResult.InvokeCallback(dataRead);
                }
            }

            if (m_DataChunkIndex == -1 && dataRead < size)
            {
                //GlobalLog.Print("HttpRequestStream#" + LoggingHash.HashString(this) + "::BeginRead() size:" + size + " offset:" + offset);
                uint statusCode = 0;
                offset += (int)dataRead;
                size -= (int)dataRead;

                //the http.sys team recommends that we limit the size to 128kb
                if (size > MaxReadSize)
                {
                    size = MaxReadSize;
                }

                asyncResult = new HttpRequestStreamAsyncResult(m_HttpContext.RequestQueueBoundHandle, this, state, callback, buffer, offset, (uint)size, dataRead);
                uint bytesReturned;

                try
                {
                    fixed (byte* pBuffer = buffer)
                    {
                        // issue unmanaged blocking call
                        //GlobalLog.Print("HttpRequestStream#" + LoggingHash.HashString(this) + "::BeginRead() calling Interop.HttpApi.HttpReceiveRequestEntityBody");

                        m_HttpContext.EnsureBoundHandle();
                        uint flags = 0;

                        if (!m_InOpaqueMode)
                        {
                            flags = (uint)Interop.HttpApi.HTTP_FLAGS.HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY;
                        }

                        statusCode =
                            Interop.HttpApi.HttpReceiveRequestEntityBody(
                                m_HttpContext.RequestQueueHandle,
                                m_HttpContext.RequestId,
                                flags,
                                asyncResult.m_pPinnedBuffer,
                                (uint)size,
                                out bytesReturned,
                                asyncResult.m_pOverlapped);

                        //GlobalLog.Print("HttpRequestStream#" + LoggingHash.HashString(this) + "::BeginRead() call to Interop.HttpApi.HttpReceiveRequestEntityBody returned:" + statusCode + " dataRead:" + dataRead);
                    }
                }
                catch (Exception /*e*/)
                {
                    //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "BeginRead", e);
                    asyncResult.InternalCleanup();
                    throw;
                }

                if (statusCode != Interop.HttpApi.ERROR_SUCCESS && statusCode != Interop.HttpApi.ERROR_IO_PENDING)
                {
                    asyncResult.InternalCleanup();
                    if (statusCode == Interop.HttpApi.ERROR_HANDLE_EOF)
                    {
                        asyncResult = new HttpRequestStreamAsyncResult(this, state, callback, dataRead);
                        asyncResult.InvokeCallback((uint)0);
                    }
                    else
                    {
                        Exception exception = new HttpListenerException((int)statusCode);
                        //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "BeginRead", exception);
                        asyncResult.InternalCleanup();
                        throw exception;
                    }
                }
                else if (statusCode == Interop.HttpApi.ERROR_SUCCESS &&
                         HttpListener.SkipIOCPCallbackOnSuccess)
                {
                    // IO operation completed synchronously - callback won't be called to signal completion.
                    asyncResult.IOCompleted(statusCode, bytesReturned);
                }
            }
            //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "BeginRead", "");
            return asyncResult;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            //if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "EndRead", "");
            //GlobalLog.Print("HttpRequestStream#" + LoggingHash.HashString(this) + "::EndRead() asyncResult#" + LoggingHash.HashString(asyncResult));
            if (asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }
            HttpRequestStreamAsyncResult castedAsyncResult = asyncResult as HttpRequestStreamAsyncResult;
            if (castedAsyncResult == null || castedAsyncResult.AsyncObject != this)
            {
                throw new ArgumentException(SR.net_io_invalidasyncresult, "asyncResult");
            }
            if (castedAsyncResult.EndCalled)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndRead"));
            }
            castedAsyncResult.EndCalled = true;
            // wait & then check for errors
            object returnValue = castedAsyncResult.InternalWaitForCompletion();
            Exception exception = returnValue as Exception;
            if (exception != null)
            {
                //GlobalLog.Print("HttpRequestStream#" + LoggingHash.HashString(this) + "::EndRead() rethrowing exception:" + exception);
                //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "EndRead", exception);
                throw exception;
            }
            // TODO:
            // add nesting detection?
            // Interlocked.Decrement(ref m_CallNesting);
            uint dataRead = (uint)returnValue;
            UpdateAfterRead((uint)castedAsyncResult.ErrorCode, dataRead);
            //GlobalLog.Print("HttpRequestStream#" + LoggingHash.HashString(this) + "::EndRead() returning returnValue:" + LoggingHash.ObjectToString(returnValue));
            //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "EndRead", "");

            return (int)dataRead + (int)castedAsyncResult.m_dataAlreadyRead;
        }

        public override void Write(byte[] buffer, int offset, int size)
        {
            throw new InvalidOperationException(SR.net_readonlystream);
        }


        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            throw new InvalidOperationException(SR.net_readonlystream);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            throw new InvalidOperationException(SR.net_readonlystream);
        }

        protected override void Dispose(bool disposing)
        {
            //if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "Dispose", "");
            try
            {
                //GlobalLog.Print("HttpRequestStream#" + LoggingHash.HashString(this) + "::Dispose(bool) m_Closed:" + m_Closed);
                m_Closed = true;
            }
            finally
            {
                base.Dispose(disposing);
            }
            //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Dispose", "");
        }

        internal void SwitchToOpaqueMode()
        {
            //GlobalLog.Print("HttpRequestStream#" + LoggingHash.HashString(this) + "::SwitchToOpaqueMode()");
            m_InOpaqueMode = true;
        }

        // This low level API should only be consumed if the caller can make sure that the state is not corrupted
        // WebSocketHttpListenerDuplexStream (a duplex wrapper around HttpRequestStream/HttpResponseStream)
        // is currenlty the only consumer of this API
        internal uint GetChunks(byte[] buffer, int offset, int size)
        {
            return Interop.HttpApi.GetChunks(m_HttpContext.Request.RequestBuffer,
                m_HttpContext.Request.OriginalBlobAddress,
                ref m_DataChunkIndex,
                ref m_DataChunkOffset,
                buffer,
                offset,
                size);
        }

        unsafe class HttpRequestStreamAsyncResult : LazyAsyncResult
        {
            private ThreadPoolBoundHandle m_boundHandle;
            internal NativeOverlapped* m_pOverlapped;
            internal void* m_pPinnedBuffer;
            internal uint m_dataAlreadyRead = 0;

            private static readonly IOCompletionCallback s_IOCallback = new IOCompletionCallback(Callback);

            internal HttpRequestStreamAsyncResult(object asyncObject, object userState, AsyncCallback callback) : base(asyncObject, userState, callback)
            {
            }

            internal HttpRequestStreamAsyncResult(object asyncObject, object userState, AsyncCallback callback, uint dataAlreadyRead) : base(asyncObject, userState, callback)
            {
                m_dataAlreadyRead = dataAlreadyRead;
            }

            internal HttpRequestStreamAsyncResult(ThreadPoolBoundHandle boundHandle, object asyncObject, object userState, AsyncCallback callback, byte[] buffer, int offset, uint size, uint dataAlreadyRead) : base(asyncObject, userState, callback)
            {
                m_dataAlreadyRead = dataAlreadyRead;
                m_boundHandle = boundHandle;
                m_pOverlapped = boundHandle.AllocateNativeOverlapped(s_IOCallback, state: this, pinData: buffer);
                m_pPinnedBuffer = (void*)(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset));
            }

            internal void IOCompleted(uint errorCode, uint numBytes)
            {
                IOCompleted(this, errorCode, numBytes);
            }

            private static void IOCompleted(HttpRequestStreamAsyncResult asyncResult, uint errorCode, uint numBytes)
            {
                //GlobalLog.Print("HttpRequestStreamAsyncResult#" + LoggingHash.HashString(asyncResult) + "::Callback() errorCode:0x" + errorCode.ToString("x8") + " numBytes:" + numBytes);
                object result = null;
                try
                {
                    if (errorCode != Interop.HttpApi.ERROR_SUCCESS && errorCode != Interop.HttpApi.ERROR_HANDLE_EOF)
                    {
                        asyncResult.ErrorCode = (int)errorCode;
                        result = new HttpListenerException((int)errorCode);
                    }
                    else
                    {
                        result = numBytes;
                        //if (NetEventSource.Log.IsEnabled()) NetEventSource.Dump(NetEventSource.ComponentType.HttpListener, asyncResult, "Callback", (IntPtr)asyncResult.m_pPinnedBuffer, (int)numBytes);
                    }
                    //GlobalLog.Print("HttpRequestStreamAsyncResult#" + LoggingHash.HashString(asyncResult) + "::Callback() calling Complete()");
                }
                catch (Exception e)
                {
                    result = e;
                }
                asyncResult.InvokeCallback(result);
            }

            private static unsafe void Callback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
            {
                HttpRequestStreamAsyncResult asyncResult = (HttpRequestStreamAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);

                //GlobalLog.Print("HttpRequestStreamAsyncResult#" + LoggingHash.HashString(asyncResult) + "::Callback() errorCode:0x" + errorCode.ToString("x8") + " numBytes:" + numBytes + " nativeOverlapped:0x" + ((IntPtr)nativeOverlapped).ToString("x8"));

                IOCompleted(asyncResult, errorCode, numBytes);
            }

            // Will be called from the base class upon InvokeCallback()
            protected override void Cleanup()
            {
                base.Cleanup();
                if (m_pOverlapped != null)
                {
                    m_boundHandle.FreeNativeOverlapped(m_pOverlapped);
                }
            }
        }
    }
}
