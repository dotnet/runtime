// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
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
            get { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
            set { throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented"); }
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
            throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented");
        }

        #endregion De/Constructors

        #region Request Execution

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw NotImplemented.ByDesignWithMessage("HTTP stack not implemented");
        }

        #endregion Request Execution
    }
}

