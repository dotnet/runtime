// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class HttpClientHandler : HttpMessageHandler
    {
        #region Properties

        public virtual bool SupportsAutomaticDecompression
        {
            get { return _curlHandler.SupportsAutomaticDecompression; }
        }

        public virtual bool SupportsProxy
        {
            get { return this._curlHandler.SupportsProxy; }
        }

        public virtual bool SupportsRedirectConfiguration
        {
            get { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
        }

        public bool UseCookies
        {
            get
            {
                return _curlHandler.UseCookie;
            }

            set
            {
                _curlHandler.UseCookie = value;
            }          
        }

        public CookieContainer CookieContainer
        {
            get
            {
                return _curlHandler.CookieContainer;
            }

            set
            {
                _curlHandler.CookieContainer = value;
            }
        }

        public ClientCertificateOption ClientCertificateOptions
        {
            get
            {
                return _curlHandler.ClientCertificateOptions;
            }

            set
            {
                _curlHandler.ClientCertificateOptions = value;
            }
        }

        public DecompressionMethods AutomaticDecompression
        {
            get { return _curlHandler.AutomaticDecompression; }
            set { _curlHandler.AutomaticDecompression = value; }
        }

        public bool UseProxy
        {
            get { return this._curlHandler.UseProxy; }
            set { this._curlHandler.UseProxy = value; }
        }

        public IWebProxy Proxy
        {
            get { return this._curlHandler.Proxy; }
            set { this._curlHandler.Proxy = value; }
        }

        public bool PreAuthenticate
        {
            get { return _curlHandler.PreAuthenticate; }
            set { _curlHandler.PreAuthenticate = value;} 
        }

        public bool UseDefaultCredentials
        {
            get { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
            set { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
        }

        public ICredentials Credentials
        {
            get { return this._curlHandler.Credentials; }
            set { this._curlHandler.Credentials = value; }
        }

        public bool AllowAutoRedirect
        {
            get
            {
                return _curlHandler.AutomaticRedirection;
            }

            set
            {
                _curlHandler.AutomaticRedirection = value;
            }
        }

        public int MaxAutomaticRedirections
        {
            get
            {
                return _curlHandler.MaxAutomaticRedirections;
            }

            set
            {
                _curlHandler.MaxAutomaticRedirections = value;
            }
        }

        public long MaxRequestContentBufferSize
        {
            get { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
            set { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
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

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _curlHandler.SendAsync(request, cancellationToken);
        }

        #endregion Request Execution

        #region Private

        private readonly CurlHandler _curlHandler;

        #endregion Private
    }
}
