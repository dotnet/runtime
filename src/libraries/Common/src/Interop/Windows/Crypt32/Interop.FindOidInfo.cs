// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPT_OID_INFO
        {
            public int cbSize;
            public IntPtr pszOID;
            public IntPtr pwszName;
            public OidGroup dwGroupId;
            public int AlgId;
            public int cbData;
            public IntPtr pbData;

            public string? OID
            {
                get
                {
                    return Marshal.PtrToStringAnsi(pszOID);
                }
            }

            public string? Name
            {
                get
                {
                    return Marshal.PtrToStringUni(pwszName);
                }
            }
        }

        internal enum CryptOidInfoKeyType : int
        {
            CRYPT_OID_INFO_OID_KEY = 1,
            CRYPT_OID_INFO_NAME_KEY = 2,
            CRYPT_OID_INFO_ALGID_KEY = 3,
            CRYPT_OID_INFO_SIGN_KEY = 4,
            CRYPT_OID_INFO_CNG_ALGID_KEY = 5,
            CRYPT_OID_INFO_CNG_SIGN_KEY = 6,
        }

        internal static unsafe CRYPT_OID_INFO FindOidInfo(CryptOidInfoKeyType keyType, string key, OidGroup group, bool fallBackToAllGroups)
        {
            const OidGroup CRYPT_OID_DISABLE_SEARCH_DS_FLAG = unchecked((OidGroup)0x80000000);
            Debug.Assert(key != null);

            IntPtr rawKey = IntPtr.Zero;

            try
            {
                if (keyType == CryptOidInfoKeyType.CRYPT_OID_INFO_OID_KEY)
                {
                    rawKey = Marshal.StringToCoTaskMemAnsi(key);
                }
                else if (keyType == CryptOidInfoKeyType.CRYPT_OID_INFO_NAME_KEY)
                {
                    rawKey = Marshal.StringToCoTaskMemUni(key);
                }
                else
                {
                    throw new NotSupportedException();
                }

                // If the group alone isn't sufficient to suppress an active directory lookup, then our
                // first attempt should also include the suppression flag
                if (!OidGroupWillNotUseActiveDirectory(group))
                {
                    OidGroup localGroup = group | CRYPT_OID_DISABLE_SEARCH_DS_FLAG;
                    CRYPT_OID_INFO* localOidInfo = CryptFindOIDInfo(keyType, rawKey, localGroup);
                    if (localOidInfo != null)
                    {
                        return *localOidInfo;
                    }
                }

                // Attempt to query with a specific group, to make try to avoid an AD lookup if possible
                CRYPT_OID_INFO* fullOidInfo = CryptFindOIDInfo(keyType, rawKey, group);
                if (fullOidInfo != null)
                {
                    return *fullOidInfo;
                }

                if (fallBackToAllGroups && group != OidGroup.All)
                {
                    // Finally, for compatibility with previous runtimes, if we have a group specified retry the
                    // query with no group
                    CRYPT_OID_INFO* allGroupOidInfo = CryptFindOIDInfo(keyType, rawKey, OidGroup.All);
                    if (allGroupOidInfo != null)
                    {
                        return *allGroupOidInfo;
                    }
                }

                // Otherwise the lookup failed.
                return new CRYPT_OID_INFO() { AlgId = -1 };
            }
            finally
            {
                if (rawKey != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(rawKey);
                }
            }
        }

        private static bool OidGroupWillNotUseActiveDirectory(OidGroup group)
        {
            // These groups will never cause an Active Directory query
            return group == OidGroup.HashAlgorithm ||
                   group == OidGroup.EncryptionAlgorithm ||
                   group == OidGroup.PublicKeyAlgorithm ||
                   group == OidGroup.SignatureAlgorithm ||
                   group == OidGroup.Attribute ||
                   group == OidGroup.ExtensionOrAttribute ||
                   group == OidGroup.KeyDerivationFunction;
        }

        [LibraryImport(Interop.Libraries.Crypt32)]
        private static unsafe partial CRYPT_OID_INFO* CryptFindOIDInfo(CryptOidInfoKeyType dwKeyType, IntPtr pvKey, OidGroup group);
    }
}
