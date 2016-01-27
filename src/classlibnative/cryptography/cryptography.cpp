// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: Cryptography.cpp
// 

// 
// Native method implementations and helper code for supporting CAPI based operations 
//---------------------------------------------------------------------------



#include "common.h"

#include "field.h"
#include "cryptography.h"

#if defined(FEATURE_CRYPTO) || defined(FEATURE_LEGACYNETCFCRYPTO)
const BYTE g_rgbPrivKey[] =
{
    0x07, 0x02, 0x00, 0x00, 0x00, 0xA4, 0x00, 0x00,
    0x52, 0x53, 0x41, 0x32, 0x00, 0x02, 0x00, 0x00,
    0x01, 0x00, 0x00, 0x00, 0xAB, 0xEF, 0xFA, 0xC6,
    0x7D, 0xE8, 0xDE, 0xFB, 0x68, 0x38, 0x09, 0x92,
    0xD9, 0x42, 0x7E, 0x6B, 0x89, 0x9E, 0x21, 0xD7,
    0x52, 0x1C, 0x99, 0x3C, 0x17, 0x48, 0x4E, 0x3A,
    0x44, 0x02, 0xF2, 0xFA, 0x74, 0x57, 0xDA, 0xE4,
    0xD3, 0xC0, 0x35, 0x67, 0xFA, 0x6E, 0xDF, 0x78,
    0x4C, 0x75, 0x35, 0x1C, 0xA0, 0x74, 0x49, 0xE3,
    0x20, 0x13, 0x71, 0x35, 0x65, 0xDF, 0x12, 0x20,
    0xF5, 0xF5, 0xF5, 0xC1, 0xED, 0x5C, 0x91, 0x36,
    0x75, 0xB0, 0xA9, 0x9C, 0x04, 0xDB, 0x0C, 0x8C,
    0xBF, 0x99, 0x75, 0x13, 0x7E, 0x87, 0x80, 0x4B,
    0x71, 0x94, 0xB8, 0x00, 0xA0, 0x7D, 0xB7, 0x53,
    0xDD, 0x20, 0x63, 0xEE, 0xF7, 0x83, 0x41, 0xFE,
    0x16, 0xA7, 0x6E, 0xDF, 0x21, 0x7D, 0x76, 0xC0,
    0x85, 0xD5, 0x65, 0x7F, 0x00, 0x23, 0x57, 0x45,
    0x52, 0x02, 0x9D, 0xEA, 0x69, 0xAC, 0x1F, 0xFD,
    0x3F, 0x8C, 0x4A, 0xD0,

    0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

    0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

    0x64, 0xD5, 0xAA, 0xB1,
    0xA6, 0x03, 0x18, 0x92, 0x03, 0xAA, 0x31, 0x2E,
    0x48, 0x4B, 0x65, 0x20, 0x99, 0xCD, 0xC6, 0x0C,
    0x15, 0x0C, 0xBF, 0x3E, 0xFF, 0x78, 0x95, 0x67,
    0xB1, 0x74, 0x5B, 0x60,

    0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
};

const BYTE g_rgbSymKey[] = 
{
    0x01, 0x02, 0x00, 0x00, 0x02, 0x66, 0x00, 0x00,
    0x00, 0xA4, 0x00, 0x00, 0xAD, 0x89, 0x5D, 0xDA,
    0x82, 0x00, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12,
    0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12,
    0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12,
    0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12,
    0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12,
    0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12,
    0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12,
    0x12, 0x12, 0x02, 0x00
};

const BYTE g_rgbPubKey[] = 
{
    0x06, 0x02, 0x00, 0x00, 0x00, 0xa4, 0x00, 0x00,
    0x52, 0x53, 0x41, 0x31, 0x00, 0x02, 0x00, 0x00,
    0x01, 0x00, 0x00, 0x00, 0xab, 0xef, 0xfa, 0xc6,
    0x7d, 0xe8, 0xde, 0xfb, 0x68, 0x38, 0x09, 0x92,
    0xd9, 0x42, 0x7e, 0x6b, 0x89, 0x9e, 0x21, 0xd7,
    0x52, 0x1c, 0x99, 0x3c, 0x17, 0x48, 0x4e, 0x3a,
    0x44, 0x02, 0xf2, 0xfa, 0x74, 0x57, 0xda, 0xe4,
    0xd3, 0xc0, 0x35, 0x67, 0xfa, 0x6e, 0xdf, 0x78,
    0x4c, 0x75, 0x35, 0x1c, 0xa0, 0x74, 0x49, 0xe3,
    0x20, 0x13, 0x71, 0x35, 0x65, 0xdf, 0x12, 0x20,
    0xf5, 0xf5, 0xf5, 0xc1
};


ProviderCache *ProviderCache::s_pCache = NULL;

//---------------------------------------------------------------------------------------
//
// Associate a default CSP name with a CSP type
//
// Arguments:
//    dwType      - type of CSP to associate the default name with
//    pwzProvider - name of the default CSP for dwType
//
// Notes:
//    Can throw an OOM if this is the first call into the method and
//    the underlying cache has yet to be allocated. See ProviderCache::InternalCacheProvider
//    for other details.

// static
void ProviderCache::CacheProvider(DWORD dwType, __in_z LPWSTR pwzProvider)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pwzProvider != NULL);
    }
    CONTRACTL_END;

    if (s_pCache == NULL)
    {
        NewHolder<ProviderCache> cacheHolder(new ProviderCache());
        LPVOID pvExchange = InterlockedCompareExchangeT(&s_pCache,
                                                        cacheHolder.GetValue(),
                                                        NULL);
        if (pvExchange == NULL)
            cacheHolder.SuppressRelease();
    }

    s_pCache->InternalCacheProvider(dwType, pwzProvider);
}

//---------------------------------------------------------------------------------------
//
// Get the CSP name associated with the CSP type
//
// Arguments:
//    dwType - type of CSP to lookup the name of
//
// Return Value:
//    Name of the CSP if it is cached, NULL if there is no association yet

// static
LPCWSTR ProviderCache::GetProvider(DWORD dwType)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (s_pCache != NULL)
        return s_pCache->InternalGetProvider(dwType);
    else
        return NULL;
}

//---------------------------------------------------------------------------------------
//
// Initialize the CSP cache
//
// Notes:
//    Can throw an OOM the hashtable could not be setup properly
//

ProviderCache::ProviderCache()
    : m_crstCache(CrstCSPCache)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    LockOwner lockOwner = { &m_crstCache, IsOwnerOfCrst };
    if (!m_htCache.Init(MaxWindowsProviderType, &lockOwner))
        COMPlusThrowOM();
}

//---------------------------------------------------------------------------------------
//
// Associate a default CSP name with a CSP type
//
// Arguments:
//    dwType      - type of CSP to associate the default name with
//    pwzProvider - name of the default CSP for dwType
//

void ProviderCache::InternalCacheProvider(DWORD dwType, __in_z LPWSTR pwzProvider)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
        PRECONDITION(pwzProvider != NULL);
    }
    CONTRACTL_END;

    CrstHolder lockHolder(&m_crstCache);
    if (GetProvider(dwType) == NULL)
        m_htCache.InsertValue(dwType, reinterpret_cast<HashDatum>(pwzProvider));
}

//---------------------------------------------------------------------------------------
//
// Get the CSP name associated with the CSP type
//
// Arguments:
//    dwType - type of CSP to lookup the name of
//
// Return Value:
//    Name of the CSP if it is cached, NULL if there is no association yet

LPCWSTR ProviderCache::InternalGetProvider(DWORD dwType)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    HashDatum datum;
    if (!m_htCache.GetValue(dwType, &datum))
        return NULL;

    _ASSERTE(datum != NULL);
    return reinterpret_cast<LPCWSTR>(datum);
}
#endif // FEATURE_CRYPTO

#if defined(FEATURE_X509) || defined(FEATURE_CRYPTO) || defined(FEATURE_LEGACYNETCFCRYPTO)
//
// Throw a runtime exception based on the HRESULT passed in.
//

void CryptoHelper::COMPlusThrowCrypto(HRESULT hr) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    GCX_COOP();

    MethodDescCallSite throwMethod(METHOD__CRYPTO_EXCEPTION__THROW);

    ARG_SLOT args[] = {
        (ARG_SLOT) hr
    };
    throwMethod.Call(args);
}

BOOL CryptoHelper::WszCryptAcquireContext_SO_TOLERANT (HCRYPTPROV *phProv, LPCWSTR pwszContainer, LPCWSTR pwszProvider, DWORD dwProvType, DWORD dwFlags)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_PREEMPTIVE;
    STATIC_CONTRACT_SO_INTOLERANT;

    BOOL fResult = FALSE;
    DWORD dwLastError = 0;

#ifdef FEATURE_CRYPTO
    // Specifying both verify context (for an ephemeral key) and machine keyset (for a persisted machine key)
    // does not make sense.  Additionally, Widows is beginning to lock down against uses of MACHINE_KEYSET
    // (for instance in the app container), even if verify context is present.   Therefore, if we're using
    // an ephemeral key, strip out MACHINE_KEYSET from the flags.
    if ((dwFlags & CRYPT_VERIFYCONTEXT) && (dwFlags & CRYPT_MACHINE_KEYSET))
    {
        dwFlags &= ~CRYPT_MACHINE_KEYSET;
    }
#endif // FEATURE_CRYPTO

    BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread())

    {
        LeaveRuntimeHolder lrh((size_t)::CryptAcquireContextW);
        fResult = WszCryptAcquireContext (phProv, pwszContainer, pwszProvider, dwProvType, dwFlags);
        if (!fResult)
            dwLastError = ::GetLastError();
    }

    END_SO_TOLERANT_CODE_CALLING_HOST;

    // END_SO_TOLERANT_CODE overwrites lasterror. Let's reset it.
    ::SetLastError(dwLastError);
    return fResult;
}

//
// Helper method to get a Unicode copy of a managed string object. The Unicode string has to be freed with delete [].
//

WCHAR* CryptoHelper::STRINGREFToUnicode (STRINGREF s) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(s != NULL);
    } CONTRACTL_END;

    int cchUnicodeChar = s->GetStringLength();
    WCHAR* pwszUnicode = new WCHAR[cchUnicodeChar + 1];
    memcpy (pwszUnicode, s->GetBuffer(), cchUnicodeChar * sizeof(WCHAR));
    pwszUnicode[cchUnicodeChar] = W('\0');

    return pwszUnicode;
}
#endif // FEATURE_X509 || FEATURE_CRYPTO

#if defined(FEATURE_CRYPTO) || defined(FEATURE_LEGACYNETCFCRYPTO)
//
// Helper method to generate a random key container name. 
// The caller is responsible for freeing the memory allocated.
//

WCHAR* CryptoHelper::GetRandomKeyContainer() {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    GUID guid;
    HRESULT hr = CoCreateGuid(&guid);
    if (hr != S_OK)
        COMPlusThrowHR(hr);

    WCHAR* pwszKeyContainerName = new WCHAR[50];
    memcpy(pwszKeyContainerName, W("CLR"), 4 * sizeof(WCHAR));

    if (GuidToLPWSTR(guid, &pwszKeyContainerName[3], 45) == 0) {
        DWORD lastError = GetLastError();
        delete [] pwszKeyContainerName;
        COMPlusThrowHR(HRESULT_FROM_WIN32(lastError));
    }

    return pwszKeyContainerName;
}
#endif // FEATURE_CRYPTO

#if defined(FEATURE_CRYPTO) || defined(FEATURE_LEGACYNETCFCRYPTO) || defined(FEATURE_X509)
//
// Helper method to get a Unicode string from an ANSI string. The Unicode string has to be freed with delete [].
//

WCHAR* CryptoHelper::AnsiToUnicode (__in_z char* pszAnsi) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    int cchUnicodeChar = WszMultiByteToWideChar(CP_ACP,
                                         0,
                                         pszAnsi,
                                         -1,
                                         NULL,
                                         0);
    if (cchUnicodeChar == 0)
        COMPlusThrowWin32();

    WCHAR* pwszUnicode = new WCHAR[cchUnicodeChar];
    cchUnicodeChar = WszMultiByteToWideChar(CP_ACP,
                             0,
                             pszAnsi,
                             -1,
                             pwszUnicode,
                             cchUnicodeChar);
    if (cchUnicodeChar == 0) {
        DWORD lastError = GetLastError();
        delete [] pwszUnicode;
        COMPlusThrowHR(HRESULT_FROM_WIN32(lastError));
    }

    return pwszUnicode;
}

//
// Helper method to construct a managed array from an unamanged byte array. The array ref has to be protected.
//

void CryptoHelper::ByteArrayToU1ARRAYREF (LPBYTE pb, DWORD cb, U1ARRAYREF* u1) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pb));
    } CONTRACTL_END;

    OBJECTREF array = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cb);
    SetObjectReference((OBJECTREF*) u1, array, NULL);
    memcpyNoGCRefs((*u1)->GetDirectPointerToNonObjectElements(), pb, cb);
}
#endif // FEATURE_CRYPTO || FEATURE_X509

#if defined(FEATURE_CRYPTO) || defined(FEATURE_LEGACYNETCFCRYPTO)
//
// memrev
//

inline void CryptoHelper::memrev(LPBYTE pb, DWORD cb) {
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    BYTE b;
    LPBYTE pbEnd = pb+cb-1;
    LPBYTE pbStart = pb;

    for (DWORD i=0; i<cb/2; i++, pbStart++, pbEnd--) {
        b = *pbStart;
        *pbStart = *pbEnd;
        *pbEnd = b;
    }
}
#endif // FEATURE_CRYPTO

#if defined(FEATURE_CRYPTO) || defined(FEATURE_LEGACYNETCFCRYPTO) || defined(FEATURE_X509)
//
// Helper method to construct a byte array from a managed array ref. The unmanaged byte array pointer has to freed with delete [].
//

BYTE* CryptoHelper::U1ARRAYREFToByteArray (U1ARRAYREF u1) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(u1 != NULL);
    } CONTRACTL_END;

    BYTE* pb = new BYTE[u1->GetNumComponents()];
    memcpy(pb, (LPBYTE) u1->GetDirectPointerToNonObjectElements(), u1->GetNumComponents());

    return pb;
}

//
// Helper method to get an ANSI string from a Unicode string. The ANSI string has to be freed with delete [].
//

char* CryptoHelper::UnicodeToAnsi (__in_z WCHAR* pwszUnicode) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    int cchAnsiChar;
    cchAnsiChar = WszWideCharToMultiByte(CP_ACP,
                                  0,
                                  pwszUnicode,
                                  -1,
                                  NULL,
                                  0,
                                  NULL,
                                  NULL);
    if (cchAnsiChar == 0)
        COMPlusThrowWin32();

    char* pszAnsi = new char[cchAnsiChar];
    cchAnsiChar = WszWideCharToMultiByte(CP_ACP,
                                  0,
                                  pwszUnicode,
                                  -1,
                                  pszAnsi,
                                  cchAnsiChar,
                                  NULL,
                                  NULL);
    if (cchAnsiChar == 0) {
        DWORD lastError = GetLastError();
        delete [] pszAnsi;
        COMPlusThrowHR(HRESULT_FROM_WIN32(lastError));
    }

    return pszAnsi;
}
#endif // FEATURE_CRYPTO || FEATURE_X509

#if defined(FEATURE_CRYPTO) || defined(FEATURE_LEGACYNETCFCRYPTO)
BOOL CryptoHelper::CryptGenKey_SO_TOLERANT (HCRYPTPROV hProv, ALG_ID Algid, DWORD dwFlags, HCRYPTKEY* phKey)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_PREEMPTIVE;
    STATIC_CONTRACT_SO_INTOLERANT;

    BOOL fResult = FALSE;
    DWORD dwLastError = 0;

    BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread())

    fResult = CryptGenKey (hProv, Algid, dwFlags, phKey);
    if (!fResult)
        dwLastError = ::GetLastError();

    END_SO_TOLERANT_CODE_CALLING_HOST;

    // END_SO_TOLERANT_CODE overwrites lasterror. Let's reset it.
    ::SetLastError(dwLastError);
    return fResult;
}

