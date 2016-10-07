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
    unsafe internal class ListenerClientCertAsyncResult : LazyAsyncResult
    {
        private ThreadPoolBoundHandle m_boundHandle;
        private NativeOverlapped* m_pOverlapped;
        private byte[] m_BackingBuffer;
        private Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO* m_MemoryBlob;
        private uint m_Size;

        internal NativeOverlapped* NativeOverlapped
        {
            get
            {
                return m_pOverlapped;
            }
        }

        internal Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO* RequestBlob
        {
            get
            {
                return m_MemoryBlob;
            }
        }

        private static readonly IOCompletionCallback s_IOCallback = new IOCompletionCallback(WaitCallback);

        internal ListenerClientCertAsyncResult(ThreadPoolBoundHandle boundHandle, object asyncObject, object userState, AsyncCallback callback, uint size) : base(asyncObject, userState, callback)
        {
            // we will use this overlapped structure to issue async IO to ul
            // the event handle will be put in by the BeginHttpApi2.ERROR_SUCCESS() method
            m_boundHandle = boundHandle;
            Reset(size);
        }

        internal void Reset(uint size)
        {
            if (size == m_Size)
            {
                return;
            }
            if (m_Size != 0)
            {
                m_boundHandle.FreeNativeOverlapped(m_pOverlapped);
            }
            m_Size = size;
            if (size == 0)
            {
                m_pOverlapped = null;
                m_MemoryBlob = null;
                m_BackingBuffer = null;
                return;
            }
            m_BackingBuffer = new byte[checked((int)size)];
            m_pOverlapped = m_boundHandle.AllocateNativeOverlapped(s_IOCallback, state: this, pinData: m_BackingBuffer);
            m_MemoryBlob = (Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO*)Marshal.UnsafeAddrOfPinnedArrayElement(m_BackingBuffer, 0);
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
                            httpListenerRequest.m_ConnectionId,
                            (uint)Interop.HttpApi.HTTP_FLAGS.NONE,
                            asyncResult.m_MemoryBlob,
                            asyncResult.m_Size,
                            &bytesReceived,
                            asyncResult.m_pOverlapped);

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
                    Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO* pClientCertInfo = asyncResult.m_MemoryBlob;
                    if (pClientCertInfo != null)
                    {
                        GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(httpListenerRequest) + "::ProcessClientCertificate() pClientCertInfo:" + LoggingHash.ObjectToString((IntPtr)pClientCertInfo)
                            + " pClientCertInfo->CertFlags:" + LoggingHash.ObjectToString(pClientCertInfo->CertFlags)
                            + " pClientCertInfo->CertEncodedSize:" + LoggingHash.ObjectToString(pClientCertInfo->CertEncodedSize)
                            + " pClientCertInfo->pCertEncoded:" + LoggingHash.ObjectToString((IntPtr)pClientCertInfo->pCertEncoded)
                            + " pClientCertInfo->Token:" + LoggingHash.ObjectToString((IntPtr)pClientCertInfo->Token)
                            + " pClientCertInfo->CertDeniedByMapper:" + LoggingHash.ObjectToString(pClientCertInfo->CertDeniedByMapper));
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
                                GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(httpListenerRequest) + "::ProcessClientCertificate() caught CryptographicException in X509Certificate2..ctor():" + LoggingHash.ObjectToString(exception));
                                result = exception;
                            }
                            catch (SecurityException exception)
                            {
                                GlobalLog.Print("HttpListenerRequest#" + LoggingHash.HashString(httpListenerRequest) + "::ProcessClientCertificate() caught SecurityException in X509Certificate2..ctor():" + LoggingHash.ObjectToString(exception));
                                result = exception;
                            }
                        }
                        httpListenerRequest.SetClientCertificateError((int)pClientCertInfo->CertFlags);
                    }

                }

                // complete the async IO and invoke the callback
                GlobalLog.Print("ListenerClientCertAsyncResult#" + LoggingHash.HashString(asyncResult) + "::WaitCallback() calling Complete()");
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

            GlobalLog.Print("ListenerClientCertAsyncResult#" + LoggingHash.HashString(asyncResult) + "::WaitCallback() errorCode:[" + errorCode.ToString() + "] numBytes:[" + numBytes.ToString() + "] nativeOverlapped:[" + ((long)nativeOverlapped).ToString() + "]");

            IOCompleted(asyncResult, errorCode, numBytes);
        }

        // Will be called from the base class upon InvokeCallback()
        protected override void Cleanup()
        {
            if (m_pOverlapped != null)
            {
                m_MemoryBlob = null;
                m_boundHandle.FreeNativeOverlapped(m_pOverlapped);
                m_pOverlapped = null;
                m_boundHandle = null;
            }
            GC.SuppressFinalize(this);
            base.Cleanup();
        }

        ~ListenerClientCertAsyncResult()
        {
            if (m_pOverlapped != null && !Environment.HasShutdownStarted)
            {
                m_boundHandle.FreeNativeOverlapped(m_pOverlapped);
                m_pOverlapped = null;  // Must do this in case application calls GC.ReRegisterForFinalize().
                m_boundHandle = null;
            }
        }
    }

}

