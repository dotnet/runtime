// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class HttpClientHandler : HttpMessageHandler
    {
        #region Properties

        public virtual bool SupportsAutomaticDecompression => _curlHandler.SupportsAutomaticDecompression;

        public virtual bool SupportsProxy => _curlHandler.SupportsProxy;

        public virtual bool SupportsRedirectConfiguration => _curlHandler.SupportsRedirectConfiguration;

        public bool UseCookies
        {
            get { return _curlHandler.UseCookie; }
            set { _curlHandler.UseCookie = value; }
        }

        public CookieContainer CookieContainer
        {
            get { return _curlHandler.CookieContainer; }
            set { _curlHandler.CookieContainer = value; }
        }

        public ClientCertificateOption ClientCertificateOptions
        {
            get { return _curlHandler.ClientCertificateOptions; }
            set { _curlHandler.ClientCertificateOptions = value; }
        }

        public X509CertificateCollection ClientCertificates => _curlHandler.ClientCertificates;

        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback
        {
            get { return _curlHandler.ServerCertificateValidationCallback; }
            set { _curlHandler.ServerCertificateValidationCallback = value; }
        }

        public bool CheckCertificateRevocationList
        {
            get { return _curlHandler.CheckCertificateRevocationList; }
            set { _curlHandler.CheckCertificateRevocationList = value; }
        }

        public SslProtocols SslProtocols
        {
            get { return _curlHandler.SslProtocols; }
            set { _curlHandler.SslProtocols = value; }
        }

        public DecompressionMethods AutomaticDecompression
        {
            get { return _curlHandler.AutomaticDecompression; }
            set { _curlHandler.AutomaticDecompression = value; }
        }

        public bool UseProxy
        {
            get { return _curlHandler.UseProxy; }
            set { _curlHandler.UseProxy = value; }
        }

        public IWebProxy Proxy
        {
            get { return _curlHandler.Proxy; }
            set { _curlHandler.Proxy = value; }
        }

        public ICredentials DefaultProxyCredentials
        {
            get { return _curlHandler.DefaultProxyCredentials; }
            set { _curlHandler.DefaultProxyCredentials = value; }
        }

        public bool PreAuthenticate
        {
            get { return _curlHandler.PreAuthenticate; }
            set { _curlHandler.PreAuthenticate = value;} 
        }

        public bool UseDefaultCredentials
        {
            get { return _curlHandler.UseDefaultCredentials; }
            set { _curlHandler.UseDefaultCredentials = value; }
        }

        public ICredentials Credentials
        {
            get { return _curlHandler.Credentials; }
            set { _curlHandler.Credentials = value; }
        }

        public bool AllowAutoRedirect
        {
            get { return _curlHandler.AutomaticRedirection; }
            set { _curlHandler.AutomaticRedirection = value; }
        }

        public int MaxAutomaticRedirections
        {
            get { return _curlHandler.MaxAutomaticRedirections; }
            set { _curlHandler.MaxAutomaticRedirections = value; }
        }

        public int MaxConnectionsPerServer
        {
            get { return _curlHandler.MaxConnectionsPerServer; }
            set { _curlHandler.MaxConnectionsPerServer = value; }
        }

        public long MaxRequestContentBufferSize
        {
            // See comments in HttpClientHandler.Windows.cs.  This behavior matches.
            get { return 0; }
            set { throw new PlatformNotSupportedException(); }
        }

        public int MaxResponseHeadersLength
        {
            get { return _curlHandler.MaxResponseHeadersLength; }
            set { _curlHandler.MaxResponseHeadersLength = value; }
        }

        public IDictionary<String, object> Properties
        {
            get { return _curlHandler.Properties; }
        }
        #endregion Properties

        #region De/Constructors

        public HttpClientHandler()
        {
            _curlHandler = new CurlHandler();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _curlHandler.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion De/Constructors

        #region Request Execution

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _curlHandler.SendAsync(request, cancellationToken);
        }

        #endregion Request Execution

        #region Private

        private readonly CurlHandler _curlHandler;

        #endregion Private
    }
}
