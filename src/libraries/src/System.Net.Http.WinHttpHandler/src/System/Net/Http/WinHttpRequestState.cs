// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
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
        // TODO (Issue 2506): The current locking mechanism doesn't allow any two WinHttp functions executing at
        // the same time for the same handle. Enahnce locking to prevent only WinHttpCloseHandle being called
        // during other API execution. E.g. using a Reader/Writer model or, even better, Interlocked functions.

        // The _lock object must be used during the execution of any WinHttp function to ensure no race conditions with 
        // calling WinHttpCloseHandle.
        private readonly object _lock = new object();

        // A GCHandle for this operation object.
        // This is owned by the callback and will be deallocated when the sessionHandle has been closed.
        private GCHandle _operationHandle = new GCHandle();

        private volatile bool _disposed = false; // To detect redundant calls.

        public WinHttpRequestState()
        {
            TransportContext = new WinHttpTransportContext();
            _operationHandle = GCHandle.Alloc(this);
        }

        public static WinHttpRequestState FromIntPtr(IntPtr gcHandle)
        {
            GCHandle stateHandle = GCHandle.FromIntPtr(gcHandle);
            return (WinHttpRequestState)stateHandle.Target;
        }        

        public IntPtr ToIntPtr()
        {
            return GCHandle.ToIntPtr(_operationHandle);
        }

        public object Lock
        {
            get
            {
                return _lock;
            }
        }

        public void ClearSendRequestState()
        {
            // Since WinHttpRequestState has a self-referenced strong GCHandle, we
            // need to clear out object references to break cycles and prevent leaks.
            Tcs = null;
            TcsSendRequest = null;
            TcsWriteToRequestStream = null;
            TcsInternalWriteDataToRequestStream = null;
            TcsReceiveResponseHeaders = null;
            RequestMessage = null;
            Handler = null;
            ServerCertificateValidationCallback = null;
            TransportContext = null;
            Proxy = null;
            ServerCredentials = null;
            DefaultProxyCredentials = null;
        }

        public TaskCompletionSource<HttpResponseMessage> Tcs { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public HttpRequestMessage RequestMessage { get; set; }

        public WinHttpHandler Handler { get; set; }

        public SafeWinHttpHandle RequestHandle { get; set; }

        public Exception SavedException { get; set; }

        public bool CheckCertificateRevocationList { get; set; }

        public Func<
            HttpRequestMessage,
            X509Certificate2,
            X509Chain,
            SslPolicyErrors,
            bool> ServerCertificateValidationCallback { get; set; }

        public WinHttpTransportContext TransportContext { get; private set; }

        public WindowsProxyUsePolicy WindowsProxyUsePolicy { get; set; }

        public IWebProxy Proxy { get; set; }

        public ICredentials ServerCredentials { get; set; }

        public ICredentials DefaultProxyCredentials { get; set; }

        public bool PreAuthenticate { get; set; }

        public HttpStatusCode LastStatusCode { get; set; }

        public bool RetryRequest { get; set; }

        // Important: do not hold _lock while signaling completion of any of below TaskCompletionSources.
        public TaskCompletionSource<bool> TcsSendRequest { get; set; }
        public TaskCompletionSource<bool> TcsWriteToRequestStream { get; set; }
        public TaskCompletionSource<bool> TcsInternalWriteDataToRequestStream { get; set; }
        public TaskCompletionSource<bool> TcsReceiveResponseHeaders { get; set; }
        
        // WinHttpResponseStream state.
        public TaskCompletionSource<int> TcsQueryDataAvailable { get; set; }
        public TaskCompletionSource<int> TcsReadFromResponseStream { get; set; }

        #region IDisposable Members
        private void Dispose(bool disposing)
        {
            if (WinHttpTraceHelper.IsTraceEnabled())
            {
                WinHttpTraceHelper.Trace(
                    "WinHttpRequestState.Dispose, GCHandle=0x{0:X}, disposed={1}, disposing={2}",
                    ToIntPtr(),
                    _disposed,
                    disposing);
            }

            // Since there is no finalizer and this class is sealed, the disposing parameter should be TRUE.
            Debug.Assert(disposing, "WinHttpRequestState.Dispose() should have disposing=TRUE");

            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_operationHandle.IsAllocated)
            {
                _operationHandle.Free();
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
