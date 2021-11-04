// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Internal.Cryptography.Pal.Native
{
    internal enum CertStoreProvider : int
    {
        CERT_STORE_PROV_MEMORY = 2,
        CERT_STORE_PROV_SYSTEM_W = 10,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CMSG_SIGNER_INFO_Partial  // This is not the full definition of CMSG_SIGNER_INFO. Only defining the part we use.
    {
        public int dwVersion;
        public Interop.Crypt32.DATA_BLOB Issuer;
        public Interop.Crypt32.DATA_BLOB SerialNumber;
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
        public Interop.Crypt32.FILETIME* pftCacheResync;
        public int pStrongSignPara;
        public int dwStrongSignFlags;
    }

    [Flags]
    internal enum CertChainFlags : int
    {
        None                                           = 0x00000000,
        CERT_CHAIN_DISABLE_AUTH_ROOT_AUTO_UPDATE       = 0x00000100,
        CERT_CHAIN_DISABLE_AIA                         = 0x00002000,
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
        public Interop.Crypt32.DATA_BLOB p;
        public Interop.Crypt32.DATA_BLOB q;
        public Interop.Crypt32.DATA_BLOB g;
    }

    internal enum PubKeyMagic : int
    {
        DSS_MAGIC = 0x31535344,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CERT_BASIC_CONSTRAINTS_INFO
    {
        public Interop.Crypt32.CRYPT_BIT_BLOB SubjectType;
        public int fPathLenConstraint;
        public int dwPathLenConstraint;
        public int cSubtreesConstraint;
        public Interop.Crypt32.DATA_BLOB* rgSubtreesConstraint; // PCERT_NAME_BLOB

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
        public Interop.Crypt32.DATA_BLOB Value;
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
        public Interop.Crypt32.CERT_CONTEXT* pCertContext;
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

    [Flags]
    internal enum ChainEngineConfigFlags : int
    {
        CERT_CHAIN_CACHE_END_CERT = 0x00000001,
        CERT_CHAIN_CACHE_ONLY_URL_RETRIEVAL = 0x00000004,
        CERT_CHAIN_USE_LOCAL_MACHINE_STORE = 0x00000008,
        CERT_CHAIN_ENABLE_CACHE_AUTO_UPDATE = 0x00000010,
        CERT_CHAIN_ENABLE_SHARE_STORE = 0x00000020,
        CERT_CHAIN_DISABLE_AIA = 0x00002000,
    }

    // Windows 7 definition of the struct
    [StructLayout(LayoutKind.Sequential)]
    internal struct CERT_CHAIN_ENGINE_CONFIG
    {
        public int cbSize;
        public IntPtr hRestrictedRoot;
        public IntPtr hRestrictedTrust;
        public IntPtr hRestrictedOther;
        public int cAdditionalStore;
        public IntPtr rghAdditionalStore;
        public ChainEngineConfigFlags dwFlags;
        public int dwUrlRetrievalTimeout;
        public int MaximumCachedCertificates;
        public int CycleDetectionModulus;
        public IntPtr hExclusiveRoot;
        public IntPtr hExclusiveTrustedPeople;
    }

    [Flags]
    internal enum CryptImportPublicKeyInfoFlags
    {
        NONE = 0,
        CRYPT_OID_INFO_PUBKEY_ENCRYPT_KEY_FLAG = 0x40000000,
    }
}
