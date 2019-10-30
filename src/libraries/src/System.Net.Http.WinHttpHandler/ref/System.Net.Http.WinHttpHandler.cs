// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net.Http
{
    public enum CookieUsePolicy
    {
        IgnoreCookies = 0,
        UseInternalCookieStoreOnly = 1,
        UseSpecifiedCookieContainer = 2,
    }
    public enum WindowsProxyUsePolicy
    {
        DoNotUseProxy = 0,
        UseWinHttpProxy = 1,
        UseWinInetProxy = 2,
        UseCustomProxy = 3,
    }
    public partial class WinHttpHandler : System.Net.Http.HttpMessageHandler
    {
        public WinHttpHandler() { }
        public System.Net.DecompressionMethods AutomaticDecompression { get { throw null; } set { } }
        public bool AutomaticRedirection { get { throw null; } set { } }
        public bool CheckCertificateRevocationList { get { throw null; } set { } }
        public System.Net.Http.ClientCertificateOption ClientCertificateOption { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection ClientCertificates { get { throw null; } }
        public System.Net.CookieContainer CookieContainer { get { throw null; } set { } }
        public System.Net.Http.CookieUsePolicy CookieUsePolicy { get { throw null; } set { } }
        public System.Net.ICredentials DefaultProxyCredentials { get { throw null; } set { } }
        public int MaxAutomaticRedirections { get { throw null; } set { } }
        public int MaxConnectionsPerServer { get { throw null; } set { } }
        public int MaxResponseDrainSize { get { throw null; } set { } }
        public int MaxResponseHeadersLength { get { throw null; } set { } }
        public bool PreAuthenticate { get { throw null; } set { } }
        public System.Collections.Generic.IDictionary<string, object> Properties { get { throw null; } }
        public System.Net.IWebProxy Proxy { get { throw null; } set { } }
        public System.TimeSpan ReceiveDataTimeout { get { throw null; } set { } }
        public System.TimeSpan ReceiveHeadersTimeout { get { throw null; } set { } }
        public System.TimeSpan SendTimeout { get { throw null; } set { } }
        public System.Func<System.Net.Http.HttpRequestMessage, System.Security.Cryptography.X509Certificates.X509Certificate2, System.Security.Cryptography.X509Certificates.X509Chain, System.Net.Security.SslPolicyErrors, bool> ServerCertificateValidationCallback { get { throw null; } set { } }
        public System.Net.ICredentials ServerCredentials { get { throw null; } set { } }
        public System.Security.Authentication.SslProtocols SslProtocols { get { throw null; } set { } }
        public System.Net.Http.WindowsProxyUsePolicy WindowsProxyUsePolicy { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