//
// Check to see if a better CSP than the one requested is available
// DSS providers are supersets of each other in the following order:
//    1. MS_ENH_DSS_DH_PROV
//    2. MS_DEF_DSS_DH_PROV
//
// This will return the best provider which is a superset of wszProvider,
// or NULL if there is no upgrade available on the machine.
//
LPCWSTR CryptoHelper::UpgradeDSS(DWORD dwProvType, __in_z LPCWSTR wszProvider)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(dwProvType == PROV_DSS_DH);
        PRECONDITION(wszProvider != NULL);
    }
    CONTRACTL_END;

    LPCWSTR wszUpgrade = NULL;
    HandleCSPHolder hProv = NULL;

    if (wcscmp(wszProvider, MS_DEF_DSS_DH_PROV_W) == 0)
    {
        // If this is the base DSS/DH provider, see if we can use the enhanced provider instead.
        if (CryptoHelper::WszCryptAcquireContext_SO_TOLERANT(&hProv, NULL, MS_ENH_DSS_DH_PROV_W, dwProvType, CRYPT_VERIFYCONTEXT))
            wszUpgrade = MS_ENH_DSS_DH_PROV_W;
    }

    return wszUpgrade;
}

//
// Check to see if a better CSP than the one requested is available
// RSA providers are supersets of each other in the following order:
//    1. MS_ENH_RSA_AES_PROV
//    2. MS_ENHANCED_PROV
//    3. MS_DEF_PROV
//
// This will return the best provider which is a superset of wszProvider,
// or NULL if there is no upgrade available on the machine.
//
LPCWSTR CryptoHelper::UpgradeRSA(DWORD dwProvType, __in_z LPCWSTR wszProvider)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(dwProvType == PROV_RSA_FULL);
        PRECONDITION(wszProvider != NULL);
    }
    CONTRACTL_END;

    bool requestedEnhanced = wcscmp(wszProvider, MS_ENHANCED_PROV_W) == 0;
    bool requestedBase = wcscmp(wszProvider, MS_DEF_PROV_W) == 0;

    LPCWSTR wszUpgrade = NULL;
    HandleCSPHolder hProv = NULL;

    if (requestedBase || requestedEnhanced)
    {
        // attempt to use the AES provider
        if (CryptoHelper::WszCryptAcquireContext_SO_TOLERANT(&hProv, NULL, MS_ENH_RSA_AES_PROV_W, dwProvType, CRYPT_VERIFYCONTEXT))
            wszUpgrade = MS_ENH_RSA_AES_PROV_W;
    }
    else if (wszUpgrade == NULL && requestedBase)
    {
        // if AES wasn't available and we requested the base CSP, try the enhanced one
        if (CryptoHelper::WszCryptAcquireContext_SO_TOLERANT(&hProv, NULL, MS_ENHANCED_PROV_W, dwProvType, CRYPT_VERIFYCONTEXT))
            wszUpgrade = MS_ENHANCED_PROV_W;
    }

    return wszUpgrade;
}

//
// WARNING: This function side-effects its first argument (hProv)
// MSProviderCryptImportKey does an "exponent-of-one" import of specified
// symmetric key material into a CSP. However, it clobbers any exchange key pair
// already in hProv.
//

HRESULT COMCryptography::ExponentOfOneImport (HCRYPTPROV hProv,
                                              LPBYTE     rgbKeyMaterial,
                                              DWORD      cbKeyMaterial,
                                              DWORD      dwKeyAlg,
                                              DWORD      dwFlags,
                                              HCRYPTKEY* phKey) {
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    HRESULT hr = S_OK;

    LPBYTE       pb = NULL;
    BLOBHEADER * pbhdr = NULL;
    BYTE         rgb[sizeof(g_rgbSymKey)];

    // Do this check here as a sanity check to avoid buffer overruns
    // variable bufSize used to allow for overflow.
    DWORD bufSize= cbKeyMaterial + sizeof(ALG_ID) + sizeof(BLOBHEADER);
    if (bufSize < cbKeyMaterial || bufSize >= sizeof(g_rgbSymKey))
        return E_FAIL;

    memcpy(rgb, g_rgbSymKey, sizeof(g_rgbSymKey));

    pbhdr = (BLOBHEADER *) rgb;
    pbhdr->aiKeyAlg = dwKeyAlg;
    pb = &rgb[sizeof(*pbhdr)];
    *((ALG_ID *) pb) = CALG_RSA_KEYX;

    pb += sizeof(ALG_ID);
    for (DWORD i=0; i<cbKeyMaterial; i++)
        pb[cbKeyMaterial-i-1] = rgbKeyMaterial[i];
    pb[cbKeyMaterial] = 0;

    HandleKeyHolder hPrivKey(NULL);
    if (!CryptImportKey(hProv, g_rgbPrivKey, sizeof(g_rgbPrivKey), 0, 0, &hPrivKey)) {
        hr = HRESULT_FROM_GetLastError();
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: COMCryptography::ExponentOfOneImport --> CryptImportKey failed.\n", hr));
    }

    if (!CryptImportKey(hProv, rgb, sizeof(rgb), hPrivKey, dwFlags, phKey)) {
        hr = HRESULT_FROM_GetLastError();
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: COMCryptography::ExponentOfOneImport --> CryptImportKey failed.\n", hr));
    }

    return hr;
}

HRESULT COMCryptography::PlainTextKeyBlobImport (HCRYPTPROV hProv,
                                                 LPBYTE     rgbKeyMaterial,
                                                 DWORD      cbKeyMaterial,
                                                 DWORD      dwKeyAlg,
                                                 DWORD      dwFlags,
                                                 HCRYPTKEY* phKey) {
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    HRESULT hr = S_OK;

    DWORD cb = cbKeyMaterial + sizeof(DWORD) + sizeof(BLOBHEADER);
    NewArrayHolder<BYTE> pbHolder(new BYTE[cb]);
    LPBYTE pb = (LPBYTE) pbHolder.GetValue();

    BLOBHEADER * pbhdr = (BLOBHEADER *) pb;
    pbhdr->bType = PLAINTEXTKEYBLOB;
    pbhdr->bVersion = CUR_BLOB_VERSION;
    pbhdr->reserved = 0x0000;
    pbhdr->aiKeyAlg = dwKeyAlg;

    pb += sizeof(*pbhdr);
    *((DWORD *) pb) = cbKeyMaterial;
    pb += sizeof(DWORD);
    memcpy(pb, rgbKeyMaterial, cbKeyMaterial);

    if (!CryptImportKey(hProv, pbHolder, cb, 0, dwFlags, phKey)) {
        hr = HRESULT_FROM_GetLastError();
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: COMCryptography::PlainTextKeyBlobImport --> CryptImportKey failed.\n", hr));
    }

    return hr;
}

HRESULT COMCryptography::LoadKey (LPBYTE     rgbKeyMaterial,
                                  DWORD      cbKeyMaterial,
                                  HCRYPTPROV hprov,
                                  DWORD      dwCalg,
                                  DWORD      dwFlags,
                                  HCRYPTKEY* phkey) {
    WRAPPER_NO_CONTRACT;

    HRESULT hr = PlainTextKeyBlobImport(hprov, rgbKeyMaterial, cbKeyMaterial, dwCalg, dwFlags, phkey);
    if (FAILED(hr))
        hr = ExponentOfOneImport(hprov, rgbKeyMaterial, cbKeyMaterial, dwCalg, dwFlags, phkey);
    return hr;
}

//
// WARNING: This function side-effects its first argument (hProv)
//

HRESULT COMCryptography::UnloadKey(HCRYPTPROV hprov,
                                   HCRYPTKEY  hkey,
                                   LPBYTE*    ppb,
                                   DWORD*     pcb) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    DWORD cbOut = 0;
    HandleKeyHolder hPubKey(NULL);

    HRESULT hr = S_OK;
    if (!CryptImportKey(hprov, g_rgbPubKey, sizeof(g_rgbPubKey), 0, 0, &hPubKey)) {
        hr = HRESULT_FROM_GetLastError();
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: COMCryptography::UnloadKey --> CryptImportKey failed.\n", hr));
        return hr;
    }

    if (!CryptExportKey(hkey, hPubKey, SIMPLEBLOB, 0, NULL, &cbOut)) {
        hr = HRESULT_FROM_GetLastError();
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: COMCryptography::UnloadKey --> CryptExportKey failed.\n", hr));
        return hr;
    }

    NewArrayHolder<BYTE> pbOut(new BYTE[cbOut]);
    if (!CryptExportKey(hkey, hPubKey, SIMPLEBLOB, 0, pbOut, &cbOut)) {
        hr = HRESULT_FROM_GetLastError();
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: COMCryptography::UnloadKey --> CryptExportKey failed.\n", hr));
        return hr;
    }

    // Get size of the item
    LPBYTE pb2 = pbOut + sizeof(BLOBHEADER) + sizeof(DWORD);
    DWORD i= cbOut - sizeof(BLOBHEADER) - sizeof(DWORD) - 2;
    if (i >= cbOut) {
        // integer overflow
        return E_FAIL;
    }
    while (i > 0) {
        if (pb2[i] == 0)
            break;
        i--;
    }

    // Now allocate the return buffer
    *ppb = new BYTE[i];
    
    memcpy(*ppb, pb2, i);
    CryptoHelper::memrev(*ppb, i);
    *pcb = i;

    return hr;
}

//
//  GetDefaultProvider
//
//  Description:
//      Find the default provider name to be used in the case that we
//      were not actually passed in a provider name. The main purpose
//      of this code is really to deal with the enhanched/default provider
//      problems given to us by CAPI.
//
//  Returns:
//      name of the provider to be used.
//

LPCWSTR COMCryptography::GetDefaultProvider(DWORD dwType)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // If we have already gotten a name for this provider type, then just return it.
    LPCWSTR pwszCached = ProviderCache::GetProvider(dwType);
    if (pwszCached != NULL)
        return pwszCached;

    // figure out how big the provider name is
    DWORD cbProviderName = 0;
    if (!WszCryptGetDefaultProvider(dwType, NULL, CRYPT_MACHINE_DEFAULT, NULL, &cbProviderName))
    {
        DWORD dwLastError = GetLastError();
        LOG((LF_SECURITY, LL_INFO10, "Error [%#x]: CryptGetDefaultProvider(%d)", dwLastError, dwType));
        return NULL;
    }

    // get the CSP name from CAPI
    NewArrayHolder<WCHAR> pwszProviderName(new WCHAR[cbProviderName]);
    if (!WszCryptGetDefaultProvider(dwType, NULL, CRYPT_MACHINE_DEFAULT, pwszProviderName, &cbProviderName))
    {
        DWORD dwLastError = GetLastError();
        LOG((LF_SECURITY, LL_INFO10, "Error [%#x]: CryptGetDefaultProvider(%d)", dwLastError, dwType));
        return NULL;
    }

    {
        GCX_PREEMP();

        // check to see if there are upgrades available for the requested CSP
        LPCWSTR wszUpgrade = NULL;
        if (dwType == PROV_RSA_FULL)
            wszUpgrade = CryptoHelper::UpgradeRSA(dwType, pwszProviderName);
        else if (dwType == PROV_DSS_DH)
            wszUpgrade = CryptoHelper::UpgradeDSS(dwType, pwszProviderName);

        if (wszUpgrade != NULL)
        {
            LOG((LF_SECURITY, LL_INFO10, "Upgrading from CSP %s to CSP %s", pwszProviderName, wszUpgrade));

            pwszProviderName.Release();
            const size_t cchProvider = wcslen(wszUpgrade) + 1;
            pwszProviderName = new WCHAR[cchProvider];
            wcscpy_s(pwszProviderName, cchProvider, wszUpgrade);
        }
    }

    ProviderCache::CacheProvider(dwType, pwszProviderName);
    pwszProviderName.SuppressRelease();

    LOG((LF_SECURITY, LL_INFO100, "Using CSP %s as default for CSP type %d", pwszProviderName, dwType));
    return pwszProviderName;
}

// converts a big-endian byte array to a DWORD value
inline DWORD COMCryptography::ConvertByteArrayToDWORD (LPBYTE pb, DWORD cb) {
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION((cb <= 4 && cb >= 0));
    } CONTRACTL_END;
    
    DWORD dwOutput = 0;
    for (DWORD i = 0; i < cb; i++) {
        dwOutput = dwOutput << 8;
        dwOutput += pb[i];
    }
    return dwOutput;
}

// output of this routine is always big endian
inline void COMCryptography::ConvertIntToByteArray(DWORD dwInput, LPBYTE * ppb, DWORD * pcb) {
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_INTOLERANT;
    } CONTRACTL_END;

    if (dwInput == 0) {
        *ppb = new BYTE[1];
        (*ppb)[0] = 0;
        return;
    }

    *ppb = new BYTE[4]; 

    DWORD t1 = dwInput; // t1 is remaining value to account for
    DWORD t2; // t2 is (t1 % 256) & 0xFF
    DWORD i = 0;

    while (t1 > 0) {
        t2 = (t1 % 256) & 0xFF;
        (*ppb)[i] = static_cast<BYTE>(t2);
        t1 = (t1 - t2) >> 8;
        i++;
    }

    *pcb = i;
    CryptoHelper::memrev(*ppb, i);
}

// Maps CspProviderFlags enumeration into CAPI flags.
DWORD COMCryptography::MapCspKeyFlags (DWORD dwFlags) {
    DWORD dwCapiFlags = 0;
    if ((dwFlags & CSP_PROVIDER_FLAGS_USE_NON_EXPORTABLE_KEY) == 0)
        dwCapiFlags |= CRYPT_EXPORTABLE;
    if (dwFlags & CSP_PROVIDER_FLAGS_USE_ARCHIVABLE_KEY)
        dwCapiFlags |= CRYPT_ARCHIVABLE;
    if (dwFlags & CSP_PROVIDER_FLAGS_USE_USER_PROTECTED_KEY)
        dwCapiFlags |= CRYPT_USER_PROTECTED;

    return dwCapiFlags;
}

// Maps CspProviderFlags enumeration into CAPI flags.
DWORD COMCryptography::MapCspProviderFlags (DWORD dwFlags) {
    DWORD dwCapiFlags = 0;
    if (dwFlags & CSP_PROVIDER_FLAGS_USE_MACHINE_KEYSTORE)
        dwCapiFlags |= CRYPT_MACHINE_KEYSET;
    if (dwFlags & CSP_PROVIDER_FLAGS_USE_CRYPT_SILENT)
        dwCapiFlags |= CRYPT_SILENT;
    if (dwFlags & CSP_PROVIDER_FLAGS_CREATE_EPHEMERAL_KEY)
        dwCapiFlags |= CRYPT_VERIFYCONTEXT;

    return dwCapiFlags;
}

//
//  OpenCSP
//
//  Description:
//      OpenCSP performs the core work of opening and creating CSPs and
//      containers in CSPs.
//

