// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32.SafeHandles;

using System.Net.Http.WinHttpHandlerUnitTests;

using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        public static bool CertFreeCertificateContext(IntPtr certContext)
        {
            return true;
        }
        
        public static bool CertVerifyCertificateChainPolicy(
            IntPtr pszPolicyOID,
            SafeX509ChainHandle pChainContext,
            ref CERT_CHAIN_POLICY_PARA pPolicyPara,
            ref CERT_CHAIN_POLICY_STATUS pPolicyStatus)
        {
            return true;
        }
    }

    internal static partial class Kernel32
    {
        public static string GetMessage(IntPtr moduleName, int error)
        {
            string messageFormat = "Fake error message, error code: {0}";
            return string.Format(messageFormat, error);
        }

        public static IntPtr GetModuleHandle(string moduleName)
        {
            return IntPtr.Zero;
        }
    }

    internal static partial class WinHttp
    {
        public static SafeWinHttpHandle WinHttpOpen(
            IntPtr userAgent,
            uint accessType,
            string proxyName,
            string proxyBypass,
            uint flags)
        {
            if (TestControl.WinHttpOpen.ErrorWithApiCall)
            {
                TestControl.LastWin32Error = (int)Interop.WinHttp.ERROR_INVALID_HANDLE;
                return new FakeSafeWinHttpHandle(false);
            }
            
            if (accessType == Interop.WinHttp.WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY && 
                !TestControl.WinHttpAutomaticProxySupport)
            {
                TestControl.LastWin32Error = (int)Interop.WinHttp.ERROR_INVALID_PARAMETER;
                return new FakeSafeWinHttpHandle(false);
            }

            APICallHistory.ProxyInfo proxyInfo;
            proxyInfo.AccessType = accessType;
            proxyInfo.Proxy = proxyName;
            proxyInfo.ProxyBypass = proxyBypass;
            APICallHistory.SessionProxySettings = proxyInfo;

            return new FakeSafeWinHttpHandle(true);
        }

        public static bool WinHttpCloseHandle(IntPtr sessionHandle)
        {
            Marshal.FreeHGlobal(sessionHandle);

            return true;
        }

        public static SafeWinHttpHandle WinHttpConnect(
            SafeWinHttpHandle sessionHandle,
            string serverName,
            ushort serverPort,
            uint reserved)
        {
            return new FakeSafeWinHttpHandle(true);
        }

        public static bool WinHttpAddRequestHeaders(
            SafeWinHttpHandle requestHandle,
            StringBuilder headers,
            uint headersLength,
            uint modifiers)
        {
            return true;
        }

        public static bool WinHttpAddRequestHeaders(
            SafeWinHttpHandle requestHandle,
            string headers,
            uint headersLength,
            uint modifiers)
        {
            return true;
        }

        public static SafeWinHttpHandle WinHttpOpenRequest(
            SafeWinHttpHandle connectHandle,
            string verb,
            string objectName,
            string version,
            string referrer,
            string acceptTypes,
            uint flags)
        {
            return new FakeSafeWinHttpHandle(true);
        }

        public static bool WinHttpSendRequest(
            SafeWinHttpHandle requestHandle,
            StringBuilder headers,
            uint headersLength,
            IntPtr optional,
            uint optionalLength,
            uint totalLength,
            IntPtr context)
        {
            Task.Run(() => {
                var fakeHandle = (FakeSafeWinHttpHandle)requestHandle;
                fakeHandle.Context = context;
                fakeHandle.InvokeCallback(Interop.WinHttp.WINHTTP_CALLBACK_STATUS_SENDREQUEST_COMPLETE, IntPtr.Zero, 0);
            });

            return true;
        }

        public static bool WinHttpReceiveResponse(SafeWinHttpHandle requestHandle, IntPtr reserved)
        {
            Task.Run(() => {
                var fakeHandle = (FakeSafeWinHttpHandle)requestHandle;
                bool aborted = !fakeHandle.DelayOperation(TestControl.WinHttpReceiveResponse.Delay);

                if (aborted || TestControl.WinHttpReadData.ErrorOnCompletion)
                {
                    Interop.WinHttp.WINHTTP_ASYNC_RESULT asyncResult;
                    asyncResult.dwResult = new IntPtr((int)Interop.WinHttp.API_RECEIVE_RESPONSE);
                    asyncResult.dwError = aborted ? Interop.WinHttp.ERROR_WINHTTP_OPERATION_CANCELLED :
                        Interop.WinHttp.ERROR_WINHTTP_CONNECTION_ERROR;

                    TestControl.WinHttpReadData.Wait();
                    fakeHandle.InvokeCallback(Interop.WinHttp.WINHTTP_CALLBACK_STATUS_REQUEST_ERROR, asyncResult);
                }
                else
                {
                    TestControl.WinHttpReceiveResponse.Wait();
                    fakeHandle.InvokeCallback(Interop.WinHttp.WINHTTP_CALLBACK_STATUS_HEADERS_AVAILABLE, IntPtr.Zero, 0);
                }
            });

            return true;
        }

        public static bool WinHttpQueryDataAvailable(
            SafeWinHttpHandle requestHandle,
            IntPtr bytesAvailableShouldBeNullForAsync)
        {
            if (bytesAvailableShouldBeNullForAsync != IntPtr.Zero)
            {
                return false;
            }
            
            if (TestControl.WinHttpQueryDataAvailable.ErrorWithApiCall)
            {
                return false;
            }

            Task.Run(() => {
                var fakeHandle = (FakeSafeWinHttpHandle)requestHandle;
                bool aborted = !fakeHandle.DelayOperation(TestControl.WinHttpReadData.Delay);

                if (aborted || TestControl.WinHttpQueryDataAvailable.ErrorOnCompletion)
                {
                    Interop.WinHttp.WINHTTP_ASYNC_RESULT asyncResult;
                    asyncResult.dwResult = new IntPtr((int)Interop.WinHttp.API_QUERY_DATA_AVAILABLE);
                    asyncResult.dwError = aborted ? Interop.WinHttp.ERROR_WINHTTP_OPERATION_CANCELLED :
                        Interop.WinHttp.ERROR_WINHTTP_CONNECTION_ERROR;

                    TestControl.WinHttpQueryDataAvailable.Wait();
                    fakeHandle.InvokeCallback(Interop.WinHttp.WINHTTP_CALLBACK_STATUS_REQUEST_ERROR, asyncResult);
                }
                else
                {
                    int bufferSize = sizeof(int);
                    IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
                    Marshal.WriteInt32(buffer, TestServer.DataAvailable);
                    fakeHandle.InvokeCallback(Interop.WinHttp.WINHTTP_CALLBACK_STATUS_DATA_AVAILABLE, buffer, (uint)bufferSize);
                    Marshal.FreeHGlobal(buffer);
                }
            });            
            
            return true;
        }

        public static bool WinHttpReadData(
            SafeWinHttpHandle requestHandle,
            IntPtr buffer,
            uint bufferSize,
            IntPtr bytesReadShouldBeNullForAsync)
        {
            if (bytesReadShouldBeNullForAsync != IntPtr.Zero)
            {
                return false;
            }

            if (TestControl.WinHttpReadData.ErrorWithApiCall)
            {
                return false;
            }

            uint bytesRead;
            TestServer.ReadFromResponseBody(buffer, bufferSize, out bytesRead);

            Task.Run(() => {
                var fakeHandle = (FakeSafeWinHttpHandle)requestHandle;
                bool aborted = !fakeHandle.DelayOperation(TestControl.WinHttpReadData.Delay);

                if (aborted || TestControl.WinHttpReadData.ErrorOnCompletion)
                {
                    Interop.WinHttp.WINHTTP_ASYNC_RESULT asyncResult;
                    asyncResult.dwResult = new IntPtr((int)Interop.WinHttp.API_READ_DATA);
                    asyncResult.dwError = aborted ? Interop.WinHttp.ERROR_WINHTTP_OPERATION_CANCELLED :
                        Interop.WinHttp.ERROR_WINHTTP_CONNECTION_ERROR;

                    TestControl.WinHttpReadData.Wait();
                    fakeHandle.InvokeCallback(Interop.WinHttp.WINHTTP_CALLBACK_STATUS_REQUEST_ERROR, asyncResult);
                }
                else
                {
                    TestControl.WinHttpReadData.Wait();
                    fakeHandle.InvokeCallback(Interop.WinHttp.WINHTTP_CALLBACK_STATUS_READ_COMPLETE, buffer, bytesRead);
                }
            });

            return true;
        }

        public static bool WinHttpQueryHeaders(
            SafeWinHttpHandle requestHandle,
            uint infoLevel, string name,
            IntPtr buffer,
            ref uint bufferLength,
            ref uint index)
        {
            string httpVersion = "HTTP/1.1";
            string statusText = "OK";

            if (infoLevel == Interop.WinHttp.WINHTTP_QUERY_SET_COOKIE)
            {
                TestControl.LastWin32Error = (int)Interop.WinHttp.ERROR_WINHTTP_HEADER_NOT_FOUND;
                return false;
            }

            if (infoLevel == Interop.WinHttp.WINHTTP_QUERY_VERSION)
            {
                return CopyToBufferOrFailIfInsufficientBufferLength(httpVersion, buffer, ref bufferLength);
            }

            if (infoLevel == Interop.WinHttp.WINHTTP_QUERY_STATUS_TEXT)
            {
                return CopyToBufferOrFailIfInsufficientBufferLength(statusText, buffer, ref bufferLength);
            }

            if (infoLevel == Interop.WinHttp.WINHTTP_QUERY_CONTENT_ENCODING)
            {
                string compression =
                    TestServer.ResponseHeaders.Contains("Content-Encoding: deflate") ? "deflate" :
                    TestServer.ResponseHeaders.Contains("Content-Encoding: gzip") ? "gzip" :
                    null;

                if (compression == null)
                {
                    TestControl.LastWin32Error = (int)Interop.WinHttp.ERROR_WINHTTP_HEADER_NOT_FOUND;
                    return false;
                }

                return CopyToBufferOrFailIfInsufficientBufferLength(compression, buffer, ref bufferLength);
            }

            if (infoLevel == Interop.WinHttp.WINHTTP_QUERY_RAW_HEADERS_CRLF)
            {
                return CopyToBufferOrFailIfInsufficientBufferLength(TestServer.ResponseHeaders, buffer, ref bufferLength);
            }

            return false;
        }

        private static bool CopyToBufferOrFailIfInsufficientBufferLength(string value, IntPtr buffer, ref uint bufferLength)
        {
            // The length of the string (plus terminating null char) in bytes.
            uint bufferLengthNeeded = ((uint)value.Length + 1) * sizeof(char);

            if (buffer == IntPtr.Zero || bufferLength < bufferLengthNeeded)
            {
                bufferLength = bufferLengthNeeded;
                TestControl.LastWin32Error = (int)Interop.WinHttp.ERROR_INSUFFICIENT_BUFFER;
                return false;
            }

            // Copy the string to the buffer.
            char[] temp = new char[value.Length + 1]; // null terminated.
            value.CopyTo(0, temp, 0, value.Length);
            Marshal.Copy(temp, 0, buffer, temp.Length);

            // The length in bytes, minus the length of the null char at the end.
            bufferLength = (uint)value.Length * sizeof(char);
            return true;
        }

        public static bool WinHttpQueryHeaders(
            SafeWinHttpHandle requestHandle,
            uint infoLevel,
            string name,
            ref uint number,
            ref uint bufferLength,
            IntPtr index)
        {
            infoLevel &= ~Interop.WinHttp.WINHTTP_QUERY_FLAG_NUMBER;

            if (infoLevel == Interop.WinHttp.WINHTTP_QUERY_STATUS_CODE)
            {
                number = (uint)HttpStatusCode.OK;
                return true;
            }

            return false;
        }

        public static bool WinHttpQueryOption(
            SafeWinHttpHandle handle,
            uint option,
            StringBuilder buffer,
            ref uint bufferSize)
        {
            string uri = "http://www.contoso.com/";

            if (option == Interop.WinHttp.WINHTTP_OPTION_URL)
            {
                if (buffer == null)
                {
                    bufferSize = ((uint)uri.Length + 1) * 2;
                    TestControl.LastWin32Error = (int)Interop.WinHttp.ERROR_INSUFFICIENT_BUFFER;
                    return false;
                }

                buffer.Append(uri);
                return true;
            }

            return false;
        }

        public static bool WinHttpQueryOption(
            SafeWinHttpHandle handle,
            uint option,
            ref IntPtr buffer,
            ref uint bufferSize)
        {
            return true;
        }

        public static bool WinHttpQueryOption(
            SafeWinHttpHandle handle,
            uint option,
            IntPtr buffer,
            ref uint bufferSize)
        {
            return true;
        }

        public static bool WinHttpQueryOption(
            SafeWinHttpHandle handle,
            uint option,
            ref uint buffer,
            ref uint bufferSize)
        {
            return true;
        }

        public static bool WinHttpWriteData(
            SafeWinHttpHandle requestHandle,
            IntPtr buffer,
            uint bufferSize,
            IntPtr bytesWrittenShouldBeNullForAsync)
        {
            if (bytesWrittenShouldBeNullForAsync != IntPtr.Zero)
            {
                return false;
            }

            if (TestControl.WinHttpWriteData.ErrorWithApiCall)
            {
                return false;
            }

            uint bytesWritten;
            TestServer.WriteToRequestBody(buffer, bufferSize);
            bytesWritten = bufferSize;

            Task.Run(() => {
                var fakeHandle = (FakeSafeWinHttpHandle)requestHandle;
                bool aborted = !fakeHandle.DelayOperation(TestControl.WinHttpWriteData.Delay);

                if (aborted || TestControl.WinHttpWriteData.ErrorOnCompletion)
                {
                    Interop.WinHttp.WINHTTP_ASYNC_RESULT asyncResult;
                    asyncResult.dwResult = new IntPtr((int)Interop.WinHttp.API_WRITE_DATA);
                    asyncResult.dwError = Interop.WinHttp.ERROR_WINHTTP_CONNECTION_ERROR;

                    TestControl.WinHttpWriteData.Wait();
                    fakeHandle.InvokeCallback(aborted ? Interop.WinHttp.ERROR_WINHTTP_OPERATION_CANCELLED :
                        Interop.WinHttp.WINHTTP_CALLBACK_STATUS_REQUEST_ERROR, asyncResult);
                }
                else
                {
                    TestControl.WinHttpWriteData.Wait();
                    fakeHandle.InvokeCallback(Interop.WinHttp.WINHTTP_CALLBACK_STATUS_WRITE_COMPLETE, IntPtr.Zero, 0);
                }
            });

            return true;
        }

        public static bool WinHttpSetOption(
            SafeWinHttpHandle handle,
            uint option,
            ref uint optionData,
            uint optionLength = sizeof(uint))
        {
            if (option == Interop.WinHttp.WINHTTP_OPTION_DECOMPRESSION & !TestControl.WinHttpDecompressionSupport)
            {
                TestControl.LastWin32Error = (int)Interop.WinHttp.ERROR_WINHTTP_INVALID_OPTION;
                return false;
            }

            if (option == Interop.WinHttp.WINHTTP_OPTION_DISABLE_FEATURE && 
                optionData == Interop.WinHttp.WINHTTP_DISABLE_COOKIES)
            {
                APICallHistory.WinHttpOptionDisableCookies = true;
            }
            else if (option == Interop.WinHttp.WINHTTP_OPTION_ENABLE_FEATURE &&
                     optionData == Interop.WinHttp.WINHTTP_ENABLE_SSL_REVOCATION)
            {
                APICallHistory.WinHttpOptionEnableSslRevocation = true;
            }
            else if (option == Interop.WinHttp.WINHTTP_OPTION_SECURE_PROTOCOLS)
            {
                APICallHistory.WinHttpOptionSecureProtocols = optionData;
            }
            else if (option == Interop.WinHttp.WINHTTP_OPTION_SECURITY_FLAGS)
            {
                APICallHistory.WinHttpOptionSecurityFlags = optionData;
            }
            else if (option == Interop.WinHttp.WINHTTP_OPTION_MAX_HTTP_AUTOMATIC_REDIRECTS)
            {
                APICallHistory.WinHttpOptionMaxHttpAutomaticRedirects = optionData;
            }
            else if (option == Interop.WinHttp.WINHTTP_OPTION_REDIRECT_POLICY)
            {
                APICallHistory.WinHttpOptionRedirectPolicy = optionData;
            }

            return true;
        }

        public static bool WinHttpSetOption(
            SafeWinHttpHandle handle,
            uint option,
            string optionData,
            uint optionLength)
        {
            if (option == Interop.WinHttp.WINHTTP_OPTION_PROXY_USERNAME)
            {
                APICallHistory.ProxyUsernameWithDomain = optionData;
            }
            else if (option == Interop.WinHttp.WINHTTP_OPTION_PROXY_PASSWORD)
            {
                APICallHistory.ProxyPassword = optionData;
            }
            else if (option == Interop.WinHttp.WINHTTP_OPTION_USERNAME)
            {
                APICallHistory.ServerUsernameWithDomain = optionData;
            }
            else if (option == Interop.WinHttp.WINHTTP_OPTION_PASSWORD)
            {
                APICallHistory.ServerPassword = optionData;
            }

            return true;
        }

        public static bool WinHttpSetOption(
            SafeWinHttpHandle handle,
            uint option,
            IntPtr optionData,
            uint optionLength)
        {
            if (option == Interop.WinHttp.WINHTTP_OPTION_PROXY)
            {
                var proxyInfo = Marshal.PtrToStructure<Interop.WinHttp.WINHTTP_PROXY_INFO>(optionData);
                var proxyInfoHistory = new APICallHistory.ProxyInfo();
                proxyInfoHistory.AccessType = proxyInfo.AccessType;
                proxyInfoHistory.Proxy = Marshal.PtrToStringUni(proxyInfo.Proxy);
                proxyInfoHistory.ProxyBypass = Marshal.PtrToStringUni(proxyInfo.ProxyBypass);
                APICallHistory.RequestProxySettings = proxyInfoHistory;
            }
            else if (option == Interop.WinHttp.WINHTTP_OPTION_CLIENT_CERT_CONTEXT)
            {
                APICallHistory.WinHttpOptionClientCertContext.Add(optionData);
            }

            return true;
        }

        public static bool WinHttpSetCredentials(
            SafeWinHttpHandle requestHandle,
            uint authTargets,
            uint authScheme,
            string userName,
            string password,
            IntPtr reserved)
        {
            return true;
        }

        public static bool WinHttpQueryAuthSchemes(
            SafeWinHttpHandle requestHandle,
            out uint supportedSchemes,
            out uint firstScheme,
            out uint authTarget)
        {
            supportedSchemes = 0;
            firstScheme = 0;
            authTarget = 0;

            return true;
        }

        public static bool WinHttpSetTimeouts(
            SafeWinHttpHandle handle,
            int resolveTimeout,
            int connectTimeout,
            int sendTimeout,
            int receiveTimeout)
        {
            return true;
        }

        public static bool WinHttpGetIEProxyConfigForCurrentUser(
            out Interop.WinHttp.WINHTTP_CURRENT_USER_IE_PROXY_CONFIG proxyConfig)
        {
            if (FakeRegistry.WinInetProxySettings.RegistryKeyMissing)
            {
                proxyConfig.AutoDetect = false;
                proxyConfig.AutoConfigUrl = IntPtr.Zero;
                proxyConfig.Proxy = IntPtr.Zero;
                proxyConfig.ProxyBypass = IntPtr.Zero;

                TestControl.LastWin32Error = (int)Interop.WinHttp.ERROR_FILE_NOT_FOUND;
                return false;
            }

            proxyConfig.AutoDetect = FakeRegistry.WinInetProxySettings.AutoDetect;
            proxyConfig.AutoConfigUrl = Marshal.StringToHGlobalUni(FakeRegistry.WinInetProxySettings.AutoConfigUrl);
            proxyConfig.Proxy = Marshal.StringToHGlobalUni(FakeRegistry.WinInetProxySettings.Proxy);
            proxyConfig.ProxyBypass = Marshal.StringToHGlobalUni(FakeRegistry.WinInetProxySettings.ProxyBypass);

            return true;
        }

        public static bool WinHttpGetProxyForUrl(
            SafeWinHttpHandle sessionHandle,
            string url,
            ref Interop.WinHttp.WINHTTP_AUTOPROXY_OPTIONS autoProxyOptions,
            out Interop.WinHttp.WINHTTP_PROXY_INFO proxyInfo)
        {
            if (TestControl.PACFileNotDetectedOnNetwork)
            {
                proxyInfo.AccessType = WINHTTP_ACCESS_TYPE_NO_PROXY;
                proxyInfo.Proxy = IntPtr.Zero;
                proxyInfo.ProxyBypass = IntPtr.Zero;

                TestControl.LastWin32Error = (int)Interop.WinHttp.ERROR_WINHTTP_AUTODETECTION_FAILED;
                return false;
            }

            proxyInfo.AccessType = Interop.WinHttp.WINHTTP_ACCESS_TYPE_NAMED_PROXY;
            proxyInfo.Proxy = Marshal.StringToHGlobalUni(FakeRegistry.WinInetProxySettings.Proxy);
            proxyInfo.ProxyBypass = IntPtr.Zero;

            return true;
        }

        public static IntPtr WinHttpSetStatusCallback(
            SafeWinHttpHandle handle,
            Interop.WinHttp.WINHTTP_STATUS_CALLBACK callback,
            uint notificationFlags,
            IntPtr reserved)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }
            
            var fakeHandle = (FakeSafeWinHttpHandle)handle;
            fakeHandle.Callback = callback;
            
            return IntPtr.Zero;
        }
    }
}
