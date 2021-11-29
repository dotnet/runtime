// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Test.Common
{
    public static class EnterpriseTestConfiguration
    {
        public const string Realm = "LINUX.CONTOSO.COM";
        public const string NegotiateAuthWebServer = "http://apacheweb.linux.contoso.com/auth/kerberos/";
        public const string NegotiateAuthWebServerNotDefaultPort = "http://apacheweb.linux.contoso.com:8081/auth/kerberos/";
        public const string AlternativeService = "http://altweb.linux.contoso.com:8080/auth/kerberos/";
        public const string NtlmAuthWebServer = "http://apacheweb.linux.contoso.com:8080/auth/ntlm/";
        public const string DigestAuthWebServer = "http://apacheweb.linux.contoso.com/auth/digest/";

        public static bool Enabled => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNTIME_ENTERPRISETESTS_ENABLED"));
        // Folowing credentials are used only in docker scenario, it is not leaking any secrets.
        public static NetworkCredential ValidNetworkCredentials => new NetworkCredential("user1", "PLACEHOLDERcorrect20");
        public static NetworkCredential ValidDomainNetworkCredentials => new NetworkCredential("user1", "PLACEHOLDERcorrect20", "LINUX" );
        public static NetworkCredential InvalidNetworkCredentials => new NetworkCredential("user1", "PLACEHOLDERwong");
    }
}
