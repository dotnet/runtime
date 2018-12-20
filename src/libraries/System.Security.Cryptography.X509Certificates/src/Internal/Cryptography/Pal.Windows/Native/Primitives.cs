// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Internal.Cryptography.Pal.Native
{
    internal enum CertQueryObjectType : int
    {
        CERT_QUERY_OBJECT_FILE = 0x00000001,
        CERT_QUERY_OBJECT_BLOB = 0x00000002,
    }

    [Flags]
    internal enum ExpectedContentTypeFlags : int
    {
        //encoded single certificate
         CERT_QUERY_CONTENT_FLAG_CERT = 1 << ContentType.CERT_QUERY_CONTENT_CERT,

        //encoded single CTL
         CERT_QUERY_CONTENT_FLAG_CTL = 1 << ContentType.CERT_QUERY_CONTENT_CTL,

        //encoded single CRL
         CERT_QUERY_CONTENT_FLAG_CRL = 1 << ContentType.CERT_QUERY_CONTENT_CRL,

        //serialized store
         CERT_QUERY_CONTENT_FLAG_SERIALIZED_STORE = 1 << ContentType.CERT_QUERY_CONTENT_SERIALIZED_STORE,

        //serialized single certificate
         CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT = 1 << ContentType.CERT_QUERY_CONTENT_SERIALIZED_CERT,

        //serialized single CTL
         CERT_QUERY_CONTENT_FLAG_SERIALIZED_CTL = 1 << ContentType.CERT_QUERY_CONTENT_SERIALIZED_CTL,

        //serialized single CRL
         CERT_QUERY_CONTENT_FLAG_SERIALIZED_CRL = 1 << ContentType.CERT_QUERY_CONTENT_SERIALIZED_CRL,

        //an encoded PKCS#7 signed message
         CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED = 1 << ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED,

        //an encoded PKCS#7 message.  But it is not a signed message
         CERT_QUERY_CONTENT_FLAG_PKCS7_UNSIGNED = 1 << ContentType.CERT_QUERY_CONTENT_PKCS7_UNSIGNED,

        //the content includes an embedded PKCS7 signed message
         CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED = 1 << ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED,

        //an encoded PKCS#10
         CERT_QUERY_CONTENT_FLAG_PKCS10 = 1 << ContentType.CERT_QUERY_CONTENT_PKCS10,

        //an encoded PFX BLOB
         CERT_QUERY_CONTENT_FLAG_PFX = 1 << ContentType.CERT_QUERY_CONTENT_PFX,

        //an encoded CertificatePair (contains forward and/or reverse cross certs)
         CERT_QUERY_CONTENT_FLAG_CERT_PAIR = 1 << ContentType.CERT_QUERY_CONTENT_CERT_PAIR,

        //an encoded PFX BLOB, and we do want to load it (not included in
        //CERT_QUERY_CONTENT_FLAG_ALL)
         CERT_QUERY_CONTENT_FLAG_PFX_AND_LOAD = 1 << ContentType.CERT_QUERY_CONTENT_PFX_AND_LOAD,

         CERT_QUERY_CONTENT_FLAG_ALL = 
             CERT_QUERY_CONTENT_FLAG_CERT | 
             CERT_QUERY_CONTENT_FLAG_CTL |
             CERT_QUERY_CONTENT_FLAG_CRL |
             CERT_QUERY_CONTENT_FLAG_SERIALIZED_STORE |
             CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT |
             CERT_QUERY_CONTENT_FLAG_SERIALIZED_CTL |
             CERT_QUERY_CONTENT_FLAG_SERIALIZED_CRL |
             CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED |
             CERT_QUERY_CONTENT_FLAG_PKCS7_UNSIGNED | 
             CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED |
             CERT_QUERY_CONTENT_FLAG_PKCS10 |
             CERT_QUERY_CONTENT_FLAG_PFX |
             CERT_QUERY_CONTENT_FLAG_CERT_PAIR,
    }

    [Flags]
    internal enum ExpectedFormatTypeFlags : int
    {
        CERT_QUERY_FORMAT_FLAG_BINARY = 1 << FormatType.CERT_QUERY_FORMAT_BINARY,
        CERT_QUERY_FORMAT_FLAG_BASE64_ENCODED = 1 << FormatType.CERT_QUERY_FORMAT_BASE64_ENCODED,
        CERT_QUERY_FORMAT_FLAG_ASN_ASCII_HEX_ENCODED = 1 << FormatType.CERT_QUERY_FORMAT_ASN_ASCII_HEX_ENCODED,

        CERT_QUERY_FORMAT_FLAG_ALL = CERT_QUERY_FORMAT_FLAG_BINARY | CERT_QUERY_FORMAT_FLAG_BASE64_ENCODED | CERT_QUERY_FORMAT_FLAG_ASN_ASCII_HEX_ENCODED,
    }

    internal enum CertEncodingType : int
    {
        PKCS_7_ASN_ENCODING = 0x10000,
        X509_ASN_ENCODING   = 0x00001,

        All = PKCS_7_ASN_ENCODING | X509_ASN_ENCODING,
    }

    internal enum ContentType : int
    {
        //encoded single certificate
        CERT_QUERY_CONTENT_CERT                = 1,
        //encoded single CTL                   
        CERT_QUERY_CONTENT_CTL                 = 2,
        //encoded single CRL
        CERT_QUERY_CONTENT_CRL                 = 3,
        //serialized store
        CERT_QUERY_CONTENT_SERIALIZED_STORE    = 4,
        //serialized single certificate
        CERT_QUERY_CONTENT_SERIALIZED_CERT     = 5,
        //serialized single CTL
        CERT_QUERY_CONTENT_SERIALIZED_CTL      = 6,
        //serialized single CRL
        CERT_QUERY_CONTENT_SERIALIZED_CRL      = 7,
        //a PKCS#7 signed message
        CERT_QUERY_CONTENT_PKCS7_SIGNED        = 8,
        //a PKCS#7 message, such as enveloped message.  But it is not a signed message,
        CERT_QUERY_CONTENT_PKCS7_UNSIGNED      = 9,
        //a PKCS7 signed message embedded in a file
        CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED  = 10,
        //an encoded PKCS#10
        CERT_QUERY_CONTENT_PKCS10              = 11,
        //an encoded PFX BLOB
        CERT_QUERY_CONTENT_PFX                 = 12,
        //an encoded CertificatePair (contains forward and/or reverse cross certs)
        CERT_QUERY_CONTENT_CERT_PAIR           = 13,
        //an encoded PFX BLOB, which was loaded to phCertStore
        CERT_QUERY_CONTENT_PFX_AND_LOAD        = 14,
    }

    internal enum FormatType : int
    {
        CERT_QUERY_FORMAT_BINARY = 1,
        CERT_QUERY_FORMAT_BASE64_ENCODED = 2,
        CERT_QUERY_FORMAT_ASN_ASCII_HEX_ENCODED = 3,
    }

    // CRYPTOAPI_BLOB has many typedef aliases in the C++ world (CERT_BLOB, DATA_BLOB, etc.) We'll just stick to one name here.
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CRYPTOAPI_BLOB
    {
        public CRYPTOAPI_BLOB(int cbData, byte* pbData)
        {
            this.cbData = cbData;
            this.pbData = pbData;
        }

        public int cbData;
        public byte* pbData;

        public byte[] ToByteArray()
        {
            if (cbData == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] array = new byte[cbData];
            Marshal.Copy((IntPtr)pbData, array, 0, cbData);
            return array;
        }
    }

    internal enum CertContextPropId : int
    {
        CERT_KEY_PROV_INFO_PROP_ID   = 2,
        CERT_SHA1_HASH_PROP_ID       = 3,
        CERT_KEY_CONTEXT_PROP_ID     = 5,
        CERT_FRIENDLY_NAME_PROP_ID   = 11,
        CERT_ARCHIVED_PROP_ID        = 19,
        CERT_KEY_IDENTIFIER_PROP_ID  = 20,
        CERT_PUBKEY_ALG_PARA_PROP_ID = 22,
        CERT_NCRYPT_KEY_HANDLE_PROP_ID = 78,
        CERT_CLR_DELETE_KEY_PROP_ID = 125,
    }

    [Flags]
    internal enum CertSetPropertyFlags : int
    {
        CERT_SET_PROPERTY_INHIBIT_PERSIST_FLAG = 0x40000000,
        None                                   = 0x00000000,
    }

    internal enum CertNameType : int
    {
        CERT_NAME_EMAIL_TYPE = 1,
        CERT_NAME_RDN_TYPE = 2,
        CERT_NAME_ATTR_TYPE = 3,
        CERT_NAME_SIMPLE_DISPLAY_TYPE = 4,
        CERT_NAME_FRIENDLY_DISPLAY_TYPE = 5,
        CERT_NAME_DNS_TYPE = 6,
        CERT_NAME_URL_TYPE = 7,
        CERT_NAME_UPN_TYPE = 8,
    }

    [Flags]
    internal enum CertNameFlags : int
    {
        None                  = 0x00000000,
        CERT_NAME_ISSUER_FLAG = 0x00000001,
    }

    internal enum CertNameStringType : int
    {
        CERT_X500_NAME_STR = 3,

        CERT_NAME_STR_REVERSE_FLAG = 0x02000000,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CERT_CONTEXT
    {
        public CertEncodingType dwCertEncodingType;
        public byte* pbCertEncoded;
        public int cbCertEncoded;
        public CERT_INFO* pCertInfo;
        public IntPtr hCertStore;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CERT_INFO
    {
        public int dwVersion;
        public CRYPTOAPI_BLOB SerialNumber;
        public CRYPT_ALGORITHM_IDENTIFIER SignatureAlgorithm;
        public CRYPTOAPI_BLOB Issuer;
        public FILETIME NotBefore;
        public FILETIME NotAfter;
        public CRYPTOAPI_BLOB Subject;
        public CERT_PUBLIC_KEY_INFO SubjectPublicKeyInfo;
        public CRYPT_BIT_BLOB IssuerUniqueId;
        public CRYPT_BIT_BLOB SubjectUniqueId;
        public int cExtension;
        public CERT_EXTENSION* rgExtension;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CRYPT_ALGORITHM_IDENTIFIER
    {
        public IntPtr pszObjId;
        public CRYPTOAPI_BLOB Parameters;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CERT_PUBLIC_KEY_INFO
    {
        public CRYPT_ALGORITHM_IDENTIFIER Algorithm;
        public CRYPT_BIT_BLOB PublicKey;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CRYPT_BIT_BLOB
    {
        public int cbData;
        public byte* pbData;
        public int cUnusedBits;

        public byte[] ToByteArray()
        {
            if (cbData == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] array = new byte[cbData];
            Marshal.Copy((IntPtr)pbData, array, 0, cbData);
            return array;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CERT_EXTENSION
    {
        public IntPtr pszObjId;
        public int fCritical;
        public CRYPTOAPI_BLOB Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FILETIME
    {
        private uint ftTimeLow;
        private uint ftTimeHigh;

        public DateTime ToDateTime()
        {
            long fileTime = (((long)ftTimeHigh) << 32) + ftTimeLow;
            return DateTime.FromFileTime(fileTime);
        }

        public static FILETIME FromDateTime(DateTime dt)
        {
            long fileTime = dt.ToFileTime();

            unchecked
            {
                return new FILETIME()
                {
                    ftTimeLow = (uint)fileTime,
                    ftTimeHigh = (uint)(fileTime >> 32),
                };
            }
        }
    }

    internal enum CertStoreProvider : int
    {
        CERT_STORE_PROV_MEMORY = 2,
        CERT_STORE_PROV_SYSTEM_W = 10,
    }

    [Flags]
    internal enum CertStoreFlags : int
    {
        CERT_STORE_NO_CRYPT_RELEASE_FLAG                = 0x00000001,
        CERT_STORE_SET_LOCALIZED_NAME_FLAG              = 0x00000002,
        CERT_STORE_DEFER_CLOSE_UNTIL_LAST_FREE_FLAG     = 0x00000004,
        CERT_STORE_DELETE_FLAG                          = 0x00000010,
        CERT_STORE_UNSAFE_PHYSICAL_FLAG                 = 0x00000020,
        CERT_STORE_SHARE_STORE_FLAG                     = 0x00000040,
        CERT_STORE_SHARE_CONTEXT_FLAG                   = 0x00000080,
        CERT_STORE_MANIFOLD_FLAG                        = 0x00000100,
        CERT_STORE_ENUM_ARCHIVED_FLAG                   = 0x00000200,
        CERT_STORE_UPDATE_KEYID_FLAG                    = 0x00000400,
        CERT_STORE_BACKUP_RESTORE_FLAG                  = 0x00000800,
        CERT_STORE_READONLY_FLAG                        = 0x00008000,
        CERT_STORE_OPEN_EXISTING_FLAG                   = 0x00004000,
        CERT_STORE_CREATE_NEW_FLAG                      = 0x00002000,
        CERT_STORE_MAXIMUM_ALLOWED_FLAG                 = 0x00001000,   
 
        CERT_SYSTEM_STORE_CURRENT_USER                  = 0x00010000,
        CERT_SYSTEM_STORE_LOCAL_MACHINE                 = 0x00020000,

        None                                            = 0x00000000,
    }

    internal enum CertStoreAddDisposition : int
    {
        CERT_STORE_ADD_NEW                                  = 1,
        CERT_STORE_ADD_USE_EXISTING                         = 2,
        CERT_STORE_ADD_REPLACE_EXISTING                     = 3,
        CERT_STORE_ADD_ALWAYS                               = 4,
        CERT_STORE_ADD_REPLACE_EXISTING_INHERIT_PROPERTIES  = 5,
        CERT_STORE_ADD_NEWER                                = 6,
        CERT_STORE_ADD_NEWER_INHERIT_PROPERTIES             = 7,
    }

    [Flags]
    internal enum PfxCertStoreFlags : int
    {
        CRYPT_EXPORTABLE                   = 0x00000001,
        CRYPT_USER_PROTECTED               = 0x00000002,
        CRYPT_MACHINE_KEYSET               = 0x00000020,
        CRYPT_USER_KEYSET                  = 0x00001000,
        PKCS12_PREFER_CNG_KSP              = 0x00000100,
        PKCS12_ALWAYS_CNG_KSP              = 0x00000200,
        PKCS12_ALLOW_OVERWRITE_KEY         = 0x00004000,
        PKCS12_NO_PERSIST_KEY              = 0x00008000,
        PKCS12_INCLUDE_EXTENDED_PROPERTIES = 0x00000010,
        None                               = 0x00000000,
    }

    internal enum CryptMessageParameterType : int
    {
        CMSG_SIGNER_COUNT_PARAM = 5,
        CMSG_SIGNER_INFO_PARAM = 6,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CMSG_SIGNER_INFO_Partial  // This is not the full definition of CMSG_SIGNER_INFO. Only defining the part we use.
    {
        public int dwVersion;
        public CRYPTOAPI_BLOB Issuer;
        public CRYPTOAPI_BLOB SerialNumber;
        //... more fields follow ...
    }

    [Flags]
    internal enum CertFindFlags : int
    {
        None = 0x00000000,
    }

    internal enum CertFindType : int
    {
        CERT_FIND_SUBJECT_CERT = 0x000b0000,
        CERT_FIND_HASH         = 0x00010000,
        CERT_FIND_SUBJECT_STR  = 0x00080007,
        CERT_FIND_ISSUER_STR   = 0x00080004,
        CERT_FIND_EXISTING     = 0x000d0000,
        CERT_FIND_ANY          = 0x00000000,
    }

    [Flags]
    internal enum PFXExportFlags : int
    {
        REPORT_NO_PRIVATE_KEY                 = 0x00000001,
        REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY = 0x00000002,
        EXPORT_PRIVATE_KEYS                   = 0x00000004,
        None                                  = 0x00000000,
    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CRYPT_KEY_PROV_INFO
    {
        public char* pwszContainerName;
        public char* pwszProvName;
        public int dwProvType;
        public CryptAcquireContextFlags dwFlags;
        public int cProvParam;
        public IntPtr rgProvParam;
        public int dwKeySpec;
    }

    [Flags]
    internal enum CryptAcquireContextFlags : int
    {
        CRYPT_DELETEKEYSET = 0x00000010,
        CRYPT_MACHINE_KEYSET = 0x00000020,
        None = 0x00000000,
    }

    [Flags]
    internal enum CertNameStrTypeAndFlags : int
    {
        CERT_SIMPLE_NAME_STR                   = 1,
        CERT_OID_NAME_STR                      = 2,
        CERT_X500_NAME_STR                     = 3,

        CERT_NAME_STR_SEMICOLON_FLAG           = 0x40000000,
        CERT_NAME_STR_NO_PLUS_FLAG             = 0x20000000,
        CERT_NAME_STR_NO_QUOTING_FLAG          = 0x10000000,
        CERT_NAME_STR_CRLF_FLAG                = 0x08000000,
        CERT_NAME_STR_COMMA_FLAG               = 0x04000000,
        CERT_NAME_STR_REVERSE_FLAG             = 0x02000000,

        CERT_NAME_STR_DISABLE_IE4_UTF8_FLAG    = 0x00010000,
        CERT_NAME_STR_ENABLE_T61_UNICODE_FLAG  = 0x00020000,
        CERT_NAME_STR_ENABLE_UTF8_UNICODE_FLAG = 0x00040000,
        CERT_NAME_STR_FORCE_UTF8_DIR_STR_FLAG  = 0x00080000,
    }

    internal enum FormatObjectType : int
    {
        None = 0,
    }

    internal enum FormatObjectStructType : int
    {
        X509_NAME = 7,
    }

    internal static class AlgId
    {
        public const int CALG_RSA_KEYX = 0xa400;
        public const int CALG_RSA_SIGN = 0x2400;
        public const int CALG_DSS_SIGN = 0x2200;
        public const int CALG_SHA1     = 0x8004;
    }

    [Flags]
    internal enum CryptDecodeObjectFlags : int
    {
        None = 0x00000000,
    }

    internal enum CryptDecodeObjectStructType : int
    {
        CNG_RSA_PUBLIC_KEY_BLOB = 72,
        X509_DSS_PUBLICKEY = 38,
        X509_DSS_PARAMETERS = 39,
        X509_KEY_USAGE = 14,
        X509_BASIC_CONSTRAINTS = 13,
        X509_BASIC_CONSTRAINTS2 = 15,
        X509_ENHANCED_KEY_USAGE = 36,
        X509_CERT_POLICIES = 16,
        X509_UNICODE_ANY_STRING = 24,
        X509_CERTIFICATE_TEMPLATE = 64,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CTL_USAGE
    {
        public int cUsageIdentifier;
        public IntPtr rgpszUsageIdentifier;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CERT_USAGE_MATCH
    {
        public CertUsageMatchType dwType;
        public CTL_USAGE Usage;
    }

    internal enum CertUsageMatchType : int
    {
        USAGE_MATCH_TYPE_AND = 0x00000000,
        USAGE_MATCH_TYPE_OR  = 0x00000001,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CERT_CHAIN_PARA
    {
        public int cbSize;
        public CERT_USAGE_MATCH RequestedUsage;
        public CERT_USAGE_MATCH RequestedIssuancePolicy;
        public int dwUrlRetrievalTimeout;
        public int fCheckRevocationFreshnessTime;
        public int dwRevocationFreshnessTime;
        public FILETIME* pftCacheResync;
        public int pStrongSignPara;
        public int dwStrongSignFlags;
    }

    [Flags]
    internal enum CertChainFlags : int
    {
        None                                           = 0x00000000,
        CERT_CHAIN_REVOCATION_CHECK_END_CERT           = 0x10000000,
        CERT_CHAIN_REVOCATION_CHECK_CHAIN              = 0x20000000,
        CERT_CHAIN_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT = 0x40000000,
        CERT_CHAIN_REVOCATION_CHECK_CACHE_ONLY         = unchecked((int)0x80000000),
    }

    internal enum ChainEngine : int
    {
        HCCE_CURRENT_USER = 0x0,
        HCCE_LOCAL_MACHINE = 0x1,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CERT_DSS_PARAMETERS
    {
        public CRYPTOAPI_BLOB p;
        public CRYPTOAPI_BLOB q;
        public CRYPTOAPI_BLOB g;
    }

    internal enum PubKeyMagic : int
    {
        DSS_MAGIC = 0x31535344,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CERT_BASIC_CONSTRAINTS_INFO
    {
        public CRYPT_BIT_BLOB SubjectType;
        public int fPathLenConstraint;
        public int dwPathLenConstraint;
        public int cSubtreesConstraint;
        public CRYPTOAPI_BLOB* rgSubtreesConstraint; // PCERT_NAME_BLOB

        // SubjectType.pbData[0] can contain a CERT_CA_SUBJECT_FLAG that when set indicates that the certificate's subject can act as a CA
        public const byte CERT_CA_SUBJECT_FLAG = 0x80;
    };

    [StructLayout(LayoutKind.Sequential)]
    internal struct CERT_BASIC_CONSTRAINTS2_INFO
    {
        public int fCA;
        public int fPathLenConstraint;
        public int dwPathLenConstraint;
    };

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CERT_ENHKEY_USAGE
    {
        public int cUsageIdentifier;
        public IntPtr* rgpszUsageIdentifier; // LPSTR*
    }

    internal enum CertStoreSaveAs :  int
    {
        CERT_STORE_SAVE_AS_STORE = 1,
        CERT_STORE_SAVE_AS_PKCS7 = 2,
    }

    internal enum CertStoreSaveTo : int
    {
        CERT_STORE_SAVE_TO_MEMORY = 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CERT_POLICY_INFO
    {
        public IntPtr pszPolicyIdentifier;
        public int cPolicyQualifier;
        public IntPtr rgPolicyQualifier;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CERT_POLICIES_INFO
    {
        public int cPolicyInfo;
        public CERT_POLICY_INFO* rgPolicyInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CERT_NAME_VALUE
    {
        public int dwValueType;
        public CRYPTOAPI_BLOB Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CERT_TEMPLATE_EXT
    {
        public IntPtr pszObjId;
        public int dwMajorVersion;
        public int fMinorVersion;
        public int dwMinorVersion;
    }

    [Flags]
    internal enum CertControlStoreFlags : int
    {
        None = 0x00000000,
    }

    internal enum CertControlStoreType : int
    {
        CERT_STORE_CTRL_AUTO_RESYNC = 4,
    }

    [Flags]
    internal enum CertTrustErrorStatus : int
    {
        CERT_TRUST_NO_ERROR                            = 0x00000000,
        CERT_TRUST_IS_NOT_TIME_VALID                   = 0x00000001,
        CERT_TRUST_IS_NOT_TIME_NESTED                  = 0x00000002,
        CERT_TRUST_IS_REVOKED                          = 0x00000004,
        CERT_TRUST_IS_NOT_SIGNATURE_VALID              = 0x00000008,
        CERT_TRUST_IS_NOT_VALID_FOR_USAGE              = 0x00000010,
        CERT_TRUST_IS_UNTRUSTED_ROOT                   = 0x00000020,
        CERT_TRUST_REVOCATION_STATUS_UNKNOWN           = 0x00000040,
        CERT_TRUST_IS_CYCLIC                           = 0x00000080,

        CERT_TRUST_INVALID_EXTENSION                   = 0x00000100,
        CERT_TRUST_INVALID_POLICY_CONSTRAINTS          = 0x00000200,
        CERT_TRUST_INVALID_BASIC_CONSTRAINTS           = 0x00000400,
        CERT_TRUST_INVALID_NAME_CONSTRAINTS            = 0x00000800,
        CERT_TRUST_HAS_NOT_SUPPORTED_NAME_CONSTRAINT   = 0x00001000,
        CERT_TRUST_HAS_NOT_DEFINED_NAME_CONSTRAINT     = 0x00002000,
        CERT_TRUST_HAS_NOT_PERMITTED_NAME_CONSTRAINT   = 0x00004000,
        CERT_TRUST_HAS_EXCLUDED_NAME_CONSTRAINT        = 0x00008000,

        CERT_TRUST_IS_OFFLINE_REVOCATION               = 0x01000000,
        CERT_TRUST_NO_ISSUANCE_CHAIN_POLICY            = 0x02000000,
        CERT_TRUST_IS_EXPLICIT_DISTRUST                = 0x04000000,
        CERT_TRUST_HAS_NOT_SUPPORTED_CRITICAL_EXT      = 0x08000000,
        CERT_TRUST_HAS_WEAK_SIGNATURE                  = 0x00100000,

        // These can be applied to chains only
        CERT_TRUST_IS_PARTIAL_CHAIN                    = 0x00010000,
        CERT_TRUST_CTL_IS_NOT_TIME_VALID               = 0x00020000,
        CERT_TRUST_CTL_IS_NOT_SIGNATURE_VALID          = 0x00040000,
        CERT_TRUST_CTL_IS_NOT_VALID_FOR_USAGE          = 0x00080000,
    }

    [Flags]
    internal enum CertTrustInfoStatus : int
    {
        // These can be applied to certificates only
        CERT_TRUST_HAS_EXACT_MATCH_ISSUER      = 0x00000001,
        CERT_TRUST_HAS_KEY_MATCH_ISSUER        = 0x00000002,
        CERT_TRUST_HAS_NAME_MATCH_ISSUER       = 0x00000004,
        CERT_TRUST_IS_SELF_SIGNED              = 0x00000008,

        // These can be applied to certificates and chains
        CERT_TRUST_HAS_PREFERRED_ISSUER        = 0x00000100,
        CERT_TRUST_HAS_ISSUANCE_CHAIN_POLICY   = 0x00000200,
        CERT_TRUST_HAS_VALID_NAME_CONSTRAINTS  = 0x00000400,

        // These can be applied to chains only
        CERT_TRUST_IS_COMPLEX_CHAIN            = 0x00010000,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CERT_TRUST_STATUS
    {
        public CertTrustErrorStatus dwErrorStatus;
        public CertTrustInfoStatus dwInfoStatus;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CERT_CHAIN_ELEMENT
    {
        public int cbSize;
        public CERT_CONTEXT* pCertContext;
        public CERT_TRUST_STATUS TrustStatus;
        public IntPtr pRevocationInfo;
        public IntPtr pIssuanceUsage;
        public IntPtr pApplicationUsage;
        public IntPtr pwszExtendedErrorInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CERT_SIMPLE_CHAIN
    {
        public int cbSize;
        public CERT_TRUST_STATUS TrustStatus;
        public int cElement;
        public CERT_CHAIN_ELEMENT** rgpElement;
        public IntPtr pTrustListInfo;

        // fHasRevocationFreshnessTime is only set if we are able to retrieve
        // revocation information for all elements checked for revocation.
        // For a CRL its CurrentTime - ThisUpdate.
        //
        // dwRevocationFreshnessTime is the largest time across all elements
        // checked.
        public int fHasRevocationFreshnessTime;
        public int dwRevocationFreshnessTime;    // seconds
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CERT_CHAIN_CONTEXT
    {
        public int cbSize;
        public CERT_TRUST_STATUS TrustStatus;
        public int cChain;
        public CERT_SIMPLE_CHAIN** rgpChain;

        // Following is returned when CERT_CHAIN_RETURN_LOWER_QUALITY_CONTEXTS
        // is set in dwFlags
        public int cLowerQualityChainContext;
        public CERT_CHAIN_CONTEXT** rgpLowerQualityChainContext;

        // fHasRevocationFreshnessTime is only set if we are able to retrieve
        // revocation information for all elements checked for revocation.
        // For a CRL its CurrentTime - ThisUpdate.
        //
        // dwRevocationFreshnessTime is the largest time across all elements
        // checked.
        public int fHasRevocationFreshnessTime;
        public int dwRevocationFreshnessTime;    // seconds

        // Flags passed when created via CertGetCertificateChain
        public int dwCreateFlags;

        // Following is updated with unique Id when the chain context is logged.
        public Guid ChainId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CERT_CHAIN_POLICY_PARA
    {
        public int cbSize;
        public int dwFlags;
        public IntPtr pvExtraPolicyPara;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CERT_CHAIN_POLICY_STATUS
    {
        public int cbSize;
        public int dwError;
        public IntPtr lChainIndex;
        public IntPtr lElementIndex;
        public IntPtr pvExtraPolicyStatus;
    }

    internal enum ChainPolicy : int
    {
        // Predefined verify chain policies
        CERT_CHAIN_POLICY_BASE = 1,
    }

    internal enum CryptAcquireFlags : int
    {
        CRYPT_ACQUIRE_ONLY_NCRYPT_KEY_FLAG = 0x00040000,
    }
}


