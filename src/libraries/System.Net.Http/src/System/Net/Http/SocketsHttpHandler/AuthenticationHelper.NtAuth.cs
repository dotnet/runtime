// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Http
{
    internal static partial class AuthenticationHelper
    {
        private static Task<HttpResponseMessage> InnerSendAsync(HttpRequestMessage request, bool async, bool isProxyAuth, HttpConnectionPool pool, HttpConnection connection, CancellationToken cancellationToken)
        {
            return isProxyAuth ?
                connection.SendAsyncCore(request, async, cancellationToken) :
                pool.SendWithNtProxyAuthAsync(connection, request, async, cancellationToken);
        }

        private static bool ProxySupportsConnectionAuth(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues(KnownHeaders.ProxySupport.Descriptor, out IEnumerable<string>? values))
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

        private static async Task<HttpResponseMessage> SendWithNtAuthAsync(HttpRequestMessage request, Uri authUri, bool async, ICredentials credentials, bool isProxyAuth, HttpConnection connection, HttpConnectionPool connectionPool, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await InnerSendAsync(request, async, isProxyAuth, connectionPool, connection, cancellationToken).ConfigureAwait(false);
            if (!isProxyAuth && connection.Kind == HttpConnectionKind.Proxy && !ProxySupportsConnectionAuth(response))
            {
                // Proxy didn't indicate that it supports connection-based auth, so we can't proceed.
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(connection, $"Proxy doesn't support connection-based auth, uri={authUri}");
                }
                return response;
            }

            if (TryGetAuthenticationChallenge(response, isProxyAuth, authUri, credentials, out AuthenticationChallenge challenge))
            {
                if (challenge.AuthenticationType == AuthenticationType.Negotiate ||
                    challenge.AuthenticationType == AuthenticationType.Ntlm)
                {
                    bool isNewConnection = false;
                    bool needDrain = true;
                    try
                    {
                        if (response.Headers.ConnectionClose.GetValueOrDefault())
                        {
                            // Server is closing the connection and asking us to authenticate on a new connection.
                            connection = await connectionPool.CreateHttp11ConnectionAsync(request, async, cancellationToken).ConfigureAwait(false);
                            connectionPool.IncrementConnectionCount();
                            connection!.Acquire();
                            isNewConnection = true;
                            needDrain = false;
                        }

                        if (NetEventSource.Log.IsEnabled())
                        {
                            NetEventSource.Info(connection, $"Authentication: {challenge.AuthenticationType}, Uri: {authUri.AbsoluteUri}");
                        }

                        // Calculate SPN (Service Principal Name) using the host name of the request.
                        // Use the request's 'Host' header if available. Otherwise, use the request uri.
                        // Ignore the 'Host' header if this is proxy authentication since we need to use
                        // the host name of the proxy itself for SPN calculation.
                        string hostName;
                        if (!isProxyAuth && request.HasHeaders && request.Headers.Host != null)
                        {
                            // Use the host name without any normalization.
                            hostName = request.Headers.Host;
                            if (NetEventSource.Log.IsEnabled())
                            {
                                NetEventSource.Info(connection, $"Authentication: {challenge.AuthenticationType}, Host: {hostName}");
                            }
                        }
                        else
                        {
                            // Need to use FQDN normalized host so that CNAME's are traversed.
                            // Use DNS to do the forward lookup to an A (host) record.
                            // But skip DNS lookup on IP literals. Otherwise, we would end up
                            // doing an unintended reverse DNS lookup.
                            UriHostNameType hnt = authUri.HostNameType;
                            if (hnt == UriHostNameType.IPv6 || hnt == UriHostNameType.IPv4)
                            {
                                hostName = authUri.IdnHost;
                            }
                            else
                            {
                                IPHostEntry result = await Dns.GetHostEntryAsync(authUri.IdnHost, cancellationToken).ConfigureAwait(false);
                                hostName = result.HostName;
                            }

                            if (!isProxyAuth && !authUri.IsDefaultPort)
                            {
                                hostName = string.Create(null, stackalloc char[128], $"{hostName}:{authUri.Port}");
                            }
                        }

                        string spn = "HTTP/" + hostName;
                        if (NetEventSource.Log.IsEnabled())
                        {
                            NetEventSource.Info(connection, $"Authentication: {challenge.AuthenticationType}, SPN: {spn}");
                        }

                        ChannelBinding? channelBinding = connection.TransportContext?.GetChannelBinding(ChannelBindingKind.Endpoint);
                        NTAuthentication authContext = new NTAuthentication(isServer: false, challenge.SchemeName, challenge.Credential, spn, ContextFlagsPal.Connection | ContextFlagsPal.InitIntegrity, channelBinding);
                        string? challengeData = challenge.ChallengeData;
                        try
                        {
                            while (true)
                            {
                                string? challengeResponse = authContext.GetOutgoingBlob(challengeData);
                                if (challengeResponse == null)
                                {
                                    // Response indicated denial even after login, so stop processing and return current response.
                                    break;
                                }

                                if (needDrain)
                                {
                                    await connection.DrainResponseAsync(response!, cancellationToken).ConfigureAwait(false);
                                }

                                SetRequestAuthenticationHeaderValue(request, new AuthenticationHeaderValue(challenge.SchemeName, challengeResponse), isProxyAuth);

                                response = await InnerSendAsync(request, async, isProxyAuth, connectionPool, connection, cancellationToken).ConfigureAwait(false);
                                if (authContext.IsCompleted || !TryGetRepeatedChallenge(response, challenge.SchemeName, isProxyAuth, out challengeData))
                                {
                                    break;
                                }

                                needDrain = true;
                            }
                        }
                        finally
                        {
                            authContext.CloseContext();
                        }
                    }
                    finally
                    {
                        if (isNewConnection)
                        {
                            connection!.Release();
                        }
                    }
                }
            }

            return response!;
        }

        public static Task<HttpResponseMessage> SendWithNtProxyAuthAsync(HttpRequestMessage request, Uri proxyUri, bool async, ICredentials proxyCredentials, HttpConnection connection, HttpConnectionPool connectionPool, CancellationToken cancellationToken)
        {
            return SendWithNtAuthAsync(request, proxyUri, async, proxyCredentials, isProxyAuth: true, connection, connectionPool, cancellationToken);
        }

        public static Task<HttpResponseMessage> SendWithNtConnectionAuthAsync(HttpRequestMessage request, bool async, ICredentials credentials, HttpConnection connection, HttpConnectionPool connectionPool, CancellationToken cancellationToken)
        {
            Debug.Assert(request.RequestUri != null);
            return SendWithNtAuthAsync(request, request.RequestUri, async, credentials, isProxyAuth: false, connection, connectionPool, cancellationToken);
        }
    }
}
