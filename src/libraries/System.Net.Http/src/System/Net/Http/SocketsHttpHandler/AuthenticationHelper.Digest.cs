// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal static partial class AuthenticationHelper
    {
        // Define digest constants
        private const string Qop = "qop";
        private const string Auth = "auth";
        private const string AuthInt = "auth-int";
        private const string Domain = "domain";
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

        public static async Task<string?> GetDigestTokenForCredential(NetworkCredential credential, HttpRequestMessage request, DigestResponse digestResponse)
        {
            StringBuilder sb = StringBuilderCache.Acquire();

            // It is mandatory for servers to implement sha-256 per RFC 7616
            // Keep MD5 for backward compatibility.
            string? algorithm;
            bool isAlgorithmSpecified = digestResponse.Parameters.TryGetValue(Algorithm, out algorithm);
            if (isAlgorithmSpecified)
            {
                if (!algorithm!.Equals(Sha256, StringComparison.OrdinalIgnoreCase) &&
                    !algorithm.Equals(Md5, StringComparison.OrdinalIgnoreCase) &&
                    !algorithm.Equals(Sha256Sess, StringComparison.OrdinalIgnoreCase) &&
                    !algorithm.Equals(MD5Sess, StringComparison.OrdinalIgnoreCase))
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(digestResponse, $"Algorithm not supported: {algorithm}");
                    return null;
                }
            }
            else
            {
                algorithm = Md5;
            }

            // Check if nonce is there in challenge
            string? nonce;
            if (!digestResponse.Parameters.TryGetValue(Nonce, out nonce))
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(digestResponse, "Nonce missing");
                return null;
            }

            // opaque token may or may not exist
            string? opaque;
            digestResponse.Parameters.TryGetValue(Opaque, out opaque);

            string? realm;
            if (!digestResponse.Parameters.TryGetValue(Realm, out realm))
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(digestResponse, "Realm missing");
                return null;
            }

            // Add username
            string? userhash;
            if (digestResponse.Parameters.TryGetValue(UserHash, out userhash) && userhash == "true")
            {
                sb.AppendKeyValue(Username, ComputeHash(credential.UserName + ":" + realm, algorithm));
                sb.AppendKeyValue(UserHash, userhash, includeQuotes: false);
            }
            else
            {
                if (!Ascii.IsValid(credential.UserName))
                {
                    string usernameStar = HeaderUtilities.Encode5987(credential.UserName);
                    sb.AppendKeyValue(UsernameStar, usernameStar, includeQuotes: false);
                }
                else
                {
                    sb.AppendKeyValue(Username, credential.UserName);
                }
            }

            // Add realm
            sb.AppendKeyValue(Realm, realm);

            // Add nonce
            sb.AppendKeyValue(Nonce, nonce);

            Debug.Assert(request.RequestUri != null);
            // Add uri
            sb.AppendKeyValue(Uri, request.RequestUri.PathAndQuery);

            // Set qop, default is auth
            string qop = Auth;
            bool isQopSpecified = digestResponse.Parameters.ContainsKey(Qop);
            if (isQopSpecified)
            {
                // Check if auth-int present in qop string
                int index1 = digestResponse.Parameters[Qop].IndexOf(AuthInt, StringComparison.Ordinal);
                if (index1 != -1)
                {
                    // Get index of auth if present in qop string
                    int index2 = digestResponse.Parameters[Qop].IndexOf(Auth, StringComparison.Ordinal);

                    // If index2 < index1, auth option is available
                    // If index2 == index1, check if auth option available later in string after auth-int.
                    if (index2 == index1)
                    {
                        index2 = digestResponse.Parameters[Qop].IndexOf(Auth, index1 + AuthInt.Length, StringComparison.Ordinal);
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
            if (algorithm.EndsWith("sess", StringComparison.OrdinalIgnoreCase))
            {
                a1 = ComputeHash(a1, algorithm) + ":" + nonce + ":" + cnonce;
            }

            string a2 = request.Method.Method + ":" + request.RequestUri.PathAndQuery;
            if (qop == AuthInt)
            {
                string content = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                a2 = a2 + ":" + ComputeHash(content, algorithm);
            }

            string response;
            if (isQopSpecified)
            {
                response = ComputeHash(ComputeHash(a1, algorithm) + ":" +
                                            nonce + ":" +
                                            DigestResponse.NonceCount + ":" +
                                            cnonce + ":" +
                                            qop + ":" +
                                            ComputeHash(a2, algorithm), algorithm);
            }
            else
            {
                response = ComputeHash(ComputeHash(a1, algorithm) + ":" +
                            nonce + ":" +
                            ComputeHash(a2, algorithm), algorithm);
            }

            // Add response
            sb.AppendKeyValue(Response, response, includeComma: opaque != null || isAlgorithmSpecified || isQopSpecified);

            // Add opaque
            if (opaque != null)
            {
                sb.AppendKeyValue(Opaque, opaque, includeComma: isAlgorithmSpecified || isQopSpecified);
            }

            if (isAlgorithmSpecified)
            {
                // Add algorithm
                sb.AppendKeyValue(Algorithm, algorithm, includeQuotes: false, includeComma: isQopSpecified);
            }

            if (isQopSpecified)
            {
                // Add qop
                sb.AppendKeyValue(Qop, qop, includeQuotes: false);

                // Add nc
                sb.AppendKeyValue(NC, DigestResponse.NonceCount, includeQuotes: false);

                // Add cnonce
                sb.AppendKeyValue(CNonce, cnonce, includeComma: false);
            }

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public static bool IsServerNonceStale(DigestResponse digestResponse)
        {
            return digestResponse.Parameters.TryGetValue(Stale, out string? stale) && stale == "true";
        }

        private static string GetRandomAlphaNumericString()
        {
            const int Length = 16;
            const string CharacterSet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return RandomNumberGenerator.GetString(CharacterSet, Length);
        }

        private static string ComputeHash(string data, string algorithm)
        {
            Span<byte> hashBuffer = stackalloc byte[SHA256.HashSizeInBytes]; // SHA256 is the largest hash produced
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            int written;

            if (algorithm.StartsWith(Sha256, StringComparison.OrdinalIgnoreCase))
            {
                written = SHA256.HashData(dataBytes, hashBuffer);
                Debug.Assert(written == SHA256.HashSizeInBytes);
            }
            else
            {
                // Disable MD5 insecure warning.
#pragma warning disable CA5351
                written = MD5.HashData(dataBytes, hashBuffer);
                Debug.Assert(written == MD5.HashSizeInBytes);
#pragma warning restore CA5351
            }

            return Convert.ToHexStringLower(hashBuffer.Slice(0, written));
        }

        internal sealed class DigestResponse
        {
            internal readonly Dictionary<string, string> Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            internal const string NonceCount = "00000001";

            internal DigestResponse(string? challenge)
            {
                if (!string.IsNullOrEmpty(challenge))
                    Parse(challenge);
            }

            private static bool CharIsSpaceOrTab(char ch)
            {
                return ch == ' ' || ch == '\t';
            }

            private static bool MustValueBeQuoted(string key)
            {
                // As per the RFC, these string must be quoted for historical reasons.
                return key.Equals(Realm, StringComparison.OrdinalIgnoreCase) || key.Equals(Nonce, StringComparison.OrdinalIgnoreCase) ||
                    key.Equals(Opaque, StringComparison.OrdinalIgnoreCase) || key.Equals(Qop, StringComparison.OrdinalIgnoreCase);
            }

            private static string? GetNextKey(string data, int currentIndex, out int parsedIndex)
            {
                // Skip leading space or tab.
                while (currentIndex < data.Length && CharIsSpaceOrTab(data[currentIndex]))
                {
                    currentIndex++;
                }

                // Start parsing key
                int start = currentIndex;

                // Parse till '=' is encountered marking end of key.
                // Key cannot contain space or tab, break if either is found.
                while (currentIndex < data.Length && data[currentIndex] != '=' && !CharIsSpaceOrTab(data[currentIndex]))
                {
                    currentIndex++;
                }

                if (currentIndex == data.Length)
                {
                    // Key didn't terminate with '='
                    parsedIndex = currentIndex;
                    return null;
                }

                // Record end of key.
                int length = currentIndex - start;
                if (CharIsSpaceOrTab(data[currentIndex]))
                {
                    // Key parsing terminated due to ' ' or '\t'.
                    // Parse till '=' is found.
                    while (currentIndex < data.Length && CharIsSpaceOrTab(data[currentIndex]))
                    {
                        currentIndex++;
                    }

                    if (currentIndex == data.Length || data[currentIndex] != '=')
                    {
                        // Key is invalid.
                        parsedIndex = currentIndex;
                        return null;
                    }
                }

                // Skip trailing space and tab and '='
                while (currentIndex < data.Length && (CharIsSpaceOrTab(data[currentIndex]) || data[currentIndex] == '='))
                {
                    currentIndex++;
                }

                // Set the parsedIndex to current valid char.
                parsedIndex = currentIndex;
                return data.Substring(start, length);
            }

            private static string? GetNextValue(string data, int currentIndex, bool expectQuotes, out int parsedIndex)
            {
                Debug.Assert(currentIndex < data.Length && !CharIsSpaceOrTab(data[currentIndex]));

                // If quoted value, skip first quote.
                bool quotedValue = false;
                if (data[currentIndex] == '"')
                {
                    quotedValue = true;
                    currentIndex++;
                }

                if (expectQuotes && !quotedValue)
                {
                    parsedIndex = currentIndex;
                    return null;
                }

                StringBuilder sb = StringBuilderCache.Acquire();
                while (currentIndex < data.Length && ((quotedValue && data[currentIndex] != '"') || (!quotedValue && data[currentIndex] != ',')))
                {
                    sb.Append(data[currentIndex]);
                    currentIndex++;

                    if (currentIndex == data.Length)
                        break;

                    if (!quotedValue && CharIsSpaceOrTab(data[currentIndex]))
                        break;

                    if (quotedValue && data[currentIndex] == '"' && data[currentIndex - 1] == '\\')
                    {
                        // Include the escaped quote.
                        sb.Append(data[currentIndex]);
                        currentIndex++;
                    }
                }

                // Skip the quote.
                if (quotedValue)
                    currentIndex++;

                // Skip any whitespace.
                while (currentIndex < data.Length && CharIsSpaceOrTab(data[currentIndex]))
                    currentIndex++;

                // Return if this is last value.
                if (currentIndex == data.Length)
                {
                    parsedIndex = currentIndex;
                    return StringBuilderCache.GetStringAndRelease(sb);
                }

                // A key-value pair should end with ','
                if (data[currentIndex++] != ',')
                {
                    parsedIndex = currentIndex;
                    return null;
                }

                // Skip space and tab
                while (currentIndex < data.Length && CharIsSpaceOrTab(data[currentIndex]))
                {
                    currentIndex++;
                }

                // Set parsedIndex to current valid char.
                parsedIndex = currentIndex;
                return StringBuilderCache.GetStringAndRelease(sb);
            }

            private void Parse(string challenge)
            {
                int parsedIndex = 0;
                while (parsedIndex < challenge.Length)
                {
                    // Get the key.
                    string? key = GetNextKey(challenge, parsedIndex, out parsedIndex);
                    // Ensure key is not empty and parsedIndex is still in range.
                    if (string.IsNullOrEmpty(key) || parsedIndex >= challenge.Length)
                        break;

                    // Get the value.
                    string? value = GetNextValue(challenge, parsedIndex, MustValueBeQuoted(key), out parsedIndex);
                    if (value == null)
                        break;

                    // Ensure value is valid.
                    // Opaque, Domain and Realm can have empty string
                    if (value == string.Empty &&
                        !key.Equals(Opaque, StringComparison.OrdinalIgnoreCase) &&
                        !key.Equals(Domain, StringComparison.OrdinalIgnoreCase) &&
                        !key.Equals(Realm, StringComparison.OrdinalIgnoreCase))
                        break;

                    // Add the key-value pair to Parameters.
                    Parameters.Add(key, value);
                }
            }
        }
    }

    internal static class StringBuilderExtensions
    {
        public static void AppendKeyValue(this StringBuilder sb, string key, string value, bool includeQuotes = true, bool includeComma = true)
        {
            sb.Append(key).Append('=');

            if (includeQuotes)
            {
                ReadOnlySpan<char> valueSpan = value;
                sb.Append('"');
                while (true)
                {
                    int i = valueSpan.IndexOfAny('"', '\\'); // Characters that require escaping in quoted string
                    if (i >= 0)
                    {
                        sb.Append(valueSpan.Slice(0, i)).Append('\\').Append(valueSpan[i]);
                        valueSpan = valueSpan.Slice(i + 1);
                    }
                    else
                    {
                        sb.Append(valueSpan);
                        break;
                    }
                }
                sb.Append('"');
            }
            else
            {
                sb.Append(value);
            }

            if (includeComma)
            {
                sb.Append(',').Append(' ');
            }
        }
    }
}
