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
        UseCustomProxy = 3,
        UseWinHttpProxy = 1,
        UseWinInetProxy = 2,
    }
    public partial class WinHttpHandler : System.Net.Http.HttpMessageHandler
    {
        public WinHttpHandler() { }
        public System.Net.DecompressionMethods AutomaticDecompression { get { return default(System.Net.DecompressionMethods); } set { } }
        public bool AutomaticRedirection { get { return default(bool); } set { } }
        public bool CheckCertificateRevocationList { get { return default(bool); } set { } }
        public System.Net.Http.ClientCertificateOption ClientCertificateOption { get { return default(System.Net.Http.ClientCertificateOption); } set { } }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection ClientCertificates { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate2Collection); } }
        public System.TimeSpan ConnectTimeout { get { return default(System.TimeSpan); } set { } }
        public System.Net.CookieContainer CookieContainer { get { return default(System.Net.CookieContainer); } set { } }
        public System.Net.Http.CookieUsePolicy CookieUsePolicy { get { return default(System.Net.Http.CookieUsePolicy); } set { } }
        public System.Net.ICredentials DefaultProxyCredentials { get { return default(System.Net.ICredentials); } set { } }
        public int MaxAutomaticRedirections { get { return default(int); } set { } }
        public int MaxConnectionsPerServer { get { return default(int); } set { } }
        public int MaxResponseDrainSize { get { return default(int); } set { } }
        public int MaxResponseHeadersLength { get { return default(int); } set { } }
        public bool PreAuthenticate { get { return default(bool); } set { } }
        public System.Net.IWebProxy Proxy { get { return default(System.Net.IWebProxy); } set { } }
        public System.TimeSpan ReceiveDataTimeout { get { return default(System.TimeSpan); } set { } }
        public System.TimeSpan ReceiveHeadersTimeout { get { return default(System.TimeSpan); } set { } }
        public System.TimeSpan SendTimeout { get { return default(System.TimeSpan); } set { } }
        public System.Func<System.Net.Http.HttpRequestMessage, System.Security.Cryptography.X509Certificates.X509Certificate2, System.Security.Cryptography.X509Certificates.X509Chain, System.Net.Security.SslPolicyErrors, bool> ServerCertificateValidationCallback { get { return default(System.Func<System.Net.Http.HttpRequestMessage, System.Security.Cryptography.X509Certificates.X509Certificate2, System.Security.Cryptography.X509Certificates.X509Chain, System.Net.Security.SslPolicyErrors, bool>); } set { } }
        public System.Net.ICredentials ServerCredentials { get { return default(System.Net.ICredentials); } set { } }
        public System.Security.Authentication.SslProtocols SslProtocols { get { return default(System.Security.Authentication.SslProtocols); } set { } }
        public System.Net.Http.WindowsProxyUsePolicy WindowsProxyUsePolicy { get { return default(System.Net.Http.WindowsProxyUsePolicy); } set { } }
        protected override void Dispose(bool disposing) { }
        protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
    }
}
