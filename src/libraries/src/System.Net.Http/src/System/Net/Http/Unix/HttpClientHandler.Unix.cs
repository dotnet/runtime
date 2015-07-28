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
            get { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
        }

        public virtual bool SupportsProxy
        {
            get { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
        }

        public virtual bool SupportsRedirectConfiguration
        {
            get { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
        }

        public bool UseCookies
        {
            get { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
            set { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
        }

        public CookieContainer CookieContainer
        {
            get { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
            set { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
        }

        public ClientCertificateOption ClientCertificateOptions
        {
            get { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
            set { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
        }

        public DecompressionMethods AutomaticDecompression
        {
            get { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
            set { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
        }

        public bool UseProxy
        {
            get { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
            set { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
        }

        public IWebProxy Proxy
        {
            get { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
            set { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
        }

        public bool PreAuthenticate
        {
            get { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
            set { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
        }

        public bool UseDefaultCredentials
        {
            get { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
            set { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
        }

        public ICredentials Credentials
        {
            get { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
            set { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
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
            get { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
            set { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
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

            // TODO: Set same defaults as Windows handler
            AllowAutoRedirect = true;
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
