// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    public static class APICallHistory
    {
        public const string StringNotSet = "NOT SET";

        private static ProxyInfo sessionProxySettings;
        private static ProxyInfo requestProxySettings;
        private static List<IntPtr> winHttpOptionClientCertContextList = new List<IntPtr>();

        public static ProxyInfo SessionProxySettings
        {
            get
            {
                return sessionProxySettings;
            }

            set
            {
                sessionProxySettings.AccessType = value.AccessType;
                sessionProxySettings.Proxy = value.Proxy;
                sessionProxySettings.ProxyBypass = value.ProxyBypass;
            }
        }

        public static ProxyInfo RequestProxySettings
        {
            get
            {
                return requestProxySettings;
            }

            set
            {
                requestProxySettings.AccessType = value.AccessType;
                requestProxySettings.Proxy = value.Proxy;
                requestProxySettings.ProxyBypass = value.ProxyBypass;
            }
        }

        public static string ProxyUsernameWithDomain { get; set; }

        public static string ProxyPassword { get; set; }

        public static string ServerUsernameWithDomain { get; set; }

        public static string ServerPassword { get; set; }

        public static bool? WinHttpOptionDisableCookies { get; set; }

        public static bool? WinHttpOptionEnableSslRevocation { get; set; }

        public static uint? WinHttpOptionSecureProtocols { get; set; }

        public static uint? WinHttpOptionSecurityFlags { get; set; }

        public static uint? WinHttpOptionMaxHttpAutomaticRedirects { get; set; }

        public static uint? WinHttpOptionRedirectPolicy { get; set; }

        public static int? WinHttpOptionSendTimeout { get; set; }

        public static int? WinHttpOptionReceiveTimeout { get; set; }

        public static List<IntPtr> WinHttpOptionClientCertContext { get { return winHttpOptionClientCertContextList; } }

        public static void Reset()
        {
            sessionProxySettings.AccessType = null;
            sessionProxySettings.Proxy = APICallHistory.StringNotSet;
            sessionProxySettings.ProxyBypass = APICallHistory.StringNotSet;

            requestProxySettings.AccessType = null;
            requestProxySettings.Proxy = APICallHistory.StringNotSet;
            requestProxySettings.ProxyBypass = APICallHistory.StringNotSet;

            ProxyUsernameWithDomain = APICallHistory.StringNotSet;
            ProxyPassword = APICallHistory.StringNotSet;
            ServerUsernameWithDomain = APICallHistory.StringNotSet;
            ServerPassword = APICallHistory.StringNotSet;

            WinHttpOptionDisableCookies = null;
            WinHttpOptionEnableSslRevocation = null;
            WinHttpOptionSecureProtocols = null;
            WinHttpOptionSecurityFlags = null;
            WinHttpOptionMaxHttpAutomaticRedirects = null;
            WinHttpOptionRedirectPolicy = null;
            WinHttpOptionSendTimeout = null;
            WinHttpOptionReceiveTimeout = null;
            winHttpOptionClientCertContextList.Clear();
        }

        public struct ProxyInfo
        {
            public uint? AccessType;
            public string Proxy;
            public string ProxyBypass;
        }
    }
}
