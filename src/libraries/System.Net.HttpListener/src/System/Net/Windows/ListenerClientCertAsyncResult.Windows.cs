// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net
{
    internal unsafe class ListenerClientCertAsyncResult : LazyAsyncResult
    {
        private ThreadPoolBoundHandle _boundHandle;
        private NativeOverlapped* _pOverlapped;
        private byte[] _backingBuffer;
        private Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO* _memoryBlob;
        private uint _size;

        internal NativeOverlapped* NativeOverlapped
        {
            get
            {
                return _pOverlapped;
            }
        }

        internal Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO* RequestBlob
        {
            get
            {
                return _memoryBlob;
            }
        }

        private static readonly IOCompletionCallback s_IOCallback = new IOCompletionCallback(WaitCallback);

        internal ListenerClientCertAsyncResult(ThreadPoolBoundHandle boundHandle, object asyncObject, object userState, AsyncCallback callback, uint size) : base(asyncObject, userState, callback)
        {
            // we will use this overlapped structure to issue async IO to ul
            // the event handle will be put in by the BeginHttpApi2.ERROR_SUCCESS() method
            _boundHandle = boundHandle;
            Reset(size);
        }

        internal void Reset(uint size)
        {
            if (size == _size)
            {
                return;
            }
            if (_size != 0)
            {
                _boundHandle.FreeNativeOverlapped(_pOverlapped);
            }
            _size = size;
            if (size == 0)
            {
                _pOverlapped = null;
                _memoryBlob = null;
                _backingBuffer = null;
                return;
            }
            _backingBuffer = new byte[checked((int)size)];
            _pOverlapped = _boundHandle.AllocateNativeOverlapped(s_IOCallback, state: this, pinData: _backingBuffer);
            _memoryBlob = (Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO*)Marshal.UnsafeAddrOfPinnedArrayElement(_backingBuffer, 0);
        }

        internal unsafe void IOCompleted(uint errorCode, uint numBytes)
        {
            IOCompleted(this, errorCode, numBytes);
        }

        private static unsafe void IOCompleted(ListenerClientCertAsyncResult asyncResult, uint errorCode, uint numBytes)
        {
            HttpListenerRequest httpListenerRequest = (HttpListenerRequest)asyncResult.AsyncObject;
            object result = null;
            try
            {
                if (errorCode == Interop.HttpApi.ERROR_MORE_DATA)
                {
                    //There is a bug that has existed in http.sys since w2k3.  Bytesreceived will only
                    //return the size of the inital cert structure.  To get the full size,
                    //we need to add the certificate encoding size as well.

                    Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO* pClientCertInfo = asyncResult.RequestBlob;
                    asyncResult.Reset(numBytes + pClientCertInfo->CertEncodedSize);

                    uint bytesReceived = 0;
                    errorCode =
                        Interop.HttpApi.HttpReceiveClientCertificate(
                            httpListenerRequest.HttpListenerContext.RequestQueueHandle,
                            httpListenerRequest._connectionId,
                            (uint)Interop.HttpApi.HTTP_FLAGS.NONE,
                            asyncResult._memoryBlob,
                            asyncResult._size,
                            &bytesReceived,
                            asyncResult._pOverlapped);

                    if (errorCode == Interop.HttpApi.ERROR_IO_PENDING ||
                       (errorCode == Interop.HttpApi.ERROR_SUCCESS && !HttpListener.SkipIOCPCallbackOnSuccess))
                    {
                        return;
                    }
                }

                if (errorCode != Interop.HttpApi.ERROR_SUCCESS)
                {
                    asyncResult.ErrorCode = (int)errorCode;
                    result = new HttpListenerException((int)errorCode);
                }
                else
                {
                    Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO* pClientCertInfo = asyncResult._memoryBlob;
                    if (pClientCertInfo != null)
                    {
                        if (NetEventSource.IsEnabled)
                            NetEventSource.Info(null,
  $"pClientCertInfo:{(IntPtr)pClientCertInfo} pClientCertInfo->CertFlags: {pClientCertInfo->CertFlags} pClientCertInfo->CertEncodedSize: {pClientCertInfo->CertEncodedSize} pClientCertInfo->pCertEncoded: {(IntPtr)pClientCertInfo->pCertEncoded} pClientCertInfo->Token: {(IntPtr)pClientCertInfo->Token} pClientCertInfo->CertDeniedByMapper: {pClientCertInfo->CertDeniedByMapper}");
                        if (pClientCertInfo->pCertEncoded != null)
                        {
                            try
                            {
                                byte[] certEncoded = new byte[pClientCertInfo->CertEncodedSize];
                                Marshal.Copy((IntPtr)pClientCertInfo->pCertEncoded, certEncoded, 0, certEncoded.Length);
                                result = httpListenerRequest.ClientCertificate = new X509Certificate2(certEncoded);
                            }
                            catch (CryptographicException exception)
                            {
                                if (NetEventSource.IsEnabled)
                                    NetEventSource.Info(null,
          $"HttpListenerRequest: {httpListenerRequest} caught CryptographicException: {exception}");
                                result = exception;
                            }
                            catch (SecurityException exception)
                            {
                                if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"HttpListenerRequest: {httpListenerRequest} caught SecurityException: {exception}");
                                result = exception;
                            }
                        }
                        httpListenerRequest.SetClientCertificateError((int)pClientCertInfo->CertFlags);
                    }
                }

                // complete the async IO and invoke the callback
                if (NetEventSource.IsEnabled) NetEventSource.Info(null, "Calling Complete()");
            }
            catch (Exception exception) when (!ExceptionCheck.IsFatal(exception))
            {
                result = exception;
            }
            finally
            {
                if (errorCode != Interop.HttpApi.ERROR_IO_PENDING)
                {
                    httpListenerRequest.ClientCertState = ListenerClientCertState.Completed;
                }
            }

            asyncResult.InvokeCallback(result);
        }

        private static unsafe void WaitCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            ListenerClientCertAsyncResult asyncResult = (ListenerClientCertAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
            if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"errorCode:[{errorCode}] numBytes:[{numBytes}] nativeOverlapped:[{((long)nativeOverlapped)}]");
            IOCompleted(asyncResult, errorCode, numBytes);
        }

        // Will be called from the base class upon InvokeCallback()
        protected override void Cleanup()
        {
            if (_pOverlapped != null)
            {
                _memoryBlob = null;
                _boundHandle.FreeNativeOverlapped(_pOverlapped);
                _pOverlapped = null;
                _boundHandle = null;
            }
            GC.SuppressFinalize(this);
            base.Cleanup();
        }

        ~ListenerClientCertAsyncResult()
        {
            if (_pOverlapped != null && !Environment.HasShutdownStarted)
            {
                _boundHandle.FreeNativeOverlapped(_pOverlapped);
                _pOverlapped = null;  // Must do this in case application calls GC.ReRegisterForFinalize().
                _boundHandle = null;
            }
        }
    }
}

