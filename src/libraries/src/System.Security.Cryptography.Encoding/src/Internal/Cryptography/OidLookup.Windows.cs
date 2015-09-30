// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

using Internal.NativeCrypto;

namespace Internal.Cryptography
{
    internal static partial class OidLookup
    {
        private static bool ShouldUseCache(OidGroup oidGroup)
        {
            return oidGroup == OidGroup.All;
        }

        private static string NativeOidToFriendlyName(string oid, OidGroup oidGroup, bool fallBackToAllGroups)
        {
            CRYPT_OID_INFO oidInfo = OidInfo.FindOidInfo(CryptOidInfoKeyType.CRYPT_OID_INFO_OID_KEY, oid, oidGroup, fallBackToAllGroups);
            return oidInfo.Name;
        }

        private static string NativeFriendlyNameToOid(string friendlyName, OidGroup oidGroup, bool fallBackToAllGroups)
        {
            CRYPT_OID_INFO oidInfo = OidInfo.FindOidInfo(CryptOidInfoKeyType.CRYPT_OID_INFO_NAME_KEY, friendlyName, oidGroup, fallBackToAllGroups);
            return oidInfo.OID;
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------
    }
}