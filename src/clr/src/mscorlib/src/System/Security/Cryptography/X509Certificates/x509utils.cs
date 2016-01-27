// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.X509Certificates
{
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using System.Runtime.InteropServices;
    using Microsoft.Win32;
    using System.Diagnostics.Contracts;

    internal static class X509Constants {
        internal const uint CRYPT_EXPORTABLE     = 0x00000001;
        internal const uint CRYPT_USER_PROTECTED = 0x00000002;
        internal const uint CRYPT_MACHINE_KEYSET = 0x00000020;
        internal const uint CRYPT_USER_KEYSET    = 0x00001000;

        internal const uint CERT_QUERY_CONTENT_CERT               = 1;
        internal const uint CERT_QUERY_CONTENT_CTL                = 2;
        internal const uint CERT_QUERY_CONTENT_CRL                = 3;
        internal const uint CERT_QUERY_CONTENT_SERIALIZED_STORE   = 4;
        internal const uint CERT_QUERY_CONTENT_SERIALIZED_CERT    = 5;
        internal const uint CERT_QUERY_CONTENT_SERIALIZED_CTL     = 6;
        internal const uint CERT_QUERY_CONTENT_SERIALIZED_CRL     = 7;
        internal const uint CERT_QUERY_CONTENT_PKCS7_SIGNED       = 8;
        internal const uint CERT_QUERY_CONTENT_PKCS7_UNSIGNED     = 9;
        internal const uint CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED = 10;
        internal const uint CERT_QUERY_CONTENT_PKCS10             = 11;
        internal const uint CERT_QUERY_CONTENT_PFX                = 12;
        internal const uint CERT_QUERY_CONTENT_CERT_PAIR          = 13;

        internal const uint CERT_STORE_PROV_MEMORY   = 2;
        internal const uint CERT_STORE_PROV_SYSTEM   = 10;

        // cert store flags
        internal const uint CERT_STORE_NO_CRYPT_RELEASE_FLAG            = 0x00000001;
        internal const uint CERT_STORE_SET_LOCALIZED_NAME_FLAG          = 0x00000002;
        internal const uint CERT_STORE_DEFER_CLOSE_UNTIL_LAST_FREE_FLAG = 0x00000004;
        internal const uint CERT_STORE_DELETE_FLAG                      = 0x00000010;
        internal const uint CERT_STORE_SHARE_STORE_FLAG                 = 0x00000040;
        internal const uint CERT_STORE_SHARE_CONTEXT_FLAG               = 0x00000080;
        internal const uint CERT_STORE_MANIFOLD_FLAG                    = 0x00000100;
        internal const uint CERT_STORE_ENUM_ARCHIVED_FLAG               = 0x00000200;
        internal const uint CERT_STORE_UPDATE_KEYID_FLAG                = 0x00000400;
        internal const uint CERT_STORE_BACKUP_RESTORE_FLAG              = 0x00000800;
        internal const uint CERT_STORE_READONLY_FLAG                    = 0x00008000;
        internal const uint CERT_STORE_OPEN_EXISTING_FLAG               = 0x00004000;
        internal const uint CERT_STORE_CREATE_NEW_FLAG                  = 0x00002000;
        internal const uint CERT_STORE_MAXIMUM_ALLOWED_FLAG             = 0x00001000;

        internal const uint CERT_NAME_EMAIL_TYPE            = 1;
        internal const uint CERT_NAME_RDN_TYPE              = 2;
        internal const uint CERT_NAME_SIMPLE_DISPLAY_TYPE   = 4;
        internal const uint CERT_NAME_FRIENDLY_DISPLAY_TYPE = 5;
        internal const uint CERT_NAME_DNS_TYPE              = 6;
        internal const uint CERT_NAME_URL_TYPE              = 7;
        internal const uint CERT_NAME_UPN_TYPE              = 8;
    }

    /// <summary>
    ///     Groups of OIDs supported by CryptFindOIDInfo
    /// </summary>
    internal enum OidGroup {
        AllGroups = 0,
        HashAlgorithm = 1,                              // CRYPT_HASH_ALG_OID_GROUP_ID
        EncryptionAlgorithm = 2,                        // CRYPT_ENCRYPT_ALG_OID_GROUP_ID
        PublicKeyAlgorithm = 3,                         // CRYPT_PUBKEY_ALG_OID_GROUP_ID
        SignatureAlgorithm = 4,                         // CRYPT_SIGN_ALG_OID_GROUP_ID
        Attribute = 5,                                  // CRYPT_RDN_ATTR_OID_GROUP_ID
        ExtensionOrAttribute = 6,                       // CRYPT_EXT_OR_ATTR_OID_GROUP_ID
        EnhancedKeyUsage = 7,                           // CRYPT_ENHKEY_USAGE_OID_GROUP_ID
        Policy = 8,                                     // CRYPT_POLICY_OID_GROUP_ID
        Template = 9,                                   // CRYPT_TEMPLATE_OID_GROUP_ID
        KeyDerivationFunction = 10,                     // CRYPT_KDF_OID_GROUP_ID

        // This can be ORed into the above groups to turn off an AD search
        DisableSearchDS = unchecked((int)0x80000000)    // CRYPT_OID_DISABLE_SEARCH_DS_FLAG
    }

    /// <summary>
    ///     Keys that can be used to query information on via CryptFindOIDInfo
    /// </summary>
    internal enum OidKeyType {
        Oid = 1,                                        // CRYPT_OID_INFO_OID_KEY
        Name = 2,                                       // CRYPT_OID_INFO_NAME_KEY
        AlgorithmID = 3,                                // CRYPT_OID_INFO_ALGID_KEY
        SignatureID = 4,                                // CRYPT_OID_INFO_SIGN_KEY
        CngAlgorithmID = 5,                             // CRYPT_OID_INFO_CNG_ALGID_KEY
        CngSignatureID = 6,                             // CRYPT_OID_INFO_CNG_SIGN_KEY
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CRYPT_OID_INFO {
        internal int cbSize;
        [MarshalAs(UnmanagedType.LPStr)]
        internal string pszOID;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string pwszName;
        internal OidGroup dwGroupId;
        internal int AlgId;
        internal int cbData;
        internal IntPtr pbData;
    }

    internal static class X509Utils
    {
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
        private static bool OidGroupWillNotUseActiveDirectory(OidGroup group) {
            // These groups will never cause an Active Directory query
            return group == OidGroup.HashAlgorithm ||
                   group == OidGroup.EncryptionAlgorithm ||
                   group == OidGroup.PublicKeyAlgorithm ||
                   group == OidGroup.SignatureAlgorithm  ||
                   group == OidGroup.Attribute ||
                   group == OidGroup.ExtensionOrAttribute ||
                   group == OidGroup.KeyDerivationFunction;
        }

        [SecurityCritical]
        private static CRYPT_OID_INFO FindOidInfo(OidKeyType keyType, string key, OidGroup group) {
            Contract.Requires(key != null);

            IntPtr rawKey = IntPtr.Zero;

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                if (keyType == OidKeyType.Oid) {
                    rawKey = Marshal.StringToCoTaskMemAnsi(key);
                }
                else {
                    rawKey = Marshal.StringToCoTaskMemUni(key);
                }

                // If the group alone isn't sufficient to suppress an active directory lookup, then our
                // first attempt should also include the suppression flag
                if (!OidGroupWillNotUseActiveDirectory(group)) {
                    OidGroup localGroup = group | OidGroup.DisableSearchDS;
                    IntPtr localOidInfo = CryptFindOIDInfo(keyType, rawKey, localGroup);
                    if (localOidInfo != IntPtr.Zero) {
                        return (CRYPT_OID_INFO)Marshal.PtrToStructure(localOidInfo, typeof(CRYPT_OID_INFO));
                    }
                }

                // Attempt to query with a specific group, to make try to avoid an AD lookup if possible
                IntPtr fullOidInfo = CryptFindOIDInfo(keyType, rawKey, group);
                if (fullOidInfo != IntPtr.Zero) {
                    return (CRYPT_OID_INFO)Marshal.PtrToStructure(fullOidInfo, typeof(CRYPT_OID_INFO));
                }

                // Finally, for compatibility with previous runtimes, if we have a group specified retry the
                // query with no group
                if (group != OidGroup.AllGroups) {
                    IntPtr allGroupOidInfo = CryptFindOIDInfo(keyType, rawKey, OidGroup.AllGroups);
                    if (allGroupOidInfo != IntPtr.Zero) {
                        return (CRYPT_OID_INFO)Marshal.PtrToStructure(fullOidInfo, typeof(CRYPT_OID_INFO));
                    }
                }

                // Otherwise the lookup failed
                return new CRYPT_OID_INFO();
            }
            finally {
                if (rawKey != IntPtr.Zero) {
                    Marshal.FreeCoTaskMem(rawKey);
                }
            }
        }

        [SecuritySafeCritical]
        internal static int GetAlgIdFromOid(string oid, OidGroup oidGroup) {
            Contract.Requires(oid != null);

            // CAPI does not have ALGID mappings for all of the hash algorithms - see if we know the mapping
            // first to avoid doing an AD lookup on these values
            if (String.Equals(oid, Constants.OID_OIWSEC_SHA256, StringComparison.Ordinal)) {
                return Constants.CALG_SHA_256;
            }
            else if (String.Equals(oid, Constants.OID_OIWSEC_SHA384, StringComparison.Ordinal)) {
                return Constants.CALG_SHA_384;
            }
            else if (String.Equals(oid, Constants.OID_OIWSEC_SHA512, StringComparison.Ordinal)) {
                return Constants.CALG_SHA_512;
            }
            else {
                return FindOidInfo(OidKeyType.Oid, oid, oidGroup).AlgId;
            }
        }

        [SecuritySafeCritical]
        internal static string GetFriendlyNameFromOid(string oid, OidGroup oidGroup) {
            Contract.Requires(oid != null);
            CRYPT_OID_INFO oidInfo = FindOidInfo(OidKeyType.Oid, oid, oidGroup);
            return oidInfo.pwszName;
        }

        [SecuritySafeCritical]
        internal static string GetOidFromFriendlyName(string friendlyName, OidGroup oidGroup) {
            Contract.Requires(friendlyName != null);
            CRYPT_OID_INFO oidInfo = FindOidInfo(OidKeyType.Name, friendlyName, oidGroup);
            return oidInfo.pszOID;
        }

        internal static int NameOrOidToAlgId (string oid, OidGroup oidGroup) {
            // Default Algorithm Id is CALG_SHA1
            if (oid == null)
                return Constants.CALG_SHA1;
            string oidValue = CryptoConfig.MapNameToOID(oid, oidGroup);
            if (oidValue == null)
                oidValue = oid; // we were probably passed an OID value directly

            int algId = GetAlgIdFromOid(oidValue, oidGroup);
            if (algId == 0 || algId == -1) {
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidOID"));
            }
            return algId;
        }
#endif // FEATURE_CRYPTO

        // this method maps a cert content type returned from CryptQueryObject
        // to a value in the managed X509ContentType enum
        internal static X509ContentType MapContentType (uint contentType) {
            switch (contentType) {
            case X509Constants.CERT_QUERY_CONTENT_CERT:
                return X509ContentType.Cert;
#if !FEATURE_CORECLR
            case X509Constants.CERT_QUERY_CONTENT_SERIALIZED_STORE:
                return X509ContentType.SerializedStore;
            case X509Constants.CERT_QUERY_CONTENT_SERIALIZED_CERT:
                return X509ContentType.SerializedCert;
            case X509Constants.CERT_QUERY_CONTENT_PKCS7_SIGNED:
            case X509Constants.CERT_QUERY_CONTENT_PKCS7_UNSIGNED:
                return X509ContentType.Pkcs7;
            case X509Constants.CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED:
                return X509ContentType.Authenticode;
            case X509Constants.CERT_QUERY_CONTENT_PFX:
                return X509ContentType.Pkcs12;
#endif // !FEATURE_CORECLR
            default:
                return X509ContentType.Unknown;
            }
        }

        // this method maps a X509KeyStorageFlags enum to a combination of crypto API flags
        internal static uint MapKeyStorageFlags(X509KeyStorageFlags keyStorageFlags) {
#if FEATURE_LEGACYNETCF
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8) {
                if (keyStorageFlags != X509KeyStorageFlags.DefaultKeySet)
                    throw new NotSupportedException(Environment.GetResourceString("Argument_InvalidFlag"), 
                                                    new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "keyStorageFlags"));
            }            
#endif
            if ((keyStorageFlags & (X509KeyStorageFlags) ~0x1F) != 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "keyStorageFlags");

#if !FEATURE_LEGACYNETCF  // CompatibilitySwitches causes problems with CCRewrite
            Contract.EndContractBlock();
#endif

            uint dwFlags = 0;
#if FEATURE_CORECLR
            if (keyStorageFlags != X509KeyStorageFlags.DefaultKeySet) {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "keyStorageFlags",
                                            new NotSupportedException());
            }
