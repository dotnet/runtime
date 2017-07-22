// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

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
        private const string Algorithm = "algorithm";
        private const string Uri = "uri";
        private const string Sha256 = "SHA-256";
        private const string Md5 = "MD5";
        private const string Sha256Sess = "SHA-256-sess";
        private const string CNonce = "cnonce";
        private const string Opaque = "opaque";
        private const string Response = "response";
        private const string Colon = ":";

        // Define alphanumeric characters for cnonce
        // 48='0', 65='A', 97='a'
        private static int[] AlphaNumChooser = new int[] { 48, 65, 97 };

        public static bool TrySetDigestAuthToken(HttpRequestMessage request, ICredentials credentials, DigestResponse digestResponse)
        {
            NetworkCredential credential = credentials.GetCredential(request.RequestUri, Digest);
            if (credential == null)
            {
                return false;
            }

            try
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(Digest, GetDigestTokenForCredential(credential, request, digestResponse));
                return true;
            }
            catch
            {
                // Return false in case of any digest header calculation errors.
                return false;
            }
        }

        public static string GetDigestTokenForCredential(NetworkCredential credential, HttpRequestMessage request, DigestResponse digestResponse)
        {
            StringBuilder sb = StringBuilderCache.Acquire();

            // It is mandatory for servers to implement sha-256
            // Keep MD5 for backward compatibility.
            string algorithm = Md5;
            if (digestResponse.Parameters[Algorithm].Contains(Sha256))
            {
                algorithm = Sha256;
            }

            string realm = digestResponse.Parameters.ContainsKey(Realm) ? digestResponse.Parameters[Realm] : string.Empty;

            // Add username
            if (digestResponse.Parameters.ContainsKey(UserHash) && digestResponse.Parameters[UserHash] == "true")
            {
                sb.AppendKeyValue(Username, ComputeHash(digestResponse.Parameters[Username] + Colon + realm, algorithm));
            }
            else
            {
                sb.AppendKeyValue(Username, credential.UserName);
            }

            // Add realm
            if (realm != string.Empty)
                sb.AppendKeyValue(Realm, realm);

            // If nonce is same as previous request, update nonce count.
            if (digestResponse.Parameters[Nonce] == digestResponse.Nonce)
            {
                digestResponse.NonceCount++;
            }

            // Add nonce
            sb.AppendKeyValue(Nonce, digestResponse.Parameters[Nonce]);
            digestResponse.Nonce = digestResponse.Parameters[Nonce];

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
            string a1 = credential.UserName + Colon + realm + Colon + credential.Password;
            if (digestResponse.Parameters[Algorithm].Contains(Sha256Sess))
            {
                a1 = ComputeHash(a1, algorithm) + Colon + digestResponse.Parameters[Nonce] + Colon + cnonce;
            }

            string a2 = request.Method.Method + Colon + request.RequestUri.PathAndQuery;
            if (qop == AuthInt)
            {
                a2 = a2 + Colon + ComputeHash(request.Content.ReadAsStringAsync().Result, algorithm);
            }

            string response = ComputeHash(ComputeHash(a1, algorithm) + Colon +
                                        digestResponse.Parameters[Nonce] + Colon +
                                        digestResponse.NonceCount.ToString("x8") + Colon +
                                        cnonce + Colon +
                                        qop + Colon +
                                        ComputeHash(a2, algorithm), algorithm);

            // Add response
            sb.AppendKeyValue(Response, response);

            // Add algorithm
            sb.AppendKeyValue(Algorithm, algorithm, includeQuotes: false);

            // Add opaque
            sb.AppendKeyValue(Opaque, digestResponse.Parameters[Opaque]);

            // Add qop
            sb.AppendKeyValue(Qop, qop, includeQuotes: false);

            // Add nc
            sb.AppendKeyValue(NC, digestResponse.NonceCount.ToString("x8"), includeQuotes: false);

            // Add cnonce
            sb.AppendKeyValue(CNonce, cnonce, includeComma: false);

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        private static string GetRandomAlphaNumericString()
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                const int length = 16;
                StringBuilder sb = StringBuilderCache.Acquire(length);
                for (int i = 0; i < length; i++)
                {
                    byte[] randomNumber = new byte[1];
                    rng.GetBytes(randomNumber);

                    int rangeIndex = (randomNumber[0] % 3);
                    rng.GetBytes(randomNumber);

                    if (rangeIndex == 0)
                    {
                        // Get a random digit 0-9
                        sb.Append((char)(AlphaNumChooser[rangeIndex] + randomNumber[0] % 10));
                    }
                    else
                    {
                        // Get a random alphabet in a-z, A-Z
                        sb.Append((char)(AlphaNumChooser[rangeIndex] + randomNumber[0] % 26));
                    }
                }

                return StringBuilderCache.GetStringAndRelease(sb);
            }
        }

        private static string ComputeHash(string data, string algorithm)
        {
            if (algorithm == Sha256)
            {
                using (HashAlgorithm hash = SHA256.Create())
                {
                    return ComputeHash(data, hash);
                }
            }
            else
            {
// Disable MD5 insecure warning.
#pragma warning disable CA5351
                using (HashAlgorithm hash = MD5.Create())
                {
                    return ComputeHash(data, hash);
                }
#pragma warning restore CA5351
            }
        }

        private static string ComputeHash(string data, HashAlgorithm hash)
        {
            Encoding enc = Encoding.UTF8;
            byte[] result = hash.ComputeHash(enc.GetBytes(data));

            StringBuilder sb = StringBuilderCache.Acquire(result.Length * 2);
            foreach (byte b in result)
                sb.Append(b.ToString("x2"));

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public class DigestResponse
        {
            public string Value => _value;
            public Dictionary<string, string> Parameters => _parameters;

            // Keep track of request values for this response.
            public string Nonce;
            public uint NonceCount;

            private string _value;
            private Dictionary<string, string> _parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public DigestResponse(string value)
            {
                Nonce = null;
                NonceCount = 1;

                _value = value;
                Parse();
            }

            private unsafe string GetNextKey(ref char* p)
            {
                StringBuilder sb = StringBuilderCache.Acquire();

                // Skip leading whitespace
                while (*p == ' ')
                {
                    p++;
                }

                // Start parsing key
                while (*p != '=')
                {
                    // Key cannot have whitespace
                    if (*p == ' ')
                        break;

                    sb.Append(*p);
                    p++;
                }

                // Skip trailing whitespace and '='
                while (*p == ' ' || *p == '=')
                {
                    p++;
                }

                return StringBuilderCache.GetStringAndRelease(sb);
            }

            private unsafe string GetNextValue(ref char* p)
            {
                StringBuilder sb = StringBuilderCache.Acquire();

                // Skip leading whitespace
                while (*p == ' ')
                {
                    p++;
                }

                // If quoted value, skip first quote.
                bool quotedValue = false;
                if (*p == '"')
                {
                    quotedValue = true;
                    p++;
                }

                while ((quotedValue && *p != '"') || (!quotedValue && *p != ',' && *p != '\0'))
                {
                    sb.Append(*p);
                    p++;

                    if (!quotedValue && *p == ' ')
                        break;

                    if (quotedValue && *p == '"' && *(p - 1) == '\\')
                    {
                        // Include the escaped quote.
                        sb.Append(*p);
                        p++;
                    }
                }

                // Return if this is last value.
                if (*p == '\0')
                    return sb.ToString();

                // Skip the end quote or ',' or whitespace
                p++;

                // Skip whitespace and ,
                while (*p == ' ' || *p == ',')
                {
                    p++;
                }

                return StringBuilderCache.GetStringAndRelease(sb);
            }

            private unsafe void Parse()
            {
                fixed (char* p = _value)
                {
                    char* counter = p;
                    while (*counter != '\0')
                    {
                        string key = GetNextKey(ref counter);
                        string value = GetNextValue(ref counter);

                        _parameters.Add(key, value);
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
