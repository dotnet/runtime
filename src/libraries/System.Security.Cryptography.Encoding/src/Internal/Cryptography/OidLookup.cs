// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static partial class OidLookup
    {
        private static readonly ConcurrentDictionary<string, string> s_lateBoundOidToFriendlyName =
            new ConcurrentDictionary<string, string>();

        private static readonly ConcurrentDictionary<string, string?> s_lateBoundFriendlyNameToOid =
            new ConcurrentDictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        //
        // Attempts to map a friendly name to an OID. Returns null if not a known name.
        //
        public static string? ToFriendlyName(string oid, OidGroup oidGroup, bool fallBackToAllGroups)
        {
            if (oid == null)
                throw new ArgumentNullException(nameof(oid));

            string? mappedName;
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
        public static string? ToOid(string friendlyName, OidGroup oidGroup, bool fallBackToAllGroups)
        {
            if (friendlyName == null)
                throw new ArgumentNullException(nameof(friendlyName));
            if (friendlyName.Length == 0)
                return null;

            string? mappedOid;
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

            if (shouldUseCache)
            {
                s_lateBoundFriendlyNameToOid.TryAdd(friendlyName, mappedOid);

                // Don't add the reverse here.  Friendly Name => OID is a case insensitive search,
                // so the casing provided as input here may not be the 'correct' one.  Just let
                // ToFriendlyName capture the response and cache it itself.

                // Also, mappedOid could be null here if the lookup failed.  Allowing storing null
                // means we're able to cache that a lookup failed so we don't repeat it.  It's
                // theoretically possible, however, the failure could have been intermittent, e.g.
                // if the call was forced to follow an AD fallback path and the relevant servers
                // were offline.
            }

            return mappedOid;
        }

        /// <summary>Expected size of <see cref="s_friendlyNameToOid"/>.</summary>
        private const int FriendlyNameToOidCount = 111;

        /// <summary>Expected size of <see cref="s_oidToFriendlyName"/>.</summary>
        private const int OidToFriendlyNameCount = 103;

        private static readonly Dictionary<string, string> s_friendlyNameToOid =
            new Dictionary<string, string>(FriendlyNameToOidCount, StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> s_oidToFriendlyName =
            new Dictionary<string, string>(OidToFriendlyNameCount, StringComparer.Ordinal);

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

        static OidLookup()
        {
            InitializeLookupDictionaries();
#if DEBUG
            // Validate we hardcoded the right dictionary size
            Debug.Assert(s_friendlyNameToOid.Count == FriendlyNameToOidCount,
                $"Expected {nameof(s_friendlyNameToOid)}.{nameof(s_friendlyNameToOid.Count)} == {FriendlyNameToOidCount}, got {s_friendlyNameToOid.Count}");
            Debug.Assert(s_oidToFriendlyName.Count == OidToFriendlyNameCount,
                $"Expected {nameof(s_oidToFriendlyName)}.{nameof(s_oidToFriendlyName.Count)} == {OidToFriendlyNameCount}, got {s_oidToFriendlyName.Count}");

            ExtraStaticDebugValidation();
#endif
        }

        private static void InitializeLookupDictionaries()
        {
            static void AddEntry(string oid, string primaryFriendlyName, string[]? additionalFriendlyNames = null)
            {
                s_oidToFriendlyName.Add(oid, primaryFriendlyName);
                s_friendlyNameToOid.Add(primaryFriendlyName, oid);

                if (additionalFriendlyNames != null)
                {
                    foreach (var additionalName in additionalFriendlyNames)
                    {
                        s_friendlyNameToOid.Add(additionalName, oid);
                    }
                }
            }

            // This lookup was originally built by extracting every szOID #define out of wincrypt.h,
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
            // is to prevent issues wherein an identifier is different between Windows and Unix;
            // since any existing code would be using the Windows identifier, it is the de facto standard.
            AddEntry("1.2.840.113549.3.7", "3des");
            AddEntry("2.16.840.1.101.3.4.1.2", "aes128");
            AddEntry("2.16.840.1.101.3.4.1.5", "aes128wrap");
            AddEntry("2.16.840.1.101.3.4.1.22", "aes192");
            AddEntry("2.16.840.1.101.3.4.1.25", "aes192wrap");
            AddEntry("2.16.840.1.101.3.4.1.42", "aes256");
            AddEntry("2.16.840.1.101.3.4.1.45", "aes256wrap");
            AddEntry("1.3.36.3.3.2.8.1.1.1", "brainpoolP160r1");
            AddEntry("1.3.36.3.3.2.8.1.1.2", "brainpoolP160t1");
            AddEntry("1.3.36.3.3.2.8.1.1.3", "brainpoolP192r1");
            AddEntry("1.3.36.3.3.2.8.1.1.4", "brainpoolP192t1");
            AddEntry("1.3.36.3.3.2.8.1.1.5", "brainpoolP224r1");
            AddEntry("1.3.36.3.3.2.8.1.1.6", "brainpoolP224t1");
            AddEntry("1.3.36.3.3.2.8.1.1.7", "brainpoolP256r1");
            AddEntry("1.3.36.3.3.2.8.1.1.8", "brainpoolP256t1");
            AddEntry("1.3.36.3.3.2.8.1.1.9", "brainpoolP320r1");
            AddEntry("1.3.36.3.3.2.8.1.1.10", "brainpoolP320t1");
            AddEntry("1.3.36.3.3.2.8.1.1.11", "brainpoolP384r1");
            AddEntry("1.3.36.3.3.2.8.1.1.12", "brainpoolP384t1");
            AddEntry("1.3.36.3.3.2.8.1.1.13", "brainpoolP512r1");
            AddEntry("1.3.36.3.3.2.8.1.1.14", "brainpoolP512t1");
            AddEntry("2.5.4.6", "C");
            AddEntry("1.2.840.113549.1.9.16.3.6", "CMS3DESwrap");
            AddEntry("1.2.840.113549.1.9.16.3.7", "CMSRC2wrap");
            AddEntry("2.5.4.3", "CN");
            AddEntry("1.3.6.1.5.5.7.2.1", "CPS");
            AddEntry("0.9.2342.19200300.100.1.25", "DC");
            AddEntry("1.3.14.3.2.7", "des");
            AddEntry("2.5.4.13", "Description");
            AddEntry("1.2.840.10046.2.1", "DH");
            AddEntry("2.5.4.46", "dnQualifier");
            AddEntry("1.2.840.10040.4.1", "DSA");
            AddEntry("1.3.14.3.2.27", "dsaSHA1");
            AddEntry("1.2.840.113549.1.9.1", "E");
            AddEntry("1.2.156.11235.1.1.2.1", "ec192wapi");
            AddEntry("1.2.840.10045.2.1", "ECC");
            AddEntry("1.3.133.16.840.63.0.2", "ECDH_STD_SHA1_KDF");
            AddEntry("1.3.132.1.11.1", "ECDH_STD_SHA256_KDF");
            AddEntry("1.3.132.1.11.2", "ECDH_STD_SHA384_KDF");
            AddEntry("1.2.840.10045.3.1.7", "ECDSA_P256", new[] { "nistP256", "secP256r1", "x962P256v1" } );
            AddEntry("1.3.132.0.34", "ECDSA_P384", new[] { "nistP384", "secP384r1" });
            AddEntry("1.3.132.0.35", "ECDSA_P521", new[] { "nistP521", "secP521r1" });
            AddEntry("1.2.840.113549.1.9.16.3.5", "ESDH");
            AddEntry("2.5.4.42", "G");
            AddEntry("2.5.4.43", "I");
            AddEntry("2.5.4.7", "L");
            AddEntry("1.2.840.113549.2.2", "md2");
            AddEntry("1.2.840.113549.1.1.2", "md2RSA");
            AddEntry("1.2.840.113549.2.4", "md4");
            AddEntry("1.2.840.113549.1.1.3", "md4RSA");
            AddEntry("1.2.840.113549.2.5", "md5");
            AddEntry("1.2.840.113549.1.1.4", "md5RSA");
            AddEntry("1.2.840.113549.1.1.8", "mgf1");
            AddEntry("2.16.840.1.101.2.1.1.20", "mosaicKMandUpdSig");
            AddEntry("2.16.840.1.101.2.1.1.19", "mosaicUpdatedSig");
            AddEntry("1.2.840.10045.3.1.1", "nistP192");
            AddEntry("1.3.132.0.33", "nistP224");
            AddEntry("1.3.6.1.5.5.7.6.2", "NO_SIGN");
            AddEntry("2.5.4.10", "O");
            AddEntry("2.5.4.11", "OU");
            AddEntry("2.5.4.20", "Phone");
            AddEntry("2.5.4.18", "POBox");
            AddEntry("2.5.4.17", "PostalCode");
            AddEntry("1.2.840.113549.3.2", "rc2");
            AddEntry("1.2.840.113549.3.4", "rc4");
            AddEntry("1.2.840.113549.1.1.1", "RSA");
            AddEntry("1.2.840.113549.1.1.7", "RSAES_OAEP");
            AddEntry("1.2.840.113549.1.1.10", "RSASSA-PSS");
            AddEntry("2.5.4.8", "S", new[] { "ST" });
            AddEntry("1.3.132.0.9", "secP160k1");
            AddEntry("1.3.132.0.8", "secP160r1");
            AddEntry("1.3.132.0.30", "secP160r2");
            AddEntry("1.3.132.0.31", "secP192k1");
            AddEntry("1.3.132.0.32", "secP224k1");
            AddEntry("1.3.132.0.10", "secP256k1");
            AddEntry("2.5.4.5", "SERIALNUMBER");
            AddEntry("1.3.14.3.2.26", "sha1");
            AddEntry("1.2.840.10040.4.3", "sha1DSA");
            AddEntry("1.2.840.10045.4.1", "sha1ECDSA");
            AddEntry("1.2.840.113549.1.1.5", "sha1RSA");
            AddEntry("2.16.840.1.101.3.4.2.1", "sha256");
            AddEntry("1.2.840.10045.4.3.2", "sha256ECDSA");
            AddEntry("1.2.840.113549.1.1.11", "sha256RSA");
            AddEntry("2.16.840.1.101.3.4.2.2", "sha384");
            AddEntry("1.2.840.10045.4.3.3", "sha384ECDSA");
            AddEntry("1.2.840.113549.1.1.12", "sha384RSA");
            AddEntry("2.16.840.1.101.3.4.2.3", "sha512");
            AddEntry("1.2.840.10045.4.3.4", "sha512ECDSA");
            AddEntry("1.2.840.113549.1.1.13", "sha512RSA");
            AddEntry("2.5.4.4", "SN");
            AddEntry("1.2.840.10045.4.3", "specifiedECDSA");
            AddEntry("2.5.4.9", "STREET");
            AddEntry("2.5.4.12", "T");
            AddEntry("2.23.133.2.1", "TPMManufacturer");
            AddEntry("2.23.133.2.2", "TPMModel");
            AddEntry("2.23.133.2.3", "TPMVersion");
            AddEntry("2.23.43.1.4.9", "wtls9");
            AddEntry("2.5.4.24", "X21Address");
            AddEntry("1.2.840.10045.3.1.2", "x962P192v2");
            AddEntry("1.2.840.10045.3.1.3", "x962P192v3");
            AddEntry("1.2.840.10045.3.1.4", "x962P239v1");
            AddEntry("1.2.840.10045.3.1.5", "x962P239v2");
            AddEntry("1.2.840.10045.3.1.6", "x962P239v3");
        }

        static partial void ExtraStaticDebugValidation();
    }
}
