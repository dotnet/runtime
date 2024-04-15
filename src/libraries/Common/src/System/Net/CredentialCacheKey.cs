// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Net
{
    internal sealed class CredentialCacheKey : IEquatable<CredentialCacheKey?>
    {
        public readonly Uri UriPrefix;
        public readonly int UriPrefixLength = -1;
        public readonly string AuthenticationType;

        internal CredentialCacheKey(Uri uriPrefix, string authenticationType)
        {
            Debug.Assert(uriPrefix != null);
            Debug.Assert(authenticationType != null);

            UriPrefix = uriPrefix;
            UriPrefixLength = UriPrefix.AbsolutePath.LastIndexOf('/');
            AuthenticationType = authenticationType;
        }

        internal bool Match(Uri uri, string authenticationType)
        {
            if (uri == null || authenticationType == null)
            {
                return false;
            }

            // If the protocols don't match, this credential is not applicable for the given Uri.
            if (!string.Equals(authenticationType, AuthenticationType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Match({UriPrefix} & {uri})");

            return IsPrefix(uri, UriPrefix);
        }

        // IsPrefix (Uri)
        //
        // Determines whether <prefixUri> is a prefix of this URI. A prefix
        // match is defined as:
        //
        //     scheme match
        //     + host match
        //     + port match, if any
        //     + <prefix> path is a prefix of <URI> path, if any
        //
        // Returns:
        // True if <prefixUri> is a prefix of this URI
        private static bool IsPrefix(Uri uri, Uri prefixUri)
        {
            Debug.Assert(uri != null);
            Debug.Assert(prefixUri != null);

            if (prefixUri.Scheme != uri.Scheme || prefixUri.Host != uri.Host || prefixUri.Port != uri.Port)
            {
                return false;
            }

            int prefixLen = prefixUri.AbsolutePath.LastIndexOf('/');
            if (prefixLen > uri.AbsolutePath.LastIndexOf('/'))
            {
                return false;
            }

            return string.Compare(uri.AbsolutePath, 0, prefixUri.AbsolutePath, 0, prefixLen, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public override int GetHashCode() =>
            StringComparer.OrdinalIgnoreCase.GetHashCode(AuthenticationType) ^
            UriPrefix.GetHashCode();

        public bool Equals([NotNullWhen(true)] CredentialCacheKey? other)
        {
            if (other == null)
            {
                return false;
            }

            bool equals =
                string.Equals(AuthenticationType, other.AuthenticationType, StringComparison.OrdinalIgnoreCase) &&
                UriPrefix.Equals(other.UriPrefix);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Equals({this},{other}) returns {equals}");

            return equals;
        }

        public override bool Equals([NotNullWhen(true)] object? obj) => Equals(obj as CredentialCacheKey);

        public override string ToString() =>
            string.Create(CultureInfo.InvariantCulture, $"[{UriPrefixLength}]:{UriPrefix}:{AuthenticationType}");
    }

    internal static class CredentialCacheHelper
    {
        public static bool TryGetCredential(Dictionary<CredentialCacheKey, NetworkCredential> cache, Uri uriPrefix, string authType, [NotNullWhen(true)] out Uri? mostSpecificMatchUri, [NotNullWhen(true)] out NetworkCredential? mostSpecificMatch)
        {
            int longestMatchPrefix = -1;
            mostSpecificMatch = null;
            mostSpecificMatchUri = null;

            // Enumerate through every credential in the cache
            foreach ((CredentialCacheKey key, NetworkCredential value) in cache)
            {
                // Determine if this credential is applicable to the current Uri/AuthType
                if (key.Match(uriPrefix, authType))
                {
                    int prefixLen = key.UriPrefixLength;

                    // Check if the match is better than the current-most-specific match
                    if (prefixLen > longestMatchPrefix)
                    {
                        // Yes: update the information about currently preferred match
                        longestMatchPrefix = prefixLen;
                        mostSpecificMatch = value;
                        mostSpecificMatchUri = key.UriPrefix;
                    }
                }
            }

            return mostSpecificMatch != null;
        }
    }
}