#else // FEATURE_CORECLR                        
            if ((keyStorageFlags & X509KeyStorageFlags.UserKeySet) == X509KeyStorageFlags.UserKeySet)
                dwFlags |= X509Constants.CRYPT_USER_KEYSET;
            else if ((keyStorageFlags & X509KeyStorageFlags.MachineKeySet) == X509KeyStorageFlags.MachineKeySet)
                dwFlags |= X509Constants.CRYPT_MACHINE_KEYSET;

            if ((keyStorageFlags & X509KeyStorageFlags.Exportable) == X509KeyStorageFlags.Exportable)
                dwFlags |= X509Constants.CRYPT_EXPORTABLE;
            if ((keyStorageFlags & X509KeyStorageFlags.UserProtected) == X509KeyStorageFlags.UserProtected)
                dwFlags |= X509Constants.CRYPT_USER_PROTECTED;
#endif // FEATURE_CORECLR else

            return dwFlags;
        }

#if !FEATURE_CORECLR
        // this method creates a memory store from a certificate
        [System.Security.SecurityCritical]  // auto-generated
        internal static SafeCertStoreHandle ExportCertToMemoryStore (X509Certificate certificate) {
            SafeCertStoreHandle safeCertStoreHandle = SafeCertStoreHandle.InvalidHandle;
            X509Utils._OpenX509Store(X509Constants.CERT_STORE_PROV_MEMORY, 
                                     X509Constants.CERT_STORE_ENUM_ARCHIVED_FLAG | X509Constants.CERT_STORE_CREATE_NEW_FLAG,
                                     null, 
                                     ref safeCertStoreHandle);
            X509Utils._AddCertificateToStore(safeCertStoreHandle, certificate.CertContext);
            return safeCertStoreHandle;
        }