HRESULT COMCryptography::OpenCSP(OBJECTREF * pSafeThis, DWORD dwFlags, CRYPT_PROV_CTX * pProvCtx) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    NewArrayHolder<WCHAR> pwszProviderHolder = NULL;    // if we need to allocate the CSP name ourselves
                                                        // store it here so it can be released
    LPCWSTR pwszProvider = NULL;                        // location where the CSP name will be read
                                                        // regardless of where we loaded it from

    NewArrayHolder<WCHAR> pwszContainer = NULL;

    //
    // Look for the provider type
    //

    FieldDesc * pFD = MscorlibBinder::GetField(FIELD__CSP_PARAMETERS__PROVIDER_TYPE);
    DWORD dwType = pFD->GetValue32(*pSafeThis);

    //
    // Look for the provider name
    //

    pFD = MscorlibBinder::GetField(FIELD__CSP_PARAMETERS__PROVIDER_NAME);

    OBJECTREF objref = pFD->GetRefValue(*pSafeThis);
    STRINGREF strProvider = ObjectToSTRINGREF(*(StringObject **) &objref);
    if (strProvider != NULL) {
        LPCWSTR pwsz = strProvider->GetBuffer();
        if ((pwsz != NULL) && (*pwsz != 0)) {
            pwszProviderHolder = CryptoHelper::STRINGREFToUnicode(strProvider);
            pProvCtx->m_fReleaseProvider = TRUE;
            pwszProvider = pwszProviderHolder;
        }
        else {
            pwszProvider = GetDefaultProvider(dwType);
            pProvCtx->m_fReleaseProvider = FALSE;
            STRINGREF str = StringObject::NewString(pwszProvider);
            pFD->SetRefValue(*pSafeThis, (OBJECTREF)str);
        }
    } else {
        pwszProvider = GetDefaultProvider(dwType);
        pProvCtx->m_fReleaseProvider = FALSE;
        STRINGREF str = StringObject::NewString(pwszProvider);
        pFD->SetRefValue(*pSafeThis, (OBJECTREF)str);
    }

    // look to see if the user specified that we should pass
    // CRYPT_MACHINE_KEYSET to CAPI to use machine key storage instead
    // of user key storage
    DWORD dwCspProviderFlags = 0;

    objref=NULL;
    GCPROTECT_BEGIN (objref);

    pFD = MscorlibBinder::GetField(FIELD__CSP_PARAMETERS__FLAGS);
    dwCspProviderFlags = pFD->GetValue32(*pSafeThis);

    // If the user specified CSP_PROVIDER_FLAGS_USE_DEFAULT_KEY_CONTAINER,
    // then ignore the container name and hand back the default container

    pFD = MscorlibBinder::GetField(FIELD__CSP_PARAMETERS__KEY_CONTAINER_NAME);
    if ((dwCspProviderFlags & CSP_PROVIDER_FLAGS_USE_DEFAULT_KEY_CONTAINER) == 0) {
        // Look for the key container name
        objref = pFD->GetRefValue(*pSafeThis);
        STRINGREF strContainer = ObjectToSTRINGREF(*(StringObject **) &objref);
        if (strContainer != NULL) {
            LPWSTR pwsz = strContainer->GetBuffer();
            if ((pwsz != NULL) && (*pwsz != 0))
                pwszContainer = CryptoHelper::STRINGREFToUnicode(strContainer);
        }
    }

    GCPROTECT_END ();

    // Go ahead and try to open the CSP.  If we fail, make sure the CSP
    // returned is 0 as that is going to be the error check in the caller.
    HandleCSPHolder hProv(NULL);
    {
        GCX_PREEMP();
        dwFlags |= MapCspProviderFlags(dwCspProviderFlags);

        if (!CryptoHelper::WszCryptAcquireContext_SO_TOLERANT(&hProv, pwszContainer, pwszProvider, dwType, dwFlags))
            return HRESULT_FROM_GetLastError();
    }

    // CRYPT_PROV_CTX takes ownership of these resources, and frees them in its Release
    hProv.SuppressRelease();
    pwszContainer.SuppressRelease();
    pwszProviderHolder.SuppressRelease();

    pProvCtx->m_hProv = hProv;
    pProvCtx->m_pwszContainer = pwszContainer;
    pProvCtx->m_pwszProvider = pwszProvider;
    pProvCtx->m_dwType = dwType;
    pProvCtx->m_dwFlags = dwFlags;

    // If we are using CRYPT_VERIFYCONTEXT this is an ephemeral key, so clear the persist flag
    if (dwFlags & CRYPT_VERIFYCONTEXT)
        pProvCtx->m_fPersistKeyInCsp = FALSE;

    return S_OK;
}

//
// FCALL functions
//

//
// Native method to open a CSP using CRYPT_VERIFYCONTEXT
//

FCIMPL2(void, COMCryptography::_AcquireCSP, Object* cspParametersUNSAFE, SafeHandle** hProvUNSAFE)
{
    FCALL_CONTRACT;

    OBJECTREF cspParameters = (OBJECTREF) cspParametersUNSAFE;
    SAFEHANDLE hProvSAFE = (SAFEHANDLE) *hProvUNSAFE;
    
    HELPER_METHOD_FRAME_BEGIN_2(cspParameters, hProvSAFE);

    //
    // We want to just open this CSP.  Passing in verify context will
    // open it and, if a container is given, map to open the container.
    //

    NewHolder<CRYPT_PROV_CTX> pProvCtx(new CRYPT_PROV_CTX());
    // protect the allocated structure with a holder
    HRESULT hr = OpenCSP(&cspParameters, CRYPT_VERIFYCONTEXT, pProvCtx);

    if (FAILED(hr)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: COMCryptography::OpenCSP failed.\n", hr));
        CryptoHelper::COMPlusThrowCrypto(hr);
    }

    // we never want to delete a key container when using CRYPT_VERIFYCONTEXT
    pProvCtx->m_fPersistKeyInCsp = TRUE;

    // Set the handle field
    hProvSAFE->SetHandle((void*) pProvCtx.GetValue());
    pProvCtx.SuppressRelease();

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

//
// This method opens an existing key container. 
// It returns FALSE if the container could not be found.
//

FCIMPL3(HRESULT, COMCryptography::_OpenCSP, Object* cspParametersUNSAFE, DWORD dwFlags, SafeHandle** hProvUNSAFE)
{
    FCALL_CONTRACT;

    HRESULT hr = S_OK;
    OBJECTREF cspParameters = (OBJECTREF) cspParametersUNSAFE;
    SAFEHANDLE hProvSAFE = (SAFEHANDLE) *hProvUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_2(cspParameters, hProvSAFE);

    NewHolder<CRYPT_PROV_CTX> pProvCtx(new CRYPT_PROV_CTX());
    // We never want to delete a key container if it's already there.
    pProvCtx->m_fPersistKeyInCsp = TRUE;

    hr = OpenCSP(&cspParameters, dwFlags, pProvCtx);
    if (SUCCEEDED(hr)) {
        // Set the handle field
        hProvSAFE->SetHandle((void*) pProvCtx.GetValue());
        pProvCtx.SuppressRelease();
    }

    HELPER_METHOD_FRAME_END();
    return hr;
}
FCIMPLEND

//
// Native method for calling a CSP to get random bytes.
//

void QCALLTYPE COMCryptography::GetBytes(CRYPT_PROV_CTX * pProvCtx, BYTE * pbOut, INT32 cb)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    NewArrayHolder<BYTE> buffer = new BYTE[cb];

    if (!CryptGenRandom(pProvCtx->m_hProv, cb, buffer))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    memcpyNoGCRefs(pbOut, buffer, cb);

    END_QCALL;
}

//
// Native method for calling a CSP to get random bytes.
//

void QCALLTYPE COMCryptography::GetNonZeroBytes(CRYPT_PROV_CTX * pProvCtx, BYTE * pbOut, INT32 cb)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    NewArrayHolder<BYTE> pb = new BYTE[cb];
    INT32 i = 0;

    while (i < cb) {
        if (!CryptGenRandom(pProvCtx->m_hProv, cb, pb))
            CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

        for (INT32 j=0; (i<cb) && (j<cb); j++) {
            if (pb[j] != 0) pbOut[i++] = pb[j];
        }
    }

    END_QCALL;
}

//
// Release our handle to a CSP, potentially deleting the referenced key
//
// Arguments:
//    pProviderContext - CSP context to release
//
//
// Notes:
//    This is the target of the System.Security.Cryptography.SafeProvHandle.FreeCsp QCall
//

// static
void QCALLTYPE COMCryptography::FreeCsp(__in_opt CRYPT_PROV_CTX *pProviderContext)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    if (pProviderContext)
        pProviderContext->Release();

    END_QCALL;
}

//
// _SearchForAlgorithm
// 
// Method for determining whether a CSP supports a particular
// algorithm and (optionally) a key size of that algorithm
//
BOOL QCALLTYPE COMCryptography::SearchForAlgorithm(CRYPT_PROV_CTX * pProvCtx, DWORD dwAlgID, DWORD dwKeyLength)
{
    QCALL_CONTRACT;

    BOOL result = FALSE;

    BEGIN_QCALL;

    DWORD dwFlags = CRYPT_FIRST;
    DWORD cbData = 0;
    // First, we have to get the max size of the PP
    if (CryptGetProvParam(pProvCtx->m_hProv, PP_ENUMALGS_EX, NULL, &cbData, dwFlags)) {

        // Allocate pbData
        NewArrayHolder<BYTE> pbData = new BYTE[cbData];
        while (CryptGetProvParam(pProvCtx->m_hProv, PP_ENUMALGS_EX, pbData, &cbData, dwFlags)) {
            dwFlags = 0;  // so we don't use CRYPT_FIRST more than once
            PROV_ENUMALGS_EX *provdata = (PROV_ENUMALGS_EX *) pbData.GetValue();
            ALG_ID provAlgID = provdata->aiAlgid;
            DWORD provMinLength = provdata->dwMinLen;
            DWORD provMaxLength = provdata->dwMaxLen;

            // OK, now check to see if we have an alg match
            if ((ALG_ID) dwAlgID == provAlgID) {
                // OK, see if we have a keylength match, or if we don't care
                if ((dwKeyLength == 0) || 
                    (dwKeyLength >= provMinLength) && 
                    (dwKeyLength <= provMaxLength)) {
                    result = TRUE;
                    break;
                }
            } // keep looping
        }
    }

    END_QCALL;

    return result;
}

//
// This method creates a new key container.
//

FCIMPL3(void, COMCryptography::_CreateCSP, Object* cspParametersUNSAFE, CLR_BOOL randomKeyContainer, SafeHandle** hProvUNSAFE)
{
    FCALL_CONTRACT;

    OBJECTREF cspParameters = (OBJECTREF) cspParametersUNSAFE;
    SAFEHANDLE hProvSAFE = (SAFEHANDLE) *hProvUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_2(cspParameters, hProvSAFE);

    NewHolder<CRYPT_PROV_CTX> pProvCtx(new CRYPT_PROV_CTX());

    // We always want to delete the random key container we create
    pProvCtx->m_fPersistKeyInCsp = (randomKeyContainer ? FALSE : TRUE);
    
    DWORD dwFlags = CRYPT_NEWKEYSET;
    if (randomKeyContainer) {
        dwFlags |= CRYPT_VERIFYCONTEXT;
    }

    HRESULT hr = OpenCSP(&cspParameters, dwFlags, pProvCtx);

    if (FAILED(hr)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: COMCryptography::OpenCSP failed.\n", hr));
        CryptoHelper::COMPlusThrowCrypto(hr);
    }

    // Set the handle field
    hProvSAFE->SetHandle((void*) pProvCtx.GetValue());
    pProvCtx.SuppressRelease();

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

CRYPT_HASH_CTX * COMCryptography::CreateHash(CRYPT_PROV_CTX * pProvCtx, DWORD dwHashType)
{
    QCALL_CONTRACT;

    CRYPT_HASH_CTX * pHashCtx = NULL;

    BEGIN_QCALL;

    HandleHashHolder hHash = NULL;
    HRESULT hr = S_OK;

    if (!CryptCreateHash(pProvCtx->m_hProv, dwHashType, NULL, 0, &hHash)) {
        hr = HRESULT_FROM_GetLastError();
    }

    if (FAILED(hr)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: COMCryptography::_CreateHash failed.\n", hr));
        CryptoHelper::COMPlusThrowCrypto(hr);
    }

    pHashCtx = new CRYPT_HASH_CTX(pProvCtx, hHash);
    hHash.SuppressRelease();

    END_QCALL;

    return pHashCtx;
}

void QCALLTYPE COMCryptography::DeriveKey(CRYPT_PROV_CTX * pProvCtx, DWORD dwCalgKey, DWORD dwCalgHash, 
                                          LPCBYTE pbPwd, DWORD cbPwd, DWORD dwFlags, LPBYTE pbIVIn, DWORD cbIVIn,
                                          QCall::ObjectHandleOnStack retKey)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    HandleHashHolder hHash(NULL);
    HandleKeyHolder hKey(NULL);

    NewArrayHolder<BYTE> bufferPwd = new BYTE[cbPwd];
    memcpyNoGCRefs (bufferPwd, pbPwd, cbPwd * sizeof(BYTE));

    NewArrayHolder<BYTE> rgbKey(NULL);
    NewArrayHolder<BYTE> pbIV(NULL);
    DWORD cb = 0;
    DWORD cbIV = 0;

    if (!CryptCreateHash(pProvCtx->m_hProv, dwCalgHash, NULL, 0, &hHash))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    // Hash the password string
    if (!CryptHashData(hHash, pbPwd, cbPwd, 0))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    // Create a block cipher session key based on the hash of the password
    if (!CryptDeriveKey(pProvCtx->m_hProv, dwCalgKey, hHash, dwFlags | CRYPT_EXPORTABLE, &hKey))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    HRESULT hr = UnloadKey(pProvCtx->m_hProv, hKey, &rgbKey, &cb);
    if (FAILED(hr)) 
        CryptoHelper::COMPlusThrowCrypto(hr);

    // Get the length of the IV
    cbIV = 0;
    if (!CryptGetKeyParam(hKey, KP_IV, NULL, &cbIV, 0))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    // Now allocate space for the IV vector
    pbIV = new BYTE[cbIV];
    if (!CryptGetKeyParam(hKey, KP_IV, pbIV, &cbIV, 0))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    // Check to avoid writing in the wrong location of the GC heap
    if (cbIV != cbIVIn)
        COMPlusThrow(kCryptographicException, W("Cryptography_PasswordDerivedBytes_InvalidIV"));
    memcpyNoGCRefs (pbIVIn, pbIV, cbIV);

    retKey.SetByteArray(rgbKey, cb);

    END_QCALL;
}

FCIMPL8(DWORD, COMCryptography::_DecryptData, SafeHandle* hKeyUNSAFE, U1Array* dataUNSAFE, 
        INT32 dwOffset, INT32 dwCount, U1Array** outputUNSAFE, INT32 dwOutputOffset, DWORD dwPaddingMode, CLR_BOOL fLast)
{
    FCALL_CONTRACT;

    struct _gc
    {
        U1ARRAYREF data;
        U1ARRAYREF output;
        SAFEHANDLE hKeySAFE;
    } gc;

    gc.data = (U1ARRAYREF) dataUNSAFE;
    gc.output = (U1ARRAYREF) *outputUNSAFE;
    gc.hKeySAFE = (SAFEHANDLE) hKeyUNSAFE;

    INT32 dwResult = 0;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    DWORD cb2 = dwCount;
    // Do this check here as a sanity check. Also, this will catch bugs in CryptoAPITransform
    if (dwOffset < 0 || dwCount < 0 || dwCount > (INT32) gc.data->GetNumComponents() || dwOffset > ((INT32) gc.data->GetNumComponents() - dwCount))
        CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);

    NewArrayHolder<BYTE> pb(new BYTE[cb2]);
    memcpy(pb, dwOffset + (LPBYTE) gc.data->GetDirectPointerToNonObjectElements(), cb2);

    {
        SafeHandleHolder shh(&gc.hKeySAFE);
        CRYPT_KEY_CTX * pKeyCtx = CryptoHelper::DereferenceSafeHandle<CRYPT_KEY_CTX>(gc.hKeySAFE);
        {
            GCX_PREEMP();
            // always call decryption with false, deal with padding manually
            if (!CryptDecrypt(pKeyCtx->m_hKey, NULL, FALSE, 0, pb, &cb2)) 
                CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
        }
    }

    DWORD dwPadLen = 0;
    if (fLast) {
        switch(dwPaddingMode) {
        case CRYPTO_PADDING_NONE:
            // we don't remove any padding
            break;
        case CRYPTO_PADDING_Zeros:
            // nothing to check for here
            break;
        case CRYPTO_PADDING_PKCS5:
            // PKCS5 padding is as follows: FF FF FF FF FF FF FF FF FF 07 07 07 07 07 07 07
            dwPadLen = cb2 > 0 ? pb[cb2 - 1] : 0;

            if (cb2 < BLOCK_LEN || dwPadLen <= 0 || dwPadLen > BLOCK_LEN)
                CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);

            // Check the padding bytes are all correct
            for (DWORD index = cb2 - dwPadLen; index + 1 < cb2; index++)
                if (pb[index] != dwPadLen)
                    CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);
            break;
        case CRYPTO_PADDING_ISO_10126:
            // The padding is as follows: FF FF FF FF FF FF FF FF FF 7D 2A 75 EF F8 EF 07
            dwPadLen = cb2 > 0 ? pb[cb2 - 1] : 0;
            if (cb2 < BLOCK_LEN || dwPadLen <= 0 || dwPadLen > BLOCK_LEN)
                CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);

            // Just ignore the random bytes
            break;
        case CRYPTO_PADDING_ANSI_X_923:
            // The padding is as follows: FF FF FF FF FF FF FF FF FF 00 00 00 00 00 00 07
            dwPadLen = cb2 > 0 ? pb[cb2 - 1] : 0;
            if (cb2 < BLOCK_LEN || dwPadLen <= 0 || dwPadLen > BLOCK_LEN)
                CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);

            // Check the padding bytes are all zeros
            for (DWORD index = cb2 - dwPadLen; index + 1 < cb2; index++)
                if (pb[index] != 0)
                    CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);
            break;
        }
    }

    dwResult = (cb2 - dwPadLen);
    if (dwResult < 0)
        CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);

    if (gc.output == NULL) {
        gc.output = (U1ARRAYREF) AllocatePrimitiveArray(ELEMENT_TYPE_U1, dwResult);
        memcpyNoGCRefs(gc.output->GetDirectPointerToNonObjectElements(), pb, dwResult);
        SetObjectReference((OBJECTREF*) outputUNSAFE, (OBJECTREF) gc.output, gc.output->GetAppDomain());
    } else {
        if (dwOutputOffset < 0 || dwResult < 0 || dwResult > (INT32) gc.output->GetNumComponents() || dwOutputOffset > ((INT32) gc.output->GetNumComponents() - dwResult))
            CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);
        memcpyNoGCRefs(dwOutputOffset + (LPBYTE) gc.output->GetDirectPointerToNonObjectElements(), pb, dwResult);
    }

    HELPER_METHOD_FRAME_END();
    return dwResult;
}
FCIMPLEND

