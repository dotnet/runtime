// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Test.Common
{
    public static partial class Configuration
    {
        public static partial class Security
        {
            private static readonly string DefaultAzureServer = "corefx-net.cloudapp.net";

            public static string ActiveDirectoryName => GetValue("COREFX_NET_AD_DOMAINNAME");

            public static string ActiveDirectoryUserName => GetValue("COREFX_NET_AD_USERNAME");

            public static string ActiveDirectoryUserPassword => GetValue("COREFX_NET_AD_PASSWORD");

            public static Uri TlsServer => GetUriValue("COREFX_NET_SECURITY_TLSSERVERURI", new Uri("https://" + DefaultAzureServer));

            public static Uri NegotiateServer => GetUriValue("COREFX_NET_SECURITY_NEGOSERVERURI");
        }
    }
}
