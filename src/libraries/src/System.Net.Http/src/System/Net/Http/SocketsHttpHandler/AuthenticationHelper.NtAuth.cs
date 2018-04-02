// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Http
{
    internal partial class AuthenticationHelper
    {
        private static Task<HttpResponseMessage> InnerSendAsync(HttpRequestMessage request, bool isProxyAuth, HttpConnection connection, CancellationToken cancellationToken)
        {
            return isProxyAuth ?
                connection.SendAsyncCore(request, cancellationToken) :
                connection.SendWithNtProxyAuthAsync(request, cancellationToken);
        }

        private static bool ProxySupportsConnectionAuth(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues(KnownHeaders.ProxySupport.Descriptor, out IEnumerable<string> values))
            {
                return false;
            }

            foreach (string v in values)
            {
                if (v == "Session-Based-Authentication")
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task<HttpResponseMessage> SendWithNtAuthAsync(HttpRequestMessage request, Uri authUri, ICredentials credentials, bool isProxyAuth, HttpConnection connection, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await InnerSendAsync(request, isProxyAuth, connection, cancellationToken).ConfigureAwait(false);

            if (!isProxyAuth && connection.UsingProxy && !ProxySupportsConnectionAuth(response))
            {
                // Proxy didn't indicate that it supports connection-based auth, so we can't proceed.
                return response;
            }

            if (TryGetAuthenticationChallenge(response, isProxyAuth, authUri, credentials, out AuthenticationChallenge challenge))
            {
                if (challenge.AuthenticationType == AuthenticationType.Negotiate || 
                    challenge.AuthenticationType == AuthenticationType.Ntlm)
                {
                    string challengeData = challenge.ChallengeData;

                    string spn = "HTTP/" + authUri.IdnHost;
                    ChannelBinding channelBinding = connection.TransportContext?.GetChannelBinding(ChannelBindingKind.Endpoint);
                    NTAuthentication authContext = new NTAuthentication(isServer:false, challenge.SchemeName, challenge.Credential, spn, ContextFlagsPal.Connection, channelBinding);
                    try
                    {
                        while (true)
                        {
                            string challengeResponse = authContext.GetOutgoingBlob(challengeData);
                            if (challengeResponse == null)
                            {
                                // Response indicated denial even after login, so stop processing and return current response.
                                break;
                            }

                            await connection.DrainResponseAsync(response).ConfigureAwait(false);

                            SetRequestAuthenticationHeaderValue(request, new AuthenticationHeaderValue(challenge.SchemeName, challengeResponse), isProxyAuth);

                            response = await InnerSendAsync(request, isProxyAuth, connection, cancellationToken).ConfigureAwait(false);
                            if (authContext.IsCompleted || !TryGetRepeatedChallenge(response, challenge.SchemeName, isProxyAuth, out challengeData))
                            {
                                break;
                            }
                        }
                    }
                    finally
                    {
                        authContext.CloseContext();
                    }
                }
            }

            return response;
        }

        public static Task<HttpResponseMessage> SendWithNtProxyAuthAsync(HttpRequestMessage request, Uri proxyUri, ICredentials proxyCredentials, HttpConnection connection, CancellationToken cancellationToken)
        {
            return SendWithNtAuthAsync(request, proxyUri, proxyCredentials, isProxyAuth:true, connection, cancellationToken);
        }

        public static Task<HttpResponseMessage> SendWithNtConnectionAuthAsync(HttpRequestMessage request, ICredentials credentials, HttpConnection connection, CancellationToken cancellationToken)
        {
            return SendWithNtAuthAsync(request, request.RequestUri, credentials, isProxyAuth:false, connection, cancellationToken);
        }
    }
}

