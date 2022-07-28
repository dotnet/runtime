// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using static Interop.Crypt32;

namespace System.Security.Cryptography
{
    internal static partial class OidLookup
    {
        private static bool ShouldUseCache(OidGroup oidGroup)
        {
            return oidGroup == OidGroup.All;
        }

        private static string? NativeOidToFriendlyName(string oid, OidGroup oidGroup, bool fallBackToAllGroups)
        {
            CRYPT_OID_INFO oidInfo = Interop.Crypt32.FindOidInfo(CryptOidInfoKeyType.CRYPT_OID_INFO_OID_KEY, oid, oidGroup, fallBackToAllGroups);
            return oidInfo.Name;
        }

        private static string? NativeFriendlyNameToOid(string friendlyName, OidGroup oidGroup, bool fallBackToAllGroups)
        {
            CRYPT_OID_INFO oidInfo = Interop.Crypt32.FindOidInfo(CryptOidInfoKeyType.CRYPT_OID_INFO_NAME_KEY, friendlyName, oidGroup, fallBackToAllGroups);
            return oidInfo.OID;
        }
    }
}
