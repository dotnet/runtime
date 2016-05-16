// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Test.Common
{
    internal static class TestSettings
    {
        public static class Http
        {
            public static string Host
            {
                get
                {
                    string server = Environment.GetEnvironmentVariable("COREFX_HTTPHOST");
                    if (string.IsNullOrEmpty(server))
                    {
                        return "corefx-net.cloudapp.net";
                    }
                    else
                    {
                        return server;
                    }
                }
            }

            public static string SecureHost
            {
                get
                {
                    string server = Environment.GetEnvironmentVariable("COREFX_SECUREHTTPHOST");
                    if (string.IsNullOrEmpty(server))
                    {
                        return "corefx-net.cloudapp.net";
                    }
                    else
                    {
                        return server;
                    }
                }
            }

            public static string Http2Host
            {
                get
                {
                    string server = Environment.GetEnvironmentVariable("COREFX_HTTP2HOST");
                    if (string.IsNullOrEmpty(server))
                    {
                        return "http2.akamai.com";
                    }
                    else
                    {
                        return server;
                    }
                }
            }

            public static string DomainJoinedHttpHost => Environment.GetEnvironmentVariable("COREFX_DOMAINJOINED_HTTPHOST");
            public static string DomainJoinedProxyHost => Environment.GetEnvironmentVariable("COREFX_DOMAINJOINED_PROXYHOST");
            public static string DomainJoinedProxyPort => Environment.GetEnvironmentVariable("COREFX_DOMAINJOINED_PROXYPORT");

            public static bool StressEnabled => Environment.GetEnvironmentVariable("COREFX_STRESS_HTTP") == "1";
        }

        public static class WebSocket
        {
            public static string Host
            {
                get
                {
                    string server = Environment.GetEnvironmentVariable("COREFX_WEBSOCKETHOST");
                    if (string.IsNullOrEmpty(server))
                    {
                        return "corefx-net.cloudapp.net";
                    }
                    else
                    {
                        return server;
                    }
                }
            }

            public static string SecureHost
            {
                get
                {
                    string server = Environment.GetEnvironmentVariable("COREFX_SECUREWEBSOCKETHOST");
                    if (string.IsNullOrEmpty(server))
                    {
                        return "corefx-net.cloudapp.net";
                    }
                    else
                    {
                        return server;
                    }
                }
            }
        }
    }
}
