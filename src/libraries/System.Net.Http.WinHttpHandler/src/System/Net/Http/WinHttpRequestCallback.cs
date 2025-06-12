// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http
{
    /// <summary>
    /// Static class containing the WinHttp global callback and associated routines.
    /// </summary>
    internal static class WinHttpRequestCallback
    {
        private static readonly Oid ServerAuthOid = new Oid("1.3.6.1.5.5.7.3.1", "1.3.6.1.5.5.7.3.1");

        public static Interop.WinHttp.WINHTTP_STATUS_CALLBACK StaticCallbackDelegate =
            new Interop.WinHttp.WINHTTP_STATUS_CALLBACK(WinHttpCallback);

        public static void WinHttpCallback(
            IntPtr handle,
            IntPtr context,
            uint internetStatus,
            IntPtr statusInformation,
            uint statusInformationLength)
        {
            if (NetEventSource.Log.IsEnabled()) WinHttpTraceHelper.TraceCallbackStatus(null, handle, context, internetStatus);

            if (Environment.HasShutdownStarted)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "Environment.HasShutdownStarted returned True");
                return;
            }

            if (context == IntPtr.Zero)
            {
                return;
            }

            WinHttpRequestState? state = WinHttpRequestState.FromIntPtr(context);
            Debug.Assert(state != null, "WinHttpCallback must have a non-null state object");

            RequestCallback(state, internetStatus, statusInformation, statusInformationLength);
        }

        private static void RequestCallback(
            WinHttpRequestState state,
            uint internetStatus,
            IntPtr statusInformation,
            uint statusInformationLength)
        {
            try
            {
                switch (internetStatus)
                {
                    case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_CONNECTED_TO_SERVER:
                        if (WinHttpHandler.CertificateCachingAppContextSwitchEnabled)
                        {
                            IPAddress connectedToIPAddress = IPAddress.Parse(Marshal.PtrToStringUni(statusInformation)!);
                            OnRequestConnectedToServer(state, connectedToIPAddress);
                        }
                        return;

                    case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING:
                        OnRequestHandleClosing(state);
                        return;

                    case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_SENDREQUEST_COMPLETE:
                        OnRequestSendRequestComplete(state);
                        return;

                    case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_DATA_AVAILABLE:
                        Debug.Assert(statusInformationLength == sizeof(int));
                        int bytesAvailable = Marshal.ReadInt32(statusInformation);
                        OnRequestDataAvailable(state, bytesAvailable);
                        return;

                    case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_READ_COMPLETE:
                        OnRequestReadComplete(state, statusInformationLength);
                        return;

                    case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_WRITE_COMPLETE:
                        OnRequestWriteComplete(state);
                        return;

                    case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_HEADERS_AVAILABLE:
                        OnRequestReceiveResponseHeadersComplete(state);
                        return;

                    case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_REDIRECT:
                        var redirectUri = new Uri(Marshal.PtrToStringUni(statusInformation)!);
                        OnRequestRedirect(state, redirectUri);
                        return;

                    case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_SENDING_REQUEST:
                        OnRequestSendingRequest(state);
                        return;

                    case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_REQUEST_ERROR:
                        Debug.Assert(
                            statusInformationLength == Marshal.SizeOf<Interop.WinHttp.WINHTTP_ASYNC_RESULT>(),
                            "RequestCallback: statusInformationLength=" + statusInformationLength +
                            " must be sizeof(WINHTTP_ASYNC_RESULT)=" + Marshal.SizeOf<Interop.WinHttp.WINHTTP_ASYNC_RESULT>());

                        var asyncResult = Marshal.PtrToStructure<Interop.WinHttp.WINHTTP_ASYNC_RESULT>(statusInformation);
                        OnRequestError(state, asyncResult);
                        return;

                    default:
                        return;
                }
            }
            catch (Exception ex)
            {
                state.SavedException = ex;

                // Since we got a fatal error processing the request callback,
                // we need to close the WinHttp request handle in order to
                // abort the currently executing WinHttp async operation.
                //
                // We must always call Dispose() against the SafeWinHttpHandle
                // wrapper and never close directly the raw WinHttp handle.
                // The SafeWinHttpHandle wrapper is thread-safe and guarantees
                // calling the underlying WinHttpCloseHandle() function only once.
                state.RequestHandle?.Dispose();
            }
        }

        private static void OnRequestConnectedToServer(WinHttpRequestState state, IPAddress connectedIPAddress)
        {
            Debug.Assert(state != null);
            Debug.Assert(state.Handler != null);
            Debug.Assert(state.RequestMessage != null);

            if (state.Handler.TryRemoveCertificateFromCache(new CachedCertificateKey(connectedIPAddress, state.RequestMessage)))
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(state, $"Removed cached certificate for {connectedIPAddress}");
            }
            else
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(state, $"No cached certificate for {connectedIPAddress} to remove");
            }
        }

        private static void OnRequestHandleClosing(WinHttpRequestState state)
        {
            Debug.Assert(state != null, "OnRequestSendRequestComplete: state is null");

            // This is the last notification callback that WinHTTP will send. Therefore, we can
            // now explicitly dispose the state object which will free its corresponding GCHandle.
            // This will then allow the state object to be garbage collected.
            state.Dispose();
        }

        private static void OnRequestSendRequestComplete(WinHttpRequestState state)
        {
            Debug.Assert(state != null, "OnRequestSendRequestComplete: state is null");
            Debug.Assert(state.LifecycleAwaitable != null, "OnRequestSendRequestComplete: LifecycleAwaitable is null");

            state.LifecycleAwaitable.SetResult(1);
        }

        private static void OnRequestDataAvailable(WinHttpRequestState state, int bytesAvailable)
        {
            Debug.Assert(state != null, "OnRequestDataAvailable: state is null");

            state.LifecycleAwaitable.SetResult(bytesAvailable);
        }

        private static void OnRequestReadComplete(WinHttpRequestState state, uint bytesRead)
        {
            Debug.Assert(state != null, "OnRequestReadComplete: state is null");

            // If we read to the end of the stream and we're using 'Content-Length' semantics on the response body,
            // then verify we read at least the number of bytes required.
            if (bytesRead == 0
                && state.ExpectedBytesToRead.HasValue
                && state.CurrentBytesRead < state.ExpectedBytesToRead.Value)
            {
                state.LifecycleAwaitable.SetException(new IOException(SR.Format(
                    SR.net_http_io_read_incomplete,
                    state.ExpectedBytesToRead.Value,
                    state.CurrentBytesRead)));
            }
            else
            {
                state.CurrentBytesRead += (long)bytesRead;
                state.LifecycleAwaitable.SetResult((int)bytesRead);
            }
        }

        private static void OnRequestWriteComplete(WinHttpRequestState state)
        {
            Debug.Assert(state != null, "OnRequestWriteComplete: state is null");
            Debug.Assert(state.TcsInternalWriteDataToRequestStream != null, "TcsInternalWriteDataToRequestStream is null");
            Debug.Assert(!state.TcsInternalWriteDataToRequestStream.Task.IsCompleted, "TcsInternalWriteDataToRequestStream.Task is completed");

            state.TcsInternalWriteDataToRequestStream.TrySetResult(true);
        }

        private static void OnRequestReceiveResponseHeadersComplete(WinHttpRequestState state)
        {
            Debug.Assert(state != null, "OnRequestReceiveResponseHeadersComplete: state is null");
            Debug.Assert(state.LifecycleAwaitable != null, "LifecycleAwaitable is null");

            state.LifecycleAwaitable.SetResult(1);
        }

        private static void OnRequestRedirect(WinHttpRequestState state, Uri redirectUri)
        {
            Debug.Assert(state != null, "OnRequestRedirect: state is null");
            Debug.Assert(state.Handler != null, "OnRequestRedirect: state.Handler is null");
            Debug.Assert(state.RequestMessage != null, "OnRequestRedirect: state.RequestMessage is null");
            Debug.Assert(redirectUri != null, "OnRequestRedirect: redirectUri is null");

            // If we're manually handling cookies, we need to reset them based on the new URI.
            if (state.Handler.CookieUsePolicy == CookieUsePolicy.UseSpecifiedCookieContainer)
            {
                // Add any cookies that may have arrived with redirect response.
                WinHttpCookieContainerAdapter.AddResponseCookiesToContainer(state);

                // Reset cookie request headers based on redirectUri.
                WinHttpCookieContainerAdapter.ResetCookieRequestHeaders(state, redirectUri);
            }

            state.RequestMessage.RequestUri = redirectUri;

            // Redirection to a new uri may require a new connection through a potentially different proxy.
            // If so, we will need to respond to additional 407 proxy auth demands and re-attach any
            // proxy credentials. The ProcessResponse() method looks at the state.LastStatusCode
            // before attaching proxy credentials and marking the HTTP request to be re-submitted.
            // So we need to reset the LastStatusCode remembered. Otherwise, it will see additional 407
            // responses as an indication that proxy auth failed and won't retry the HTTP request.
            if (state.LastStatusCode == HttpStatusCode.ProxyAuthenticationRequired)
            {
                state.LastStatusCode = 0;
            }

            // For security reasons, we drop the server credential if it is a
            // NetworkCredential.  But we allow credentials in a CredentialCache
            // since they are specifically tied to URI's.
            if (!(state.ServerCredentials is CredentialCache))
            {
                state.ServerCredentials = null;
            }

            // Similarly, we need to clear any Auth headers that were added to the request manually or
            // through the default headers.
            ResetAuthRequestHeaders(state);
        }

        private static void OnRequestSendingRequest(WinHttpRequestState state)
        {
            Debug.Assert(state != null, "OnRequestSendingRequest: state is null");
            Debug.Assert(state.Handler != null, "OnRequestSendingRequest: state.Handler is null");
            Debug.Assert(state.RequestMessage != null, "OnRequestSendingRequest: state.RequestMessage is null");
            Debug.Assert(state.RequestMessage.RequestUri != null, "OnRequestSendingRequest: state.RequestMessage.RequestUri is null");

            if (state.RequestMessage.RequestUri.Scheme != UriScheme.Https || state.RequestHandle == null)
            {
                // Not SSL/TLS or request already gone
                return;
            }

            // Grab the channel binding token (CBT) information from the request handle and put it into
            // the TransportContext object.
            state.TransportContext.SetChannelBinding(state.RequestHandle);

            if (state.ServerCertificateValidationCallback != null)
            {
                IntPtr certHandle = IntPtr.Zero;
                uint certHandleSize = (uint)IntPtr.Size;

                if (!Interop.WinHttp.WinHttpQueryOption(
                    state.RequestHandle,
                    Interop.WinHttp.WINHTTP_OPTION_SERVER_CERT_CONTEXT,
                    ref certHandle,
                    ref certHandleSize))
                {
                    int lastError = Marshal.GetLastWin32Error();
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(state, $"Error getting WINHTTP_OPTION_SERVER_CERT_CONTEXT, {lastError}");

                    if (lastError == Interop.WinHttp.ERROR_WINHTTP_INCORRECT_HANDLE_STATE)
                    {
                        // Not yet an SSL/TLS connection. This occurs while connecting thru a proxy where the
                        // CONNECT verb hasn't yet been processed due to the proxy requiring authentication.
                        // We need to ignore this notification. Another notification will be sent once the final
                        // connection thru the proxy is completed.
                        return;
                    }

                    throw WinHttpException.CreateExceptionUsingError(lastError, "WINHTTP_CALLBACK_STATUS_SENDING_REQUEST/WinHttpQueryOption");
                }

                // Get any additional certificates sent from the remote server during the TLS/SSL handshake.
                X509Certificate2Collection remoteCertificateStore = new X509Certificate2Collection();
                UnmanagedCertificateContext.GetRemoteCertificatesFromStoreContext(certHandle, remoteCertificateStore);

                // Create a managed wrapper around the certificate handle. Since this results in duplicating
                // the handle, we will close the original handle after creating the wrapper.
                var serverCertificate = new X509Certificate2(certHandle);
                Interop.Crypt32.CertFreeCertificateContext(certHandle);

                IPAddress? ipAddress = null;
                if (WinHttpHandler.CertificateCachingAppContextSwitchEnabled)
                {
                    unsafe
                    {
                        Interop.WinHttp.WINHTTP_CONNECTION_INFO connectionInfo;
                        Interop.WinHttp.WINHTTP_CONNECTION_INFO* pConnectionInfo = &connectionInfo;
                        uint infoSize = (uint)sizeof(Interop.WinHttp.WINHTTP_CONNECTION_INFO);
                        if (Interop.WinHttp.WinHttpQueryOption(
                            state.RequestHandle,
                            // This option is available on Windows XP SP2 and later; Windows 2003 with SP1 and later.
                            Interop.WinHttp.WINHTTP_OPTION_CONNECTION_INFO,
                            (IntPtr)pConnectionInfo,
                            ref infoSize))
                        {
                            // RemoteAddress is SOCKADDR_STORAGE structure, which is 128 bytes.
                            // See: https://learn.microsoft.com/en-us/windows/win32/api/winhttp/ns-winhttp-winhttp_connection_info
                            // SOCKADDR_STORAGE can hold either IPv4 or IPv6 address.
                            // For offset numbers: https://learn.microsoft.com/en-us/windows/win32/winsock/sockaddr-2
                            ReadOnlySpan<byte> remoteAddressSpan = new ReadOnlySpan<byte>(connectionInfo.RemoteAddress, 128);
                            AddressFamily addressFamily = (AddressFamily)(remoteAddressSpan[0] + (remoteAddressSpan[1] << 8));
                            ipAddress = addressFamily switch
                            {
                                AddressFamily.InterNetwork => new IPAddress(BinaryPrimitives.ReadUInt32LittleEndian(remoteAddressSpan.Slice(4))),
                                AddressFamily.InterNetworkV6 => new IPAddress(remoteAddressSpan.Slice(8, 16).ToArray()),
                                _ => null
                            };
                            Debug.Assert(ipAddress != null, "AddressFamily is not supported");
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(state, $"ipAddress: {ipAddress}");

                        }
                        else
                        {
                            int lastError = Marshal.GetLastWin32Error();
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(state, $"Error getting WINHTTP_OPTION_CONNECTION_INFO, {lastError}");
                        }
                    }

                    if (ipAddress is not null &&
                        state.Handler.GetCertificateFromCache(new CachedCertificateKey(ipAddress, state.RequestMessage), out byte[]? rawCertData) &&
#if NETFRAMEWORK
                        rawCertData.AsSpan().SequenceEqual(serverCertificate.RawData))
#else
                        rawCertData.AsSpan().SequenceEqual(serverCertificate.RawDataMemory.Span))
#endif
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(state, $"Skipping certificate validation. ipAddress: {ipAddress}, Thumbprint: {serverCertificate.Thumbprint}");
                        serverCertificate.Dispose();
                        return;
                    }
                    else
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(state, $"Certificate validation is required! IPAddress = {ipAddress}, Thumbprint: {serverCertificate.Thumbprint}");
                    }
                }

                X509Chain? chain = null;
                SslPolicyErrors sslPolicyErrors;
                bool result = false;

                try
                {
                    // Create and configure the X509Chain
                    chain = new X509Chain();
                    chain.ChainPolicy.RevocationMode = state.CheckCertificateRevocationList ? X509RevocationMode.Online : X509RevocationMode.NoCheck;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                    // Authenticate the remote party: (e.g. when operating in client mode, authenticate the server).
                    chain.ChainPolicy.ApplicationPolicy.Add(ServerAuthOid);

                    if (remoteCertificateStore.Count > 0)
                    {
                        if (NetEventSource.Log.IsEnabled())
                        {
                            foreach (X509Certificate cert in remoteCertificateStore)
                            {
                                NetEventSource.Info(remoteCertificateStore, $"Adding cert to ExtraStore: {cert.Subject}");
                            }
                        }

                        chain.ChainPolicy.ExtraStore.AddRange(remoteCertificateStore);
                    }

                    // Call the shared BuildChainAndVerifyProperties method
                    // isServer=false because WinHttpHandler is a client validating a server certificate
                    sslPolicyErrors = System.Net.CertificateValidation.BuildChainAndVerifyProperties(
                        chain,
                        serverCertificate,
                        checkCertName: true,
                        isServer: false,
                        hostName: state.RequestMessage.RequestUri.Host);

                    result = state.ServerCertificateValidationCallback(
                        state.RequestMessage,
                        serverCertificate,
                        chain,
                        sslPolicyErrors);
                    if (WinHttpHandler.CertificateCachingAppContextSwitchEnabled && result && ipAddress is not null)
                    {
                        state.Handler.AddCertificateToCache(new CachedCertificateKey(ipAddress, state.RequestMessage), serverCertificate.RawData);
                    }
                }
                catch (Exception ex)
                {
                    throw WinHttpException.CreateExceptionUsingError(
                        (int)Interop.WinHttp.ERROR_WINHTTP_SECURE_FAILURE, "X509Chain.Build", ex);
                }
                finally
                {
                    chain?.Dispose();
                    serverCertificate.Dispose();
                }

                if (!result)
                {
                    throw WinHttpException.CreateExceptionUsingError(
                        (int)Interop.WinHttp.ERROR_WINHTTP_SECURE_FAILURE, "ServerCertificateValidationCallback");
                }
            }
        }

        private static void OnRequestError(WinHttpRequestState state, Interop.WinHttp.WINHTTP_ASYNC_RESULT asyncResult)
        {
            Debug.Assert(state != null, "OnRequestError: state is null");

            if (NetEventSource.Log.IsEnabled()) WinHttpTraceHelper.TraceAsyncError(state, asyncResult);

            Exception innerException = WinHttpException.CreateExceptionUsingError(unchecked((int)asyncResult.dwError), "WINHTTP_CALLBACK_STATUS_REQUEST_ERROR");

            switch (unchecked((uint)asyncResult.dwResult.ToInt32()))
            {
                case Interop.WinHttp.API_SEND_REQUEST:
                    state.LifecycleAwaitable.SetException(innerException);
                    break;

                case Interop.WinHttp.API_RECEIVE_RESPONSE:
                    if (asyncResult.dwError == Interop.WinHttp.ERROR_WINHTTP_RESEND_REQUEST)
                    {
                        state.RetryRequest = true;
                        state.LifecycleAwaitable.SetResult(0);
                    }
                    else if (asyncResult.dwError == Interop.WinHttp.ERROR_WINHTTP_CLIENT_AUTH_CERT_NEEDED)
                    {
                        // WinHttp will automatically drop any client SSL certificates that we
                        // have pre-set into the request handle including the NULL certificate
                        // (which means we have no certs to send). For security reasons, we don't
                        // allow the certificate to be re-applied. But we need to tell WinHttp
                        // explicitly that we don't have any certificate to send.
                        Debug.Assert(state.RequestHandle != null, "OnRequestError: state.RequestHandle is null");
                        WinHttpHandler.SetNoClientCertificate(state.RequestHandle);
                        state.RetryRequest = true;
                        state.LifecycleAwaitable.SetResult(0);
                    }
                    else if (asyncResult.dwError == Interop.WinHttp.ERROR_WINHTTP_OPERATION_CANCELLED)
                    {
                        state.LifecycleAwaitable.SetCanceled(state.CancellationToken);
                    }
                    else
                    {
                        state.LifecycleAwaitable.SetException(innerException);
                    }
                    break;

                case Interop.WinHttp.API_QUERY_DATA_AVAILABLE:
                    if (asyncResult.dwError == Interop.WinHttp.ERROR_WINHTTP_OPERATION_CANCELLED)
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(state, "QUERY_DATA_AVAILABLE - ERROR_WINHTTP_OPERATION_CANCELLED");
                        state.LifecycleAwaitable.SetCanceled();
                    }
                    else
                    {
                        state.LifecycleAwaitable.SetException(
                            new IOException(SR.net_http_io_read, innerException));
                    }
                    break;

                case Interop.WinHttp.API_READ_DATA:
                    if (asyncResult.dwError == Interop.WinHttp.ERROR_WINHTTP_OPERATION_CANCELLED)
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(state, "API_READ_DATA - ERROR_WINHTTP_OPERATION_CANCELLED");
                        state.LifecycleAwaitable.SetCanceled();
                    }
                    else
                    {
                        state.LifecycleAwaitable.SetException(new IOException(SR.net_http_io_read, innerException));
                    }
                    break;

                case Interop.WinHttp.API_WRITE_DATA:
                    Debug.Assert(state.TcsInternalWriteDataToRequestStream != null);
                    if (asyncResult.dwError == Interop.WinHttp.ERROR_WINHTTP_OPERATION_CANCELLED)
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(state, "API_WRITE_DATA - ERROR_WINHTTP_OPERATION_CANCELLED");
                        state.TcsInternalWriteDataToRequestStream.TrySetCanceled();
                    }
                    else
                    {
                        state.TcsInternalWriteDataToRequestStream.TrySetException(
                            new IOException(SR.net_http_io_write, innerException));
                    }
                    break;

                default:
                    Debug.Fail(
                        "OnRequestError: Result (" + asyncResult.dwResult + ") is not expected.",
                        "Error code: " + asyncResult.dwError + " (" + innerException.Message + ")");
                    break;
            }
        }

        private static void ResetAuthRequestHeaders(WinHttpRequestState state)
        {
            const string AuthHeaderNameWithColon = "Authorization:";
            SafeWinHttpHandle? requestHandle = state.RequestHandle;
            Debug.Assert(requestHandle != null);

            // Clear auth headers.
            if (!Interop.WinHttp.WinHttpAddRequestHeaders(
                requestHandle,
                AuthHeaderNameWithColon,
                (uint)AuthHeaderNameWithColon.Length,
                Interop.WinHttp.WINHTTP_ADDREQ_FLAG_REPLACE))
            {
                int lastError = Marshal.GetLastWin32Error();
                if (lastError != Interop.WinHttp.ERROR_WINHTTP_HEADER_NOT_FOUND)
                {
                    throw WinHttpException.CreateExceptionUsingError(lastError, "WINHTTP_CALLBACK_STATUS_REDIRECT/WinHttpAddRequestHeaders");
                }
            }
        }
    }
}
