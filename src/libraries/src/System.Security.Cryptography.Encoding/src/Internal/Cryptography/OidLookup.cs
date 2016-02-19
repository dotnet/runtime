// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static partial class OidLookup
    {
        private static readonly ConcurrentDictionary<string, string> s_lateBoundOidToFriendlyName =
            new ConcurrentDictionary<string, string>();

        private static readonly ConcurrentDictionary<string, string> s_lateBoundFriendlyNameToOid =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        //
        // Attempts to map a friendly name to an OID. Returns null if not a known name.
        //
        public static string ToFriendlyName(string oid, OidGroup oidGroup, bool fallBackToAllGroups)
        {
            if (oid == null)
                throw new ArgumentNullException(nameof(oid));

            string mappedName;
            bool shouldUseCache = ShouldUseCache(oidGroup);

            // On Unix shouldUseCache is always true, so no matter what OidGroup is passed in the Windows
            // friendly name will be returned.
            //
            // On Windows shouldUseCache is only true for OidGroup.All, because otherwise the OS may filter
            // out the answer based on the group criteria.
            if (shouldUseCache)
            {
                if (s_oidToFriendlyName.TryGetValue(oid, out mappedName) ||
                    s_compatOids.TryGetValue(oid, out mappedName) ||
                    s_lateBoundOidToFriendlyName.TryGetValue(oid, out mappedName))
                {
                    return mappedName;
                }
            }

            mappedName = NativeOidToFriendlyName(oid, oidGroup, fallBackToAllGroups);

            if (shouldUseCache && mappedName != null)
            {
                s_lateBoundOidToFriendlyName.TryAdd(oid, mappedName);

                // Don't add the reverse here.  Just because oid => name doesn't mean name => oid.
                // And don't bother doing the reverse lookup proactively, just wait until they ask for it.
            }

            return mappedName;
        }

        //
        // Attempts to retrieve the friendly name for an OID. Returns null if not a known or valid OID.
        //
        public static string ToOid(string friendlyName, OidGroup oidGroup, bool fallBackToAllGroups)
        {
            if (friendlyName == null)
                throw new ArgumentNullException(nameof(friendlyName));
            if (friendlyName.Length == 0)
                return null;

            string mappedOid;
            bool shouldUseCache = ShouldUseCache(oidGroup);

            if (shouldUseCache)
            {
                if (s_friendlyNameToOid.TryGetValue(friendlyName, out mappedOid) ||
                    s_lateBoundFriendlyNameToOid.TryGetValue(friendlyName, out mappedOid))
                {
                    return mappedOid;
                }
            }

            mappedOid = NativeFriendlyNameToOid(friendlyName, oidGroup, fallBackToAllGroups);

            if (shouldUseCache && mappedOid != null)
            {
                s_lateBoundFriendlyNameToOid.TryAdd(friendlyName, mappedOid);

                // Don't add the reverse here.  Friendly Name => OID is a case insensitive search,
                // so the casing provided as input here may not be the 'correct' one.  Just let
                // ToFriendlyName capture the response and cache it itself.
            }

            return mappedOid;
        }

        // This table was originally built by extracting every szOID #define out of wincrypt.h,
        // and running them through new Oid(string) on Windows 10.  Then, take the list of everything
        // which produced a FriendlyName value, and run it through two other languages. If all 3 agree
        // on the mapping, consider the value to be non-localized.
        //
        // This original list was produced on English (Win10), cross-checked with Spanish (Win8.1) and
        // Japanese (Win10).
        //
        // Sometimes wincrypt.h has more than one OID which results in the same name.  The OIDs whose value
        // doesn't roundtrip (new Oid(new Oid(value).FriendlyName).Value) are contained in s_compatOids.
        //
        // X-Plat: The names (and casing) in this table come from Windows. Part of the intent of this table
        // is to prevent issues wherein an identifier is different between CoreFX\Windows and CoreFX\Unix;
        // since any existing code would be using the Windows identifier, it is the de facto standard.
        private static readonly Dictionary<string, string> s_friendlyNameToOid =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "3des", "1.2.840.113549.3.7" },
                { "aes128", "2.16.840.1.101.3.4.1.2" },
                { "aes128wrap", "2.16.840.1.101.3.4.1.5" },
                { "aes192", "2.16.840.1.101.3.4.1.22" },
                { "aes192wrap", "2.16.840.1.101.3.4.1.25" },
                { "aes256", "2.16.840.1.101.3.4.1.42" },
                { "aes256wrap", "2.16.840.1.101.3.4.1.45" },
                { "brainpoolP160r1", "1.3.36.3.3.2.8.1.1.1" },
                { "brainpoolP160t1", "1.3.36.3.3.2.8.1.1.2" },
                { "brainpoolP192r1", "1.3.36.3.3.2.8.1.1.3" },
                { "brainpoolP192t1", "1.3.36.3.3.2.8.1.1.4" },
                { "brainpoolP224r1", "1.3.36.3.3.2.8.1.1.5" },
                { "brainpoolP224t1", "1.3.36.3.3.2.8.1.1.6" },
                { "brainpoolP256r1", "1.3.36.3.3.2.8.1.1.7" },
                { "brainpoolP256t1", "1.3.36.3.3.2.8.1.1.8" },
                { "brainpoolP320r1", "1.3.36.3.3.2.8.1.1.9" },
                { "brainpoolP320t1", "1.3.36.3.3.2.8.1.1.10" },
                { "brainpoolP384r1", "1.3.36.3.3.2.8.1.1.11" },
                { "brainpoolP384t1", "1.3.36.3.3.2.8.1.1.12" },
                { "brainpoolP512r1", "1.3.36.3.3.2.8.1.1.13" },
                { "brainpoolP512t1", "1.3.36.3.3.2.8.1.1.14" },
                { "C", "2.5.4.6" },
                { "CMS3DESwrap", "1.2.840.113549.1.9.16.3.6" },
                { "CMSRC2wrap", "1.2.840.113549.1.9.16.3.7" },
                { "CN", "2.5.4.3" },
                { "CPS", "1.3.6.1.5.5.7.2.1" },
                { "DC", "0.9.2342.19200300.100.1.25" },
                { "des", "1.3.14.3.2.7" },
                { "Description", "2.5.4.13" },
                { "DH", "1.2.840.10046.2.1" },
                { "dnQualifier", "2.5.4.46" },
                { "DSA", "1.2.840.10040.4.1" },
                { "dsaSHA1", "1.3.14.3.2.27" },
                { "E", "1.2.840.113549.1.9.1" },
                { "ec192wapi", "1.2.156.11235.1.1.2.1" },
                { "ECC", "1.2.840.10045.2.1" },
                { "ECDH_STD_SHA1_KDF", "1.3.133.16.840.63.0.2" },
                { "ECDH_STD_SHA256_KDF", "1.3.132.1.11.1" },
                { "ECDH_STD_SHA384_KDF", "1.3.132.1.11.2" },
                { "ECDSA_P256", "1.2.840.10045.3.1.7" },
                { "ECDSA_P384", "1.3.132.0.34" },
                { "ECDSA_P521", "1.3.132.0.35" },
                { "ESDH", "1.2.840.113549.1.9.16.3.5" },
                { "G", "2.5.4.42" },
                { "I", "2.5.4.43" },
                { "L", "2.5.4.7" },
                { "md2", "1.2.840.113549.2.2" },
                { "md2RSA", "1.2.840.113549.1.1.2" },
                { "md4", "1.2.840.113549.2.4" },
                { "md4RSA", "1.2.840.113549.1.1.3" },
                { "md5", "1.2.840.113549.2.5" },
                { "md5RSA", "1.2.840.113549.1.1.4" },
                { "mgf1", "1.2.840.113549.1.1.8" },
                { "mosaicKMandUpdSig", "2.16.840.1.101.2.1.1.20" },
                { "mosaicUpdatedSig", "2.16.840.1.101.2.1.1.19" },
                { "nistP192", "1.2.840.10045.3.1.1" },
                { "nistP224", "1.3.132.0.33" },
                { "NO_SIGN", "1.3.6.1.5.5.7.6.2" },
                { "O", "2.5.4.10" },
                { "OU", "2.5.4.11" },
                { "Phone", "2.5.4.20" },
                { "POBox", "2.5.4.18" },
                { "PostalCode", "2.5.4.17" },
                { "rc2", "1.2.840.113549.3.2" },
                { "rc4", "1.2.840.113549.3.4" },
                { "RSA", "1.2.840.113549.1.1.1" },
                { "RSAES_OAEP", "1.2.840.113549.1.1.7" },
                { "RSASSA-PSS", "1.2.840.113549.1.1.10" },
                { "S", "2.5.4.8" },
                { "secP160k1", "1.3.132.0.9" },
                { "secP160r1", "1.3.132.0.8" },
                { "secP160r2", "1.3.132.0.30" },
                { "secP192k1", "1.3.132.0.31" },
                { "secP224k1", "1.3.132.0.32" },
                { "secP256k1", "1.3.132.0.10" },
                { "SERIALNUMBER", "2.5.4.5" },
                { "sha1", "1.3.14.3.2.26" },
                { "sha1DSA", "1.2.840.10040.4.3" },
                { "sha1ECDSA", "1.2.840.10045.4.1" },
                { "sha1RSA", "1.2.840.113549.1.1.5" },
                { "sha256", "2.16.840.1.101.3.4.2.1" },
                { "sha256ECDSA", "1.2.840.10045.4.3.2" },
                { "sha256RSA", "1.2.840.113549.1.1.11" },
                { "sha384", "2.16.840.1.101.3.4.2.2" },
                { "sha384ECDSA", "1.2.840.10045.4.3.3" },
                { "sha384RSA", "1.2.840.113549.1.1.12" },
                { "sha512", "2.16.840.1.101.3.4.2.3" },
                { "sha512ECDSA", "1.2.840.10045.4.3.4" },
                { "sha512RSA", "1.2.840.113549.1.1.13" },
                { "SN", "2.5.4.4" },
                { "specifiedECDSA", "1.2.840.10045.4.3" },
                { "STREET", "2.5.4.9" },
                { "T", "2.5.4.12" },
                { "wtls9", "2.23.43.1.4.9" },
                { "X21Address", "2.5.4.24" },
                { "x962P192v2", "1.2.840.10045.3.1.2" },
                { "x962P192v3", "1.2.840.10045.3.1.3" },
                { "x962P239v1", "1.2.840.10045.3.1.4" },
                { "x962P239v2", "1.2.840.10045.3.1.5" },
                { "x962P239v3", "1.2.840.10045.3.1.6" },
            };

        private static readonly Dictionary<string, string> s_oidToFriendlyName =
            s_friendlyNameToOid.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        private static readonly Dictionary<string, string> s_compatOids =
            new Dictionary<string, string>
            {
                { "1.2.840.113549.1.3.1", "DH" },
                { "1.3.14.3.2.12", "DSA" },
                { "1.3.14.3.2.13", "sha1DSA" },
                { "1.3.14.3.2.15", "shaRSA" },
                { "1.3.14.3.2.18", "sha" },
                { "1.3.14.3.2.2", "md4RSA" },
                { "1.3.14.3.2.22", "RSA_KEYX" },
                { "1.3.14.3.2.29", "sha1RSA" },
                { "1.3.14.3.2.3", "md5RSA" },
                { "1.3.14.3.2.4", "md4RSA" },
                { "1.3.14.7.2.3.1", "md2RSA" },
            };
    }
}
