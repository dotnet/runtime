// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net
{
    internal sealed class HttpListenerSession
    {
        public readonly HttpListener Listener;
        public readonly SafeHandle RequestQueueHandle;
        private ThreadPoolBoundHandle? _requestQueueBoundHandle;

        public ThreadPoolBoundHandle RequestQueueBoundHandle
        {
            get
            {
                if (_requestQueueBoundHandle == null)
                {
                    lock (this)
                    {
                        if (_requestQueueBoundHandle == null)
                        {
                            _requestQueueBoundHandle = ThreadPoolBoundHandle.BindHandle(RequestQueueHandle);
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info($"ThreadPoolBoundHandle.BindHandle({RequestQueueHandle}) -> {_requestQueueBoundHandle}");
                        }
                    }
                }

                return _requestQueueBoundHandle;
            }
        }

        public unsafe HttpListenerSession(HttpListener listener)
        {
            Listener = listener;

            uint statusCode =
                Interop.HttpApi.HttpCreateRequestQueue(
                    Interop.HttpApi.s_version, null!, null, 0, out HttpRequestQueueV2Handle requestQueueHandle);

            if (statusCode != Interop.HttpApi.ERROR_SUCCESS)
            {
                throw new HttpListenerException((int)statusCode);
            }

            // Disabling callbacks when IO operation completes synchronously (returns ErrorCodes.ERROR_SUCCESS)
            if (HttpListener.SkipIOCPCallbackOnSuccess &&
                !Interop.Kernel32.SetFileCompletionNotificationModes(
                    requestQueueHandle,
                    Interop.Kernel32.FileCompletionNotificationModes.SkipCompletionPortOnSuccess |
                    Interop.Kernel32.FileCompletionNotificationModes.SkipSetEventOnHandle))
            {
                throw new HttpListenerException(Marshal.GetLastPInvokeError());
            }

            RequestQueueHandle = requestQueueHandle;
        }

        public unsafe void CloseRequestQueueHandle()
        {
            lock (this)
            {
                if (!RequestQueueHandle.IsInvalid)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info($"Dispose ThreadPoolBoundHandle: {_requestQueueBoundHandle}");
                    _requestQueueBoundHandle?.Dispose();
                    RequestQueueHandle.Dispose();

                    // CancelIoEx is called after Dispose to prevent a race condition involving parallel GetContext and
                    // HttpReceiveHttpRequest calls. Otherwise, calling CancelIoEx before Dispose might block the synchronous
                    // GetContext call until the next request arrives.
                    try
                    {
                        Interop.Kernel32.CancelIoEx(RequestQueueHandle, null); // This cancels the synchronous call to HttpReceiveHttpRequest
                    }
                    catch (ObjectDisposedException)
                    {
                        // Ignore the exception since it only means that the queue handle has been successfully disposed
                    }
                }
            }
        }
    }
}