//---------------------------------------------------------------------------------------
//
// Decrypt a symmetric key using the private key in pKeyContext
//
// Arguments:
//    pKeyContext       - private key used for decrypting pbEncryptedKey
//    pbEncryptedKey    - [in] encrypted symmetric key
//    cbEncryptedKey    - size, in bytes, of pbEncryptedKey
//    fOAEP             - TRUE to use OAEP padding, FALSE to use PKCS #1 type 2 padding
//    ohRetDecryptedKey - [out] decrypted key
//
// Notes:
//    pbEncryptedKey is byte-reversed from the format that CAPI expects. This is for compatibility with
//    previous CLR versions and other RSA implementations.
//
//    This method is the target of the System.Security.Cryptography.RSACryptoServiceProvider.DecryptKey QCall
//

// static
void QCALLTYPE COMCryptography::DecryptKey(__in CRYPT_KEY_CTX *pKeyContext,
                                           __in_bcount(cbEncryptedKey) BYTE *pbEncryptedKey,
                                           DWORD cbEncryptedKey,
                                           BOOL fOAEP,
                                           QCall::ObjectHandleOnStack ohRetDecryptedKey)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pKeyContext));
        PRECONDITION(CheckPointer(pbEncryptedKey));
        PRECONDITION(cbEncryptedKey >= 0);
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    NewArrayHolder<BYTE> pbKey = new BYTE[cbEncryptedKey];
    memcpy_s(pbKey, cbEncryptedKey, pbEncryptedKey, cbEncryptedKey);
    CryptoHelper::memrev(pbKey, cbEncryptedKey);

    DWORD dwDecryptFlags = fOAEP ? CRYPT_OAEP : 0;
    DWORD cbDecryptedKey = cbEncryptedKey;
    if (!CryptDecrypt(pKeyContext->m_hKey, NULL, TRUE, dwDecryptFlags, pbKey, &cbDecryptedKey))
    {
        HRESULT hrDecrypt = HRESULT_FROM_GetLastError();

        // If we're using OAEP mode and we recieved an NTE_BAD_FLAGS error, then OAEP is not supported on
        // this platform (XP+ only).  Throw a generic cryptographic exception if we failed to decrypt OAEP
        // padded data in order to prevent a chosen ciphertext attack.  We will allow NTE_BAD_KEY out, since
        // that error does not relate to the padding.  Otherwise just throw a cryptographic exception based on
        // the error code.
        if ((dwDecryptFlags & CRYPT_OAEP) == CRYPT_OAEP && hrDecrypt != NTE_BAD_KEY)
        {
            if (hrDecrypt == NTE_BAD_FLAGS)
                COMPlusThrow(kCryptographicException, W("Cryptography_OAEP_XPOnly"));
            else
                COMPlusThrow(kCryptographicException, W("Cryptography_OAEPDecoding"));
        }
        else
        {
            CryptoHelper::COMPlusThrowCrypto(hrDecrypt);
        }
    }

    // CryptDecrypt operates in place, so pbKey now has the plaintext version of the key.
    // cbDecryptedKey was updated to indicate the number of bytes of plaintext that are in the buffer.
    ohRetDecryptedKey.SetByteArray(pbKey, cbDecryptedKey);
    END_QCALL;
}

FCIMPL8(DWORD, COMCryptography::_EncryptData, SafeHandle* hKeyUNSAFE, U1Array* dataUNSAFE, 
        INT32 dwOffset, INT32 dwCount, U1Array** outputUNSAFE, INT32 dwOutputOffset, DWORD dwPaddingMode, CLR_BOOL fLast)
{
    FCALL_CONTRACT;

    struct _gc
    {
        U1ARRAYREF data;
        U1ARRAYREF output;
        SAFEHANDLE hKeySAFE;
    } gc;

    gc.data = (U1ARRAYREF) dataUNSAFE;
    gc.output = (U1ARRAYREF) *outputUNSAFE;
    gc.hKeySAFE = (SAFEHANDLE) hKeyUNSAFE;

    DWORD cb2 = dwCount;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    DWORD cb = dwCount + (fLast ? 16 : 0); // account for an extra padding block that will be added by CAPI
    DWORD cbPartial = (dwCount % BLOCK_LEN);

    // Do this check here as a sanity check. Also, this will catch bugs in CryptoAPITransform
    if (dwOffset < 0 || dwCount < 0 || dwCount > (INT32) gc.data->GetNumComponents() || dwOffset > ((INT32) gc.data->GetNumComponents() - dwCount))
        CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);

    NewArrayHolder<BYTE> pb(new BYTE[cb]);

    // initialize memory
    memset(pb, 0, cb);
    memcpy(pb, dwOffset + (LPBYTE) gc.data->GetDirectPointerToNonObjectElements(), dwCount);

    {
        SafeHandleHolder shh(&gc.hKeySAFE);
        CRYPT_KEY_CTX * pKeyCtx = CryptoHelper::DereferenceSafeHandle<CRYPT_KEY_CTX>(gc.hKeySAFE);
        _ASSERTE(pKeyCtx->m_pProvCtx);
        CRYPT_PROV_CTX * pProvCtx = pKeyCtx->m_pProvCtx;

        // Deal with padding modes by hand: we need this because Crypto API only supports PKCS#5 padding
        if (fLast) {
            DWORD dwPadLen = BLOCK_LEN - cbPartial;
            switch(dwPaddingMode) {
            case CRYPTO_PADDING_NONE:
                if (cbPartial > 0)
                    COMPlusThrow(kCryptographicException, W("Cryptography_SSE_InvalidDataSize"));
                break;
            case CRYPTO_PADDING_Zeros:
                // no further processing required, just adjust the input count
                // we don't add zeros if we've got a full number of blocks
                if (cbPartial != 0) 
                    cb2 += dwPadLen;
                break;
            case CRYPTO_PADDING_PKCS5:
                // PKCS5 padding is as follows: FF FF FF FF FF FF FF FF FF 07 07 07 07 07 07 07
                cb2 += dwPadLen;
                for (DWORD index = dwCount; index < cb2; index++)
                    pb[index] = static_cast<BYTE>(dwPadLen);
                break;
            case CRYPTO_PADDING_ISO_10126:
                // The padding is as follows: FF FF FF FF FF FF FF FF FF 7D 2A 75 EF F8 EF 07
                cb2 += dwPadLen;
                {
                    GCX_PREEMP();
                    // get some random bytes
                    if (!CryptGenRandom(pProvCtx->m_hProv, dwPadLen-1, pb+dwCount)) {
                        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
                    }
                }
                pb[cb2 - 1] = static_cast<BYTE>(dwPadLen);
                break;
            case CRYPTO_PADDING_ANSI_X_923:
                // The padding is as follows: FF FF FF FF FF FF FF FF FF 00 00 00 00 00 00 07
                cb2 += dwPadLen;
                for (DWORD index = dwCount; index < cb2-1; index++)
                    pb[index] = 0;
                pb[cb2 - 1] = static_cast<BYTE>(dwPadLen);
                break;
            }
        }

        {
            GCX_PREEMP();
            // We have done the padding ourselves, so let's just pass the provided fLast flag
            // so we ensure the key is initialized for future use
            if (!CryptEncrypt(pKeyCtx->m_hKey, NULL, fLast, 0, pb, &cb2, cb))
                CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
        }
    }

    // ignore the last padding block added by CAPI
    if (fLast) cb2 -= BLOCK_LEN;

    if (gc.output == NULL) {
        gc.output = (U1ARRAYREF) AllocatePrimitiveArray(ELEMENT_TYPE_U1, cb2);
        memcpyNoGCRefs(gc.output->GetDirectPointerToNonObjectElements(), pb, cb2);
        SetObjectReference((OBJECTREF*) outputUNSAFE, (OBJECTREF) gc.output, gc.output->GetAppDomain());
    } else {
        if (dwOutputOffset < 0 || (INT32) cb2 < 0 || cb2 > gc.output->GetNumComponents() || dwOutputOffset > ((INT32) gc.output->GetNumComponents() - (INT32) cb2))
            CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);
        memcpyNoGCRefs(dwOutputOffset + (LPBYTE) gc.output->GetDirectPointerToNonObjectElements(), pb, cb2);
    }

    HELPER_METHOD_FRAME_END();
    return cb2;
}
FCIMPLEND


//---------------------------------------------------------------------------------------
//
// Encrypt a symmetric key using the public key in pKeyContext
//
// Arguments:
//    pKeyContext       - [in] public to encrypt pbKey with
//    pbKey             - [in] symmetric key to encrypt
//    cbKey             - size, in bytes, of pbKey
//    fOAEP             - TRUE to use OAEP padding, FALSE to use PKCS #1 type 2 padding
//    ohRetEncryptedKey - [out] byte array holding the encrypted key
//
// Notes:
//    The returned value in ohRetEncryptedKey is byte-reversed from the version CAPI gives us.  This is for
//    compatibility with previous releases of the CLR and other RSA implementations.
//
//    This method is the target of the EncryptKey QCall in System.Security.Cryptography.RSACryptoServiceProvider.
// 

// static
void QCALLTYPE COMCryptography::EncryptKey(__in CRYPT_KEY_CTX *pKeyContext,
                                           __in_bcount(cbKey) BYTE *pbKey,
                                           DWORD cbKey,
                                           BOOL fOAEP,
                                           QCall::ObjectHandleOnStack ohRetEncryptedKey)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pKeyContext));
        PRECONDITION(CheckPointer(pbKey));
        PRECONDITION(cbKey >= 0);
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    DWORD dwEncryptFlags = fOAEP ? CRYPT_OAEP : 0;

    // Figure out how big the encrypted key will be
    DWORD cbEncryptedKey = cbKey;
    if (!CryptEncrypt(pKeyContext->m_hKey, NULL, TRUE, dwEncryptFlags, NULL, &cbEncryptedKey, cbEncryptedKey))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    // pbData is an in/out buffer for CryptEncrypt. allocate space for the encrypted key, and copy the
    // plaintext key into that space.  Since encrypted keys will have padding applied, the size of the encrypted
    // key should always be larger than the plaintext key, so use that to determine the buffer size.
    _ASSERTE(cbEncryptedKey >= cbKey);
    NewArrayHolder<BYTE> pbEncryptedKey = new BYTE[cbEncryptedKey];
    memcpy_s(pbEncryptedKey, cbEncryptedKey, pbKey, cbKey);

    // Encrypt for real - the last parameter is the total size of the in/out buffer, while the second to last
    // parameter specifies the size of the plaintext to encrypt.
    if (!CryptEncrypt(pKeyContext->m_hKey, NULL, TRUE, dwEncryptFlags, pbEncryptedKey, &cbKey, cbEncryptedKey))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
    
    _ASSERTE(cbKey == cbEncryptedKey);
    CryptoHelper::memrev(pbEncryptedKey, cbEncryptedKey);
    ohRetEncryptedKey.SetByteArray(pbEncryptedKey, cbEncryptedKey);
    END_QCALL;
}


void QCALLTYPE COMCryptography::EndHash(CRYPT_HASH_CTX * pHashCtx, QCall::ObjectHandleOnStack retHash)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    DWORD cbHash = 0;
    DWORD cbHashCount = sizeof(cbHash);
    if (!CryptGetHashParam(pHashCtx->m_hHash, HP_HASHSIZE, reinterpret_cast<BYTE *>(&cbHash), &cbHashCount, 0))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    NewArrayHolder<BYTE> pb = new BYTE[cbHash];
    if (!CryptGetHashParam(pHashCtx->m_hHash, HP_HASHVAL, pb, &cbHash, 0))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    retHash.SetByteArray(pb, cbHash);

    END_QCALL;
}

//
// Exports key information of an RSACryptoServiceProvider/DSACryptoServiceProvider into a CAPI key blob (PKCS#1 format).
//

void QCALLTYPE COMCryptography::ExportCspBlob(CRYPT_KEY_CTX * pKeyCtx, DWORD dwBlobType, QCall::ObjectHandleOnStack retBlob)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    HRESULT hr = S_OK;
    NewArrayHolder<BYTE> pbRawData(NULL);
    DWORD cbRawData = 0;

    if (!CryptExportKey(pKeyCtx->m_hKey, NULL, dwBlobType, 0, NULL, &cbRawData))
        hr = HRESULT_FROM_GetLastError();

    if (FAILED(hr)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: COMCryptography::_ExportCspBlob failed.\n", hr));
        CryptoHelper::COMPlusThrowCrypto(hr);
    }

    pbRawData = new BYTE[cbRawData];
    if (!CryptExportKey(pKeyCtx->m_hKey, NULL, dwBlobType, 0, pbRawData, &cbRawData))
        hr = HRESULT_FROM_GetLastError();

    if (FAILED(hr)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: COMCryptography::_ExportCspBlob failed.\n", hr));
        CryptoHelper::COMPlusThrowCrypto(hr);
    }

    retBlob.SetByteArray(pbRawData, cbRawData);

    END_QCALL;
}

