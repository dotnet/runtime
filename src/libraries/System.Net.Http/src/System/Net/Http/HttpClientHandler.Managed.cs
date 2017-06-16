#if false
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Managed;
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

        public virtual bool SupportsAutomaticDecompression
        {
            get { return true; }
        }

        public virtual bool SupportsProxy
        {
            get { return true; }
        }

        public virtual bool SupportsRedirectConfiguration
        {
            get { return true; }
        }

        public bool UseCookies
        {
            get { return _managedHttpHandler.UseCookies; }
            set { _managedHttpHandler.UseCookies = value; }
        }

        public CookieContainer CookieContainer
        {
            get { return _managedHttpHandler.CookieContainer; }
            set { _managedHttpHandler.CookieContainer = value; }
        }

        public ClientCertificateOption ClientCertificateOptions
        {
            get { return _managedHttpHandler.ClientCertificateOptions; }
            set { _managedHttpHandler.ClientCertificateOptions = value; }
        }

        public DecompressionMethods AutomaticDecompression
        {
            get { return _managedHttpHandler.AutomaticDecompression; }
            set { _managedHttpHandler.AutomaticDecompression = value; }
        }

        public bool UseProxy
        {
            get { return _managedHttpHandler.UseProxy; }
            set { _managedHttpHandler.UseProxy = true; }
        }

        public IWebProxy Proxy
        {
            get { return _managedHttpHandler.Proxy; }
            set { _managedHttpHandler.Proxy = value; }
        }

        public ICredentials DefaultProxyCredentials
        {
            get { return _managedHttpHandler.DefaultProxyCredentials; }
            set { _managedHttpHandler.DefaultProxyCredentials = value; }
        }

        public bool PreAuthenticate
        {
            get { return _managedHttpHandler.PreAuthenticate; }
            set { _managedHttpHandler.PreAuthenticate = value; }
        }

        public bool UseDefaultCredentials
        {
            get { return _managedHttpHandler.UseDefaultCredentials; }
            set { _managedHttpHandler.UseDefaultCredentials = value; }
        }

        public ICredentials Credentials
        {
            get { return _managedHttpHandler.Credentials; }
            set { _managedHttpHandler.Credentials = value; }
        }

        public bool AllowAutoRedirect
        {
            get { return _managedHttpHandler.AllowAutoRedirect; }
            set { _managedHttpHandler.AllowAutoRedirect = value; }
        }

        public int MaxAutomaticRedirections
        {
            get { return _managedHttpHandler.MaxAutomaticRedirections; }
            set { _managedHttpHandler.MaxAutomaticRedirections = value; }
        }

        public int MaxConnectionsPerServer
        {
            get { return _managedHttpHandler.MaxConnectionsPerServer; }
            set { _managedHttpHandler.MaxConnectionsPerServer = value; }
        }

        public long MaxRequestContentBufferSize
        {
            // This property has been deprecated. In the .NET Desktop it was only used when the handler needed to 
            // automatically buffer the request content. That only happened if neither 'Content-Length' nor 
            // 'Transfer-Encoding: chunked' request headers were specified. So, the handler thus needed to buffer
            // in the request content to determine its length and then would choose 'Content-Length' semantics when
            // POST'ing. In CoreCLR and .NETNative, the handler will resolve the ambiguity by always choosing
            // 'Transfer-Encoding: chunked'. The handler will never automatically buffer in the request content.
            get { return 0; }
            
            // TODO (#7879): Add message/link to exception explaining the deprecation. 
            // Update corresponding exception in HttpClientHandler.Unix.cs if/when this is updated.
            set { throw new PlatformNotSupportedException(); }
        }

        public int MaxResponseHeadersLength
        {
            get { return _managedHttpHandler.MaxResponseHeadersLength; }
            set { _managedHttpHandler.MaxResponseHeadersLength = value; }
        }

        public X509CertificateCollection ClientCertificates
        {
            get { return _managedHttpHandler.ClientCertificates; }
        }

        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback
        {
            get { return _managedHttpHandler.ServerCertificateCustomValidationCallback; }
            set { _managedHttpHandler.ServerCertificateCustomValidationCallback = value; }
        }

        public bool CheckCertificateRevocationList
        {
            get { return _managedHttpHandler.CheckCertificateRevocationList; }
            set { _managedHttpHandler.CheckCertificateRevocationList = value; }
        }

        public SslProtocols SslProtocols
        {
            get { return _managedHttpHandler.SslProtocols; }
            set { _managedHttpHandler.SslProtocols = value; }
        }

        public IDictionary<String, object> Properties
        {
            get { return _managedHttpHandler.Properties; }
        }

        #endregion Properties

        #region De/Constructors

        public HttpClientHandler()
        {
            _managedHttpHandler = new ManagedHttpClientHandler();
            _diagnosticsPipeline = new DiagnosticsHandler(_managedHttpHandler);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _managedHttpHandler.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion De/Constructors

        #region Request Execution

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _managedHttpHandler.SendAsync(request, cancellationToken);
        }

        #endregion Request Execution

        #region Private

        private ManagedHttpClientHandler _managedHttpHandler;
        private readonly DiagnosticsHandler _diagnosticsPipeline;
        private volatile bool _disposed;
        #endregion Private

    }
}
#endif
