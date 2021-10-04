// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net
{
    internal sealed unsafe class HttpResponseStreamAsyncResult : LazyAsyncResult
    {
        private readonly ThreadPoolBoundHandle? _boundHandle;
        internal NativeOverlapped* _pOverlapped;
        private readonly Interop.HttpApi.HTTP_DATA_CHUNK[]? _dataChunks;
        internal bool _sentHeaders;

        private static readonly IOCompletionCallback s_IOCallback = new IOCompletionCallback(Callback);

        internal ushort dataChunkCount
        {
            get
            {
                if (_dataChunks == null)
                {
                    return 0;
                }
                else
                {
                    return (ushort)_dataChunks.Length;
                }
            }
        }

        internal Interop.HttpApi.HTTP_DATA_CHUNK* pDataChunks
        {
            get
            {
                if (_dataChunks == null)
                {
                    return null;
                }
                else
                {
                    return (Interop.HttpApi.HTTP_DATA_CHUNK*)(Marshal.UnsafeAddrOfPinnedArrayElement(_dataChunks, 0));
                }
            }
        }

        internal HttpResponseStreamAsyncResult(object asyncObject, object? userState, AsyncCallback? callback) : base(asyncObject, userState, callback)
        {
        }

        private static byte[] GetChunkHeader(int size, out int offset)
        {
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

            return Header;
        }

        private static readonly byte[] s_CRLFArray = new byte[] { (byte)'\r', (byte)'\n' };

        internal HttpResponseStreamAsyncResult(object asyncObject, object? userState, AsyncCallback? callback, byte[] buffer, int offset, int size, bool chunked, bool sentHeaders, ThreadPoolBoundHandle boundHandle) : base(asyncObject, userState, callback)
        {
            _boundHandle = boundHandle;
            _sentHeaders = sentHeaders;

            if (size == 0)
            {
                _dataChunks = null;
                _pOverlapped = boundHandle.AllocateNativeOverlapped(s_IOCallback, state: this, pinData: null);
            }
            else
            {
                _dataChunks = new Interop.HttpApi.HTTP_DATA_CHUNK[chunked ? 3 : 1];

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "m_pOverlapped:0x" + ((IntPtr)_pOverlapped).ToString("x8"));

                object[] objectsToPin = new object[1 + _dataChunks.Length];
                objectsToPin[_dataChunks.Length] = _dataChunks;


                int chunkHeaderOffset = 0;
                byte[]? chunkHeaderBuffer = null;
                if (chunked)
                {
                    chunkHeaderBuffer = GetChunkHeader(size, out chunkHeaderOffset);

                    _dataChunks[0] = default;
                    _dataChunks[0].DataChunkType = Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    _dataChunks[0].BufferLength = (uint)(chunkHeaderBuffer.Length - chunkHeaderOffset);

                    objectsToPin[0] = chunkHeaderBuffer;

                    _dataChunks[1] = default;
                    _dataChunks[1].DataChunkType = Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    _dataChunks[1].BufferLength = (uint)size;

                    objectsToPin[1] = buffer;

                    _dataChunks[2] = default;
                    _dataChunks[2].DataChunkType = Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    _dataChunks[2].BufferLength = (uint)s_CRLFArray.Length;

                    objectsToPin[2] = s_CRLFArray;
                }
                else
                {
                    _dataChunks[0] = default;
                    _dataChunks[0].DataChunkType = Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    _dataChunks[0].BufferLength = (uint)size;

                    objectsToPin[0] = buffer;
                }

                // This call will pin needed memory
                _pOverlapped = boundHandle.AllocateNativeOverlapped(s_IOCallback, state: this, pinData: objectsToPin);

                if (chunked)
                {
                    _dataChunks[0].pBuffer = (byte*)(Marshal.UnsafeAddrOfPinnedArrayElement(chunkHeaderBuffer!, chunkHeaderOffset));
                    _dataChunks[1].pBuffer = (byte*)(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset));
                    _dataChunks[2].pBuffer = (byte*)(Marshal.UnsafeAddrOfPinnedArrayElement(s_CRLFArray, 0));
                }
                else
                {
                    _dataChunks[0].pBuffer = (byte*)(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset));
                }
            }
        }

        internal void IOCompleted(uint errorCode, uint numBytes)
        {
            IOCompleted(this, errorCode, numBytes);
        }

        private static void IOCompleted(HttpResponseStreamAsyncResult asyncResult, uint errorCode, uint numBytes)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"errorCode:0x{errorCode:x8} numBytes: {numBytes}");
            object? result = null;
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
                    if (asyncResult._dataChunks == null)
                    {
                        result = (uint)0;
                        if (NetEventSource.Log.IsEnabled()) { NetEventSource.DumpBuffer(null, IntPtr.Zero, 0); }
                    }
                    else
                    {
                        result = asyncResult._dataChunks.Length == 1 ? asyncResult._dataChunks[0].BufferLength : 0;
                        if (NetEventSource.Log.IsEnabled()) { for (int i = 0; i < asyncResult._dataChunks.Length; i++) { NetEventSource.DumpBuffer(null, (IntPtr)asyncResult._dataChunks[0].pBuffer, (int)asyncResult._dataChunks[0].BufferLength); } }
                    }
                }
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "Calling Complete()");
            }
            catch (Exception e)
            {
                result = e;
            }
            asyncResult.InvokeCallback(result);
        }

        private static unsafe void Callback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            object state = ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped)!;
            HttpResponseStreamAsyncResult asyncResult = (state as HttpResponseStreamAsyncResult)!;
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "errorCode:0x" + errorCode.ToString("x8") + " numBytes:" + numBytes + " nativeOverlapped:0x" + ((IntPtr)nativeOverlapped).ToString("x8"));

            IOCompleted(asyncResult, errorCode, numBytes);
        }

        // Will be called from the base class upon InvokeCallback()
        protected override void Cleanup()
        {
            base.Cleanup();
            if (_pOverlapped != null)
            {
                _boundHandle!.FreeNativeOverlapped(_pOverlapped);
            }
        }
    }
}
