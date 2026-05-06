// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class HttpClientHandler : HttpMessageHandler
    {
        public const string Message = "HTTP stack not implemented";

        #region Properties

        public virtual bool SupportsAutomaticDecompression
        {
            get { throw NotImplemented.ByDesignWithMessage(Message); }
        }

        public virtual bool SupportsProxy
        {
            get { throw NotImplemented.ByDesignWithMessage(Message); }
        }

        public virtual bool SupportsRedirectConfiguration
        {
            get { throw NotImplemented.ByDesignWithMessage(Message); }
        }

        public bool UseCookies
        {
            get { throw NotImplemented.ByDesignWithMessage(Message); }
            set { throw NotImplemented.ByDesignWithMessage(Message); }
        }

        public CookieContainer CookieContainer
        {
            get { throw NotImplemented.ByDesignWithMessage(Message); }
            set { throw NotImplemented.ByDesignWithMessage(Message); }
        }

        public ClientCertificateOption ClientCertificateOptions
        {
            get { throw NotImplemented.ByDesignWithMessage(Message); }
            set { throw NotImplemented.ByDesignWithMessage(Message); }
        }

        public DecompressionMethods AutomaticDecompression
        {
            get { throw NotImplemented.ByDesignWithMessage(Message); }
            set { throw NotImplemented.ByDesignWithMessage(Message); }
        }

        public bool UseProxy
        {
            get { throw NotImplemented.ByDesignWithMessage(Message); }
            set { throw NotImplemented.ByDesignWithMessage(Message); }
        }

        public IWebProxy Proxy
        {
            get { throw NotImplemented.ByDesignWithMessage(Message); }
            set { throw NotImplemented.ByDesignWithMessage(Message); }
        }

        public bool PreAuthenticate
        {
            get { throw NotImplemented.ByDesignWithMessage(Message); }
            set { throw NotImplemented.ByDesignWithMessage(Message); }
        }

        public bool UseDefaultCredentials
        {
            get { throw NotImplemented.ByDesignWithMessage(Message); }
            set { throw NotImplemented.ByDesignWithMessage(Message); }
        }

        public ICredentials Credentials
        {
            get { throw NotImplemented.ByDesignWithMessage(Message); }
            set { throw NotImplemented.ByDesignWithMessage(Message); }
        }

        public bool AllowAutoRedirect
        {
            get { throw NotImplemented.ByDesignWithMessage(Message); }
            set { throw NotImplemented.ByDesignWithMessage(Message); }
        }

        public int MaxAutomaticRedirections
        {
            get { throw NotImplemented.ByDesignWithMessage(Message); }
            set { throw NotImplemented.ByDesignWithMessage(Message); }
        }

        public long MaxRequestContentBufferSize
        {
            get { throw NotImplemented.ByDesignWithMessage(Message); }
            set { throw NotImplemented.ByDesignWithMessage(Message); }
        }

        #endregion Properties

        #region Request Execution

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw NotImplemented.ByDesignWithMessage(Message);
        }

        #endregion Request Execution
    }
}
