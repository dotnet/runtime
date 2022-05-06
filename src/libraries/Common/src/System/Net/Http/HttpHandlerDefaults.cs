// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Net.Http
{
    /// <summary>
    /// Central repository for default values used in http handler settings.  Not all settings are relevant
    /// to or configurable by all handlers.
    /// </summary>
    internal static partial class HttpHandlerDefaults
    {
        public const int DefaultMaxAutomaticRedirections = 50;
        public static readonly int DefaultMaxConnectionsPerServer = GetMaxConnectionsPerServer();
        public const int DefaultMaxResponseDrainSize = 1024 * 1024;
        public static readonly TimeSpan DefaultResponseDrainTimeout = TimeSpan.FromSeconds(2);
        public const int DefaultMaxResponseHeadersLength = 64; // Units in K (1024) bytes.
        public const DecompressionMethods DefaultAutomaticDecompression = DecompressionMethods.None;
        public const bool DefaultAutomaticRedirection = true;
        public const bool DefaultUseCookies = true;
        public const bool DefaultPreAuthenticate = false;
        public const ClientCertificateOption DefaultClientCertificateOption = ClientCertificateOption.Manual;
        public const bool DefaultUseProxy = true;
        public const bool DefaultUseDefaultCredentials = false;
        public const bool DefaultCheckCertificateRevocationList = false;
        public static readonly TimeSpan DefaultPooledConnectionLifetime = Timeout.InfiniteTimeSpan;
        public static readonly TimeSpan DefaultPooledConnectionIdleTimeout = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan DefaultExpect100ContinueTimeout = TimeSpan.FromSeconds(1);
        public static readonly TimeSpan DefaultConnectTimeout = Timeout.InfiniteTimeSpan;

        private static int GetMaxConnectionsPerServer()
        {
            int result = int.MaxValue;
            try
            {
                object? config = AppContext.GetData("System.Net.Http.HttpHandlerDefaults.DefaultMaxConnectionsPerServer");
                switch (config)
                {
                    case uint value:
                        result = (int)value;
                        break;
                    case string str:
                        result = int.Parse(str, Globalization.NumberStyles.AllowLeadingSign, Globalization.NumberFormatInfo.InvariantInfo);
                        break;
                    case IConvertible convertible:
                        result = convertible.ToInt32(Globalization.NumberFormatInfo.InvariantInfo);
                        break;
                }
                return result < 1 ? int.MaxValue : result;
            }
            catch (Exception e) when (e is FormatException || e is OverflowException)
            {
                return result;
            }
        }
    }
}
