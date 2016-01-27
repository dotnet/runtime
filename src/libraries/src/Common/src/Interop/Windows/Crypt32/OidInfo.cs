// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using global::System;
using global::System.Text;
using global::System.Diagnostics;
using global::System.Diagnostics.Contracts;
using global::System.Collections.Generic;
using global::System.Security.Cryptography;
using global::System.Runtime.InteropServices;

namespace Internal.NativeCrypto
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

        public string OID
        {
            get
            {
                return Marshal.PtrToStringAnsi(pszOID);
            }
        }

        public string Name
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
    }

    internal static partial class OidInfo
    {
        private const string Capi2Dll = "Crypt32.dll";

        public static CRYPT_OID_INFO FindOidInfo(CryptOidInfoKeyType keyType, string key, OidGroup group, bool fallBackToAllGroups)
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
                    IntPtr localOidInfo = CryptFindOIDInfo(keyType, rawKey, localGroup);
                    if (localOidInfo != IntPtr.Zero)
                    {
                        return Marshal.PtrToStructure<CRYPT_OID_INFO>(localOidInfo);
                    }
                }

                // Attempt to query with a specific group, to make try to avoid an AD lookup if possible
                IntPtr fullOidInfo = CryptFindOIDInfo(keyType, rawKey, group);
                if (fullOidInfo != IntPtr.Zero)
                {
                    return Marshal.PtrToStructure<CRYPT_OID_INFO>(fullOidInfo);
                }

                if (fallBackToAllGroups && group != OidGroup.All)
                {
                    // Finally, for compatibility with previous runtimes, if we have a group specified retry the
                    // query with no group
                    IntPtr allGroupOidInfo = CryptFindOIDInfo(keyType, rawKey, OidGroup.All);
                    if (allGroupOidInfo != IntPtr.Zero)
                    {
                        return Marshal.PtrToStructure<CRYPT_OID_INFO>(fullOidInfo);
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

        [DllImport(Capi2Dll, CharSet = CharSet.Unicode)]
        private static extern IntPtr CryptFindOIDInfo(CryptOidInfoKeyType dwKeyType, IntPtr pvKey, OidGroup group);
    }
}