// 
// _ExportKey
// 

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
FCIMPL3(void, COMCryptography::_ExportKey, SafeHandle* hKeyUNSAFE, DWORD dwBlobType, Object* theKeyUNSAFE)
{
    FCALL_CONTRACT;

    HRESULT hr = S_OK;
    OBJECTREF theKey = (OBJECTREF) theKeyUNSAFE;
    SAFEHANDLE hKeySAFE = (SAFEHANDLE) hKeyUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_2(theKey, hKeySAFE);

    DWORD cb;
    BOOL f;
    DWORD dwFlags = 0;
    NewArrayHolder<BYTE> pb(NULL);

    struct __LocalGCR {
        RSA_CSPREF rsaKey;
        DSA_CSPREF dsaKey;
    } _gcr;

    _gcr.rsaKey = NULL;
    _gcr.dsaKey = NULL;

    {
        SafeHandleHolder shh(&hKeySAFE);
        CRYPT_KEY_CTX * pKeyCtx = CryptoHelper::DereferenceSafeHandle<CRYPT_KEY_CTX>(hKeySAFE);
        _ASSERTE(pKeyCtx);
        {
            GCX_PREEMP();
            // calg
            ALG_ID dwCalg;
            cb = sizeof(dwCalg);
            if (CryptGetKeyParam(pKeyCtx->m_hKey, KP_ALGID, (LPBYTE) &dwCalg, &cb, 0)) {
                // We need to add the VER3 handle for DH and DSS keys so that we can
                // get the fullest possible amount of information.
                if (dwCalg == CALG_DSS_SIGN)
                    dwFlags |= CRYPT_BLOB_VER3;
            }
retry:
            f = CryptExportKey(pKeyCtx->m_hKey, NULL, dwBlobType, dwFlags, NULL, &cb);
            if (!f) {
                if (dwFlags & CRYPT_BLOB_VER3) {
                    dwFlags &= ~CRYPT_BLOB_VER3;
                    goto retry;
                } 
                hr = HRESULT_FROM_GetLastError();
            }

            if (FAILED(hr)) {
                LOG((LF_SECURITY, LL_INFO100, "Error [%#x]: COMCryptography::_ExportKey failed.\n", hr));
                goto lExit;
            }

            pb = new BYTE[cb];
            if (!CryptExportKey(pKeyCtx->m_hKey, NULL, dwBlobType, dwFlags, pb, &cb))
                hr = HRESULT_FROM_GetLastError();
        }
    }

    DWORD cbMalloced = cb;
    LPBYTE pbX = NULL;
    DWORD cbKey = 0;

    GCPROTECT_BEGIN(_gcr);

    if (FAILED(hr)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: COMCryptography::_ExportKey failed.\n", hr));
        goto Exit;
    }

    BLOBHEADER * pblob = (BLOBHEADER *) pb.GetValue();
    KEY_HEADER * pKeyInfo = NULL;

    switch (pblob->aiKeyAlg) {
    case CALG_RSA_KEYX:
    case CALG_RSA_SIGN:
        VALIDATEOBJECTREF(theKey);
        _gcr.rsaKey = (RSA_CSPREF) theKey;

        if (dwBlobType == PUBLICKEYBLOB) {
            pKeyInfo = (KEY_HEADER *) pb.GetValue();
            cb = (pKeyInfo->rsa.bitlen/8);

            pbX = pb + sizeof(BLOBHEADER) + sizeof(RSAPUBKEY);

            // Exponent
            NewArrayHolder<BYTE> pbExponent(NULL);
            DWORD cbExponent = 0;
            ConvertIntToByteArray(pKeyInfo->rsa.pubexp, &pbExponent, &cbExponent);
            OBJECTREF arrayExponent = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbExponent);
            SetObjectReference((OBJECTREF *) &_gcr.rsaKey->m_Exponent,
                               arrayExponent,
                               _gcr.rsaKey->GetAppDomain());
            memcpyNoGCRefs(_gcr.rsaKey->m_Exponent->GetDirectPointerToNonObjectElements(), pbExponent, cbExponent);

            // Modulus
            OBJECTREF arrayModulus = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cb);
            SetObjectReference((OBJECTREF *) &_gcr.rsaKey->m_Modulus,
                               arrayModulus,
                               _gcr.rsaKey->GetAppDomain());
            CryptoHelper::memrev(pbX, cb);
            memcpyNoGCRefs(_gcr.rsaKey->m_Modulus->GetDirectPointerToNonObjectElements(),
                           pbX, cb);
            pbX += cb;
        }
        else if (dwBlobType == PRIVATEKEYBLOB) {
            pKeyInfo = (KEY_HEADER *) pb.GetValue();
            cb = (pKeyInfo->rsa.bitlen/8);
            DWORD cbHalfModulus = (cb + 1)/2;

            pbX = pb + sizeof(BLOBHEADER) + sizeof(RSAPUBKEY);

            // Exponent
            NewArrayHolder<BYTE> pbExponent(NULL);
            DWORD cbExponent = 0;
            ConvertIntToByteArray(pKeyInfo->rsa.pubexp, &pbExponent, &cbExponent);
            OBJECTREF arrayExponent = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbExponent);
            SetObjectReference((OBJECTREF *) &_gcr.rsaKey->m_Exponent,
                               arrayExponent,
                               _gcr.rsaKey->GetAppDomain());
            memcpyNoGCRefs(_gcr.rsaKey->m_Exponent->GetDirectPointerToNonObjectElements(), pbExponent, cbExponent);

            // Modulus
            OBJECTREF arrayModulus = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cb);
            SetObjectReference((OBJECTREF *) &_gcr.rsaKey->m_Modulus,
                               arrayModulus,
                               _gcr.rsaKey->GetAppDomain());
            CryptoHelper::memrev(pbX, cb);
            memcpyNoGCRefs(_gcr.rsaKey->m_Modulus->GetDirectPointerToNonObjectElements(),
                           pbX, cb);
            pbX += cb;

            // P
            OBJECTREF arrayP = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbHalfModulus);
            SetObjectReference((OBJECTREF *) &_gcr.rsaKey->m_P,
                               arrayP,
                               _gcr.rsaKey->GetAppDomain());
            CryptoHelper::memrev(pbX, cbHalfModulus);
            memcpyNoGCRefs(_gcr.rsaKey->m_P->GetDirectPointerToNonObjectElements(),
                           pbX, cbHalfModulus);
            pbX += cbHalfModulus;

            // Q
            OBJECTREF arrayQ = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbHalfModulus);
            SetObjectReference((OBJECTREF *) &_gcr.rsaKey->m_Q,
                               arrayQ,
                               _gcr.rsaKey->GetAppDomain());
            CryptoHelper::memrev(pbX, cbHalfModulus);
            memcpyNoGCRefs(_gcr.rsaKey->m_Q->GetDirectPointerToNonObjectElements(),
                           pbX, cbHalfModulus);
            pbX += cbHalfModulus;

            // dp
            OBJECTREF arrayDP = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbHalfModulus);
            SetObjectReference((OBJECTREF *) &_gcr.rsaKey->m_dp,
                               arrayDP,
                               _gcr.rsaKey->GetAppDomain());
            CryptoHelper::memrev(pbX, cbHalfModulus);
            memcpyNoGCRefs(_gcr.rsaKey->m_dp->GetDirectPointerToNonObjectElements(),
                           pbX, cbHalfModulus);
            pbX += cbHalfModulus;

            // dq
            OBJECTREF arrayDQ = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbHalfModulus);
            SetObjectReference((OBJECTREF *) &_gcr.rsaKey->m_dq,
                               arrayDQ,
                               _gcr.rsaKey->GetAppDomain());
            CryptoHelper::memrev(pbX, cbHalfModulus);
            memcpyNoGCRefs(_gcr.rsaKey->m_dq->GetDirectPointerToNonObjectElements(),
                           pbX, cbHalfModulus);
            pbX += cbHalfModulus;

            // InvQ
            OBJECTREF arrayInverseQ = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbHalfModulus);
            SetObjectReference((OBJECTREF *) &_gcr.rsaKey->m_InverseQ,
                               arrayInverseQ,
                               _gcr.rsaKey->GetAppDomain());
            CryptoHelper::memrev(pbX, cbHalfModulus);
            memcpyNoGCRefs(_gcr.rsaKey->m_InverseQ->GetDirectPointerToNonObjectElements(),
                           pbX, cbHalfModulus);
            pbX += cbHalfModulus;

            // d
            OBJECTREF arrayD = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cb);
            SetObjectReference((OBJECTREF *) &_gcr.rsaKey->m_d,
                               arrayD,
                               _gcr.rsaKey->GetAppDomain());
            CryptoHelper::memrev(pbX, cb);
            memcpyNoGCRefs(_gcr.rsaKey->m_d->GetDirectPointerToNonObjectElements(),
                           pbX, cb);
            pbX += cb;
        }
        else {
            hr = E_FAIL;
            goto Exit;
        }
        break;

    case CALG_DSS_SIGN:
        _gcr.dsaKey = (DSA_CSPREF) theKey;
        // we have to switch on whether the blob is v3 or not, because we have different
        // info available if it is...
        if (pblob->bVersion > 0x2) {
            if (dwBlobType == PUBLICKEYBLOB) {
                int cbP, cbQ, cbJ;
                DSSPUBKEY_VER3 * pdss;

                pdss = (DSSPUBKEY_VER3 *) (pb + sizeof(BLOBHEADER));
                cbP = (pdss->bitlenP+7)/8;
                cbQ = (pdss->bitlenQ+7)/8;
                cbJ = (pdss->bitlenJ+7)/8;
                pbX = pb + sizeof(BLOBHEADER) + sizeof(DSSPUBKEY_VER3);

                // P
                OBJECTREF arrayP = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbP);
                SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_P, arrayP, _gcr.dsaKey->GetAppDomain());
                CryptoHelper::memrev(pbX, cbP);
                memcpyNoGCRefs(_gcr.dsaKey->m_P->GetDirectPointerToNonObjectElements(), pbX, cbP);
                pbX += cbP;

                // Q
                OBJECTREF arrayQ = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbQ);
                SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_Q, arrayQ, _gcr.dsaKey->GetAppDomain());
                CryptoHelper::memrev(pbX, cbQ);
                memcpyNoGCRefs(_gcr.dsaKey->m_Q->GetDirectPointerToNonObjectElements(), pbX, cbQ);
                pbX += cbQ;

                // G
                OBJECTREF arrayG = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbP);
                SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_G, arrayG, _gcr.dsaKey->GetAppDomain());
                CryptoHelper::memrev(pbX, cbP);
                memcpyNoGCRefs(_gcr.dsaKey->m_G->GetDirectPointerToNonObjectElements(), pbX, cbP);
                pbX += cbP;

                // J
                if (cbJ > 0) {
                    OBJECTREF arrayJ = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbJ);
                    SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_J, arrayJ, _gcr.dsaKey->GetAppDomain());
                    CryptoHelper::memrev(pbX, cbJ);
                    memcpyNoGCRefs(_gcr.dsaKey->m_J->GetDirectPointerToNonObjectElements(), pbX, cbJ);
                    pbX += cbJ;
                }

                // Y
                OBJECTREF arrayY = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbP);
                SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_Y, arrayY, _gcr.dsaKey->GetAppDomain());
                CryptoHelper::memrev(pbX, cbP);
                memcpyNoGCRefs(_gcr.dsaKey->m_Y->GetDirectPointerToNonObjectElements(), pbX, cbP);
                pbX += cbP;

                if (pdss->DSSSeed.counter != 0xFFFFFFFF) {
                    // seed
                    OBJECTREF arraySeed = AllocatePrimitiveArray(ELEMENT_TYPE_U1, 20);
                    SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_seed, arraySeed, _gcr.dsaKey->GetAppDomain());
                    CryptoHelper::memrev(pdss->DSSSeed.seed, 20);
                    memcpyNoGCRefs(_gcr.dsaKey->m_seed->GetDirectPointerToNonObjectElements(), pdss->DSSSeed.seed, 20);
                    // pdss->DSSSeed.c
                    _gcr.dsaKey->m_counter = pdss->DSSSeed.counter;
                }
            }
            else {
                int cbP, cbQ, cbJ, cbX;
                DSSPRIVKEY_VER3 * pdss;

                pdss = (DSSPRIVKEY_VER3 *) (pb + sizeof(BLOBHEADER));
                cbP = (pdss->bitlenP+7)/8;
                cbQ = (pdss->bitlenQ+7)/8;
                cbJ = (pdss->bitlenJ+7)/8;
                cbX = (pdss->bitlenX+7)/8;
                pbX = pb + sizeof(BLOBHEADER) + sizeof(DSSPRIVKEY_VER3);

                // P
                OBJECTREF arrayP = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbP);
                SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_P, arrayP, _gcr.dsaKey->GetAppDomain());
                CryptoHelper::memrev(pbX, cbP);
                memcpyNoGCRefs(_gcr.dsaKey->m_P->GetDirectPointerToNonObjectElements(), pbX, cbP);
                pbX += cbP;

                // Q
                OBJECTREF arrayQ = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbQ);
                SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_Q, arrayQ, _gcr.dsaKey->GetAppDomain());
                CryptoHelper::memrev(pbX, cbQ);
                memcpyNoGCRefs(_gcr.dsaKey->m_Q->GetDirectPointerToNonObjectElements(), pbX, cbQ);
                pbX += cbQ;

                // G
                OBJECTREF arrayG = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbP);
                SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_G, arrayG, _gcr.dsaKey->GetAppDomain());
                CryptoHelper::memrev(pbX, cbP);
                memcpyNoGCRefs(_gcr.dsaKey->m_G->GetDirectPointerToNonObjectElements(), pbX, cbP);
                pbX += cbP;

                // J
                if (pdss->bitlenJ > 0) {
                    OBJECTREF arrayJ = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbJ);
                    SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_J, arrayJ, _gcr.dsaKey->GetAppDomain());
                    CryptoHelper::memrev(pbX, cbJ);
                    memcpyNoGCRefs(_gcr.dsaKey->m_J->GetDirectPointerToNonObjectElements(), pbX, cbJ);
                    pbX += cbJ;
                }

                // Y
                OBJECTREF arrayY = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbP);
                SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_Y, arrayY, _gcr.dsaKey->GetAppDomain());
                CryptoHelper::memrev(pbX, cbP);
                memcpyNoGCRefs(_gcr.dsaKey->m_Y->GetDirectPointerToNonObjectElements(), pbX, cbP);
                pbX += cbP;

                // X
                OBJECTREF arrayX = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbX);
                SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_X, arrayX, _gcr.dsaKey->GetAppDomain());
                CryptoHelper::memrev(pbX, cbX);
                memcpyNoGCRefs(_gcr.dsaKey->m_X->GetDirectPointerToNonObjectElements(), pbX, cbX);
                pbX += cbX;

                if (pdss->DSSSeed.counter != 0xFFFFFFFF) {
                    // seed
                    OBJECTREF arraySeed = AllocatePrimitiveArray(ELEMENT_TYPE_U1, 20);
                    SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_seed, arraySeed, _gcr.dsaKey->GetAppDomain());
                    CryptoHelper::memrev(pdss->DSSSeed.seed, 20);
                    memcpyNoGCRefs(_gcr.dsaKey->m_seed->GetDirectPointerToNonObjectElements(), pdss->DSSSeed.seed, 20);
                    // pdss->DSSSeed.c
                    _gcr.dsaKey->m_counter = pdss->DSSSeed.counter;
                }
            }
        } else {
            // old-style blobs
            if (dwBlobType == PUBLICKEYBLOB) {
                int cbP, cbQ;
                DSSPUBKEY * pdss;
                DSSSEED * pseedstruct;

                pdss = (DSSPUBKEY *) (pb + sizeof(BLOBHEADER));
                cbP = (pdss->bitlen+7)/8; // bitlen is size of modulus
                cbQ = DSS_Q_LEN; // Q is always 20 bytes in length
                pbX = pb + sizeof(BLOBHEADER) + sizeof(DSSPUBKEY);

                // P
                OBJECTREF arrayP = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbP);
                SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_P, arrayP, _gcr.dsaKey->GetAppDomain());
                CryptoHelper::memrev(pbX, cbP);
                memcpyNoGCRefs(_gcr.dsaKey->m_P->GetDirectPointerToNonObjectElements(), pbX, cbP);
                pbX += cbP;

                // Q
                OBJECTREF arrayQ = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbQ);
                SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_Q, arrayQ, _gcr.dsaKey->GetAppDomain());
                CryptoHelper::memrev(pbX, cbQ);
                memcpyNoGCRefs(_gcr.dsaKey->m_Q->GetDirectPointerToNonObjectElements(), pbX, cbQ);
                pbX += cbQ;

                // G
                OBJECTREF arrayG = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbP);
                SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_G, arrayG, _gcr.dsaKey->GetAppDomain());
                CryptoHelper::memrev(pbX, cbP);
                memcpyNoGCRefs(_gcr.dsaKey->m_G->GetDirectPointerToNonObjectElements(), pbX, cbP);
                pbX += cbP;

                // Y
                OBJECTREF arrayY = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbP);
                SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_Y, arrayY, _gcr.dsaKey->GetAppDomain());
                CryptoHelper::memrev(pbX, cbP);
                memcpyNoGCRefs(_gcr.dsaKey->m_Y->GetDirectPointerToNonObjectElements(), pbX, cbP);
                pbX += cbP;

                pseedstruct = (DSSSEED *) pbX;
                if (pseedstruct->counter > 0) {
                    // seed & counter
                    OBJECTREF arraySeed = AllocatePrimitiveArray(ELEMENT_TYPE_U1, 20);
                    SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_seed, arraySeed, _gcr.dsaKey->GetAppDomain());
                    // seed is always 20 bytes
                    CryptoHelper::memrev(pseedstruct->seed, 20);
                    memcpyNoGCRefs(_gcr.dsaKey->m_seed->GetDirectPointerToNonObjectElements(), pseedstruct->seed, 20);
                    pbX += 20;

                    // pdss->DSSSeed.c
                    _gcr.dsaKey->m_counter = pseedstruct->counter;
                    pbX += sizeof(DWORD);
                }
            }
            else {
                int cbP, cbQ, cbX;
                DSSPUBKEY * pdss;
                DSSSEED * pseedstruct;

                pdss = (DSSPUBKEY *) (pb + sizeof(BLOBHEADER));
                cbP = (pdss->bitlen+7)/8; //bitlen is size of modulus
                cbQ = DSS_Q_LEN; // Q is always 20 bytes in length
                pbX = pb + sizeof(BLOBHEADER) + sizeof(DSSPUBKEY);

                // P
                OBJECTREF arrayP = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbP);
                SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_P, arrayP, _gcr.dsaKey->GetAppDomain());
                CryptoHelper::memrev(pbX, cbP);
                memcpyNoGCRefs(_gcr.dsaKey->m_P->GetDirectPointerToNonObjectElements(), pbX, cbP);
                pbX += cbP;

                // Q
                OBJECTREF arrayQ = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbQ);
                SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_Q, arrayQ, _gcr.dsaKey->GetAppDomain());
                CryptoHelper::memrev(pbX, cbQ);
                memcpyNoGCRefs(_gcr.dsaKey->m_Q->GetDirectPointerToNonObjectElements(), pbX, cbQ);
                pbX += cbQ;

                // G
                OBJECTREF arrayG = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbP);
                SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_G, arrayG, _gcr.dsaKey->GetAppDomain());
                CryptoHelper::memrev(pbX, cbP);
                memcpyNoGCRefs(_gcr.dsaKey->m_G->GetDirectPointerToNonObjectElements(), pbX, cbP);
                pbX += cbP;

                // X
                cbX = 20; // X must be 20 bytes in length
                OBJECTREF arrayX = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbX);
                SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_X, arrayX, _gcr.dsaKey->GetAppDomain());
                CryptoHelper::memrev(pbX, cbX);
                memcpyNoGCRefs(_gcr.dsaKey->m_X->GetDirectPointerToNonObjectElements(), pbX, cbX);
                pbX += cbX;

                pseedstruct = (DSSSEED *) pbX;
                if (pseedstruct->counter > 0) {
                    // seed
                    OBJECTREF arraySeed = AllocatePrimitiveArray(ELEMENT_TYPE_U1, 20);
                    SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_seed, arraySeed, _gcr.dsaKey->GetAppDomain());
                    CryptoHelper::memrev(pseedstruct->seed, 20);
                    memcpyNoGCRefs(_gcr.dsaKey->m_seed->GetDirectPointerToNonObjectElements(), pseedstruct->seed, 20);
                    pbX += 20;
                    // pdss->DSSSeed.c
                    _gcr.dsaKey->m_counter = pseedstruct->counter;
                    pbX += sizeof(DWORD);
                }

                // Add this sanity check here to avoid reading from the heap
                cbKey = (DWORD)(pbX - pb);
                if (cbKey > cbMalloced) {
                    hr = E_FAIL;
                    goto Exit;
                }

                // OK, we have one more thing to do.  Because old DSS shared the DSSPUBKEY struct for both public and private keys,
                // when we have a private key blob we get X but not Y.  TO get Y, we have to do another export asking for a public key blob

                {
                    SafeHandleHolder shh(&hKeySAFE);
                    CRYPT_KEY_CTX * pKeyCtx = CryptoHelper::DereferenceSafeHandle<CRYPT_KEY_CTX>(hKeySAFE);
                    {
                        GCX_PREEMP();
                        f = CryptExportKey(pKeyCtx->m_hKey, NULL, PUBLICKEYBLOB, dwFlags, NULL, &cb);

                        if (!f) {
                            hr = HRESULT_FROM_GetLastError();
                            goto Exit;
                        }

                        pb = new BYTE[cb];
                        cbMalloced = cb;

                        f = CryptExportKey(pKeyCtx->m_hKey, NULL, PUBLICKEYBLOB, dwFlags, pb, &cb);
                        if (!f) {
                            hr = HRESULT_FROM_GetLastError();
                            goto Exit;
                        }
                    }
                }

                // skip over header, DSSPUBKEY, P, Q and G.  Y is of size cbP
                pbX = pb + sizeof(BLOBHEADER) + sizeof(DSSPUBKEY) + cbP + cbQ + cbP;
                OBJECTREF arrayY = AllocatePrimitiveArray(ELEMENT_TYPE_U1, cbP);
                SetObjectReference((OBJECTREF *) &_gcr.dsaKey->m_Y, arrayY, _gcr.dsaKey->GetAppDomain());
                CryptoHelper::memrev(pbX, cbP);
                memcpyNoGCRefs(_gcr.dsaKey->m_Y->GetDirectPointerToNonObjectElements(), pbX, cbP);
                pbX += cbP;
            }
        }
        break;

    default:
        hr = E_FAIL;
        goto Exit;
    }

    // Add this sanity check here to avoid reading from the heap
    cbKey = (DWORD)(pbX - pb);
    if (cbKey > cbMalloced) {
        hr = E_FAIL;
        goto Exit;
    }

    hr = S_OK;

