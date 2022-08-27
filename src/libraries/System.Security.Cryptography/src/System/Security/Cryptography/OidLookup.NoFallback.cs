// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal static partial class OidLookup
    {
        private static bool ShouldUseCache(OidGroup oidGroup)
        {
            return true;
        }

        private static string? NativeOidToFriendlyName(string oid, OidGroup oidGroup, bool fallBackToAllGroups)
        {
            if (s_extraOidToFriendlyName.TryGetValue(oid, out string? friendlyName))
            {
                return friendlyName;
            }

            return null;
        }

        private static string? NativeFriendlyNameToOid(string friendlyName, OidGroup oidGroup, bool fallBackToAllGroups)
        {
            if (s_extraFriendlyNameToOid.TryGetValue(friendlyName, out string? oid))
            {
                return oid;
            }

            return null;
        }

        /// <summary>Expected size of <see cref="s_extraFriendlyNameToOid"/>.</summary>
        private const int ExtraFriendlyNameToOidCount = 13;

        // There are places inside the framework where Oid.FromFriendlyName is called
        // (to pass in an OID group restriction for Windows) and an exception is not tolerated.
        //
        // The main place for this is X509Extension's internal ctor.
        //
        // These Name/OID pairs are not "universal", in that either Windows localizes it or Windows
        // and OpenSSL produce different answers.  Since the answers originally came from OpenSSL
        // on macOS and Android, this preserves the OpenSSL names.
        private static readonly Dictionary<string, string> s_extraFriendlyNameToOid =
            new Dictionary<string, string>(ExtraFriendlyNameToOidCount, StringComparer.OrdinalIgnoreCase)
            {
                { "pkcs7-data", "1.2.840.113549.1.7.1" },
                { "contentType", "1.2.840.113549.1.9.3" },
                { "messageDigest", "1.2.840.113549.1.9.4" },
                { "signingTime", "1.2.840.113549.1.9.5" },
                { "X509v3 Subject Key Identifier", "2.5.29.14" },
                { "X509v3 Key Usage", "2.5.29.15" },
                { "X509v3 Subject Alternative Name", "2.5.29.17" },
                { "X509v3 Basic Constraints", "2.5.29.19" },
                { "X509v3 Authority Key Identifier", "2.5.29.35" },
                { "X509v3 Extended Key Usage", "2.5.29.37" },
                { "prime256v1", "1.2.840.10045.3.1.7" },
                { "secp224r1", "1.3.132.0.33" },
                { "Authority Information Access", Oids.AuthorityInformationAccess },
            };

        private static readonly Dictionary<string, string> s_extraOidToFriendlyName =
            InvertWithDefaultComparer(s_extraFriendlyNameToOid);

        private static Dictionary<string, string> InvertWithDefaultComparer(Dictionary<string, string> source)
        {
            var result = new Dictionary<string, string>(source.Count);
            foreach (KeyValuePair<string, string> item in source)
            {
                result.Add(item.Value, item.Key);
            }
            return result;
        }

#if DEBUG
        static partial void ExtraStaticDebugValidation()
        {
            // Validate we hardcoded the right dictionary size
            Debug.Assert(s_extraFriendlyNameToOid.Count == ExtraFriendlyNameToOidCount,
                $"Expected {nameof(s_extraFriendlyNameToOid)}.{nameof(s_extraFriendlyNameToOid.Count)} == {ExtraFriendlyNameToOidCount}, got {s_extraFriendlyNameToOid.Count}");
            Debug.Assert(s_extraOidToFriendlyName.Count == ExtraFriendlyNameToOidCount,
                $"Expected {nameof(s_extraOidToFriendlyName)}.{nameof(s_extraOidToFriendlyName.Count)} == {ExtraFriendlyNameToOidCount}, got {s_extraOidToFriendlyName.Count}");
        }
#endif
    }
}
