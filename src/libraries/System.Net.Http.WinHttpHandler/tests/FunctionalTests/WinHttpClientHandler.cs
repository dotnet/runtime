// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Security;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    // Only for testing purposes.
    // Contains HttpClientHandler.Windows.cs code from before its System.Net.Http removal.
    // Follows HttpClientHandler contract so that its tests can be easily reused for WinHttpHandler.
    // The only difference is in removal of DiagnosticsHandler since it's internal to System.Net.Http.
    public class WinHttpClientHandler : WinHttpHandler
    {
        private bool _useProxy;
        private readonly Version _requestVersion;

        public WinHttpClientHandler(Version requestVersion)
        {
            // Adjust defaults to match current .NET Desktop HttpClientHandler (based on HWR stack).
            AllowAutoRedirect = true;
            AutomaticDecompression = HttpHandlerDefaults.DefaultAutomaticDecompression;
            UseProxy = true;
            UseCookies = true;
            CookieContainer = new CookieContainer();
            DefaultProxyCredentials = null;
            ServerCredentials = null;

            // The existing .NET Desktop HttpClientHandler based on the HWR stack uses only WinINet registry
            // settings for the proxy.  This also includes supporting the "Automatic Detect a proxy" using
            // WPAD protocol and PAC file. So, for app-compat, we will do the same for the default proxy setting.
            WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
            Proxy = null;

            // Since the granular WinHttpHandler timeout properties are not exposed via the HttpClientHandler API,
            // we need to set them to infinite and allow the HttpClient.Timeout property to have precedence.
            ReceiveHeadersTimeout = Timeout.InfiniteTimeSpan;
            ReceiveDataTimeout = Timeout.InfiniteTimeSpan;
            SendTimeout = Timeout.InfiniteTimeSpan;

            _requestVersion = requestVersion;
        }

        public virtual bool SupportsAutomaticDecompression => true;
        public virtual bool SupportsProxy => true;
        public virtual bool SupportsRedirectConfiguration => true;

        public bool UseCookies
        {
            get => CookieUsePolicy == CookieUsePolicy.UseSpecifiedCookieContainer;
            set => CookieUsePolicy = value ? CookieUsePolicy.UseSpecifiedCookieContainer : CookieUsePolicy.IgnoreCookies;
        }

        public new CookieContainer CookieContainer
        {
            get => base.CookieContainer;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                base.CookieContainer = value;
            }
        }

        public bool UseProxy
        {
            get => _useProxy;
            set => _useProxy = value;
        }

        public bool UseDefaultCredentials
        {
            // WinHttpHandler doesn't have a separate UseDefaultCredentials property.  There
            // is just a ServerCredentials property.  So, we need to map the behavior.
            // Do the same for SocketsHttpHandler.Credentials.
            //
            // This property only affect .ServerCredentials and not .DefaultProxyCredentials.
            get => ServerCredentials == CredentialCache.DefaultCredentials;
            set
            {
                if (value)
                {
                    ServerCredentials = CredentialCache.DefaultCredentials;
                }
                else
                {
                    if (ServerCredentials == CredentialCache.DefaultCredentials)
                    {
                        // Only clear out the ServerCredentials property if it was a DefaultCredentials.
                        ServerCredentials = null;
                    }
                }
            }
        }

        public ICredentials Credentials
        {
            get => ServerCredentials;
            set => ServerCredentials = value;
        }

        public bool AllowAutoRedirect
        {
            get => AutomaticRedirection;
            set => AutomaticRedirection = value;
        }

        public long MaxRequestContentBufferSize
        {
            // This property is not supported. In the .NET Framework it was only used when the handler needed to
            // automatically buffer the request content. That only happened if neither 'Content-Length' nor
            // 'Transfer-Encoding: chunked' request headers were specified. So, the handler thus needed to buffer
            // in the request content to determine its length and then would choose 'Content-Length' semantics when
            // POST'ing. In .NET Core, the handler will resolve the ambiguity by always choosing
            // 'Transfer-Encoding: chunked'. The handler will never automatically buffer in the request content.
            get
            {
                return 0; // Returning zero is appropriate since in .NET Framework it means no limit.
            }

            set
            {
                if (value < 0 || value > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                // No-op on property setter.
            }
        }

        public ClientCertificateOption ClientCertificateOptions
        {
            get => ClientCertificateOption;
            set => ClientCertificateOption = value;
        }

        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback
        {
            get => ServerCertificateValidationCallback;
            set => ServerCertificateValidationCallback = value;
        }

        public static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> DangerousAcceptAnyServerCertificateValidator { get; } = delegate { return true; };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Get current value of WindowsProxyUsePolicy.  Only call its WinHttpHandler
            // property setter if the value needs to change.
            var oldProxyUsePolicy = base.WindowsProxyUsePolicy;

            if (_useProxy)
            {
                if (base.Proxy == null)
                {
                    if (oldProxyUsePolicy != WindowsProxyUsePolicy.UseWinInetProxy)
                    {
                        base.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
                    }
                }
                else
                {
                    if (oldProxyUsePolicy != WindowsProxyUsePolicy.UseCustomProxy)
                    {
                        base.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseCustomProxy;
                    }
                }
            }
            else
            {
                if (oldProxyUsePolicy != WindowsProxyUsePolicy.DoNotUseProxy)
                {
                    base.WindowsProxyUsePolicy = WindowsProxyUsePolicy.DoNotUseProxy;
                }
            }

            if(_requestVersion >= HttpVersion20.Value)
            {
                request.Version = _requestVersion;
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
