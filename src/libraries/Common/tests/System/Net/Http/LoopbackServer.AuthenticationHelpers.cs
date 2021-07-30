// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    public sealed partial class LoopbackServer
    {
        internal enum AuthenticationProtocols
        {
            Basic,
            Digest,
            None
        }

        public async Task<List<string>> AcceptConnectionPerformAuthenticationAndCloseAsync(string authenticateHeaders)
        {
            List<string> lines = null;
            await AcceptConnectionAsync(async connection =>
            {
                string headerName = _options.IsProxy ? "Proxy-Authorization" : "Authorization";
                lines = await connection.ReadRequestHeaderAsync().ConfigureAwait(false);
                if (GetRequestHeaderValue(lines, headerName) == null)
                {
                    await connection.SendResponseAsync( _options.IsProxy ?
                                    HttpStatusCode.ProxyAuthenticationRequired : HttpStatusCode.Unauthorized, authenticateHeaders).ConfigureAwait(false);

                    lines = await connection.ReadRequestHeaderAsync().ConfigureAwait(false);
                }
                Debug.Assert(lines.Count > 0);

                int index = lines[0] != null ? lines[0].IndexOf(' ') : -1;
                string requestMethod = null;
                if (index != -1)
                {
                    requestMethod = lines[0].Substring(0, index);
                }

                // Read the authorization header from client.
                AuthenticationProtocols protocol = AuthenticationProtocols.None;
                string clientResponse = null;
                for (int i = 1; i < lines.Count; i++)
                {
                    if (lines[i].StartsWith(headerName))
                    {
                        clientResponse = lines[i];
                        if (lines[i].Contains(nameof(AuthenticationProtocols.Basic)))
                        {
                            protocol = AuthenticationProtocols.Basic;
                            break;
                        }
                        else if (lines[i].Contains(nameof(AuthenticationProtocols.Digest)))
                        {
                            protocol = AuthenticationProtocols.Digest;
                            break;
                        }
                    }
                }

                bool success = false;
                switch (protocol)
                {
                    case AuthenticationProtocols.Basic:
                        success = IsBasicAuthTokenValid(clientResponse, _options);
                        break;

                    case AuthenticationProtocols.Digest:
                        // Read the request content.
                        success = IsDigestAuthTokenValid(clientResponse, requestMethod, _options);
                        break;
                }

                if (success)
                {
                    await connection.SendResponseAsync(additionalHeaders: "Connection: close\r\n").ConfigureAwait(false);
                }
                else
                {
                    await connection.SendResponseAsync(HttpStatusCode.Unauthorized, "Connection: close\r\n" + authenticateHeaders).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);

            return lines;
        }

        internal static bool IsBasicAuthTokenValid(string clientResponse, LoopbackServer.Options options)
        {
            string clientHash = clientResponse.Substring(clientResponse.IndexOf(nameof(AuthenticationProtocols.Basic), StringComparison.OrdinalIgnoreCase) +
                nameof(AuthenticationProtocols.Basic).Length).Trim();
            string userPass = string.IsNullOrEmpty(options.Domain) ? options.Username + ":" + options.Password : options.Domain + "\\" + options.Username + ":" + options.Password;
            return clientHash == Convert.ToBase64String(Encoding.UTF8.GetBytes(userPass));
        }

        internal static bool IsDigestAuthTokenValid(string clientResponse, string requestMethod, LoopbackServer.Options options)
        {
            string clientHash = clientResponse.Substring(clientResponse.IndexOf(nameof(AuthenticationProtocols.Digest), StringComparison.OrdinalIgnoreCase) +
                nameof(AuthenticationProtocols.Digest).Length).Trim();
            string[] values = clientHash.Split(',');

            string username = null, uri = null, realm = null, nonce = null, response = null, algorithm = null, cnonce = null, opaque = null, qop = null, nc = null;
            bool userhash = false;
            for (int i = 0; i < values.Length; i++)
            {
                string trimmedValue = values[i].Trim();
                if (trimmedValue.StartsWith(nameof(username)))
                {
                    // Username is a quoted string.
                    int startIndex = trimmedValue.IndexOf('"');

                    if (startIndex != -1)
                    {
                        startIndex += 1;
                        username = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex - 1);
                    }

                    // Username is mandatory.
                    if (string.IsNullOrEmpty(username))
                        return false;
                }
                else if (trimmedValue.StartsWith(nameof(userhash)) && trimmedValue.Contains("true"))
                {
                    userhash = true;
                }
                else if (trimmedValue.StartsWith(nameof(uri)))
                {
                    int startIndex = trimmedValue.IndexOf('"');
                    if (startIndex != -1)
                    {
                        startIndex += 1;
                        uri = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex - 1);
                    }

                    // Request uri is mandatory.
                    if (string.IsNullOrEmpty(uri))
                        return false;
                }
                else if (trimmedValue.StartsWith(nameof(realm)))
                {
                    // Realm is a quoted string.
                    int startIndex = trimmedValue.IndexOf('"');
                    if (startIndex != -1)
                    {
                        startIndex += 1;
                        realm = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex - 1);
                    }

                    // Realm is mandatory.
                    if (realm == null)
                        return false;
                }
                else if (trimmedValue.StartsWith(nameof(cnonce)))
                {
                    // CNonce is a quoted string.
                    int startIndex = trimmedValue.IndexOf('"');
                    if (startIndex != -1)
                    {
                        startIndex += 1;
                        cnonce = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex - 1);
                    }
                }
                else if (trimmedValue.StartsWith(nameof(nonce)))
                {
                    // Nonce is a quoted string.
                    int startIndex = trimmedValue.IndexOf('"');
                    if (startIndex != -1)
                    {
                        startIndex += 1;
                        nonce = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex - 1);
                    }

                    // Nonce is mandatory.
                    if (string.IsNullOrEmpty(nonce))
                        return false;
                }
                else if (trimmedValue.StartsWith(nameof(response)))
                {
                    // response is a quoted string.
                    int startIndex = trimmedValue.IndexOf('"');
                    if (startIndex != -1)
                    {
                        startIndex += 1;
                        response = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex - 1);
                    }

                    // Response is mandatory.
                    if (string.IsNullOrEmpty(response))
                        return false;
                }
                else if (trimmedValue.StartsWith(nameof(algorithm)))
                {
                    int startIndex = trimmedValue.IndexOf('=');
                    if (startIndex != -1)
                    {
                        startIndex += 1;
                        algorithm = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex).Trim();
                    }
                }
                else if (trimmedValue.StartsWith(nameof(opaque)))
                {
                    // Opaque is a quoted string.
                    int startIndex = trimmedValue.IndexOf('"');
                    if (startIndex != -1)
                    {
                        startIndex += 1;
                        opaque = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex - 1);
                    }
                }
                else if (trimmedValue.StartsWith(nameof(qop)))
                {
                    int startIndex = trimmedValue.IndexOf('"');
                    if (startIndex != -1)
                    {
                        startIndex += 1;
                        qop = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex - 1);
                    }
                    else if ((startIndex = trimmedValue.IndexOf('=')) != -1)
                    {
                        startIndex += 1;
                        qop = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex).Trim();
                    }
                }
                else if (trimmedValue.StartsWith(nameof(nc)))
                {
                    int startIndex = trimmedValue.IndexOf('=');
                    if (startIndex != -1)
                    {
                        startIndex += 1;
                        nc = trimmedValue.Substring(startIndex, trimmedValue.Length - startIndex).Trim();
                    }
                }
            }

            // Verify username.
            if (userhash && ComputeHash(options.Username + ":" + realm, algorithm) != username)
            {
                return false;
            }

            if (!userhash && options.Username != username)
            {
                return false;
            }

            if (string.IsNullOrEmpty(algorithm))
                algorithm = "MD5";

            // Calculate response and compare with the client response hash.
            string a1 = options.Username + ":" + realm + ":" + options.Password;
            if (algorithm.EndsWith("sess", StringComparison.OrdinalIgnoreCase))
            {
                a1 = ComputeHash(a1, algorithm) + ":" + nonce;

                if (cnonce != null)
                    a1 += ":" + cnonce;
            }

            string a2 = requestMethod + ":" + uri;
            if (!string.IsNullOrEmpty(qop) && qop.Equals("auth-int"))
            {
                // Request content is empty.
                a2 = a2 + ":" + ComputeHash(string.Empty, algorithm);
            }

            string serverResponseHash = ComputeHash(a1, algorithm) + ":" + nonce + ":";

            if (nc != null)
                serverResponseHash += nc + ":";

            if (cnonce != null)
                serverResponseHash += cnonce + ":";

            if (qop != null)
                serverResponseHash += qop + ":";

            serverResponseHash += ComputeHash(a2, algorithm);
            serverResponseHash = ComputeHash(serverResponseHash, algorithm);

            return response == serverResponseHash;
        }

        private static string ComputeHash(string data, string algorithm)
        {
            // Disable MD5 insecure warning.
#pragma warning disable CA5351
            using (HashAlgorithm hash = algorithm.StartsWith("SHA-256", StringComparison.OrdinalIgnoreCase) ? SHA256.Create() : (HashAlgorithm)MD5.Create())
#pragma warning restore CA5351
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(data));

                StringBuilder sb = new StringBuilder(result.Length * 2);
                foreach (byte b in result)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
        }
    }
}
