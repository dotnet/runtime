// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static partial class OidLookup
    {
        private struct FriendlyNameAlias
        {
            internal string Windows { get; set; }
            internal string OpenSsl { get; set; }
        }

        // The Windows cryptography API "Friendly Names" don't use the same name as the OIDs were given
        // in their declaring RFCs.  If a .NET developer learned to call the algorithm "sha1RSA" then we
        // want to understand that it should be called "sha1WithRSAEncryption" when calling into OpenSSL.
        //
        // The canonical form of the Windows versions come from https://msdn.microsoft.com/en-us/library/ff635603.aspx
        private static readonly FriendlyNameAlias[] s_friendlyNameAliases =
        {
            new FriendlyNameAlias { Windows = "RSA", OpenSsl = "rsaEncryption" }, // RFC 2313
            new FriendlyNameAlias { Windows = "md2RSA", OpenSsl = "md2WithRSAEncryption" }, // RFC 2313
            new FriendlyNameAlias { Windows = "md4RSA", OpenSsl = "md4WithRSAEncryption" }, // RFC 2313
            new FriendlyNameAlias { Windows = "md5RSA", OpenSsl = "md5WithRSAEncryption" }, // RFC 2313
            new FriendlyNameAlias { Windows = "sha1RSA", OpenSsl = "sha1WithRSAEncryption" }, // RFC 3447
            new FriendlyNameAlias { Windows = "sha256RSA", OpenSsl = "sha256WithRSAEncryption" }, // RFC 3447
            new FriendlyNameAlias { Windows = "sha384RSA", OpenSsl = "sha384WithRSAEncryption" }, // RFC 3447
            new FriendlyNameAlias { Windows = "sha512RSA", OpenSsl = "sha512WithRSAEncryption" }, // RFC 3447

            new FriendlyNameAlias { Windows = "DSA", OpenSsl = "dsaEncryption" }, // RFC 3370 calls this "id-dsa"
            new FriendlyNameAlias { Windows = "sha1DSA", OpenSsl = "dsaWithSHA1" }, // RFC 3370 calls this "id-dsa-with-sha1"

            // We can keep going for as many things as we want to support.
        };

        //
        // Attempts to map a friendly name to an OID. Returns null if not a known name.
        //
        public static string ToFriendlyName(string oid, OidGroup oidGroup, bool fallBackToAllGroups)
        {
            if (oid == null)
                throw new ArgumentNullException("oid");

            return NativeOidToFriendlyName(oid, oidGroup, fallBackToAllGroups);
        }

        //
        // Attempts to retrieve the friendly name for an OID. Returns null if not a known or valid OID.
        //
        public static string ToOid(string friendlyName, OidGroup oidGroup, bool fallBackToAllGroups)
        {
            if (friendlyName == null)
                throw new ArgumentNullException("friendlyName");

            string oid = NativeFriendlyNameToOid(friendlyName, oidGroup, fallBackToAllGroups);

            if (oid == null)
            {
                string alias = FindFriendlyNameAlias(friendlyName);

                if (alias != null)
                {
                    oid = NativeFriendlyNameToOid(alias, oidGroup, fallBackToAllGroups);
                }
            }

            return oid;
        }
    }
}
