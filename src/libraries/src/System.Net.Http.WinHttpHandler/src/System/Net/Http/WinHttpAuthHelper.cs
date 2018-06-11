// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http
{
    internal class WinHttpAuthHelper
    {
        // TODO: Issue #2165. This looks messy but it is fast. Research a cleaner way
        // to do this which keeps high performance lookup.
        //
        // Fast lookup table to convert WINHTTP_AUTH constants to strings.
        // WINHTTP_AUTH_SCHEME_BASIC = 0x00000001;
        // WINHTTP_AUTH_SCHEME_NTLM = 0x00000002;
        // WINHTTP_AUTH_SCHEME_DIGEST = 0x00000008;
        // WINHTTP_AUTH_SCHEME_NEGOTIATE = 0x00000010;
        private static readonly string[] s_authSchemeStringMapping =
        {
            null,
            "Basic",
            "NTLM",
            null,
            null,
            null,
            null,
            null,
            "Digest",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "Negotiate"
        };

        private static readonly uint[] s_authSchemePriorityOrder =
        {
            Interop.WinHttp.WINHTTP_AUTH_SCHEME_NEGOTIATE,
            Interop.WinHttp.WINHTTP_AUTH_SCHEME_NTLM,
            Interop.WinHttp.WINHTTP_AUTH_SCHEME_DIGEST,
            Interop.WinHttp.WINHTTP_AUTH_SCHEME_BASIC
        };

        // TODO: Issue #2165. This current design uses a handler-wide lock to Add/Retrieve
        // from the cache.  Need to improve this for next iteration in order
        // to boost performance and scalability.
        private readonly CredentialCache _credentialCache = new CredentialCache();
        private readonly object _credentialCacheLock = new object();
        
        public void CheckResponseForAuthentication(
            WinHttpRequestState state,
            ref uint proxyAuthScheme,
            ref uint serverAuthScheme)
        {
            uint supportedSchemes = 0;
            uint firstSchemeIgnored = 0;
            uint authTarget = 0;
            Uri uri = state.RequestMessage.RequestUri;

            state.RetryRequest = false;

            // Check the status code and retry the request applying credentials if needed.
            var statusCode = (HttpStatusCode)WinHttpResponseParser.GetResponseHeaderNumberInfo(
                state.RequestHandle,
                Interop.WinHttp.WINHTTP_QUERY_STATUS_CODE);

            switch (statusCode)
            {
                case HttpStatusCode.Unauthorized:
                    if (state.ServerCredentials == null || state.LastStatusCode == HttpStatusCode.Unauthorized)
                    {
                        // Either we don't have server credentials or we already tried 
                        // to set the credentials and it failed before.
                        // So we will let the 401 be the final status code returned.
                        break;
                    }
                    
                    state.LastStatusCode = statusCode;

                    // Determine authorization scheme to use. We ignore the firstScheme
                    // parameter which is included in the supportedSchemes flags already.
                    // We pass the schemes to ChooseAuthScheme which will pick the scheme
                    // based on most secure scheme to least secure scheme ordering.
                    if (!Interop.WinHttp.WinHttpQueryAuthSchemes(
                        state.RequestHandle,
                        out supportedSchemes,
                        out firstSchemeIgnored,
                        out authTarget))
                    {
                        // WinHTTP returns an error for schemes it doesn't handle.
                        // So, we need to ignore the error and just let it stay at 401.
                        break;
                    }

                    // WinHTTP returns the proper authTarget based on the status code (401, 407).
                    // But we can validate with assert.
                    Debug.Assert(authTarget == Interop.WinHttp.WINHTTP_AUTH_TARGET_SERVER);

                    serverAuthScheme = ChooseAuthScheme(supportedSchemes, state.RequestMessage.RequestUri, state.ServerCredentials);
                    if (serverAuthScheme != 0)
                    {
                        if (SetWinHttpCredential(
                            state.RequestHandle,
                            state.ServerCredentials,
                            uri,
                            serverAuthScheme,
                            authTarget))
                        {
                            state.RetryRequest = true;
                        }
                    }
                    
                    break;

                case HttpStatusCode.ProxyAuthenticationRequired:
                    if (state.LastStatusCode == HttpStatusCode.ProxyAuthenticationRequired)
                    {
                        // We tried already to set the credentials.
                        break;
                    }
                    
                    state.LastStatusCode = statusCode;

                    // If we don't have any proxy credentials to try, then we end up with 407.
                    ICredentials proxyCreds = state.Proxy == null ?
                        state.DefaultProxyCredentials :
                        state.Proxy.Credentials;
                     if (proxyCreds == null)
                     {
                         break;
                     }

                    // Determine authorization scheme to use. We ignore the firstScheme
                    // parameter which is included in the supportedSchemes flags already.
                    // We pass the schemes to ChooseAuthScheme which will pick the scheme
                    // based on most secure scheme to least secure scheme ordering.
                    if (!Interop.WinHttp.WinHttpQueryAuthSchemes(
                        state.RequestHandle,
                        out supportedSchemes,
                        out firstSchemeIgnored,
                        out authTarget))
                    {
                        // WinHTTP returns an error for schemes it doesn't handle.
                        // So, we need to ignore the error and just let it stay at 407.
                        break;
                    }

                    // WinHTTP returns the proper authTarget based on the status code (401, 407).
                    // But we can validate with assert.
                    Debug.Assert(authTarget == Interop.WinHttp.WINHTTP_AUTH_TARGET_PROXY);

                    proxyAuthScheme = ChooseAuthScheme(
                        supportedSchemes,
                        // TODO: Issue #6997. If Proxy==null, we're using the system proxy which is possibly
                        // discovered/calculated with a PAC file. So, we can't determine the actual proxy uri at
                        // this point since it is calculated internally in WinHTTP. For now, pass in null for the uri.
                        state.Proxy?.GetProxy(state.RequestMessage.RequestUri),
                        proxyCreds);
                    state.RetryRequest = true;
                    break;

                default:
                    if (state.PreAuthenticate && serverAuthScheme != 0)
                    {
                        SaveServerCredentialsToCache(uri, serverAuthScheme, state.ServerCredentials);
                    }
                    break;
            }
        }

        public void PreAuthenticateRequest(WinHttpRequestState state, uint proxyAuthScheme)
        {
            // Set proxy credentials if we have them.
            // If a proxy authentication challenge was responded to, reset
            // those credentials before each SendRequest, because the proxy  
            // may require re-authentication after responding to a 401 or  
            // to a redirect. If you don't, you can get into a 
            // 407-401-407-401- loop.
            if (proxyAuthScheme != 0)
            {
                ICredentials proxyCredentials;
                Uri proxyUri;
                if (state.Proxy != null)
                {
                    proxyCredentials = state.Proxy.Credentials;
                    proxyUri = state.Proxy.GetProxy(state.RequestMessage.RequestUri);
                }
                else
                {
                    proxyCredentials = state.DefaultProxyCredentials;
                    proxyUri = state.RequestMessage.RequestUri;
                }

                SetWinHttpCredential(
                    state.RequestHandle,
                    proxyCredentials,
                    proxyUri,
                    proxyAuthScheme,
                    Interop.WinHttp.WINHTTP_AUTH_TARGET_PROXY);
            }

            // Apply pre-authentication headers for server authentication?
            if (state.PreAuthenticate)
            {
                uint authScheme;
                NetworkCredential serverCredentials;
                if (GetServerCredentialsFromCache(
                        state.RequestMessage.RequestUri,
                        out authScheme,
                        out serverCredentials))
                {
                    SetWinHttpCredential(
                        state.RequestHandle,
                        serverCredentials,
                        state.RequestMessage.RequestUri,
                        authScheme,
                        Interop.WinHttp.WINHTTP_AUTH_TARGET_SERVER);
                    state.LastStatusCode = HttpStatusCode.Unauthorized; // Remember we already set the creds.
                }
                
                // No cached credential to use at this time. The request will first go out with no
                // 'Authorization' header. Later, if a 401 occurs, we will be able to cache the credential
                // since we will then know the proper auth scheme to use.
                //
                // TODO: Issue #2165. Adding logging to highlight the 'cache miss'.
            }
        }

        // TODO: Issue #2165. Consider refactoring cache logic in separate class and avoid out parameters.
        public bool GetServerCredentialsFromCache(
            Uri uri,
            out uint serverAuthScheme,
            out NetworkCredential serverCredentials)
        {
            serverAuthScheme = 0;
            serverCredentials = null;

            NetworkCredential cred = null;

            lock (_credentialCacheLock)
            {
                foreach (uint authScheme in s_authSchemePriorityOrder)
                {
                    cred = _credentialCache.GetCredential(uri, s_authSchemeStringMapping[authScheme]);
                    if (cred != null)
                    {
                        serverAuthScheme = authScheme;
                        serverCredentials = cred;

                        return true;
                    }
                }
            }

            return false;
        }

        public void SaveServerCredentialsToCache(Uri uri, uint authScheme, ICredentials serverCredentials)
        {
            string authType = s_authSchemeStringMapping[authScheme];
            Debug.Assert(!string.IsNullOrEmpty(authType));

            NetworkCredential cred = serverCredentials.GetCredential(uri, authType);
            if (cred != null)
            {
                lock (_credentialCacheLock)
                {
                    try
                    {
                        _credentialCache.Add(uri, authType, cred);
                    }
                    catch (ArgumentException)
                    {
                        // The credential was already added.
                    }
                }
            }
        }

        public void ChangeDefaultCredentialsPolicy(
            SafeWinHttpHandle requestHandle,
            uint authTarget,
            bool allowDefaultCredentials)
        {
            Debug.Assert(authTarget == Interop.WinHttp.WINHTTP_AUTH_TARGET_PROXY || 
                         authTarget == Interop.WinHttp.WINHTTP_AUTH_TARGET_SERVER);

            uint optionData = allowDefaultCredentials ?
                (authTarget == Interop.WinHttp.WINHTTP_AUTH_TARGET_PROXY ?
                    Interop.WinHttp.WINHTTP_AUTOLOGON_SECURITY_LEVEL_MEDIUM :
                    Interop.WinHttp.WINHTTP_AUTOLOGON_SECURITY_LEVEL_LOW) :
                Interop.WinHttp.WINHTTP_AUTOLOGON_SECURITY_LEVEL_HIGH;

            if (!Interop.WinHttp.WinHttpSetOption(
                requestHandle,
                Interop.WinHttp.WINHTTP_OPTION_AUTOLOGON_POLICY,
                ref optionData))
            {
                WinHttpException.ThrowExceptionUsingLastError(nameof(Interop.WinHttp.WinHttpSetOption));
            }
        }

        private bool SetWinHttpCredential(
            SafeWinHttpHandle requestHandle,
            ICredentials credentials,
            Uri uri,
            uint authScheme,
            uint authTarget)
        {
            string userName;
            string password;

            Debug.Assert(credentials != null);
            Debug.Assert(authScheme != 0);
            Debug.Assert(authTarget == Interop.WinHttp.WINHTTP_AUTH_TARGET_PROXY || 
                         authTarget == Interop.WinHttp.WINHTTP_AUTH_TARGET_SERVER);

            NetworkCredential networkCredential = credentials.GetCredential(uri, s_authSchemeStringMapping[authScheme]);

            if (networkCredential == null)
            {
                return false;
            }

            if (networkCredential == CredentialCache.DefaultNetworkCredentials)
            {
                // Only Negotiate and NTLM can use default credentials. Otherwise,
                // behave as-if there were no credentials.
                if (authScheme == Interop.WinHttp.WINHTTP_AUTH_SCHEME_NEGOTIATE ||
                    authScheme == Interop.WinHttp.WINHTTP_AUTH_SCHEME_NTLM)
                {
                    // Allow WinHTTP to transmit the default credentials.
                    ChangeDefaultCredentialsPolicy(requestHandle, authTarget, allowDefaultCredentials:true);
                    userName = null;
                    password = null;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                userName = networkCredential.UserName;
                password = networkCredential.Password;
                string domain = networkCredential.Domain;

                // WinHTTP does not support a blank username.  So, we will throw an exception.
                if (string.IsNullOrEmpty(userName))
                {
                    throw new InvalidOperationException(SR.net_http_username_empty_string);
                }

                if (!string.IsNullOrEmpty(domain))
                {
                    userName = domain + "\\" + userName;
                }
            }

            if (!Interop.WinHttp.WinHttpSetCredentials(
                requestHandle,
                authTarget,
                authScheme,
                userName,
                password,
                IntPtr.Zero))
            {
                WinHttpException.ThrowExceptionUsingLastError(nameof(Interop.WinHttp.WinHttpSetCredentials));
            }

            return true;
        }

        private static uint ChooseAuthScheme(uint supportedSchemes, Uri uri, ICredentials credentials)
        {
            if (credentials == null)
            {
                return 0;
            }

            if (uri == null && !(credentials is NetworkCredential))
            {
                // TODO: Issue #6997.
                // If the credentials are a NetworkCredential, the uri isn't used when calling .GetCredential() since
                // it will work against all uri's. Otherwise, credentials is probably a CredentialCache and passing in
                // null for a uri is invalid.
                return 0;
            }

            foreach (uint authScheme in s_authSchemePriorityOrder)
            {
                if ((supportedSchemes & authScheme) != 0 && credentials.GetCredential(uri, s_authSchemeStringMapping[authScheme]) != null)
                {
                    return authScheme;
                }
            }

            return 0;
        }
    }
}
