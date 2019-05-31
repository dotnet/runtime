// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http
{
    // This class is only used on OS versions where WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY
    // is not supported (i.e. before Win8.1/Win2K12R2) in the WinHttpOpen() function.
    internal class WinInetProxyHelper
    {
        private bool _useProxy = false;
        private bool _autoDetectionFailed;
        private int _lastTimeAutoDetectionFailed; // Environment.TickCount units (milliseconds).
        private const int _recentAutoDetectionInterval = 120_000; // 2 minutes in milliseconds.

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

                    if (NetEventSource.IsEnabled)
                    {
                        NetEventSource.Info(this, $"AutoConfigUrl={AutoConfigUrl}, AutoDetect={AutoDetect}, Proxy={Proxy}, ProxyBypass={ProxyBypass}");
                    }

                    _useProxy = true;
                }
                else
                {
                    // We match behavior of WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY and ignore errors.
                    int lastError = Marshal.GetLastWin32Error();
                    if (NetEventSource.IsEnabled) NetEventSource.Error(this, $"error={lastError}");
                }

                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"_useProxy={_useProxy}");
            }

            finally
            {
                // FreeHGlobal already checks for null pointer before freeing the memory.
                Marshal.FreeHGlobal(proxyConfig.AutoConfigUrl);
                Marshal.FreeHGlobal(proxyConfig.Proxy);
                Marshal.FreeHGlobal(proxyConfig.ProxyBypass);
            }
        }

        public string AutoConfigUrl { get; private set; }

        public bool AutoDetect { get; private set; }

        public bool AutoSettingsUsed => AutoDetect || !string.IsNullOrEmpty(AutoConfigUrl);

        public bool ManualSettingsUsed => !string.IsNullOrEmpty(Proxy);

        public bool ManualSettingsOnly => !AutoSettingsUsed && ManualSettingsUsed;

        public string Proxy { get; private set; }

        public string ProxyBypass { get; private set; }

        public bool RecentAutoDetectionFailure =>
            _autoDetectionFailed &&
            Environment.TickCount - _lastTimeAutoDetectionFailed <= _recentAutoDetectionInterval;

        public bool GetProxyForUrl(
            SafeWinHttpHandle sessionHandle,
            Uri uri,
            out Interop.WinHttp.WINHTTP_PROXY_INFO proxyInfo)
        {
            proxyInfo.AccessType = Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY;
            proxyInfo.Proxy = IntPtr.Zero;
            proxyInfo.ProxyBypass = IntPtr.Zero;

            if (!_useProxy)
            {
                return false;
            }

            bool useProxy = false;

            Interop.WinHttp.WINHTTP_AUTOPROXY_OPTIONS autoProxyOptions;
            autoProxyOptions.AutoConfigUrl = AutoConfigUrl;
            autoProxyOptions.AutoDetectFlags = AutoDetect ?
                (Interop.WinHttp.WINHTTP_AUTO_DETECT_TYPE_DHCP | Interop.WinHttp.WINHTTP_AUTO_DETECT_TYPE_DNS_A) : 0;
            autoProxyOptions.AutoLoginIfChallenged = false;
            autoProxyOptions.Flags =
                (AutoDetect ? Interop.WinHttp.WINHTTP_AUTOPROXY_AUTO_DETECT : 0) |
                (!string.IsNullOrEmpty(AutoConfigUrl) ? Interop.WinHttp.WINHTTP_AUTOPROXY_CONFIG_URL : 0);
            autoProxyOptions.Reserved1 = IntPtr.Zero;
            autoProxyOptions.Reserved2 = 0;

            // AutoProxy Cache.
            // https://docs.microsoft.com/en-us/windows/desktop/WinHttp/autoproxy-cache
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
            //
            // We match behavior of WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY and ignore errors.
            var repeat = false;
            do
            {
                _autoDetectionFailed = false;
                if (Interop.WinHttp.WinHttpGetProxyForUrl(
                    sessionHandle,
                    uri.AbsoluteUri,
                    ref autoProxyOptions,
                    out proxyInfo))
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Info(this, "Using autoconfig proxy settings");
                    useProxy = true;

                    break;
                }
                else
                {
                    var lastError = Marshal.GetLastWin32Error();
                    if (NetEventSource.IsEnabled) NetEventSource.Error(this, $"error={lastError}");

                    if (lastError == Interop.WinHttp.ERROR_WINHTTP_LOGIN_FAILURE)
                    {
                        if (repeat)
                        {
                            // We don't retry more than once.
                            break;
                        }
                        else
                        {
                            repeat = true;
                            autoProxyOptions.AutoLoginIfChallenged = true;
                        }
                    }
                    else
                    {
                        if (lastError == Interop.WinHttp.ERROR_WINHTTP_AUTODETECTION_FAILED)
                        {
                            _autoDetectionFailed = true;
                            _lastTimeAutoDetectionFailed = Environment.TickCount;
                        }

                        break;
                    }
                }
            } while (repeat);

            // Fall back to manual settings if available.
            if (!useProxy && !string.IsNullOrEmpty(Proxy))
            {
                proxyInfo.AccessType = Interop.WinHttp.WINHTTP_ACCESS_TYPE_NAMED_PROXY;
                proxyInfo.Proxy = Marshal.StringToHGlobalUni(Proxy);
                proxyInfo.ProxyBypass = string.IsNullOrEmpty(ProxyBypass) ?
                    IntPtr.Zero : Marshal.StringToHGlobalUni(ProxyBypass);

                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"Fallback to Proxy={Proxy}, ProxyBypass={ProxyBypass}");
                useProxy = true;
            }

            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"useProxy={useProxy}");

            return useProxy;
        }
    }
}