// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    internal static partial class mincore
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
            if (TestControl.Fail.WinHttpOpen)
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
            return true;
        }

        public static bool WinHttpReceiveResponse(SafeWinHttpHandle requestHandle, IntPtr reserved)
        {
            if (TestControl.ResponseDelayTime > 0)
            {
                TestControl.ResponseDelayCompletedEvent.Reset();
                Thread.Sleep(TestControl.ResponseDelayTime);
                TestControl.ResponseDelayCompletedEvent.Set();
            }

            return true;
        }

        public static bool WinHttpQueryDataAvailable(SafeWinHttpHandle requestHandle, out uint bytesAvailable)
        {
            bytesAvailable = 0;
            return true;
        }

        public static bool WinHttpReadData(
            SafeWinHttpHandle requestHandle,
            IntPtr buffer,
            uint bufferSize,
            out uint bytesRead)
        {
            bytesRead = 0;

            if (TestControl.Fail.WinHttpReadData)
            {
                return false;
            }

            TestServer.ReadFromResponseBody(buffer, bufferSize, out bytesRead);
            return true;
        }

        public static bool WinHttpQueryHeaders(
            SafeWinHttpHandle requestHandle,
            uint infoLevel, string name,
            StringBuilder buffer,
            ref uint bufferLength,
            IntPtr index)
        {
            string httpVersion = "HTTP/1.1";
            string statusText = "OK";

            if (infoLevel == Interop.WinHttp.WINHTTP_QUERY_VERSION)
            {
                if (buffer == null)
                {
                    bufferLength = ((uint)httpVersion.Length + 1) * 2;
                    TestControl.LastWin32Error = (int)Interop.WinHttp.ERROR_INSUFFICIENT_BUFFER;
                    return false;
                }

                buffer.Append(httpVersion);
                return true;
            }

            if (infoLevel == Interop.WinHttp.WINHTTP_QUERY_STATUS_TEXT)
            {
                if (buffer == null)
                {
                    bufferLength = ((uint)statusText.Length + 1) * 2;
                    TestControl.LastWin32Error = (int)Interop.WinHttp.ERROR_INSUFFICIENT_BUFFER;
                    return false;
                }

                buffer.Append(statusText);
                return true;
            }

            if (infoLevel == Interop.WinHttp.WINHTTP_QUERY_CONTENT_ENCODING)
            {
                string compression = null;

                if (TestServer.ResponseHeaders.Contains("Content-Encoding: deflate"))
                {
                    compression = "deflate";
                }
                else if (TestServer.ResponseHeaders.Contains("Content-Encoding: gzip"))
                {
                    compression = "gzip";
                }

                if (compression == null)
                {
                    TestControl.LastWin32Error = (int)Interop.WinHttp.ERROR_WINHTTP_HEADER_NOT_FOUND;
                    return false;
                }

                if (buffer == null)
                {
                    bufferLength = ((uint)compression.Length + 1) * 2;
                    TestControl.LastWin32Error = (int)Interop.WinHttp.ERROR_INSUFFICIENT_BUFFER;
                    return false;
                }

                buffer.Append(compression);
                return true;
            }

            if (infoLevel == Interop.WinHttp.WINHTTP_QUERY_RAW_HEADERS_CRLF)
            {
                if (buffer == null)
                {
                    bufferLength = ((uint)TestServer.ResponseHeaders.Length + 1) * 2;
                    TestControl.LastWin32Error = (int)Interop.WinHttp.ERROR_INSUFFICIENT_BUFFER;
                    return false;
                }

                buffer.Append(TestServer.ResponseHeaders);
                return true;
            }

            return false;
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

        public static bool WinHttpWriteData(
            SafeWinHttpHandle requestHandle,
            IntPtr buffer,
            uint bufferSize,
            out uint bytesWritten)
        {
            if (TestControl.Fail.WinHttpWriteData)
            {
                bytesWritten = 0;
                return false;
            }

            TestServer.WriteToRequestBody(buffer, bufferSize);
            bytesWritten = bufferSize;
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
                throw new ArgumentNullException("handle");
            }
            
            return IntPtr.Zero;
        }
    }
}
