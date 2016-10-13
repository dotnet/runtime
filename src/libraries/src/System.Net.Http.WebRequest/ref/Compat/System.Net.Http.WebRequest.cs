// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http
{
    public partial class WebRequestHandler : System.Net.Http.HttpClientHandler
    {
        public WebRequestHandler() { }
        public bool AllowPipelining { get { throw null; } set { } }
        public System.Net.Security.AuthenticationLevel AuthenticationLevel { get { throw null; } set { } }
        public System.Net.Cache.RequestCachePolicy CachePolicy { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509CertificateCollection ClientCertificates { get { throw null; } }
        public System.TimeSpan ContinueTimeout { get { throw null; } set { } }
        public System.Security.Principal.TokenImpersonationLevel ImpersonationLevel { get { throw null; } set { } }
        public int MaxResponseHeadersLength { get { throw null; } set { } }
        public int ReadWriteTimeout { get { throw null; } set { } }
        public System.Net.Security.RemoteCertificateValidationCallback ServerCertificateValidationCallback { get { throw null; } set { } }
        public bool UnsafeAuthenticatedConnectionSharing { get { throw null; } set { } }
    }
}
