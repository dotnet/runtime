// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static partial class OidLookup
    {
        private static bool ShouldUseCache(OidGroup oidGroup)
        {
            return true;
        }

        private static string NativeOidToFriendlyName(string oid, OidGroup oidGroup, bool fallBackToAllGroups)
        {
            string friendlyName;

            if (s_extraOidToFriendlyName.TryGetValue(oid, out friendlyName))
            {
                return friendlyName;
            }

            return null;
        }

        private static string NativeFriendlyNameToOid(string friendlyName, OidGroup oidGroup, bool fallBackToAllGroups)
        {
            string oid;

            if (s_extraFriendlyNameToOid.TryGetValue(friendlyName, out oid))
            {
                return oid;
            }

            return null;
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        // There are places inside the framework where Oid.FromFriendlyName is called
        // (to pass in an OID group restriction for Windows) and an exception is not tolerated.
        //
        // The main place for this is X509Extension's internal ctor.
        //
        // These Name/OID pairs are not "universal", in that either Windows localizes it or Windows
        // and OpenSSL produce different answers.  Since the answers originally came from OpenSSL
        // on macOS, this preserves the OpenSSL names.
        private static readonly Dictionary<string, string> s_extraFriendlyNameToOid =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "pkcs7-data", "1.2.840.113549.1.7.1" },
                { "contentType", "1.2.840.113549.1.9.3" },
                { "messageDigest", "1.2.840.113549.1.9.4" },
                { "signingTime", "1.2.840.113549.1.9.5" },
                { "X509v3 Subject Key Identifier", "2.5.29.14" },
                { "X509v3 Key Usage", "2.5.29.15" },
                { "X509v3 Basic Constraints", "2.5.29.19" },
                { "X509v3 Extended Key Usage", "2.5.29.37" },
            };

        private static readonly Dictionary<string, string> s_extraOidToFriendlyName =
            s_extraFriendlyNameToOid.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    }
}
