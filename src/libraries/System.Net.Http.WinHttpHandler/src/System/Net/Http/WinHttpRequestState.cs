// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http
{
    internal sealed class WinHttpRequestState : IDisposable
    {
#if DEBUG
        private static int s_dbg_allocated;
        private static int s_dbg_pin;
        private static int s_dbg_clearSendRequestState;
        private static int s_dbg_callDispose;
        private static int s_dbg_operationHandleFree;

        private IntPtr s_dbg_requestHandle;
#endif

        // A GCHandle for this operation object.
        // This is owned by the callback and will be deallocated when the sessionHandle has been closed.
        private GCHandle _operationHandle;
        private WinHttpTransportContext? _transportContext;
        private volatile bool _disposed; // To detect redundant calls.

        public WinHttpRequestState()
        {
#if DEBUG
            Interlocked.Increment(ref s_dbg_allocated);
#endif
        }

        public void Pin()
        {
            if (!_operationHandle.IsAllocated)
            {
#if DEBUG
                Interlocked.Increment(ref s_dbg_pin);
#endif
                _operationHandle = GCHandle.Alloc(this);
            }
        }

        public static WinHttpRequestState? FromIntPtr(IntPtr gcHandle)
        {
            GCHandle stateHandle = GCHandle.FromIntPtr(gcHandle);
            return (WinHttpRequestState?)stateHandle.Target;
        }

        public IntPtr ToIntPtr()
        {
            return GCHandle.ToIntPtr(_operationHandle);
        }

        // The current locking mechanism doesn't allow any two WinHttp functions executing at
        // the same time for the same handle. Enhance locking to prevent only WinHttpCloseHandle being called
        // during other API execution. E.g. using a Reader/Writer model or, even better, Interlocked functions.
        // The lock object must be used during the execution of any WinHttp function to ensure no race conditions with
        // calling WinHttpCloseHandle.
        public object Lock => this;

        public void ClearSendRequestState()
        {
#if DEBUG
            Interlocked.Increment(ref s_dbg_clearSendRequestState);
#endif
            // Since WinHttpRequestState has a self-referenced strong GCHandle, we
            // need to clear out object references to break cycles and prevent leaks.
            Tcs = null;
            TcsInternalWriteDataToRequestStream = null;
            CancellationToken = default(CancellationToken);
            RequestMessage = null;
            Handler = null;
            ServerCertificateValidationCallback = null;
            TransportContext = null;
            Proxy = null;
            ServerCredentials = null;
            DefaultProxyCredentials = null;

            if (RequestHandle != null)
            {
                RequestHandle.Dispose();
                RequestHandle = null;
            }
        }

        public TaskCompletionSource<HttpResponseMessage>? Tcs { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public HttpRequestMessage? RequestMessage { get; set; }

        public WinHttpHandler? Handler { get; set; }

        private SafeWinHttpHandle? _requestHandle;
        public SafeWinHttpHandle? RequestHandle
        {
            get
            {
                return _requestHandle;
            }

            set
            {
#if DEBUG
                if (value != null)
                {
                    s_dbg_requestHandle = value.DangerousGetHandle();
                }
#endif
                _requestHandle = value;
            }
        }

        public Exception? SavedException { get; set; }

        public bool CheckCertificateRevocationList { get; set; }

        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool>? ServerCertificateValidationCallback { get; set; }

        [AllowNull]
        public WinHttpTransportContext TransportContext
        {
            get { return _transportContext ?? (_transportContext = new WinHttpTransportContext()); }
            set { _transportContext = value; }
        }

        public WindowsProxyUsePolicy WindowsProxyUsePolicy { get; set; }

        public IWebProxy? Proxy { get; set; }

        public ICredentials? ServerCredentials { get; set; }

        public ICredentials? DefaultProxyCredentials { get; set; }

        public bool PreAuthenticate { get; set; }

        public HttpStatusCode LastStatusCode { get; set; }

        public bool RetryRequest { get; set; }

        public RendezvousAwaitable<int> LifecycleAwaitable { get; set; } = new RendezvousAwaitable<int>();
        public TaskCompletionSource<bool>? TcsInternalWriteDataToRequestStream { get; set; }
        public bool AsyncReadInProgress { get; set; }

        // WinHttpResponseStream state.
        public long? ExpectedBytesToRead { get; set; }
        public long CurrentBytesRead { get; set; }

        private GCHandle _cachedReceivePinnedBuffer;

        public void PinReceiveBuffer(byte[] buffer)
        {
            if (!_cachedReceivePinnedBuffer.IsAllocated || _cachedReceivePinnedBuffer.Target != buffer)
            {
                if (_cachedReceivePinnedBuffer.IsAllocated)
                {
                    _cachedReceivePinnedBuffer.Free();
                }

                _cachedReceivePinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            }
        }

        #region IDisposable Members
        private void Dispose(bool disposing)
        {
#if DEBUG
            Interlocked.Increment(ref s_dbg_callDispose);
#endif
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"GCHandle=0x{ToIntPtr():X}, disposed={_disposed}, disposing={disposing}");

            // Since there is no finalizer and this class is sealed, the disposing parameter should be TRUE.
            Debug.Assert(disposing, "WinHttpRequestState.Dispose() should have disposing=TRUE");

            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_operationHandle.IsAllocated)
            {
                // This method only gets called when the WinHTTP request handle is fully closed and thus all
                // async operations are done. So, it is safe at this point to unpin the buffers and release
                // the strong GCHandle for this object.
                if (_cachedReceivePinnedBuffer.IsAllocated)
                {
                    _cachedReceivePinnedBuffer.Free();
                    _cachedReceivePinnedBuffer = default(GCHandle);
                }
#if DEBUG
                Interlocked.Increment(ref s_dbg_operationHandleFree);
#endif
                _operationHandle.Free();
                _operationHandle = default(GCHandle);
            }
        }

        public void Dispose()
        {
            // No need to suppress finalization since the finalizer is not overridden and the class is sealed.
            Dispose(true);
        }
        #endregion
    }
}
