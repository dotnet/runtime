// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Test.Common
{
    public static class EnterpriseTestConfiguration
    {
        public const string Realm = "LINUX.CONTOSO.COM";
        public const string NegotiateAuthWebServer = "http://apacheweb.linux.contoso.com";
        public const string AlternativeService = "http://altweb.linux.contoso.com:8080";

        public static bool Enabled => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNTIME_ENTERPRISETESTS_ENABLED"));
        public static NetworkCredential ValidNetworkCredentials => new NetworkCredential("user1", "password");
        public static NetworkCredential InvalidNetworkCredentials => new NetworkCredential("user1", "passwordxx");
    }
}