#endif // !FEATURE_CORECLR

        [System.Security.SecurityCritical]  // auto-generated
        internal static IntPtr PasswordToHGlobalUni (object password) {
            if (password != null) {
                string pwd = password as string;
                if (pwd != null)
                    return Marshal.StringToHGlobalUni(pwd);
#if FEATURE_X509_SECURESTRINGS
                SecureString securePwd = password as SecureString;
                if (securePwd != null)
                    return Marshal.SecureStringToGlobalAllocUnicode(securePwd);
#endif // FEATURE_X509_SECURESTRINGS
            }
            return IntPtr.Zero;
        }

#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport("crypt32")]
        private static extern IntPtr CryptFindOIDInfo(OidKeyType dwKeyType, IntPtr pvKey, OidGroup dwGroupId);
#endif // FEATURE_CRYPTO

#if !FEATURE_CORECLR
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _AddCertificateToStore(SafeCertStoreHandle safeCertStoreHandle, SafeCertContextHandle safeCertContext);
#endif // !FEATURE_CORECLR
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _DuplicateCertContext(IntPtr handle, ref SafeCertContextHandle safeCertContext);
#if !FEATURE_CORECLR
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _ExportCertificatesToBlob(SafeCertStoreHandle safeCertStoreHandle, X509ContentType contentType, IntPtr password);
#endif // !FEATURE_CORECLR
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _GetCertRawData(SafeCertContextHandle safeCertContext);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _GetDateNotAfter(SafeCertContextHandle safeCertContext, ref Win32Native.FILE_TIME fileTime);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _GetDateNotBefore(SafeCertContextHandle safeCertContext, ref Win32Native.FILE_TIME fileTime);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string _GetIssuerName(SafeCertContextHandle safeCertContext, bool legacyV1Mode);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string _GetPublicKeyOid(SafeCertContextHandle safeCertContext);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _GetPublicKeyParameters(SafeCertContextHandle safeCertContext);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _GetPublicKeyValue(SafeCertContextHandle safeCertContext);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string _GetSubjectInfo(SafeCertContextHandle safeCertContext, uint displayType, bool legacyV1Mode);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _GetSerialNumber(SafeCertContextHandle safeCertContext);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _GetThumbprint(SafeCertContextHandle safeCertContext);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _LoadCertFromBlob(byte[] rawData, IntPtr password, uint dwFlags, bool persistKeySet, ref SafeCertContextHandle pCertCtx);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _LoadCertFromFile(string fileName, IntPtr password, uint dwFlags, bool persistKeySet, ref SafeCertContextHandle pCertCtx);
#if !FEATURE_CORECLR
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _OpenX509Store(uint storeType, uint flags, string storeName, ref SafeCertStoreHandle safeCertStoreHandle);
#endif // !FEATURE_CORECLR
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern uint _QueryCertBlobType(byte[] rawData);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern uint _QueryCertFileType(string fileName);
    }
}
