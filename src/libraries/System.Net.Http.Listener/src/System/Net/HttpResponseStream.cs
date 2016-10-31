// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Net
{
    unsafe class HttpResponseStream : Stream
    {
        private HttpListenerContext m_HttpContext;
        private long m_LeftToWrite = long.MinValue;
        private bool m_Closed;
        private bool m_InOpaqueMode;
        // The last write needs special handling to cancel.
        private HttpResponseStreamAsyncResult m_LastWrite;

        internal HttpResponseStream(HttpListenerContext httpContext)
        {
            //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::.ctor() HttpListenerContext##" + LoggingHash.HashString(httpContext));
            m_HttpContext = httpContext;
        }

        internal Interop.HttpApi.HTTP_FLAGS ComputeLeftToWrite()
        {
            //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::ComputeLeftToWrite() on entry m_LeftToWrite:" + m_LeftToWrite);
            Interop.HttpApi.HTTP_FLAGS flags = Interop.HttpApi.HTTP_FLAGS.NONE;
            if (!m_HttpContext.Response.ComputedHeaders)
            {
                flags = m_HttpContext.Response.ComputeHeaders();
            }
            if (m_LeftToWrite == long.MinValue)
            {
                Interop.HttpApi.HTTP_VERB method = m_HttpContext.GetKnownMethod();
                m_LeftToWrite = method != Interop.HttpApi.HTTP_VERB.HttpVerbHEAD ? m_HttpContext.Response.ContentLength64 : 0;
                //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::ComputeLeftToWrite() computed m_LeftToWrite:" + m_LeftToWrite);
            }
            return flags;
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
                return true;
            }
        }

        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        internal bool Closed
        {
            get
            {
                return m_Closed;
            }
        }

        internal HttpListenerContext InternalHttpContext
        {
            get
            {
                return m_HttpContext;
            }
        }

        internal void SetClosedFlag()
        {
            m_Closed = true;
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
            throw new InvalidOperationException(SR.net_writeonlystream);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            throw new InvalidOperationException(SR.net_writeonlystream);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            throw new InvalidOperationException(SR.net_writeonlystream);
        }

        public override void Write(byte[] buffer, int offset, int size)
        {
            //if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "Write", "");
            //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::Write() buffer.Length:" + buffer.Length + " size:" + size + " offset:" + offset);
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
            Interop.HttpApi.HTTP_FLAGS flags = ComputeLeftToWrite();
            if (m_Closed || (size == 0 && m_LeftToWrite != 0))
            {
                //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Write", "");
                return;
            }
            if (m_LeftToWrite >= 0 && size > m_LeftToWrite)
            {
                throw new ProtocolViolationException(SR.net_entitytoobig);
            }

            uint statusCode;
            uint dataToWrite = (uint)size;
            SafeLocalAllocHandle bufferAsIntPtr = null;
            IntPtr pBufferAsIntPtr = IntPtr.Zero;
            bool sentHeaders = m_HttpContext.Response.SentHeaders;
            try
            {
                if (size == 0)
                {
                    statusCode = m_HttpContext.Response.SendHeaders(null, null, flags, false);
                }
                else
                {
                    fixed (byte* pDataBuffer = buffer)
                    {
                        byte* pBuffer = pDataBuffer;
                        if (m_HttpContext.Response.BoundaryType == BoundaryType.Chunked)
                        {
                            // TODO:
                            // here we need some heuristics, some time it is definitely better to split this in 3 write calls
                            // but for small writes it is probably good enough to just copy the data internally.
                            string chunkHeader = size.ToString("x", CultureInfo.InvariantCulture);
                            dataToWrite = dataToWrite + (uint)(chunkHeader.Length + 4);
                            bufferAsIntPtr = SafeLocalAllocHandle.LocalAlloc((int)dataToWrite);
                            pBufferAsIntPtr = bufferAsIntPtr.DangerousGetHandle();
                            for (int i = 0; i < chunkHeader.Length; i++)
                            {
                                Marshal.WriteByte(pBufferAsIntPtr, i, (byte)chunkHeader[i]);
                            }
                            Marshal.WriteInt16(pBufferAsIntPtr, chunkHeader.Length, 0x0A0D);
                            Marshal.Copy(buffer, offset, IntPtrHelper.Add(pBufferAsIntPtr, chunkHeader.Length + 2), size);
                            Marshal.WriteInt16(pBufferAsIntPtr, (int)(dataToWrite - 2), 0x0A0D);
                            pBuffer = (byte*)pBufferAsIntPtr;
                            offset = 0;
                        }
                        Interop.HttpApi.HTTP_DATA_CHUNK dataChunk = new Interop.HttpApi.HTTP_DATA_CHUNK();
                        dataChunk.DataChunkType = Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                        dataChunk.pBuffer = (byte*)(pBuffer + offset);
                        dataChunk.BufferLength = dataToWrite;

                        flags |= m_LeftToWrite == size ? Interop.HttpApi.HTTP_FLAGS.NONE : Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA;
                        if (!sentHeaders)
                        {
                            statusCode = m_HttpContext.Response.SendHeaders(&dataChunk, null, flags, false);
                        }
                        else
                        {
                            //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::Write() calling Interop.HttpApi.HttpSendResponseEntityBody");

                            statusCode =
                                Interop.HttpApi.HttpSendResponseEntityBody(
                                    m_HttpContext.RequestQueueHandle,
                                    m_HttpContext.RequestId,
                                    (uint)flags,
                                    1,
                                    &dataChunk,
                                    null,
                                    SafeLocalAllocHandle.Zero,
                                    0,
                                    null,
                                    null);

                            //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::Write() call to Interop.HttpApi.HttpSendResponseEntityBody returned:" + statusCode);
                            if (m_HttpContext.Listener.IgnoreWriteExceptions)
                            {
                                //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::Write() suppressing error");
                                statusCode = Interop.HttpApi.ERROR_SUCCESS;
                            }
                        }
                    }
                }
            }
            finally
            {
                if (bufferAsIntPtr != null)
                {
                    // free unmanaged buffer
                    bufferAsIntPtr.Close();
                }
            }

            if (statusCode != Interop.HttpApi.ERROR_SUCCESS && statusCode != Interop.HttpApi.ERROR_HANDLE_EOF)
            {
                Exception exception = new HttpListenerException((int)statusCode);
                //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "Write", exception);
                m_Closed = true;
                m_HttpContext.Abort();
                throw exception;
            }
            UpdateAfterWrite(dataToWrite);
            //if (NetEventSource.Log.IsEnabled()) NetEventSource.Dump(NetEventSource.ComponentType.HttpListener, this, "Write", buffer, offset, (int)dataToWrite);
            //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Write", "");
        }


        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::BeginWrite() buffer.Length:" + buffer.Length + " size:" + size + " offset:" + offset);
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
            Interop.HttpApi.HTTP_FLAGS flags = ComputeLeftToWrite();
            if (m_Closed || (size == 0 && m_LeftToWrite != 0))
            {
                //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "BeginWrite", "");
                HttpResponseStreamAsyncResult result = new HttpResponseStreamAsyncResult(this, state, callback);
                result.InvokeCallback((uint)0);
                return result;
            }
            if (m_LeftToWrite >= 0 && size > m_LeftToWrite)
            {
                throw new ProtocolViolationException(SR.net_entitytoobig);
            }

            uint statusCode;
            uint bytesSent = 0;
            flags |= m_LeftToWrite == size ? Interop.HttpApi.HTTP_FLAGS.NONE : Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA;
            bool sentHeaders = m_HttpContext.Response.SentHeaders;
            HttpResponseStreamAsyncResult asyncResult = new HttpResponseStreamAsyncResult(this, state, callback, buffer, offset, size, m_HttpContext.Response.BoundaryType == BoundaryType.Chunked, sentHeaders, m_HttpContext.RequestQueueBoundHandle);

            // Update m_LeftToWrite now so we can queue up additional BeginWrite's without waiting for EndWrite.
            UpdateAfterWrite((uint)((m_HttpContext.Response.BoundaryType == BoundaryType.Chunked) ? 0 : size));

            try
            {
                if (!sentHeaders)
                {
                    statusCode = m_HttpContext.Response.SendHeaders(null, asyncResult, flags, false);
                }
                else
                {
                    //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::BeginWrite() calling Interop.HttpApi.HttpSendResponseEntityBody");

                    m_HttpContext.EnsureBoundHandle();
                    statusCode =
                        Interop.HttpApi.HttpSendResponseEntityBody(
                            m_HttpContext.RequestQueueHandle,
                            m_HttpContext.RequestId,
                            (uint)flags,
                            asyncResult.dataChunkCount,
                            asyncResult.pDataChunks,
                            &bytesSent,
                            SafeLocalAllocHandle.Zero,
                            0,
                            asyncResult.m_pOverlapped,
                            null);

                    //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::BeginWrite() call to Interop.HttpApi.HttpSendResponseEntityBody returned:" + statusCode);
                }
            }
            catch (Exception /*e*/)
            {
                //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "BeginWrite", e);
                asyncResult.InternalCleanup();
                m_Closed = true;
                m_HttpContext.Abort();
                throw;
            }

            if (statusCode != Interop.HttpApi.ERROR_SUCCESS && statusCode != Interop.HttpApi.ERROR_IO_PENDING)
            {
                asyncResult.InternalCleanup();
                if (m_HttpContext.Listener.IgnoreWriteExceptions && sentHeaders)
                {
                    //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::BeginWrite() suppressing error");
                }
                else
                {
                    Exception exception = new HttpListenerException((int)statusCode);
                    //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "BeginWrite", exception);
                    m_Closed = true;
                    m_HttpContext.Abort();
                    throw exception;
                }
            }

            if (statusCode == Interop.HttpApi.ERROR_SUCCESS && HttpListener.SkipIOCPCallbackOnSuccess)
            {
                // IO operation completed synchronously - callback won't be called to signal completion.
                asyncResult.IOCompleted(statusCode, bytesSent);
            }

            // Last write, cache it for special cancelation handling.
            if ((flags & Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA) == 0)
            {
                m_LastWrite = asyncResult;
            }

            //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "BeginWrite", "");
            return asyncResult;
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            //if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "EndWrite", "");
            //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::EndWrite() asyncResult#" + LoggingHash.HashString(asyncResult));
            if (asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }
            HttpResponseStreamAsyncResult castedAsyncResult = asyncResult as HttpResponseStreamAsyncResult;
            if (castedAsyncResult == null || castedAsyncResult.AsyncObject != this)
            {
                throw new ArgumentException(SR.net_io_invalidasyncresult, "asyncResult");
            }
            if (castedAsyncResult.EndCalled)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndWrite"));
            }
            castedAsyncResult.EndCalled = true;
            // wait & then check for errors
            object returnValue = castedAsyncResult.InternalWaitForCompletion();

            Exception exception = returnValue as Exception;
            if (exception != null)
            {
                //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::EndWrite() rethrowing exception:" + exception);
                //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "EndWrite", exception);
                m_Closed = true;
                m_HttpContext.Abort();
                throw exception;
            }
            // TODO:
            // add nesting detection?
            // Interlocked.Decrement(ref m_CallNesting);
            //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::EndWrite()");
            //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "EndWrite", "");
        }

        void UpdateAfterWrite(uint dataWritten)
        {
            //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::UpdateAfterWrite() dataWritten:" + dataWritten + " m_LeftToWrite:" + m_LeftToWrite + " m_Closed:" + m_Closed);
            if (!m_InOpaqueMode)
            {
                if (m_LeftToWrite > 0)
                {
                    // keep track of the data transferred
                    m_LeftToWrite -= dataWritten;
                }
                if (m_LeftToWrite == 0)
                {
                    // in this case we already passed 0 as the flag, so we don't need to call HttpSendResponseEntityBody() when we Close()
                    m_Closed = true;
                }
            }
            //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::UpdateAfterWrite() dataWritten:" + dataWritten + " m_LeftToWrite:" + m_LeftToWrite + " m_Closed:" + m_Closed);
        }

        protected override void Dispose(bool disposing)
        {
            //if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "Close", "");

            try
            {
                if (disposing)
                {
                    //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::Close() m_Closed:" + m_Closed);
                    if (m_Closed)
                    {
                        //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Close", "");
                        return;
                    }
                    m_Closed = true;
                    Interop.HttpApi.HTTP_FLAGS flags = ComputeLeftToWrite();
                    if (m_LeftToWrite > 0 && !m_InOpaqueMode)
                    {
                        throw new InvalidOperationException(SR.net_io_notenoughbyteswritten);
                    }
                    bool sentHeaders = m_HttpContext.Response.SentHeaders;
                    if (sentHeaders && m_LeftToWrite == 0)
                    {
                        //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Close", "");
                        return;
                    }

                    uint statusCode = 0;
                    if ((m_HttpContext.Response.BoundaryType == BoundaryType.Chunked || m_HttpContext.Response.BoundaryType == BoundaryType.None) && (String.Compare(m_HttpContext.Request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase) != 0))
                    {
                        if (m_HttpContext.Response.BoundaryType == BoundaryType.None)
                        {
                            flags |= Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_DISCONNECT;
                        }
                        fixed (void* pBuffer = NclConstants.ChunkTerminator)
                        {
                            Interop.HttpApi.HTTP_DATA_CHUNK* pDataChunk = null;
                            if (m_HttpContext.Response.BoundaryType == BoundaryType.Chunked)
                            {
                                Interop.HttpApi.HTTP_DATA_CHUNK dataChunk = new Interop.HttpApi.HTTP_DATA_CHUNK();
                                dataChunk.DataChunkType = Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                                dataChunk.pBuffer = (byte*)pBuffer;
                                dataChunk.BufferLength = (uint)NclConstants.ChunkTerminator.Length;
                                pDataChunk = &dataChunk;
                            }
                            if (!sentHeaders)
                            {
                                statusCode = m_HttpContext.Response.SendHeaders(pDataChunk, null, flags, false);
                            }
                            else
                            {
                                //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::Close() calling Interop.HttpApi.HttpSendResponseEntityBody");

                                statusCode =
                                    Interop.HttpApi.HttpSendResponseEntityBody(
                                        m_HttpContext.RequestQueueHandle,
                                        m_HttpContext.RequestId,
                                        (uint)flags,
                                        pDataChunk != null ? (ushort)1 : (ushort)0,
                                        pDataChunk,
                                        null,
                                        SafeLocalAllocHandle.Zero,
                                        0,
                                        null,
                                        null);

                                //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::Close() call to Interop.HttpApi.HttpSendResponseEntityBody returned:" + statusCode);
                                if (m_HttpContext.Listener.IgnoreWriteExceptions)
                                {
                                    //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::Close() suppressing error");
                                    statusCode = Interop.HttpApi.ERROR_SUCCESS;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!sentHeaders)
                        {
                            statusCode = m_HttpContext.Response.SendHeaders(null, null, flags, false);
                        }
                    }
                    if (statusCode != Interop.HttpApi.ERROR_SUCCESS && statusCode != Interop.HttpApi.ERROR_HANDLE_EOF)
                    {
                        Exception exception = new HttpListenerException((int)statusCode);
                        //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exception(NetEventSource.ComponentType.HttpListener, this, "Close", exception);
                        m_HttpContext.Abort();
                        throw exception;
                    }
                    m_LeftToWrite = 0;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
            //if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Dispose", "");
        }

        internal void SwitchToOpaqueMode()
        {
            //GlobalLog.Print("HttpResponseStream#" + LoggingHash.HashString(this) + "::SwitchToOpaqueMode()");
            m_InOpaqueMode = true;
            m_LeftToWrite = long.MaxValue;
        }

        // The final Content-Length async write can only be cancelled by CancelIoEx.
        // Sync can only be cancelled by CancelSynchronousIo, but we don't attempt this right now.
        internal void CancelLastWrite(SafeHandle requestQueueHandle)
        {
            HttpResponseStreamAsyncResult asyncState = m_LastWrite;
            if (asyncState != null && !asyncState.IsCompleted)
            {
                // It is safe to ignore the return value on a cancel operation because the connection is being closed
                Interop.mincore.CancelIoEx(requestQueueHandle, asyncState.m_pOverlapped);
            }
        }
    }

}