Exit: ;
    GCPROTECT_END();

lExit:

    if (FAILED(hr)) 
        CryptoHelper::COMPlusThrowCrypto(hr);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//
// We implicitly assume these methods are not going to do a LoadLibrary
//


//---------------------------------------------------------------------------------------
//
// Release our handle to a hash, potentially also releasing the provider
//
// Arguments:
//    pHashContext - Hash context to release
//
// Notes:
//    This is the target of the System.Security.Cryptography.SafeHashHandle.FreeHash QCall
//

// static
void QCALLTYPE COMCryptography::FreeHash(__in_opt CRYPT_HASH_CTX *pHashContext)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    if (pHashContext)
        pHashContext->Release();

    END_QCALL;
}

//---------------------------------------------------------------------------------------
//
// Release our handle to a key, potentially also releasing the provider
//
// Arguments:
//    pKeyContext - Key context to release
//
// Notes:
//    This is the target of the System.Security.Cryptography.SafeKeyHandle.FreeKey QCall
//

// static
void QCALLTYPE COMCryptography::FreeKey(__in_opt CRYPT_KEY_CTX *pKeyContext)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    if (pKeyContext)
        pKeyContext->Release();

    END_QCALL;
}

//
// Native method for creation of a key in a CSP
//

FCIMPL5(void, COMCryptography::_GenerateKey, SafeHandle* hProvUNSAFE, DWORD dwCalg, DWORD dwFlags, DWORD dwKeySize, SafeHandle** hKeyUNSAFE)
{
    FCALL_CONTRACT;

    SAFEHANDLE hProvSAFE = (SAFEHANDLE) hProvUNSAFE;
    SAFEHANDLE hKeySAFE = (SAFEHANDLE) *hKeyUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_2(hProvSAFE, hKeySAFE);

    HandleKeyHolder hKey(NULL);
    CRYPT_PROV_CTX * pProvCtx = NULL;
    HRESULT hr = S_OK;

    {
        SafeHandleHolder shh(&hProvSAFE);
        pProvCtx = CryptoHelper::DereferenceSafeHandle<CRYPT_PROV_CTX>(hProvSAFE);
        {
            GCX_PREEMP();
            DWORD dwCapiFlags = MapCspKeyFlags (dwFlags) | (dwKeySize << 16);
            if (!CryptoHelper::CryptGenKey_SO_TOLERANT(pProvCtx->m_hProv, dwCalg, dwCapiFlags, &hKey))
                hr = HRESULT_FROM_GetLastError();
        }
    }

    if (FAILED(hr)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: COMCryptography::_GenerateKey failed.\n", hr));
        CryptoHelper::COMPlusThrowCrypto(hr);
    }

    CRYPT_KEY_CTX * pKeyCtx = new CRYPT_KEY_CTX(pProvCtx, hKey);
    pKeyCtx->m_dwKeySpec = dwCalg;
    hKeySAFE->SetHandle((void*) pKeyCtx);
    hKey.SuppressRelease();

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND


//---------------------------------------------------------------------------------------
//
// Check to see if the CLR should enforce the machine wide FIPS algorithm policy
//
// Return Value:
//    True if the CLR should prevent any non-FIPS certified algorithms from being
//    instantiated if the FIPS algorithm policy is set on the machine, false otherwise.
//

FCIMPL0(FC_BOOL_RET, COMCryptography::_GetEnforceFipsPolicySetting)
{
    FCALL_CONTRACT;
    FC_RETURN_BOOL(g_pConfig->EnforceFIPSPolicy());
}
FCIMPLEND

//
// This method acquires key specific parameters.
// It will pop up a UI if the key is user protected.
//

FCIMPL2(U1Array*, COMCryptography::_GetKeyParameter, SafeHandle* hKeyUNSAFE, DWORD dwKeyParam)
{
    FCALL_CONTRACT;

    U1ARRAYREF ret = NULL;
    SAFEHANDLE hKeySAFE = (SAFEHANDLE) hKeyUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_2(ret, hKeySAFE);

    DWORD cb = 0;
    NewArrayHolder<BYTE> pb(NULL);
    NewArrayHolder<WCHAR> pwszUnicode(NULL);

    SafeHandleHolder shh(&hKeySAFE);
    CRYPT_KEY_CTX * pKeyCtx = CryptoHelper::DereferenceSafeHandle<CRYPT_KEY_CTX>(hKeySAFE);
    switch (dwKeyParam) {
    case CLR_KEYLEN:
        // Some Csp's may pop up a UI here since we don't use CRYPT_SILENT flag
        // which is not supported in downlevel platforms
        if (!CryptGetKeyParam(pKeyCtx->m_hKey, KP_KEYLEN, NULL, &cb, 0))
            CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
        pb = new BYTE[cb];
        if (!CryptGetKeyParam(pKeyCtx->m_hKey, KP_KEYLEN, pb, &cb, 0))
            CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
        CryptoHelper::ByteArrayToU1ARRAYREF(pb, cb, &ret);
        break;

    case CLR_PUBLICKEYONLY:
        // returns whether the key is a public only key
        pb = new BYTE[4];
        *((DWORD*) pb.GetValue()) = pKeyCtx->m_fPublicOnly;
        CryptoHelper::ByteArrayToU1ARRAYREF(pb, 4, &ret);
        break;

    case CLR_ALGID:
        // returns the algorithm ID for the key
        if (!CryptGetKeyParam(pKeyCtx->m_hKey, KP_ALGID, NULL, &cb, 0))
            CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
        pb = new BYTE[cb];
        if (!CryptGetKeyParam(pKeyCtx->m_hKey, KP_ALGID, pb, &cb, 0))
            CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
        CryptoHelper::ByteArrayToU1ARRAYREF(pb, cb, &ret);
        break;

    default:
        _ASSERTE(FALSE);
    }

    HELPER_METHOD_FRAME_END();
    return (U1Array*) OBJECTREFToObject(ret);
}
FCIMPLEND

//
// _GetKeySetSecurityInfo
//

FCIMPL3(U1Array*, COMCryptography::_GetKeySetSecurityInfo, SafeHandle* hProvUNSAFE, DWORD dwSecurityInformation, DWORD* pdwErrorCode)
{
    FCALL_CONTRACT;

    U1ARRAYREF ret = NULL;
    SAFEHANDLE hProvSAFE = (SAFEHANDLE) hProvUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_1(hProvSAFE);

    SafeHandleHolder shh(&hProvSAFE);
    CRYPT_PROV_CTX * pProvCtx = CryptoHelper::DereferenceSafeHandle<CRYPT_PROV_CTX>(hProvSAFE);

    DWORD cb = 0;
    NewHolder<BYTE> pSD(NULL);
    *pdwErrorCode = 0;
    if (!CryptGetProvParam(pProvCtx->m_hProv, PP_KEYSET_SEC_DESCR, NULL, &cb, dwSecurityInformation)) {
        *pdwErrorCode = GetLastError();
        goto Error;
    }

    pSD = new BYTE[cb];
    if (!CryptGetProvParam(pProvCtx->m_hProv, PP_KEYSET_SEC_DESCR, pSD, &cb, dwSecurityInformation)) {
        *pdwErrorCode = GetLastError();
        goto Error;
    }

    if (pSD != NULL)
        CryptoHelper::ByteArrayToU1ARRAYREF(pSD, cb, &ret);

Error:;

    HELPER_METHOD_FRAME_END();
    return (U1Array*) OBJECTREFToObject(ret);
}
FCIMPLEND


BOOL QCALLTYPE COMCryptography::GetPersistKeyInCsp(CRYPT_PROV_CTX * pProvCtx)
{
    QCALL_CONTRACT;

    BOOL fResult = TRUE;

    BEGIN_QCALL;

    fResult = pProvCtx->m_fPersistKeyInCsp;

    END_QCALL;

    return fResult;
}

//
// This method queries the key container and gets some of its properties.
// Those properties should never cause a UI to display.
//

FCIMPL3(Object*, COMCryptography::_GetProviderParameter, SafeHandle* hProvUNSAFE, DWORD dwKeySpec, DWORD dwKeyParam)
{
    FCALL_CONTRACT;

    OBJECTREF ret = NULL;
    SAFEHANDLE hProvSAFE = (SAFEHANDLE) hProvUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_2(ret, hProvSAFE);

    DWORD cb = 0;
    NewArrayHolder<BYTE> pb(NULL);
    NewArrayHolder<WCHAR> pwszUnicode(NULL);
    DWORD dwImpType = 0;
    HandleKeyHolder hKey(NULL);

    SafeHandleHolder shh(&hProvSAFE);
    CRYPT_PROV_CTX * pProvCtx = CryptoHelper::DereferenceSafeHandle<CRYPT_PROV_CTX>(hProvSAFE);

    switch (dwKeyParam) {
    case CLR_EXPORTABLE:
        cb = sizeof(dwImpType);
        if (!CryptGetProvParam(pProvCtx->m_hProv, PP_IMPTYPE, (PBYTE) &dwImpType, &cb, 0)) 
            CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

        if (!(dwImpType & CRYPT_IMPL_HARDWARE)) {
            if (!CryptGetUserKey(pProvCtx->m_hProv, dwKeySpec, &hKey))
                CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

            DWORD dwPermissions;
            dwPermissions = 0;
            cb = sizeof(dwPermissions);
            if (!CryptGetKeyParam(hKey, KP_PERMISSIONS, (PBYTE) &dwPermissions, &cb, 0))
                CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

            pb = new BYTE[4];
            *((DWORD*) pb.GetValue()) = dwPermissions & CRYPT_EXPORT ? 1 : 0;
        } else {
            // We assume hardware keys are not exportable 
            pb = new BYTE[4];
            *((DWORD*) pb.GetValue()) = 0;
        }
        CryptoHelper::ByteArrayToU1ARRAYREF(pb, 4, (U1ARRAYREF*) &ret);
        break;

    case CLR_REMOVABLE:
        cb = sizeof(dwImpType);
        if (!CryptGetProvParam(pProvCtx->m_hProv, PP_IMPTYPE, (PBYTE) &dwImpType, &cb, 0)) 
            CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

        pb = new BYTE[4];
        *((DWORD*) pb.GetValue()) = dwImpType & CRYPT_IMPL_REMOVABLE ? 1 : 0;
        CryptoHelper::ByteArrayToU1ARRAYREF(pb, 4, (U1ARRAYREF*) &ret);
        break;

    case CLR_HARDWARE:
        cb = sizeof(dwImpType);
        if (!CryptGetProvParam(pProvCtx->m_hProv, PP_IMPTYPE, (PBYTE) &dwImpType, &cb, 0)) 
            CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

        pb = new BYTE[4];
        *((DWORD*) pb.GetValue()) = dwImpType & CRYPT_IMPL_HARDWARE ? 1 : 0;
        CryptoHelper::ByteArrayToU1ARRAYREF(pb, 4, (U1ARRAYREF*) &ret);
        break;

    case CLR_ACCESSIBLE:
        pb = new BYTE[4];
        *((DWORD*) pb.GetValue()) = CryptGetUserKey(pProvCtx->m_hProv, dwKeySpec, &hKey) ? 1 : 0;
        CryptoHelper::ByteArrayToU1ARRAYREF(pb, 4, (U1ARRAYREF*) &ret);
        break;

    case CLR_PROTECTED:
        cb = sizeof(dwImpType);
        if (!CryptGetProvParam(pProvCtx->m_hProv, PP_IMPTYPE, (PBYTE) &dwImpType, &cb, 0)) 
            CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

        BOOL isProtected;
        if (dwImpType & CRYPT_IMPL_HARDWARE) {
            // Assume hardware keys are protected
            isProtected = TRUE;
        } else {
            isProtected = FALSE;
        }

        pb = new BYTE[4];
        *((DWORD*) pb.GetValue()) = isProtected;
        CryptoHelper::ByteArrayToU1ARRAYREF(pb, 4, (U1ARRAYREF*) &ret);
        break;

    case CLR_UNIQUE_CONTAINER:
        if (!CryptGetProvParam(pProvCtx->m_hProv, PP_UNIQUE_CONTAINER, NULL, &cb, 0)) 
            CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
        pb = new BYTE[cb];
        if (!CryptGetProvParam(pProvCtx->m_hProv, PP_UNIQUE_CONTAINER, pb, &cb, 0)) 
            CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
        pwszUnicode = CryptoHelper::AnsiToUnicode((char*) pb.GetValue());
        ret = StringObject::NewString(pwszUnicode);
        break;

    default:
        _ASSERTE(FALSE);
    }

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(ret);
}
FCIMPLEND

//
// _GetUserKey
// 
// Native method to get the user key pair of a key container
//

FCIMPL3(HRESULT, COMCryptography::_GetUserKey, SafeHandle* hProvUNSAFE, DWORD dwKeySpec, SafeHandle** hKeyUNSAFE)
{
    FCALL_CONTRACT;

    HRESULT hr = S_OK;

    SAFEHANDLE hProvSAFE = (SAFEHANDLE) hProvUNSAFE;
    SAFEHANDLE hKeySAFE = (SAFEHANDLE) *hKeyUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_2(hProvSAFE, hKeySAFE); 

    HandleKeyHolder hKey(NULL);
    CRYPT_PROV_CTX * pProvCtx = NULL;
    {
        SafeHandleHolder shh(&hProvSAFE);
        pProvCtx = CryptoHelper::DereferenceSafeHandle<CRYPT_PROV_CTX>(hProvSAFE);
        {
            GCX_PREEMP();
            if (!CryptGetUserKey(pProvCtx->m_hProv, dwKeySpec, &hKey))
                hr = HRESULT_FROM_GetLastError();
        }
    }

    if (hr == S_OK) {
        CRYPT_KEY_CTX * pKeyCtx = new CRYPT_KEY_CTX(pProvCtx, hKey);
        pKeyCtx->m_dwKeySpec = dwKeySpec;
        hKeySAFE->SetHandle((void*) pKeyCtx);
        hKey.SuppressRelease();
    } 

    LOG((LF_SECURITY, LL_INFO10000, "COMCryptography::_GetUserKey returned error code [%#x].\n", hr));

    HELPER_METHOD_FRAME_END();
    return hr;
}
FCIMPLEND

void QCALLTYPE COMCryptography::HashData(CRYPT_HASH_CTX * pHashCtx, LPCBYTE pData, DWORD cbData, DWORD dwStart, DWORD dwSize)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    // Do this check here as a sanity check. Also, this will catch bugs in CryptoAPITransform
    if (dwStart < 0 || dwSize < 0 || dwSize > cbData || dwStart > (cbData - dwSize))
        CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);

    if (!CryptHashData(pHashCtx->m_hHash, pData + dwStart, dwSize, 0))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    END_QCALL;
}

