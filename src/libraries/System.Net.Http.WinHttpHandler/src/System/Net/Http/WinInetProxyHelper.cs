// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http
{
    // This class is only used on OS versions where WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY
    // is not supported (i.e. before Win8.1/Win2K12R2) in the WinHttpOpen() function.
    internal class WinInetProxyHelper
    {
        public WinInetProxyHelper()
        {
            var proxyConfig = new Interop.WinHttp.WINHTTP_CURRENT_USER_IE_PROXY_CONFIG();

            try
            {
                if (Interop.WinHttp.WinHttpGetIEProxyConfigForCurrentUser(out proxyConfig))
                {
                    AutoConfigUrl = Marshal.PtrToStringUni(proxyConfig.AutoConfigUrl);
                    AutoDetect = proxyConfig.AutoDetect;
                    Proxy = Marshal.PtrToStringUni(proxyConfig.Proxy);
                    ProxyBypass = Marshal.PtrToStringUni(proxyConfig.ProxyBypass);
                }
                else
                {
                    var lastError = Marshal.GetLastWin32Error();
                    if (lastError != Interop.WinHttp.ERROR_FILE_NOT_FOUND)
                    {
                        throw WinHttpException.CreateExceptionUsingError(lastError);
                    }
                }
            }
            finally
            {
                // FreeHGlobal already checks for null pointer before freeing the memory.
                Marshal.FreeHGlobal(proxyConfig.AutoConfigUrl);
                Marshal.FreeHGlobal(proxyConfig.Proxy);
                Marshal.FreeHGlobal(proxyConfig.ProxyBypass);
            }
        }

        public string AutoConfigUrl { get; set; }

        public bool AutoDetect { get; set; }

        public bool AutoSettingsUsed
        {
            get
            {
                return AutoDetect || !string.IsNullOrEmpty(AutoConfigUrl);
            }
        }

        public bool ManualSettingsOnly
        {
            get
            {
                return !AutoDetect && string.IsNullOrEmpty(AutoConfigUrl) && !string.IsNullOrEmpty(Proxy);
            }
        }

        public string Proxy { get; set; }

        public string ProxyBypass { get; set; }

        public void GetProxyForUrl(SafeWinHttpHandle sessionHandle, Uri uri, out Interop.WinHttp.WINHTTP_PROXY_INFO proxyInfo)
        {
            Interop.WinHttp.WINHTTP_AUTOPROXY_OPTIONS autoProxyOptions;
            autoProxyOptions.AutoConfigUrl = AutoConfigUrl;
            autoProxyOptions.AutoDetectFlags = AutoDetect ? (Interop.WinHttp.WINHTTP_AUTO_DETECT_TYPE_DHCP | Interop.WinHttp.WINHTTP_AUTO_DETECT_TYPE_DNS_A) : 0;
            autoProxyOptions.AutoLoginIfChallenged = false;
            autoProxyOptions.Flags =
                (AutoDetect ? Interop.WinHttp.WINHTTP_AUTOPROXY_AUTO_DETECT : 0) |
                (!string.IsNullOrEmpty(AutoConfigUrl) ? Interop.WinHttp.WINHTTP_AUTOPROXY_CONFIG_URL : 0);
            autoProxyOptions.Reserved1 = IntPtr.Zero;
            autoProxyOptions.Reserved2 = 0;

            // AutoProxy Cache.
            // http://msdn.microsoft.com/en-us/library/windows/desktop/aa383153(v=vs.85).aspx
            // If the out-of-process service is active when WinHttpGetProxyForUrl is called, the cached autoproxy
            // URL and script are available to the whole computer. However, if the out-of-process service is used,
            // and the fAutoLogonIfChallenged flag in the pAutoProxyOptions structure is true, then the autoproxy
            // URL and script are not cached. Therefore, calling WinHttpGetProxyForUrl with the fAutoLogonIfChallenged
            // member set to TRUE results in additional overhead operations that may affect performance.
            // The following steps can be used to improve performance:
            // 1. Call WinHttpGetProxyForUrl with the fAutoLogonIfChallenged parameter set to false. The autoproxy
            //    URL and script are cached for future calls to WinHttpGetProxyForUrl.
            // 2. If Step 1 fails, with ERROR_WINHTTP_LOGIN_FAILURE, then call WinHttpGetProxyForUrl with the
            //    fAutoLogonIfChallenged member set to TRUE.
            var repeat = false;
            do
            {
                if (Interop.WinHttp.WinHttpGetProxyForUrl(sessionHandle, uri.AbsoluteUri, ref autoProxyOptions, out proxyInfo))
                {
                    repeat = false;
                }
                else
                {
                    var lastError = Marshal.GetLastWin32Error();
                    if (lastError == Interop.WinHttp.ERROR_WINHTTP_AUTODETECTION_FAILED)
                    {
                        // Fall back to manual settings if available.
                        if (!string.IsNullOrEmpty(Proxy))
                        {
                            proxyInfo.AccessType = Interop.WinHttp.WINHTTP_ACCESS_TYPE_NAMED_PROXY;
                            proxyInfo.Proxy = Marshal.StringToHGlobalUni(Proxy);
                            proxyInfo.ProxyBypass = string.IsNullOrEmpty(ProxyBypass) ? IntPtr.Zero : Marshal.StringToHGlobalUni(ProxyBypass);
                        }
                        else
                        {
                            proxyInfo.AccessType = Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY;
                            proxyInfo.Proxy = IntPtr.Zero;
                            proxyInfo.ProxyBypass = IntPtr.Zero;
                        }

                        repeat = false;
                    }
                    else if (lastError == Interop.WinHttp.ERROR_WINHTTP_LOGIN_FAILURE)
                    {
                        if (repeat)
                        {
                            throw WinHttpException.CreateExceptionUsingError(lastError);
                        }

                        repeat = true;
                        autoProxyOptions.AutoLoginIfChallenged = true;
                    }
                    else
                    {
                        WinHttpException.ThrowExceptionUsingLastError();
                    }
                }
            } while (repeat);
        }
    }
}
