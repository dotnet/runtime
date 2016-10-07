// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net
{
    unsafe class HttpResponseStreamAsyncResult : LazyAsyncResult
    {
        private readonly ThreadPoolBoundHandle m_boundHandle;
        internal NativeOverlapped* m_pOverlapped;
        private Interop.HttpApi.HTTP_DATA_CHUNK[] m_DataChunks;
        internal bool m_SentHeaders;

        private static readonly IOCompletionCallback s_IOCallback = new IOCompletionCallback(Callback);

        internal ushort dataChunkCount
        {
            get
            {
                if (m_DataChunks == null)
                {
                    return 0;
                }
                else
                {
                    return (ushort)m_DataChunks.Length;
                }
            }
        }

        internal Interop.HttpApi.HTTP_DATA_CHUNK* pDataChunks
        {
            get
            {
                if (m_DataChunks == null)
                {
                    return null;
                }
                else
                {
                    return (Interop.HttpApi.HTTP_DATA_CHUNK*)(Marshal.UnsafeAddrOfPinnedArrayElement(m_DataChunks, 0));
                }
            }
        }

        internal HttpResponseStreamAsyncResult(object asyncObject, object userState, AsyncCallback callback) : base(asyncObject, userState, callback)
        {
        }

        private static byte[] GetChunkHeader(int size, out int offset)
        {
            GlobalLog.Enter("ConnectStream::GetChunkHeader", "size:" + size.ToString());

            uint Mask = 0xf0000000;
            byte[] Header = new byte[10];
            int i;
            offset = -1;

            //
            // Loop through the size, looking at each nibble. If it's not 0
            // convert it to hex. Save the index of the first non-zero
            // byte.
            //
            for (i = 0; i < 8; i++, size <<= 4)
            {
                //
                // offset == -1 means that we haven't found a non-zero nibble
                // yet. If we haven't found one, and the current one is zero,
                // don't do anything.
                //
                if (offset == -1)
                {
                    if ((size & Mask) == 0)
                    {
                        continue;
                    }
                }

                //
                // Either we have a non-zero nibble or we're no longer skipping
                // leading zeros. Convert this nibble to ASCII and save it.
                //
                uint Temp = (uint)size >> 28;

                if (Temp < 10)
                {
                    Header[i] = (byte)(Temp + '0');
                }
                else
                {
                    Header[i] = (byte)((Temp - 10) + 'A');
                }

                //
                // If we haven't found a non-zero nibble yet, we've found one
                // now, so remember that.
                //
                if (offset == -1)
                {
                    offset = i;
                }
            }

            Header[8] = (byte)'\r';
            Header[9] = (byte)'\n';

            GlobalLog.Leave("ConnectStream::GetChunkHeader");
            return Header;
        }

        const string CRLF = "\r\n";
        private static readonly byte[] CRLFArray = new byte[] { (byte)'\r', (byte)'\n' };

        internal HttpResponseStreamAsyncResult(object asyncObject, object userState, AsyncCallback callback, byte[] buffer, int offset, int size, bool chunked, bool sentHeaders, ThreadPoolBoundHandle boundHandle) : base(asyncObject, userState, callback)
        {
            m_boundHandle = boundHandle;
            m_SentHeaders = sentHeaders;

            if (size == 0)
            {
                m_DataChunks = null;
                m_pOverlapped = boundHandle.AllocateNativeOverlapped(s_IOCallback, state: this, pinData: null);
            }
            else
            {
                m_DataChunks = new Interop.HttpApi.HTTP_DATA_CHUNK[chunked ? 3 : 1];

                GlobalLog.Print("HttpResponseStreamAsyncResult#" + LoggingHash.HashString(this) + "::.ctor() m_pOverlapped:0x" + ((IntPtr)m_pOverlapped).ToString("x8"));

                object[] objectsToPin = new object[1 + m_DataChunks.Length];
                objectsToPin[m_DataChunks.Length] = m_DataChunks;


                int chunkHeaderOffset = 0;
                byte[] chunkHeaderBuffer = null;
                if (chunked)
                {
                    chunkHeaderBuffer = GetChunkHeader(size, out chunkHeaderOffset);

                    m_DataChunks[0] = new Interop.HttpApi.HTTP_DATA_CHUNK();
                    m_DataChunks[0].DataChunkType = Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    m_DataChunks[0].BufferLength = (uint)(chunkHeaderBuffer.Length - chunkHeaderOffset);

                    objectsToPin[0] = chunkHeaderBuffer;

                    m_DataChunks[1] = new Interop.HttpApi.HTTP_DATA_CHUNK();
                    m_DataChunks[1].DataChunkType = Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    m_DataChunks[1].BufferLength = (uint)size;

                    objectsToPin[1] = buffer;

                    m_DataChunks[2] = new Interop.HttpApi.HTTP_DATA_CHUNK();
                    m_DataChunks[2].DataChunkType = Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    m_DataChunks[2].BufferLength = (uint)CRLFArray.Length;

                    objectsToPin[2] = CRLFArray;

                }
                else
                {
                    m_DataChunks[0] = new Interop.HttpApi.HTTP_DATA_CHUNK();
                    m_DataChunks[0].DataChunkType = Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    m_DataChunks[0].BufferLength = (uint)size;

                    objectsToPin[0] = buffer;
                }

                // This call will pin needed memory
                m_pOverlapped = boundHandle.AllocateNativeOverlapped(s_IOCallback, state: this, pinData: objectsToPin);

                if (chunked)
                {
                    m_DataChunks[0].pBuffer = (byte*)(Marshal.UnsafeAddrOfPinnedArrayElement(chunkHeaderBuffer, chunkHeaderOffset));
                    m_DataChunks[1].pBuffer = (byte*)(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset));
                    m_DataChunks[2].pBuffer = (byte*)(Marshal.UnsafeAddrOfPinnedArrayElement(CRLFArray, 0));
                }
                else
                {
                    m_DataChunks[0].pBuffer = (byte*)(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset));
                }

            }
        }

        internal void IOCompleted(uint errorCode, uint numBytes)
        {
            IOCompleted(this, errorCode, numBytes);
        }

        private static void IOCompleted(HttpResponseStreamAsyncResult asyncResult, uint errorCode, uint numBytes)
        {
            GlobalLog.Print("HttpResponseStreamAsyncResult#" + LoggingHash.HashString(asyncResult) + "::Callback() errorCode:0x" + errorCode.ToString("x8") + " numBytes:" + numBytes);
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
                    // if we sent headers and body together, numBytes will be the total, but we need to only account for the data
                    // result = numBytes;
                    if (asyncResult.m_DataChunks == null)
                    {
                        result = (uint)0;
                        if (NetEventSource.Log.IsEnabled()) { NetEventSource.Dump(NetEventSource.ComponentType.HttpListener, asyncResult, "Callback", IntPtr.Zero, 0); }
                    }
                    else
                    {
                        result = asyncResult.m_DataChunks.Length == 1 ? asyncResult.m_DataChunks[0].BufferLength : 0;
                        if (NetEventSource.Log.IsEnabled()) { for (int i = 0; i < asyncResult.m_DataChunks.Length; i++) { NetEventSource.Dump(NetEventSource.ComponentType.HttpListener, asyncResult, "Callback", (IntPtr)asyncResult.m_DataChunks[0].pBuffer, (int)asyncResult.m_DataChunks[0].BufferLength); } }
                    }
                }
                GlobalLog.Print("HttpResponseStreamAsyncResult#" + LoggingHash.HashString(asyncResult) + "::Callback() calling Complete()");
            }
            catch (Exception e)
            {
                result = e;
            }
            asyncResult.InvokeCallback(result);
        }

        private static unsafe void Callback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            object state = ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
            HttpResponseStreamAsyncResult asyncResult = state as HttpResponseStreamAsyncResult;
            GlobalLog.Print("HttpResponseStreamAsyncResult#" + LoggingHash.HashString(asyncResult) + "::Callback() errorCode:0x" + errorCode.ToString("x8") + " numBytes:" + numBytes + " nativeOverlapped:0x" + ((IntPtr)nativeOverlapped).ToString("x8"));

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