//
// WARNING: This function side-effects hCSP
//

FCIMPL5(void, COMCryptography::_ImportBulkKey, SafeHandle* hProvUNSAFE, DWORD dwCalg, CLR_BOOL useSalt, U1Array* rgbKeyUNSAFE, SafeHandle** hKeyUNSAFE)
{
    FCALL_CONTRACT;

    struct _gc
    {
        U1ARRAYREF rgbKey;
        SAFEHANDLE hProvSAFE;
        SAFEHANDLE hKeySAFE;
    } gc;

    gc.rgbKey = (U1ARRAYREF) rgbKeyUNSAFE;
    gc.hProvSAFE = (SAFEHANDLE) hProvUNSAFE;
    gc.hKeySAFE = (SAFEHANDLE) *hKeyUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    HandleKeyHolder hKey(NULL);
    DWORD cbKey = gc.rgbKey->GetNumComponents();
    NewArrayHolder<BYTE> buffer(new BYTE[cbKey]);
    memcpyNoGCRefs (buffer, (LPBYTE) gc.rgbKey->GetDirectPointerToNonObjectElements(), cbKey);
    CRYPT_PROV_CTX * pProvCtx = NULL;
    HRESULT hr = S_OK;

    {
        SafeHandleHolder shh(&gc.hProvSAFE);
        pProvCtx = CryptoHelper::DereferenceSafeHandle<CRYPT_PROV_CTX>(gc.hProvSAFE);
        {
            GCX_PREEMP();
            // If we are running in rsabase.dll compatibility mode, make sure 11 bytes of
            // zero salt are generated when using a 40 bits RC2 key.
            DWORD dwFlags = (dwCalg == CALG_RC2 ? CRYPT_EXPORTABLE | CRYPT_NO_SALT : CRYPT_EXPORTABLE);
            if (useSalt && dwCalg == CALG_RC2)
                dwFlags &= ~CRYPT_NO_SALT;
            hr = LoadKey(buffer, cbKey, pProvCtx->m_hProv, dwCalg, dwFlags, &hKey);
        }
    }

    if (FAILED(hr)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: COMCryptography::_ImportBulkKey failed.\n", hr));
        CryptoHelper::COMPlusThrowCrypto(hr);
    }

    CRYPT_KEY_CTX * pKeyCtx = new CRYPT_KEY_CTX(pProvCtx, hKey);
    gc.hKeySAFE->SetHandle((void*) pKeyCtx);
    hKey.SuppressRelease();

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

//
// Imports a CSP blob into an RSACryptoServiceProvider/DSACryptoServiceProvider managed object. The blob format is CAPI key blob format (PKCS#1)
//

FCIMPL4(DWORD, COMCryptography::_ImportCspBlob, U1Array* rawDataUNSAFE, SafeHandle* hProvUNSAFE, DWORD dwFlags, SafeHandle** hKeyUNSAFE)
{
    FCALL_CONTRACT;

    DWORD dwKeySpec = 0;

    struct _gc
    {
        U1ARRAYREF rawDataSAFE;
        SAFEHANDLE hProvSAFE;
        SAFEHANDLE hKeySAFE;
    } gc;

    gc.rawDataSAFE = (U1ARRAYREF) rawDataUNSAFE;
    gc.hProvSAFE = (SAFEHANDLE) hProvUNSAFE;
    gc.hKeySAFE = (SAFEHANDLE) *hKeyUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    CRYPT_PROV_CTX * pProvCtx = NULL;
    HandleKeyHolder hKey(NULL);
    HRESULT hr = S_OK;

    DWORD dwCapiFlags = MapCspKeyFlags (dwFlags);

    if (gc.rawDataSAFE == NULL)
        COMPlusThrowArgumentNull(W("rawData"));

    NewArrayHolder<BYTE> pbRawData(CryptoHelper::U1ARRAYREFToByteArray(gc.rawDataSAFE));
    DWORD cbRawData = gc.rawDataSAFE->GetNumComponents();

    PUBLICKEYSTRUC* pPubKeyStruc = (PUBLICKEYSTRUC*) pbRawData.GetValue();
    // If this is a public key, ignore the CRYPT_EXPORTABLE flag.
    if (pPubKeyStruc->bType == PUBLICKEYBLOB)
        dwCapiFlags &= ~CRYPT_EXPORTABLE;

    {
        SafeHandleHolder shh(&gc.hProvSAFE);
        pProvCtx = CryptoHelper::DereferenceSafeHandle<CRYPT_PROV_CTX>(gc.hProvSAFE);
        {
            GCX_PREEMP();
            if (!CryptImportKey(pProvCtx->m_hProv, pbRawData, cbRawData, NULL, dwCapiFlags, &hKey))
                hr = HRESULT_FROM_GetLastError();
        }
    }

    if (FAILED(hr)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: COMCryptography::_ImportCspBlob failed.\n", hr));
        CryptoHelper::COMPlusThrowCrypto(hr);
    }

    CRYPT_KEY_CTX * pKeyCtx = new CRYPT_KEY_CTX(pProvCtx, hKey);
    pKeyCtx->m_dwKeySpec = (pPubKeyStruc->aiKeyAlg == CALG_RSA_KEYX ? AT_KEYEXCHANGE : AT_SIGNATURE);
    pKeyCtx->m_fPublicOnly = (pPubKeyStruc->bType == PUBLICKEYBLOB ? TRUE : FALSE);
    gc.hKeySAFE->SetHandle((void*) pKeyCtx);

    dwKeySpec = pKeyCtx->m_dwKeySpec;
    hKey.SuppressRelease();

    HELPER_METHOD_FRAME_END();
    return dwKeySpec;
}
FCIMPLEND

// 
// _ImportKey
// 

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
FCIMPL5(void, COMCryptography::_ImportKey, SafeHandle* hProvUNSAFE, DWORD dwCalg, DWORD dwFlags, Object* refKeyUNSAFE, SafeHandle** hKeyUNSAFE)
{
    FCALL_CONTRACT;

    struct _gc
    {
        OBJECTREF refKey;
        SAFEHANDLE hProvSAFE;
        SAFEHANDLE hKeySAFE;
    } gc;

    gc.refKey = (OBJECTREF) refKeyUNSAFE;
    gc.hProvSAFE = (SAFEHANDLE) hProvUNSAFE;
    gc.hKeySAFE = (SAFEHANDLE) *hKeyUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    BOOL fPrivate = FALSE;
    HandleKeyHolder hKey(NULL);
    NewArrayHolder<BYTE> pbKey(NULL);
    DWORD cbKey = 0;
    LPBYTE pbX = NULL;
    KEY_HEADER* pKeyInfo = NULL;

    switch (dwCalg) {
    case CALG_DSS_SIGN: {
        DWORD cbP;
        DWORD cbQ;
        DWORD cbX = 0;
        DWORD cbJ = 0;
        DSA_CSPREF dssKey;

        VALIDATEOBJECTREF(gc.refKey);
        dssKey = (DSA_CSPREF) gc.refKey;

        // Validate the DSA structure first
        // P, Q and G are required. Q is a 160 bit divisor of P-1 and G is an element of Z_p
        if (dssKey->m_P == NULL || dssKey->m_Q == NULL || dssKey->m_Q->GetNumComponents() != DSS_Q_LEN)
            CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);
        cbP = dssKey->m_P->GetNumComponents();
        cbQ = dssKey->m_Q->GetNumComponents();
        if (dssKey->m_G == NULL || dssKey->m_G->GetNumComponents() != cbP)
            CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);
        // If J is present, it should be less than the size of P: J = (P-1) / Q
        // This is only a sanity check. Not doing it here is not really an issue as CAPI will fail.
        if (dssKey->m_J != NULL && dssKey->m_J->GetNumComponents() >= cbP)
            CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);
        // Y is present for V3 DSA key blobs, Y = g^j mod P
        if (dssKey->m_Y != NULL && dssKey->m_Y->GetNumComponents() != cbP)
            CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);
        // The seed is allways a 20 byte array
        if (dssKey->m_seed != NULL && dssKey->m_seed->GetNumComponents() != 20)
            CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);
        // The private key is less than q-1
        if (dssKey->m_X != NULL && dssKey->m_X->GetNumComponents() != DSS_Q_LEN) 
            CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);

        // Compute size of data to include
        cbKey = 3*cbP + cbQ + sizeof(KEY_HEADER) + sizeof(DSSSEED);
        if (dssKey->m_X != 0) {
            cbX = dssKey->m_X->GetNumComponents();
            cbKey += cbX;
        } 
        if (dssKey->m_J != NULL) {
            cbJ = dssKey->m_J->GetNumComponents();
            cbKey += cbJ;
        }
        pbKey = new BYTE[cbKey];

        // Public or Private import?

        pKeyInfo = (KEY_HEADER *) pbKey.GetValue();
        pKeyInfo->blob.bType = PUBLICKEYBLOB;
        pKeyInfo->blob.bVersion = CUR_BLOB_VERSION;
        pKeyInfo->blob.reserved = 0;
        pKeyInfo->blob.aiKeyAlg = dwCalg;

        if (cbX != 0) {
            pKeyInfo->blob.bType = PRIVATEKEYBLOB;
            fPrivate = TRUE;
        }

        //
        // If y is present and this is a private key, or
        // If y and J are present and this is a public key,
        //     this should be a v3 blob
        //
        // make the assumption that if the item is present, there are bytes

        if (((dssKey->m_Y != NULL) && fPrivate) ||
            ((dssKey->m_Y != NULL) && (dssKey->m_J != NULL))) {
            pKeyInfo->blob.bVersion = 0x3;
        }

        pbX = pbKey + sizeof(pKeyInfo->blob);
        if (pKeyInfo->blob.bVersion == 0x3) {
            if (fPrivate) {
                pbX += sizeof(pKeyInfo->dss_priv_v3);
                pKeyInfo->dss_priv_v3.bitlenP = cbP*8;
                pKeyInfo->dss_priv_v3.bitlenQ = cbQ*8;
                pKeyInfo->dss_priv_v3.bitlenJ = cbJ*8;
                pKeyInfo->dss_priv_v3.bitlenX = cbX*8;
                pKeyInfo->dss_priv_v3.magic = DSS_PRIV_MAGIC_VER3;
            }
            else {
                pbX += sizeof(pKeyInfo->dss_pub_v3);
                pKeyInfo->dss_pub_v3.bitlenP = cbP*8;
                pKeyInfo->dss_pub_v3.bitlenQ = cbQ*8;
                pKeyInfo->dss_pub_v3.bitlenJ = cbJ*8;
                pKeyInfo->dss_pub_v3.magic = DSS_PUB_MAGIC_VER3;
            }
        }
        else {
            if (fPrivate) {
                pKeyInfo->dss_v2.magic = DSS_PRIVATE_MAGIC;
            }
            else {
                pKeyInfo->dss_v2.magic = DSS_MAGIC;
            }
            pKeyInfo->dss_v2.bitlen = cbP*8;
            pbX += sizeof(pKeyInfo->dss_v2);
        }

        // P
        memcpy(pbX, dssKey->m_P->GetDirectPointerToNonObjectElements(), cbP);
        CryptoHelper::memrev(pbX, cbP);
        pbX += cbP;

        // Q
        memcpy(pbX, dssKey->m_Q->GetDirectPointerToNonObjectElements(), cbQ);
        CryptoHelper::memrev(pbX, cbQ);
        pbX += cbQ;

        // G
        memcpy(pbX, dssKey->m_G->GetDirectPointerToNonObjectElements(), cbP);
        CryptoHelper::memrev(pbX, cbP);
        pbX += cbP;

        if (pKeyInfo->blob.bVersion == 0x3) {
            // J -- if present then bVersion == 3;
            if (dssKey->m_J != NULL) {
                memcpy(pbX, dssKey->m_J->GetDirectPointerToNonObjectElements(), cbJ);
                CryptoHelper::memrev(pbX, cbJ);
                pbX += cbJ;
            }
        }

        if (!fPrivate || (pKeyInfo->blob.bVersion == 0x3)) {
            // Y -- if present then bVersion == 3;
            if (dssKey->m_Y != NULL) {
                memcpy(pbX, dssKey->m_Y->GetDirectPointerToNonObjectElements(), cbP);
                CryptoHelper::memrev(pbX, cbP);
                pbX += cbP;
            }
        }

        // X -- if present then private
        if (fPrivate) {
            memcpy(pbX, dssKey->m_X->GetDirectPointerToNonObjectElements(), cbX);
            CryptoHelper::memrev(pbX, cbX);
            pbX += cbX;
        }

        if ((dssKey->m_seed == NULL) || (dssKey->m_seed->GetNumComponents() == 0)){
            // No seed present, so set them to zero
            if (pKeyInfo->blob.bVersion == 0x3) {
                if (fPrivate) {
                    memset(&pKeyInfo->dss_priv_v3.DSSSeed, 0xFFFFFFFF, sizeof(DSSSEED));
                }
                else {
                    memset(&pKeyInfo->dss_pub_v3.DSSSeed, 0xFFFFFFFF, sizeof(DSSSEED));
                }
            }
            else {
                memset(pbX, 0xFFFFFFFF, sizeof(DSSSEED));
                pbX += sizeof(DSSSEED);
            }
        } else {
            if (pKeyInfo->blob.bVersion == 0x3) {
                if (fPrivate) {
                    pKeyInfo->dss_priv_v3.DSSSeed.counter = dssKey->m_counter;
                    memcpy(pKeyInfo->dss_priv_v3.DSSSeed.seed, dssKey->m_seed->GetDirectPointerToNonObjectElements(), 20);
                    CryptoHelper::memrev(pKeyInfo->dss_priv_v3.DSSSeed.seed, 20);
                } else {
                    pKeyInfo->dss_pub_v3.DSSSeed.counter = dssKey->m_counter;
                    memcpy(pKeyInfo->dss_pub_v3.DSSSeed.seed, dssKey->m_seed->GetDirectPointerToNonObjectElements(), 20);
                    CryptoHelper::memrev(pKeyInfo->dss_pub_v3.DSSSeed.seed, 20);
                }
            } else {
                memcpy(pbX,&dssKey->m_counter, sizeof(DWORD));
                pbX += sizeof(DWORD);
                // now the seed
                memcpy(pbX, dssKey->m_seed->GetDirectPointerToNonObjectElements(), 20);
                CryptoHelper::memrev(pbX, 20);
                pbX += 20;
            }
        }

        cbKey = (DWORD)(pbX - pbKey);
        break;
        }

    case CALG_RSA_SIGN:
    case CALG_RSA_KEYX: {
        RSA_CSPREF rsaKey;

        VALIDATEOBJECTREF(gc.refKey);
        rsaKey = (RSA_CSPREF) gc.refKey;

        // Validate the RSA structure first
        if (rsaKey->m_Modulus == NULL)
            CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);
        // The exponent is a DWORD, so the byte array must not be longer than 4 bytes
        if (rsaKey->m_Exponent == NULL || rsaKey->m_Exponent->GetNumComponents() > 4)
            CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);

        DWORD cb = rsaKey->m_Modulus->GetNumComponents();
        DWORD cbHalfModulus = (cb + 1)/2;
        // We assume that if P != null, then so are Q, DP, DQ, InverseQ and D
        if (rsaKey->m_P != NULL) {
            if (rsaKey->m_P->GetNumComponents() != cbHalfModulus)
                CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);
            if (rsaKey->m_Q == NULL || rsaKey->m_Q->GetNumComponents() != cbHalfModulus) 
                CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);
            if (rsaKey->m_dp == NULL || rsaKey->m_dp->GetNumComponents() != cbHalfModulus) 
                CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);
            if (rsaKey->m_dq == NULL || rsaKey->m_dq->GetNumComponents() != cbHalfModulus) 
                CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);
            if (rsaKey->m_InverseQ == NULL || rsaKey->m_InverseQ->GetNumComponents() != cbHalfModulus) 
                CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);
            if (rsaKey->m_d == NULL || rsaKey->m_d->GetNumComponents() != cb) 
                CryptoHelper::COMPlusThrowCrypto(NTE_BAD_DATA);
        }

        // Compute the size of the data to include
        pbKey = new BYTE[2*cb + 5*cbHalfModulus + sizeof(KEY_HEADER)];

        // Public or private import?

        pKeyInfo = (KEY_HEADER *) pbKey.GetValue();
        pKeyInfo->blob.bType = PUBLICKEYBLOB;   // will change to PRIVATEKEYBLOB if necessary
        pKeyInfo->blob.bVersion = CUR_BLOB_VERSION;
        pKeyInfo->blob.reserved = 0;
        pKeyInfo->blob.aiKeyAlg = dwCalg;

        pKeyInfo->rsa.magic = RSA_PUB_MAGIC; // will change to RSA_PRIV_MAGIC below if necesary
        pKeyInfo->rsa.bitlen = cb*8;
        pKeyInfo->rsa.pubexp = ConvertByteArrayToDWORD(rsaKey->m_Exponent->GetDirectPointerToNonObjectElements(), rsaKey->m_Exponent->GetNumComponents());
        pbX = pbKey + sizeof(BLOBHEADER) + sizeof(RSAPUBKEY);

        // Copy over the modulus -- put in for both public & private

        memcpy(pbX, rsaKey->m_Modulus->GetDirectPointerToNonObjectElements(), cb);
        CryptoHelper::memrev(pbX, cb);
        pbX += cb;

        //
        // See if we are doing private keys.
        //

        if ((rsaKey->m_P != 0) && (rsaKey->m_P->GetNumComponents() != 0)) {
            pKeyInfo->blob.bType = PRIVATEKEYBLOB;
            pKeyInfo->rsa.magic = RSA_PRIV_MAGIC;
            fPrivate = TRUE;

            // Copy over P
            memcpy(pbX, rsaKey->m_P->GetDirectPointerToNonObjectElements(), cbHalfModulus);
            CryptoHelper::memrev(pbX, cbHalfModulus);
            pbX += cbHalfModulus;

            // Copy over Q
            memcpy(pbX, rsaKey->m_Q->GetDirectPointerToNonObjectElements(), cbHalfModulus);
            CryptoHelper::memrev(pbX, cbHalfModulus);
            pbX += cbHalfModulus;

            // Copy over dp
            memcpy(pbX, rsaKey->m_dp->GetDirectPointerToNonObjectElements(), cbHalfModulus);
            CryptoHelper::memrev(pbX, cbHalfModulus);
            pbX += cbHalfModulus;

            // Copy over dq
            memcpy(pbX, rsaKey->m_dq->GetDirectPointerToNonObjectElements(), cbHalfModulus);
            CryptoHelper::memrev(pbX, cbHalfModulus);
            pbX += cbHalfModulus;

            // Copy over InvQ
            memcpy(pbX, rsaKey->m_InverseQ->GetDirectPointerToNonObjectElements(), cbHalfModulus);
            CryptoHelper::memrev(pbX, cbHalfModulus);
            pbX += cbHalfModulus;

            // Copy over d
            memcpy(pbX, rsaKey->m_d->GetDirectPointerToNonObjectElements(), cb);
            CryptoHelper::memrev(pbX, cb);
            pbX += cb;
        }
        cbKey = (DWORD)(pbX - pbKey);
        break;
        }

    default:
        COMPlusThrow(kCryptographicException, IDS_EE_CRYPTO_UNKNOWN_OPERATION);
    }

    DWORD dwCapiFlags = MapCspKeyFlags(dwFlags);
    if (!fPrivate)
        dwCapiFlags &= ~CRYPT_EXPORTABLE;

    CRYPT_PROV_CTX * pProvCtx = NULL;
    HRESULT hr = S_OK;

    {
        SafeHandleHolder shh(&gc.hProvSAFE);
        pProvCtx = CryptoHelper::DereferenceSafeHandle<CRYPT_PROV_CTX>(gc.hProvSAFE);
        {
            GCX_PREEMP();
            if (!CryptImportKey(pProvCtx->m_hProv, pbKey, cbKey, NULL, dwCapiFlags, &hKey))
                hr = HRESULT_FROM_GetLastError();
        }
    }

    if (FAILED(hr)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: COMCryptography::_ImportKey failed.\n", hr));
        CryptoHelper::COMPlusThrowCrypto(hr);
    }

    CRYPT_KEY_CTX * pKeyCtx = new CRYPT_KEY_CTX(pProvCtx, hKey);
    pKeyCtx->m_dwKeySpec = (dwCalg == CALG_RSA_KEYX ? AT_KEYEXCHANGE : AT_SIGNATURE);
    pKeyCtx->m_fPublicOnly = (pKeyInfo->blob.bType == PUBLICKEYBLOB ? TRUE : FALSE);
    gc.hKeySAFE->SetHandle((void*) pKeyCtx);
    hKey.SuppressRelease();

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//---------------------------------------------------------------------------------------
//
// Check to see if we should default to calculating HMAC values correctly or in Whidbey
// compatibility mode.
// 
// Return Value:
//    Default compatibiltiy mode for HMACSHA384 and HMACSHA512. TRUE indicates that we
//    should match Whidbey, false to use the correct calculation.
//

