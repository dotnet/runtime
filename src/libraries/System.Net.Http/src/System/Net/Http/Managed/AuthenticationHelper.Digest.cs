// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal partial class AuthenticationHelper
    {
        public const string Digest = "Digest";

        // Define digest constants
        private const string Qop = "qop";
        private const string Auth = "auth";
        private const string AuthInt = "auth-int";
        private const string Nonce = "nonce";
        private const string NC = "nc";
        private const string Realm = "realm";
        private const string UserHash = "userhash";
        private const string Username = "username";
        private const string UsernameStar = "username*";
        private const string Algorithm = "algorithm";
        private const string Uri = "uri";
        private const string Sha256 = "SHA-256";
        private const string Md5 = "MD5";
        private const string Sha256Sess = "SHA-256-sess";
        private const string MD5Sess = "MD5-sess";
        private const string CNonce = "cnonce";
        private const string Opaque = "opaque";
        private const string Response = "response";
        private const string Stale = "stale";

        // Define alphanumeric characters for cnonce
        // 48='0', 65='A', 97='a'
        private static int[] s_alphaNumChooser = new int[] { 48, 65, 97 };

        // Define a random number generator for cnonce
        private static RandomNumberGenerator s_rng = RandomNumberGenerator.Create();

        public async static Task<bool> TrySetDigestAuthToken(HttpRequestMessage request, ICredentials credentials, DigestResponse digestResponse, string authHeader)
        {
            NetworkCredential credential = credentials.GetCredential(request.RequestUri, Digest);
            if (credential == null)
            {
                return false;
            }

            string parameter = await GetDigestTokenForCredential(credential, request, digestResponse).ConfigureAwait(false);

            // Any errors in obtaining parameter return false
            if (string.IsNullOrEmpty(parameter))
                return false;

            if (authHeader == HttpKnownHeaderNames.Authorization)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(Digest, parameter);
            }
            else if (authHeader == HttpKnownHeaderNames.ProxyAuthorization)
            {
                request.Headers.ProxyAuthorization = new AuthenticationHeaderValue(Digest, parameter);
            }

            return true;
        }

        public static async Task<string> GetDigestTokenForCredential(NetworkCredential credential, HttpRequestMessage request, DigestResponse digestResponse)
        {
            StringBuilder sb = StringBuilderCache.Acquire();

            // It is mandatory for servers to implement sha-256 per RFC 7616
            // Keep MD5 for backward compatibility.
            string algorithm;
            if (digestResponse.Parameters.TryGetValue(Algorithm, out algorithm))
            {
                if (algorithm != Sha256 && algorithm != Md5 && algorithm != Sha256Sess && algorithm != MD5Sess)
                    return null;
            }
            else
            {
                algorithm = Md5;
            }

            // Check if nonce is there in challenge
            string nonce;
            if (!digestResponse.Parameters.TryGetValue(Nonce, out nonce))
            {
                return null;
            }

            string opaque;
            if (!digestResponse.Parameters.TryGetValue(Opaque, out opaque))
            {
                return null;
            }

            string realm = digestResponse.Parameters.ContainsKey(Realm) ? digestResponse.Parameters[Realm] : string.Empty;

            // Add username
            string userhash;
            if (digestResponse.Parameters.TryGetValue(UserHash, out userhash) && userhash == "true")
            {
                sb.AppendKeyValue(Username, ComputeHash(credential.UserName + ":" + realm, algorithm));
                sb.AppendKeyValue(UserHash, userhash, includeQuotes: false);
            }
            else
            {
                string usernameStar;
                if (HeaderUtilities.IsInputEncoded5987(credential.UserName, out usernameStar))
                {
                    sb.AppendKeyValue(UsernameStar, usernameStar, includeQuotes: false);
                }
                else
                {
                    sb.AppendKeyValue(Username, credential.UserName);
                }
            }

            // Add realm
            if (realm != string.Empty)
                sb.AppendKeyValue(Realm, realm);

            // If nonce is same as previous request, update nonce count.
            if (nonce == digestResponse.Nonce)
            {
                digestResponse.NonceCount++;
            }

            // Add nonce
            sb.AppendKeyValue(Nonce, nonce);
            digestResponse.Nonce = nonce;

            // Add uri
            sb.AppendKeyValue(Uri, request.RequestUri.PathAndQuery);

            // Set qop, default is auth
            string qop = Auth;
            if (digestResponse.Parameters.ContainsKey(Qop))
            {
                // Check if auth-int present in qop string
                int index1 = digestResponse.Parameters[Qop].IndexOf(AuthInt);
                if (index1 != -1)
                {
                    // Get index of auth if present in qop string
                    int index2 = digestResponse.Parameters[Qop].IndexOf(Auth);

                    // If index2 < index1, auth option is available
                    // If index2 == index1, check if auth option available later in string after auth-int.
                    if (index2 == index1)
                    {
                        index2 = digestResponse.Parameters[Qop].IndexOf(Auth, index1 + AuthInt.Length);
                        if (index2 == -1)
                        {
                            qop = AuthInt;
                        }
                    }
                }
            }

            // Set cnonce
            string cnonce = GetRandomAlphaNumericString();

            // Calculate response
            string a1 = credential.UserName + ":" + realm + ":" + credential.Password;
            if (algorithm == Sha256Sess || algorithm == MD5Sess)
            {
                algorithm = algorithm == Sha256Sess ? Sha256 : Md5;
                a1 = ComputeHash(a1, algorithm) + ":" + nonce + ":" + cnonce;
            }

            string a2 = request.Method.Method + ":" + request.RequestUri.PathAndQuery;
            if (qop == AuthInt)
            {
                string content = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                a2 = a2 + ":" + ComputeHash(content, algorithm);
            }

            string response = ComputeHash(ComputeHash(a1, algorithm) + ":" +
                                        nonce + ":" +
                                        digestResponse.NonceCount.ToString("x8") + ":" +
                                        cnonce + ":" +
                                        qop + ":" +
                                        ComputeHash(a2, algorithm), algorithm);

            // Add response
            sb.AppendKeyValue(Response, response);

            // Add algorithm
            sb.AppendKeyValue(Algorithm, algorithm, includeQuotes: false);

            // Add opaque
            sb.AppendKeyValue(Opaque, opaque);

            // Add qop
            sb.AppendKeyValue(Qop, qop, includeQuotes: false);

            // Add nc
            sb.AppendKeyValue(NC, digestResponse.NonceCount.ToString("x8"), includeQuotes: false);

            // Add cnonce
            sb.AppendKeyValue(CNonce, cnonce, includeComma: false);

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public static bool IsServerNonceStale(DigestResponse digestResponse)
        {
            string stale = null;
            return digestResponse.Parameters.TryGetValue(Stale, out stale) && stale == "true";
        }

        private static string GetRandomAlphaNumericString()
        {
            const int Length = 16;
            Span<byte> randomNumbers = stackalloc byte[Length * 2];
            s_rng.GetBytes(randomNumbers);

            StringBuilder sb = StringBuilderCache.Acquire(Length);
            for (int i = 0; i < randomNumbers.Length; )
            {
                // Get a random digit 0-9, a random alphabet in a-z, or a random alphabeta in A-Z
                int rangeIndex = randomNumbers[i++] % 3;
                int value = randomNumbers[i++] % (rangeIndex == 0 ? 10 : 26);
                sb.Append((char)(s_alphaNumChooser[rangeIndex] + value));
            }

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        private static string ComputeHash(string data, string algorithm)
        {
            // Disable MD5 insecure warning.
#pragma warning disable CA5351
            using (HashAlgorithm hash = algorithm == Sha256 ? SHA256.Create() : (HashAlgorithm)MD5.Create())
#pragma warning restore CA5351
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(data));

                StringBuilder sb = StringBuilderCache.Acquire(result.Length * 2);
                foreach (byte b in result)
                    sb.Append(b.ToString("x2"));

                return StringBuilderCache.GetStringAndRelease(sb);
            }
        }

        public class DigestResponse
        {
            public readonly Dictionary<string, string> Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Keep track of request values for this response.
            public string Nonce;
            public uint NonceCount;

            public DigestResponse(string challenge)
            {
                Nonce = null;
                NonceCount = 1;

                Parse(challenge);
            }

            private unsafe string GetNextKey(ref char* p)
            {
                // It is generally cheaper to change a local and then write back to ref at the end
                // rather than updating the ref on each operation.
                char* temp = p;

                StringBuilder sb = StringBuilderCache.Acquire();

                // Skip leading whitespace
                while (*temp == ' ')
                {
                    temp++;
                }

                // Start parsing key
                while (*temp != '=')
                {
                    // Key cannot have whitespace
                    if (*temp == ' ')
                        break;

                    sb.Append(*temp);
                    temp++;
                }

                // Skip trailing whitespace and '='
                while (*temp == ' ' || *temp == '=')
                {
                    temp++;
                }

                // Set the ref p to temp
                p = temp;

                return StringBuilderCache.GetStringAndRelease(sb);
            }

            private unsafe string GetNextValue(ref char* p)
            {
                // It is generally cheaper to change a local and then write back to ref at the end
                // rather than updating the ref on each operation.
                char* temp = p;

                StringBuilder sb = StringBuilderCache.Acquire();

                // Skip leading whitespace
                while (*temp == ' ')
                {
                    temp++;
                }

                // If quoted value, skip first quote.
                bool quotedValue = false;
                if (*temp == '"')
                {
                    quotedValue = true;
                    temp++;
                }

                while ((quotedValue && *temp != '"') || (!quotedValue && *temp != ',' && *temp != '\0'))
                {
                    sb.Append(*temp);
                    temp++;

                    if (!quotedValue && *temp == ' ')
                        break;

                    if (quotedValue && *temp == '"' && *(temp - 1) == '\\')
                    {
                        // Include the escaped quote.
                        sb.Append(*temp);
                        temp++;
                    }
                }

                // Return if this is last value.
                if (*temp == '\0')
                    return sb.ToString();

                // Skip the end quote or ',' or whitespace
                temp++;

                // Skip whitespace and ,
                while (*temp == ' ' || *temp == ',')
                {
                    temp++;
                }

                // Set ref p to temp
                p = temp;

                return StringBuilderCache.GetStringAndRelease(sb);
            }

            private unsafe void Parse(string challenge)
            {
                fixed (char* p = challenge)
                {
                    char* counter = p;
                    while (*counter != '\0')
                    {
                        string key = GetNextKey(ref counter);
                        string value = GetNextValue(ref counter);

                        Parameters.Add(key, value);
                    }
                }
            }
        }
    }

    internal static class StringBuilderExtensions
    {
        public static void AppendKeyValue(this StringBuilder sb, string key, string value, bool includeQuotes = true, bool includeComma = true)
        {
            sb.Append(key);
            sb.Append('=');
            if (includeQuotes)
            {
                sb.Append('"');
            }

            sb.Append(value);
            if (includeQuotes)
            {
                sb.Append('"');
            }

            if (includeComma)
            {
                sb.Append(',');
                sb.Append(' ');
            }
        }
    }
}
