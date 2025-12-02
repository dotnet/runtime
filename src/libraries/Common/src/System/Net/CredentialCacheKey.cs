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

        internal bool Match(Uri uri, int prefixLen, string authenticationType)
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

            return IsPrefix(uri, prefixLen);
        }

        // IsPrefix (Uri)
        //
        // Determines whether <this> is a prefix of this URI. A prefix
        // match is defined as:
        //
        //     scheme match
        //     + host match
        //     + port match, if any
        //     + <prefix> path is a prefix of <URI> path, if any
        //
        // Returns:
        // True if <prefixUri> is a prefix of this URI
        private bool IsPrefix(Uri uri, int prefixLen)
        {
            Debug.Assert(uri != null);
            Uri uriPrefix = UriPrefix;

            if (uriPrefix.Scheme != uri.Scheme || uriPrefix.Host != uri.Host || uriPrefix.Port != uri.Port)
            {
                return false;
            }

            if (UriPrefixLength > prefixLen)
            {
                return false;
            }

            return string.Compare(uri.AbsolutePath, 0, uriPrefix.AbsolutePath, 0, UriPrefixLength, StringComparison.OrdinalIgnoreCase) == 0;
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

            if (cache.Count == 0)
            {
                return false;
            }

            // precompute the length of the prefix
            int uriPrefixLength = uriPrefix.AbsolutePath.LastIndexOf('/');

            // Enumerate through every credential in the cache, get match with longest prefix
            foreach ((CredentialCacheKey key, NetworkCredential value) in cache)
            {
                int prefixLen = key.UriPrefixLength;

                if (prefixLen <= longestMatchPrefix)
                {
                    // this credential can't provide a longer prefix match
                    continue;
                }

                // Determine if this credential is applicable to the current Uri/AuthType
                if (key.Match(uriPrefix, uriPrefixLength, authType))
                {
                    // update the information about currently preferred match
                    longestMatchPrefix = prefixLen;
                    mostSpecificMatch = value;
                    mostSpecificMatchUri = key.UriPrefix;

                    if (uriPrefixLength == prefixLen)
                    {
                        // we can't get any better than this
                        break;
                    }
                }
            }

            return mostSpecificMatch != null;
        }
    }
}