FCIMPL0(FC_BOOL_RET, COMCryptography::_ProduceLegacyHMACValues)
{
    FCALL_CONTRACT;

    FC_RETURN_BOOL(g_pConfig->LegacyHMACMode());
}
FCIMPLEND

// 
// SetKeyParamDw
// 
// Sets the value of a key parameter
//

void QCALLTYPE COMCryptography::SetKeyParamDw(CRYPT_KEY_CTX * pKeyCtx, DWORD param, DWORD dwValue)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    if (!CryptSetKeyParam(pKeyCtx->m_hKey, param, (LPBYTE) &dwValue, 0))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    END_QCALL;
}

//
// SetKeyParamRgb
//

void QCALLTYPE COMCryptography::SetKeyParamRgb(CRYPT_KEY_CTX * pKeyCtx, DWORD dwParam, LPCBYTE pValue, DWORD cbValue)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    NewArrayHolder<BYTE> buffer = new BYTE[cbValue];
    memcpyNoGCRefs (buffer, pValue, cbValue);

    if (!CryptSetKeyParam(pKeyCtx->m_hKey, dwParam, buffer, 0))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    END_QCALL;
}

//
// SetKeySetSecurityInfo
//

DWORD QCALLTYPE COMCryptography::SetKeySetSecurityInfo(CRYPT_PROV_CTX * pProvCtx, DWORD dwSecurityInformation, LPCBYTE pSecurityDescriptor)
{
    QCALL_CONTRACT;

    DWORD dwErrorCode = 0;

    BEGIN_QCALL;

    if (!CryptSetProvParam(pProvCtx->m_hProv,
                           PP_KEYSET_SEC_DESCR,
                           pSecurityDescriptor,
                           dwSecurityInformation))
        dwErrorCode = GetLastError();

    END_QCALL;

    return dwErrorCode;
}

//
// SetPersistKeyInCsp
//

void QCALLTYPE COMCryptography::SetPersistKeyInCsp(CRYPT_PROV_CTX * pProvCtx, BOOL fPersistKeyInCsp)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    pProvCtx->m_fPersistKeyInCsp = fPersistKeyInCsp;

    END_QCALL;
}

//
// SetProviderParameter
//

void QCALLTYPE COMCryptography::SetProviderParameter(CRYPT_PROV_CTX * pProvCtx, DWORD dwKeySpec, DWORD dwProvParam, INT_PTR pbData)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    switch (dwProvParam) {
    case CLR_PP_CLIENT_HWND:
        if (!CryptSetProvParam(pProvCtx->m_hProv, PP_CLIENT_HWND, (LPBYTE) pbData, 0))
            CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
        break;

    case CLR_PP_PIN:
        if (!CryptSetProvParam(pProvCtx->m_hProv, (dwKeySpec == AT_SIGNATURE ? PP_SIGNATURE_PIN : PP_KEYEXCHANGE_PIN), (LPBYTE) pbData, 0))
            CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
        break;

    default:
        _ASSERTE(FALSE);
    }

    END_QCALL;
}

//
// SignValue
//

void QCALLTYPE COMCryptography::SignValue(CRYPT_KEY_CTX * pKeyCtx, DWORD dwKeySpec, DWORD dwCalgKey, DWORD dwCalgHash, 
                                          LPCBYTE pbHash, DWORD cbHash, QCall::ObjectHandleOnStack retSignature)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    HandleHashHolder hHash = NULL;

    // Make sure it's either RSA or DSA key
    _ASSERTE(dwCalgKey == CALG_DSS_SIGN || dwCalgKey == CALG_RSA_SIGN);

    NewArrayHolder<BYTE> buffer = new BYTE[cbHash];
    memcpyNoGCRefs (buffer, pbHash, cbHash);

    _ASSERTE(pKeyCtx->m_pProvCtx);
    CRYPT_PROV_CTX * pProvCtx = pKeyCtx->m_pProvCtx;

    // Take the hash value and create a hash object in the correct CSP.
    if (!CryptCreateHash(pProvCtx->m_hProv, dwCalgHash, NULL, 0, &hHash))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    DWORD dwHashSize = 0;
    DWORD cbHashSize = sizeof(dwHashSize);
    if (!CryptGetHashParam(hHash, HP_HASHSIZE, reinterpret_cast<BYTE *>(&dwHashSize), &cbHashSize, 0))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    if (dwHashSize != cbHash)
        CryptoHelper::COMPlusThrowCrypto(NTE_BAD_HASH);

    if (!CryptSetHashParam(hHash, HP_HASHVAL, buffer, 0))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    // Find out how long the signature is going to be
    DWORD cbSignature = 0;
    if (!WszCryptSignHash(hHash, dwKeySpec, NULL, 0, NULL, &cbSignature))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    // Allocate the buffer to hold the signature
    NewArrayHolder<BYTE> buffer2 = new BYTE[cbSignature];

    // Now do the actual signature into the return buffer
    if (!WszCryptSignHash(hHash, dwKeySpec, NULL, 0, buffer2, &cbSignature))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    switch(dwCalgKey) {
        case CALG_RSA_SIGN:
            // the RSA signature needs to be reversed
            CryptoHelper::memrev(buffer2, cbSignature);
            break;
        case CALG_DSS_SIGN:
            // A DSA signature consists of two 20-byte components, each of which
            // must be reversed in place
            if (cbSignature != 40)
                COMPlusThrow(kCryptographicException, W("Cryptography_InvalidDSASignatureSize"));
            CryptoHelper::memrev(buffer2, 20);
            CryptoHelper::memrev(buffer2 + 20, 20);
            break;
    }

    retSignature.SetByteArray(buffer2, cbSignature);

    END_QCALL;
}

//
// VerifySign
//

BOOL QCALLTYPE COMCryptography::VerifySign(CRYPT_KEY_CTX * pKeyCtx, DWORD dwCalgKey, DWORD dwCalgHash, 
                                           LPCBYTE pbHash, DWORD cbHash, LPCBYTE pbSignature, DWORD cbSignature)
{
    QCALL_CONTRACT;

    BOOL result = FALSE;

    BEGIN_QCALL;

    // Make sure it's either RSA or DSA key
    _ASSERTE(dwCalgKey == CALG_DSS_SIGN || dwCalgKey == CALG_RSA_SIGN);

    HandleHashHolder hHash = NULL;

    //
    // Take the hash value and create a hash object in the correct CSP.
    //

    NewArrayHolder<BYTE> bufferHash(new BYTE[cbHash]);
    memcpyNoGCRefs (bufferHash, pbHash, cbHash * sizeof(BYTE));
    NewArrayHolder<BYTE> bufferSignature(new BYTE[cbSignature]);
    memcpyNoGCRefs (bufferSignature, pbSignature, cbSignature * sizeof(BYTE));

    switch(dwCalgKey) {
        case CALG_RSA_SIGN:
            // the RSA signature needs to be reversed
            CryptoHelper::memrev(bufferSignature, cbSignature);
            break;
        case CALG_DSS_SIGN:
            // A DSA signature consists of two 20-byte components, each of which
            // must be reversed in place
            if (cbSignature != 40)
                COMPlusThrow(kCryptographicException, W("Cryptography_InvalidDSASignatureSize"));
            CryptoHelper::memrev(bufferSignature, 20);
            CryptoHelper::memrev(bufferSignature + 20, 20);
            break;
    }

    CRYPT_PROV_CTX * pProvCtx = pKeyCtx->m_pProvCtx;

    if (!CryptCreateHash(pProvCtx->m_hProv, dwCalgHash, NULL, 0, &hHash))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    DWORD dwHashSize = 0;
    DWORD cbHashSize = sizeof(dwHashSize);
    if (!CryptGetHashParam(hHash, HP_HASHSIZE, reinterpret_cast<BYTE *>(&dwHashSize), &cbHashSize, 0))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    if (dwHashSize != cbHash)
        CryptoHelper::COMPlusThrowCrypto(NTE_BAD_HASH);

    if (!CryptSetHashParam(hHash, HP_HASHVAL, bufferHash, 0))
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    // Now see if the signature verifies.
    result = WszCryptVerifySignature(hHash, bufferSignature, cbSignature, pKeyCtx->m_hKey, NULL, 0);

    END_QCALL;

    return result;
}
#endif // FEATURE_CRYPTO

