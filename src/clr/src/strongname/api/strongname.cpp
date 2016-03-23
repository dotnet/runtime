// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ===========================================================================
// File: StrongName.cpp
// 
// Wrappers for signing and hashing functions needed to implement strong names
// ===========================================================================

#include "common.h"
#include <imagehlp.h>

#include <winwrap.h>
#include <windows.h>
#include <wincrypt.h>
#include <stddef.h>
#include <stdio.h>
#include <malloc.h>
#include <cor.h>
#include <corimage.h>
#include <metadata.h>
#include <daccess.h>
#include <limits.h>
#include <ecmakey.h>
#include <sha1.h>

#include "strongname.h"
#include "ex.h"
#include "pedecoder.h"
#include "strongnameholders.h"
#include "strongnameinternal.h"
#include "common.h"
#include "classnames.h"

// Debug logging.
#if !defined(_DEBUG) || defined(DACCESS_COMPILE)
#define SNLOG(args)
#endif // !_DEBUG || DACCESS_COMPILE

#ifndef DACCESS_COMPILE

// Debug logging.
#if defined(_DEBUG)
#include <stdarg.h>

BOOLEAN g_fLoggingInitialized = FALSE;
DWORD g_dwLoggingFlags = FALSE;

#define SNLOG(args)   Log args

void Log(__in_z const WCHAR *wszFormat, ...)
{
    if (g_fLoggingInitialized && !g_dwLoggingFlags)
        return;

    DWORD       dwError = GetLastError();

    if (!g_fLoggingInitialized) {
        g_dwLoggingFlags = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MscorsnLogging);
        g_fLoggingInitialized = TRUE;
    }

    if (!g_dwLoggingFlags) {
        SetLastError(dwError);
        return;
    }

    va_list     pArgs;
    WCHAR        wszBuffer[1024];
    static WCHAR wszPrefix[] = W("SN: ");

    wcscpy_s(wszBuffer, COUNTOF(wszBuffer), wszPrefix);

    va_start(pArgs, wszFormat);
    _vsnwprintf_s(&wszBuffer[COUNTOF(wszPrefix) - 1],
                  COUNTOF(wszBuffer) - COUNTOF(wszPrefix),
                  _TRUNCATE,
                  wszFormat,
                  pArgs);

    wszBuffer[COUNTOF(wszBuffer) - 1] = W('\0');
    va_end(pArgs);

    if (g_dwLoggingFlags & 1)
        wprintf(W("%s"), wszBuffer);
    if (g_dwLoggingFlags & 2)
        WszOutputDebugString(wszBuffer);
    if (g_dwLoggingFlags & 4)
    {
        MAKE_UTF8PTR_FROMWIDE_NOTHROW(szMessage, wszBuffer);
        if(szMessage != NULL)
            LOG((LF_SECURITY, LL_INFO100, szMessage));
    }

    SetLastError(dwError);
}

#endif // _DEBUG

// Size in bytes of strong name token.
#define SN_SIZEOF_TOKEN     8

enum StrongNameCachedCsp {
    None = -1,
    Sha1CachedCsp = 0,
    Sha2CachedCsp = Sha1CachedCsp + 1,
    CachedCspCount = Sha2CachedCsp + 1
};

// We cache a couple of things on a per thread basis: the last error encountered
// and (potentially) CSP contexts. The following structure tracks these and is
// allocated lazily as needed.
struct SN_THREAD_CTX {
    DWORD       m_dwLastError;
#if !defined(FEATURE_CORECLR)
    HCRYPTPROV  m_hProv[CachedCspCount];
#endif // !FEATURE_CORECLR
};

#endif // !DACCESS_COMPILE

// Macro containing common code used at the start of most APIs.
#define SN_COMMON_PROLOG() do {                             \
    HRESULT __hr = InitStrongName();                        \
    if (FAILED(__hr)) {                                     \
        SetStrongNameErrorInfo(__hr);                       \
        retVal = FALSE;                                     \
        goto Exit;                                          \
    }                                                       \
    SetStrongNameErrorInfo(S_OK);                           \
} while (0)

// Macro to return an error from a SN entrypoint API
#define SN_ERROR(__hr) do {                                 \
    if (FAILED(__hr)) {                                     \
        SetStrongNameErrorInfo(__hr);                       \
        retVal = FALSE;                                     \
        goto Exit;                                          \
    }                                                       \
} while (false)

// Determine the size of a PublicKeyBlob structure given the size of the key
// portion.
#define SN_SIZEOF_KEY(_pKeyBlob) (offsetof(PublicKeyBlob, PublicKey) + GET_UNALIGNED_VAL32(&(_pKeyBlob)->cbPublicKey))

// We allow a special abbreviated form of the Microsoft public key (16 bytes
// long: 0 for both alg ids, 4 for key length and 4 bytes of 0 for the key
// itself). This allows us to build references to system libraries that are
// platform neutral (so a 3rd party can build mscorlib replacements). The
// special zero PK is just shorthand for the local runtime's real system PK,
// which is always used to perform the signature verification, so no security
// hole is opened by this. Therefore we need to store a copy of the real PK (for
// this platform) here.

// the actual definition of the microsoft key is in separate file to allow custom keys
#include "thekey.h"


#define SN_THE_KEY() ((PublicKeyBlob*)g_rbTheKey)
#define SN_SIZEOF_THE_KEY() sizeof(g_rbTheKey)

#define SN_THE_KEYTOKEN() ((PublicKeyBlob*)g_rbTheKeyToken)

// Determine if the given public key blob is the neutral key.
#define SN_IS_NEUTRAL_KEY(_pk) (SN_SIZEOF_KEY((PublicKeyBlob*)(_pk)) == sizeof(g_rbNeutralPublicKey) && \
                                memcmp((_pk), g_rbNeutralPublicKey, sizeof(g_rbNeutralPublicKey)) == 0)

#define SN_IS_THE_KEY(_pk) (SN_SIZEOF_KEY((PublicKeyBlob*)(_pk)) == sizeof(g_rbTheKey) && \
                                memcmp((_pk), g_rbTheKey, sizeof(g_rbTheKey)) == 0)


#ifdef FEATURE_CORECLR

// Silverlight platform key
#define SN_THE_SILVERLIGHT_PLATFORM_KEYTOKEN() ((PublicKeyBlob*)g_rbTheSilverlightPlatformKeyToken)
#define SN_IS_THE_SILVERLIGHT_PLATFORM_KEY(_pk) (SN_SIZEOF_KEY((PublicKeyBlob*)(_pk)) == sizeof(g_rbTheSilverlightPlatformKey) && \
                                memcmp((_pk), g_rbTheSilverlightPlatformKey, sizeof(g_rbTheSilverlightPlatformKey)) == 0)

// Silverlight key
#define SN_IS_THE_SILVERLIGHT_KEY(_pk) (SN_SIZEOF_KEY((PublicKeyBlob*)(_pk)) == sizeof(g_rbTheSilverlightKey) && \
                                memcmp((_pk), g_rbTheSilverlightKey, sizeof(g_rbTheSilverlightKey)) == 0)

#define SN_THE_SILVERLIGHT_KEYTOKEN() ((PublicKeyBlob*)g_rbTheSilverlightKeyToken)

#ifdef FEATURE_WINDOWSPHONE
// Microsoft.Phone.* key
#define SN_THE_MICROSOFT_PHONE_KEYTOKEN() ((PublicKeyBlob*)g_rbTheMicrosoftPhoneKeyToken)

#define SN_IS_THE_MICROSOFT_PHONE_KEY(_pk) (SN_SIZEOF_KEY((PublicKeyBlob*)(_pk)) == sizeof(g_rbTheMicrosoftPhoneKey) && \
                                memcmp((_pk), g_rbTheMicrosoftPhoneKey, sizeof(g_rbTheMicrosoftPhoneKey)) == 0)

// Microsoft.Xna.* key
#define SN_THE_MICROSOFT_XNA_KEYTOKEN() ((PublicKeyBlob*)g_rbTheMicrosoftXNAKeyToken)

#define SN_IS_THE_MICROSOFT_XNA_KEY(_pk) (SN_SIZEOF_KEY((PublicKeyBlob*)(_pk)) == sizeof(g_rbTheMicrosoftXNAKey) && \
                                memcmp((_pk), g_rbTheMicrosoftXNAKey, sizeof(g_rbTheMicrosoftXNAKey)) == 0)

#endif // FEATURE_WINDOWSPHONE
#endif // FEATURE_CORECLR

#if !defined(FEATURE_CORECLR)

#ifdef FEATURE_STRONGNAME_MIGRATION
#include "caparser.h"
#include "custattr.h"
#include "cahlprinternal.h"
#endif // FEATURE_STRONGNAME_MIGRATION

// The maximum length of CSP name we support (in characters).
#define SN_MAX_CSP_NAME 1024

// If we're being built as a standalone library, then we shouldn't redirect through the hosting APIs
#if !STRONGNAME_IN_VM

#undef MapViewOfFile
#undef UnmapViewOfFile

#define CLRMapViewOfFile MapViewOfFile
#define CLRUnmapViewOfFile UnmapViewOfFile

#if FEATURE_STANDALONE_SN && !FEATURE_CORECLR

// We will need to call into shim, therefore include new hosting APIs
#include "metahost.h"
#include "clrinternal.h"

#endif //FEATURE_STANDALONE_SN && !FEATURE_CORECLR

#define DONOT_DEFINE_ETW_CALLBACK

#endif // !STRONGNAME_IN_VM
#include "eventtracebase.h"

#ifndef DACCESS_COMPILE

// Flag indicating whether the initialization of the strong name APIs has been completed.
BOOLEAN g_bStrongNamesInitialized = FALSE;

// Flag indicating whether it's OK to cache the results of verifying an assembly
// whose file is accessible to users.
BOOLEAN g_fCacheVerify = TRUE;

// Algorithm IDs for hashing and signing. Like the CSP name, these values are
// read from the registry at initialization time.
ALG_ID g_uHashAlgId;
ALG_ID g_uSignAlgId;

// Flag read from the registry at initialization time. It controls the key spec 
// to be used. AT_SIGNATURE will be the default.
DWORD g_uKeySpec;

// CSP provider type. PROV_RSA_FULL will be the default.
DWORD g_uProvType;

// Critical section used to serialize some non-thread safe crypto APIs.
CRITSEC_COOKIE g_rStrongNameMutex = NULL;

// Name of CSP to use. This is read from the registry at initialization time. If
// not found we look up a CSP by hashing and signing algorithms (see below) or
// use the default CSP.

BOOLEAN g_bHasCSPName = FALSE;
WCHAR g_wszCSPName[SN_MAX_CSP_NAME + 1] = {0};

// Flag read from the registry at initialization time. Controls whether we use
// machine or user based key containers.
BOOLEAN g_bUseMachineKeyset = TRUE;

#ifdef FEATURE_STRONGNAME_DELAY_SIGNING_ALLOWED
// Verification Skip Records
//
// These are entries in the registry (usually set up by SN) that control whether
// an assembly needs to pass signature verification to be considered valid (i.e.
// return TRUE from StrongNameSignatureVerification). This is useful during
// development when it's not feasible to fully sign each assembly on each build.
// Assemblies to be skipped can be specified by name and public key token, all
// assemblies with a given public key token or just all assemblies. Each entry
// can be further qualified by a list of user names to which the records
// applies. When matching against an entry, the most specific one wins.
//
// We read these entries at startup time and place them into a global, singly
// linked, NULL terminated list.

// Structure used to represent each record we find in the registry.
struct SN_VER_REC {
    SN_VER_REC     *m_pNext;                    // Pointer to next record (or NULL)
    WCHAR          *m_wszAssembly;              // Assembly name/public key token as a string
    WCHAR          *m_mszUserList;              // Pointer to multi-string list of valid users (or NULL)
    WCHAR          *m_wszTestPublicKey;         // Test public key to use during strong name verification (or NULL)
};

// Head of the list of entries we found in the registry during initialization.
SN_VER_REC *g_pVerificationRecords = NULL;
#endif // FEATURE_STRONGNAME_DELAY_SIGNING_ALLOWED

#ifdef FEATURE_STRONGNAME_MIGRATION

struct SN_REPLACEMENT_KEY_REC {
    SN_REPLACEMENT_KEY_REC *m_pNext;
    BYTE               *m_pbReplacementKey;
    ULONG               m_cbReplacementKey;
};

struct SN_REVOCATION_REC {
    SN_REVOCATION_REC      *m_pNext;
    BYTE                   *m_pbRevokedKey;
    ULONG                   m_cbRevokedKey;
    SN_REPLACEMENT_KEY_REC *m_pReplacementKeys;
};

SN_REVOCATION_REC *g_pRevocationRecords = NULL;

#endif // FEATURE_STRONGNAME_MIGRATION

#endif // #ifndef DACCESS_COMPILE



#include "thetestkey.h"

#ifndef DACCESS_COMPILE

// The actions that can be performed upon opening a CSP with LocateCSP.
#define SN_OPEN_CONTAINER   0
#define SN_IGNORE_CONTAINER 1
#define SN_CREATE_CONTAINER 2
#define SN_DELETE_CONTAINER 3
#define SN_HASH_SHA1_ONLY   4

// Macro to aid in setting flags for CryptAcquireContext based on container
// actions above.
#define SN_CAC_FLAGS(_act)                                                                      \
    (((_act) == SN_OPEN_CONTAINER ? 0 :                                                         \
      ((_act) == SN_HASH_SHA1_ONLY) || ((_act) == SN_IGNORE_CONTAINER) ? CRYPT_VERIFYCONTEXT :  \
      (_act) == SN_CREATE_CONTAINER ? CRYPT_NEWKEYSET :                                         \
      (_act) == SN_DELETE_CONTAINER ? CRYPT_DELETEKEYSET :                                      \
      0) |                                                                                      \
     (g_bUseMachineKeyset ? CRYPT_MACHINE_KEYSET : 0))

// Substitute a strong name error if the error we're wrapping is not transient
FORCEINLINE HRESULT SubstituteErrorIfNotTransient(HRESULT hrOriginal, HRESULT hrSubstitute)
{
    return Exception::IsTransient(hrOriginal) ? hrOriginal : hrSubstitute;
}

// Private routine prototypes.
SN_THREAD_CTX *GetThreadContext();
VOID SetStrongNameErrorInfo(DWORD dwStatus);
HCRYPTPROV LocateCSP(LPCWSTR    wszKeyContainer,
                     DWORD      dwAction,
                     ALG_ID     uHashAlgId = 0,
                     ALG_ID     uSignAlgId = 0);
VOID FreeCSP(HCRYPTPROV hProv);
HCRYPTPROV LookupCachedCSP(StrongNameCachedCsp cspNumber);
VOID CacheCSP(HCRYPTPROV hProv, StrongNameCachedCsp cspNumber);
BOOLEAN IsCachedCSP(HCRYPTPROV hProv);
HRESULT ReadRegistryConfig();
BOOLEAN LoadCryptoApis();
#ifdef FEATURE_STRONGNAME_DELAY_SIGNING_ALLOWED
HRESULT ReadVerificationRecords();

#ifdef FEATURE_STRONGNAME_MIGRATION
HRESULT ReadRevocationRecords();
#endif // FEATURE_STRONGNAME_MIGRATION
SN_VER_REC *GetVerificationRecord(__in_z __deref LPWSTR wszAssemblyName, PublicKeyBlob *pPublicKey);
#endif // FEATURE_STRONGNAME_DELAY_SIGNING_ALLOWED

BOOLEAN IsValidUser(__in_z WCHAR *mszUserList);
BOOLEAN GetKeyContainerName(LPCWSTR *pwszKeyContainer, BOOLEAN *pbTempContainer);
VOID FreeKeyContainerName(LPCWSTR wszKeyContainer, BOOLEAN bTempContainer);
HRESULT GetMetadataImport(__in const SN_LOAD_CTX *pLoadCtx,
                          __in mdAssembly *ptkAssembly,
                          __out IMDInternalImport **ppMetaDataImport);
HRESULT FindPublicKey(const SN_LOAD_CTX *pLoadCtx,
                      __out_ecount_opt(cchAssemblyName) LPWSTR         wszAssemblyName,
                      DWORD cchAssemblyName,
                      __out PublicKeyBlob **ppPublicKey,
                      DWORD *pcbPublicKey = NULL);
PublicKeyBlob *GetPublicKeyFromHex(LPCWSTR wszPublicKeyHexString);
BOOLEAN RehashModules(SN_LOAD_CTX *pLoadCtx, LPCWSTR szFilePath);
HRESULT VerifySignature(SN_LOAD_CTX *pLoadCtx,
                        DWORD dwInFlags,
                        PublicKeyBlob *pRealEcmaPublicKey,
                        DWORD *pdwOutFlags);
HRESULT InitStrongNameCriticalSection();
HRESULT InitStrongName();
typedef BOOLEAN (*HashFunc)(HCRYPTHASH hHash, PBYTE start, DWORD length, DWORD flags, void* cookie);
BOOLEAN ComputeHash(SN_LOAD_CTX *pLoadCtx, HCRYPTHASH hHash, HashFunc func, void* cookie);
bool VerifyKeyMatchesAssembly(PublicKeyBlob * pAssemblySignaturePublicKey, __in_z LPCWSTR wszKeyContainer, BYTE *pbKeyBlob, ULONG cbKeyBlob, DWORD dwFlags);

#ifdef FEATURE_STRONGNAME_MIGRATION
HRESULT GetVerifiedSignatureKey(__in SN_LOAD_CTX *pLoadCtx, __out PublicKeyBlob **ppPublicKey, __out DWORD *pcbPublicKey = NULL);
#endif // FEATURE_STRONGNAME_MIGRATION

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)

void DbgCount(__in_z WCHAR *szCounterName)
{

#ifndef FEATURE_CORECLR
    if (g_fLoggingInitialized && !(g_dwLoggingFlags & 4))
        return;

    DWORD dwError = GetLastError();

    if (!g_fLoggingInitialized) {
        g_dwLoggingFlags = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MscorsnLogging);
        g_fLoggingInitialized = TRUE;
    }

    if (!(g_dwLoggingFlags & 4)) {
        SetLastError(dwError);
        return;
    }

    HKEY    hKey = NULL;
    DWORD   dwCounter = 0;
    DWORD   dwBytes;

    if (WszRegCreateKeyEx(HKEY_LOCAL_MACHINE,
                          SN_CONFIG_KEY_W W("\\Counters"),
                          0,
                          NULL,
                          0,
                          KEY_ALL_ACCESS,
                          NULL,
                          &hKey,
                          NULL) != ERROR_SUCCESS)
        goto End;

    WszRegQueryValueEx(hKey, szCounterName, NULL, NULL, (BYTE*)&dwCounter, &dwBytes);
    dwCounter++;
    WszRegSetValueEx(hKey, szCounterName, NULL, REG_DWORD, (BYTE*)&dwCounter, sizeof(DWORD));

 End:
    if (hKey)
        RegCloseKey(hKey);
    SetLastError(dwError);

#endif //#ifndef FEATURE_CORECLR

}


void HexDump(BYTE  *pbData,
    DWORD  cbData)
{
    if (g_dwLoggingFlags == 0)
        return;

    DWORD dwRow, dwCol;
    WCHAR  wszBuffer[1024];
    WCHAR  *wszPtr = wszBuffer;

#define SN_PUSH0(_fmt)          do { wszPtr += swprintf_s(wszPtr, COUNTOF(wszBuffer) - (wszPtr - wszBuffer), _fmt); } while (false)
#define SN_PUSH1(_fmt, _arg1)   do { wszPtr += swprintf_s(wszPtr, COUNTOF(wszBuffer) - (wszPtr - wszBuffer), _fmt, _arg1); } while (false)

    wszBuffer[0] = W('\0');

    for (dwRow = 0; dwRow < ((cbData + 15) / 16); dwRow++) {
        SN_PUSH1(W("%08p "), pbData + (16 * dwRow));
        for (dwCol = 0; dwCol < 16; dwCol++)
        if (((dwRow * 16) + dwCol) < cbData)
            SN_PUSH1(W("%02X "), pbData[(dwRow * 16) + dwCol]);
        else
            SN_PUSH0(W("   "));
        for (dwCol = 0; dwCol < 16; dwCol++)
        if (((dwRow * 16) + dwCol) < cbData) {
            unsigned char c = pbData[(dwRow * 16) + dwCol];
            if ((c >= 32) && (c <= 127))
                SN_PUSH1(W("%c"), c);
            else
                SN_PUSH0(W("."));
        } else
            SN_PUSH0(W(" "));
        SN_PUSH0(W("\n"));
    }
#undef SN_PUSH1
#undef SN_PUSH0

    _ASSERTE(wszPtr < &wszBuffer[COUNTOF(wszBuffer)]);

    Log(W("%s"), wszBuffer);
}

#else // _DEBUG && !DACCESS_COMPILE

#define HexDump(x)
#define DbgCount(x)

#endif // _DEBUG && !DACCESS_COMPILE


BOOLEAN CalculateSize(HCRYPTHASH hHash, PBYTE start, DWORD length, DWORD flags, void* cookie)
{
    *(size_t*)cookie += length;
    return TRUE;
}

struct CopyDataBufferDesc
{
    PBYTE pbData;
    DWORD cbDataSize;
};

BOOLEAN CopyData(HCRYPTHASH hHash, PBYTE start, DWORD length, DWORD flags, void* cookie)
{
    _ASSERTE(cookie);

    CopyDataBufferDesc *pBuffer = reinterpret_cast<CopyDataBufferDesc *>(cookie);
    _ASSERTE(pBuffer->pbData);

    memcpy_s(pBuffer->pbData, pBuffer->cbDataSize, start, length);
    pBuffer->pbData += length;

    _ASSERTE(pBuffer->cbDataSize >= length);
    pBuffer->cbDataSize = pBuffer->cbDataSize >= length ? pBuffer->cbDataSize - length : 0;

    return TRUE;
}

BOOLEAN CalcHash(HCRYPTHASH hHash, PBYTE start, DWORD length, DWORD flags, void* cookie)
{
    return CryptHashData(hHash, start, length, flags);
}

VOID
WINAPI Fls_Callback (
    IN PVOID lpFlsData
    )
{
    STATIC_CONTRACT_SO_TOLERANT;
    SN_THREAD_CTX *pThreadCtx = (SN_THREAD_CTX*)lpFlsData;
    if (pThreadCtx != NULL) {
        for(ULONG i = 0; i < CachedCspCount; i++)
        {
            if (pThreadCtx->m_hProv[i])
                CryptReleaseContext(pThreadCtx->m_hProv[i], 0);
        }
        
        delete pThreadCtx;
    }
}

HRESULT InitStrongNameCriticalSection()
{
    if (g_rStrongNameMutex)
        return S_OK;

    CRITSEC_COOKIE pv = ClrCreateCriticalSection(CrstStrongName, CRST_DEFAULT);
    if (pv == NULL)
        return E_OUTOFMEMORY;

    if (InterlockedCompareExchangeT(&g_rStrongNameMutex, pv, NULL) != NULL)
        ClrDeleteCriticalSection(pv);
    return S_OK;
}

HRESULT InitStrongName()
{
    HRESULT hr = S_OK;
    if (g_bStrongNamesInitialized)
        return hr;

    // Read CSP configuration info from the registry (if provided).
    hr = ReadRegistryConfig();
    if (FAILED(hr))
        return hr;

    // Associate a callback for freeing our TLS data.
    ClrFlsAssociateCallback(TlsIdx_StrongName, Fls_Callback);

    g_bStrongNamesInitialized = TRUE;

    return hr;
}

// Generate a new key pair for strong name use.
SNAPI StrongNameKeyGen(LPCWSTR  wszKeyContainer,    // [in] desired key container name, must be a non-empty string
                       DWORD    dwFlags,            // [in] flags (see below)
                       BYTE   **ppbKeyBlob,         // [out] public/private key blob
                       ULONG   *pcbKeyBlob)
{
    BOOLEAN retVal = FALSE;
    BEGIN_ENTRYPOINT_VOIDRET;

    SN_COMMON_PROLOG();

    if (wszKeyContainer == NULL && ppbKeyBlob == NULL)
        SN_ERROR(E_INVALIDARG);
    if (ppbKeyBlob != NULL && pcbKeyBlob == NULL)
        SN_ERROR(E_POINTER);

    DWORD dwKeySize;

    // We set a key size of 1024 if we're using the default
    // signing algorithm (RSA), otherwise we leave it at the default.
    if (g_uSignAlgId == CALG_RSA_SIGN)
        dwKeySize = 1024;
    else
        dwKeySize = 0;

    retVal = StrongNameKeyGenEx(wszKeyContainer, dwFlags, dwKeySize, ppbKeyBlob, pcbKeyBlob);

Exit:
    END_ENTRYPOINT_VOIDRET;
    return retVal;
}

// Generate a new key pair with the specified key size for strong name use.
SNAPI StrongNameKeyGenEx(LPCWSTR  wszKeyContainer,    // [in] desired key container name, must be a non-empty string
                         DWORD    dwFlags,            // [in] flags (see below)
                         DWORD    dwKeySize,          // [in] desired key size.
                         BYTE   **ppbKeyBlob,         // [out] public/private key blob
                         ULONG   *pcbKeyBlob)
{
    BOOLEAN     retVal = FALSE;

    BEGIN_ENTRYPOINT_VOIDRET;

    HCRYPTPROV  hProv = NULL;
    HCRYPTKEY   hKey = NULL;
    BOOLEAN     bTempContainer = FALSE;

    SNLOG((W("StrongNameKeyGenEx(\"%s\", %08X, %08X, %08X, %08X)\n"), wszKeyContainer, dwFlags, dwKeySize, ppbKeyBlob, pcbKeyBlob));

    SN_COMMON_PROLOG();

    if (wszKeyContainer == NULL && ppbKeyBlob == NULL)
        SN_ERROR(E_INVALIDARG);
    if (ppbKeyBlob != NULL && pcbKeyBlob == NULL)
        SN_ERROR(E_POINTER);

    // Check to see if a temporary container name is needed.
    _ASSERTE((wszKeyContainer != NULL) || !(dwFlags & SN_LEAVE_KEY));
    if (!GetKeyContainerName(&wszKeyContainer, &bTempContainer))
    {
        goto Exit;
    }

    // Open a CSP and container.
    hProv = LocateCSP(wszKeyContainer, SN_CREATE_CONTAINER);
    if (!hProv)
        goto Error;


    // Generate the new key pair, try for exportable first.
    // Note: The key size in bits is encoded in the upper
    // 16-bits of a DWORD (and OR'd together with other flags for the
    // CryptGenKey call). 
    if (!CryptGenKey(hProv, g_uKeySpec, (dwKeySize << 16) | CRYPT_EXPORTABLE, &hKey)) {
        SNLOG((W("Couldn't create exportable key, trying for non-exportable: %08X\n"), GetLastError()));
        if (!CryptGenKey(hProv, g_uKeySpec, dwKeySize << 16, &hKey)) {
            SNLOG((W("Couldn't create key pair: %08X\n"), GetLastError()));
            goto Error;
        }
    }

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
    if (g_bHasCSPName) {
        ALG_ID  uAlgId;
        DWORD   dwAlgIdLen = sizeof(uAlgId);
        // Check that signature algorithm used was the one we expected.
        if (CryptGetKeyParam(hKey, KP_ALGID, (BYTE*)&uAlgId, &dwAlgIdLen, 0)) {
            _ASSERTE(uAlgId == g_uSignAlgId);
        } else
            SNLOG((W("Failed to get key params: %08X\n"), GetLastError()));
    }
#endif // _DEBUG

    // If the user wants the key pair back, attempt to export it.
    if (ppbKeyBlob) {

        // Calculate length of blob first;
        if (!CryptExportKey(hKey, 0, PRIVATEKEYBLOB, 0, NULL, pcbKeyBlob)) {
            SNLOG((W("Couldn't export key pair: %08X\n"), GetLastError()));
            goto Error;
        }

        // Allocate a buffer of the right size.
        *ppbKeyBlob = new (nothrow) BYTE[*pcbKeyBlob];
        if (*ppbKeyBlob == NULL) {
            SetLastError(E_OUTOFMEMORY);
            goto Error;
        }

        // Export the key pair.
        if (!CryptExportKey(hKey, 0, PRIVATEKEYBLOB, 0, *ppbKeyBlob, pcbKeyBlob)) {
            SNLOG((W("Couldn't export key pair: %08X\n"), GetLastError()));
            delete[] *ppbKeyBlob;
            *ppbKeyBlob = NULL;
            goto Error;
        }
    }

    // Destroy the key handle (but not the key pair itself).
    CryptDestroyKey(hKey);
    hKey = NULL;

    // Release the CSP.
    FreeCSP(hProv);

    // If the user didn't explicitly want to keep the key pair around, delete the
    // key container.
    if (!(dwFlags & SN_LEAVE_KEY) || bTempContainer)
        LocateCSP(wszKeyContainer, SN_DELETE_CONTAINER);

    // Free temporary key container name if allocated.
    FreeKeyContainerName(wszKeyContainer, bTempContainer);
    retVal = TRUE;
    goto Exit;

 Error:
    SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
    if (hKey)
        CryptDestroyKey(hKey);
    if (hProv) {
        FreeCSP(hProv);
        if (!(dwFlags & SN_LEAVE_KEY) || bTempContainer)
            LocateCSP(wszKeyContainer, SN_DELETE_CONTAINER);
    }
    FreeKeyContainerName(wszKeyContainer, bTempContainer);

 Exit:
    END_ENTRYPOINT_VOIDRET;
    return retVal;
}


// Import key pair into a key container.
SNAPI StrongNameKeyInstall(LPCWSTR  wszKeyContainer,// [in] desired key container name, must be a non-empty string
                           BYTE    *pbKeyBlob,      // [in] public/private key pair blob
                           ULONG    cbKeyBlob)
{
    BOOLEAN retVal = FALSE;

    BEGIN_ENTRYPOINT_VOIDRET;

    HCRYPTPROV  hProv = NULL;
    HCRYPTKEY   hKey = NULL;

    SNLOG((W("StrongNameKeyInstall(\"%s\", %08X, %08X)\n"), wszKeyContainer, pbKeyBlob, cbKeyBlob));

    SN_COMMON_PROLOG();

    if (wszKeyContainer == NULL)
        SN_ERROR(E_POINTER);
    if (pbKeyBlob == NULL)
        SN_ERROR(E_POINTER);
    if (cbKeyBlob == 0)
        SN_ERROR(E_INVALIDARG);

    // Open a CSP and container.
    hProv = LocateCSP(wszKeyContainer, SN_CREATE_CONTAINER);
    if (!hProv) {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        goto Exit;
    }

    // Import the key pair.
    if (!CryptImportKey(hProv,
                           pbKeyBlob,
                           cbKeyBlob,
                           0, 0, &hKey)) {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        FreeCSP(hProv);
        goto Exit;
    }

    // Release the CSP.
    FreeCSP(hProv);
    retVal = TRUE;
Exit:

    END_ENTRYPOINT_VOIDRET;
    return retVal;
}


// Delete a key pair.
SNAPI StrongNameKeyDelete(LPCWSTR wszKeyContainer)  // [in] desired key container name
{
    BOOLEAN retVal = FALSE;

    BEGIN_ENTRYPOINT_VOIDRET;

    HCRYPTPROV      hProv;

    SNLOG((W("StrongNameKeyDelete(\"%s\")\n"), wszKeyContainer));

    SN_COMMON_PROLOG();

    if (wszKeyContainer == NULL)
        SN_ERROR(E_POINTER);

    // Open and delete the named container.
    hProv = LocateCSP(wszKeyContainer, SN_DELETE_CONTAINER);
    if (hProv) {
        // Returned handle isn't actually valid in the delete case, so we're
        // finished.
        retVal = TRUE;
    } else {
        SetStrongNameErrorInfo(CORSEC_E_CONTAINER_NOT_FOUND);
        retVal = FALSE;
    }
Exit:

    END_ENTRYPOINT_VOIDRET;
    return retVal;

}

// Retrieve the public portion of a key pair.
SNAPI StrongNameGetPublicKey (LPCWSTR   wszKeyContainer,    // [in] desired key container name
                              BYTE     *pbKeyBlob,          // [in] public/private key blob (optional)
                              ULONG     cbKeyBlob,
                              BYTE    **ppbPublicKeyBlob,   // [out] public key blob
                              ULONG    *pcbPublicKeyBlob)
{
    LIMITED_METHOD_CONTRACT;
    BOOLEAN retVal = FALSE;

    BEGIN_ENTRYPOINT_VOIDRET;
    
    retVal = StrongNameGetPublicKeyEx(
        wszKeyContainer,
        pbKeyBlob,
        cbKeyBlob,
        ppbPublicKeyBlob,
        pcbPublicKeyBlob,
        0,
        0);
    
    END_ENTRYPOINT_VOIDRET;
    return retVal;
}

// Holder for any HCRYPTPROV handles allocated by the strong name APIs
typedef Wrapper<HCRYPTPROV, DoNothing, FreeCSP, 0> HandleStrongNameCspHolder;


SNAPI StrongNameGetPublicKeyEx (LPCWSTR   wszKeyContainer,    // [in] desired key container name
                                BYTE     *pbKeyBlob,          // [in] public/private key blob (optional)
                                ULONG     cbKeyBlob,
                                BYTE    **ppbPublicKeyBlob,   // [out] public key blob
                                ULONG    *pcbPublicKeyBlob,
                                ULONG     uHashAlgId,
                                ULONG     uReserved)         // reserved for future use as uSigAlgId (signature algorithm id)
{
    BOOLEAN         retVal = FALSE;

    BEGIN_ENTRYPOINT_VOIDRET;

    HandleStrongNameCspHolder hProv(NULL);
    CapiKeyHolder             hKey(NULL);
    DWORD                     dwKeyLen;
    PublicKeyBlob            *pKeyBlob;
    DWORD                     dwSigAlgIdLen;

    SNLOG((W("StrongNameGetPublicKeyEx(\"%s\", %08X, %08X, %08X, %08X, %08X)\n"), wszKeyContainer, pbKeyBlob, cbKeyBlob, ppbPublicKeyBlob, pcbPublicKeyBlob, uHashAlgId));

    SN_COMMON_PROLOG();

    if (wszKeyContainer == NULL && pbKeyBlob == NULL)
        SN_ERROR(E_INVALIDARG);
    if (pbKeyBlob != NULL && !(StrongNameIsEcmaKey(pbKeyBlob, cbKeyBlob) || StrongNameIsValidKeyPair(pbKeyBlob, cbKeyBlob)))
        SN_ERROR(E_INVALIDARG);
    if (ppbPublicKeyBlob == NULL)
        SN_ERROR(E_POINTER);
    if (pcbPublicKeyBlob == NULL)
        SN_ERROR(E_POINTER);
    if (uReserved != 0)
        SN_ERROR(E_INVALIDARG);

    bool fHashAlgorithmValid;
    fHashAlgorithmValid = uHashAlgId == 0 || 
                          (GET_ALG_CLASS(uHashAlgId) == ALG_CLASS_HASH && GET_ALG_SID(uHashAlgId) >= ALG_SID_SHA1 && GET_ALG_SID(uHashAlgId) <= ALG_SID_SHA_512);
    if(!fHashAlgorithmValid)
        SN_ERROR(E_INVALIDARG);

    if(uHashAlgId == 0)
        uHashAlgId = g_uHashAlgId;

    // If we're handed a platform neutral public key, just hand it right back to
    // the user. Well, hand back a copy at least.
    if (pbKeyBlob && cbKeyBlob && SN_IS_NEUTRAL_KEY(pbKeyBlob)) {
        *pcbPublicKeyBlob = sizeof(g_rbNeutralPublicKey);
        *ppbPublicKeyBlob = (BYTE*)g_rbNeutralPublicKey;
        retVal = TRUE;
        goto Exit;
    }

    // Open a CSP. Create a key container if a public/private key blob is
    // provided, otherwise we assume a key container already exists.
    if (pbKeyBlob)
        hProv = LocateCSP(NULL, SN_IGNORE_CONTAINER);
    else
        hProv = LocateCSP(wszKeyContainer, SN_OPEN_CONTAINER);
    if (!hProv)
        goto Error;

    // If a key blob was provided, import the key pair into the container.
    if (pbKeyBlob) {
        if (!CryptImportKey(hProv,
                               pbKeyBlob,
                               cbKeyBlob,
                               0, 0, &hKey))
            goto Error;
    } else {
#if !defined(FEATURE_CORESYSTEM)
        // Else fetch the signature key pair from the container.
        if (!CryptGetUserKey(hProv, g_uKeySpec, &hKey))
            goto Error;
#else // FEATURE_CORESYSTEM
        SetLastError(E_NOTIMPL);
        goto Error;
#endif // !FEATURE_CORESYSTEM
    }

    // Determine the length of the public key part as a blob.
    if (!CryptExportKey(hKey, 0, PUBLICKEYBLOB, 0, NULL, &dwKeyLen))
        goto Error;


#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:22011) // Suppress this PREFast warning which gets triggered by the offset macro expansion.
#endif
    // And then the length of the PublicKeyBlob structure we return to the
    // caller.
    *pcbPublicKeyBlob = offsetof(PublicKeyBlob, PublicKey) + dwKeyLen;
#ifdef _PREFAST_
#pragma warning(pop)
#endif

    // Allocate a large enough buffer.
    *ppbPublicKeyBlob = new (nothrow) BYTE[*pcbPublicKeyBlob];
    if (*ppbPublicKeyBlob == NULL) {
        SetLastError(E_OUTOFMEMORY);
        goto Error;
    }

    pKeyBlob = (PublicKeyBlob*)*ppbPublicKeyBlob;

    // Extract the public part as a blob.
    if (!CryptExportKey(hKey, 0, PUBLICKEYBLOB, 0, pKeyBlob->PublicKey, &dwKeyLen)) {
        delete[] *ppbPublicKeyBlob;
        *ppbPublicKeyBlob = NULL;
        goto Error;
    }

    // Extract key's signature algorithm and store it in the key blob.
    dwSigAlgIdLen = sizeof(unsigned int);
    ALG_ID SigAlgID;
    if (!CryptGetKeyParam(hKey, KP_ALGID, (BYTE*)&SigAlgID, &dwSigAlgIdLen, 0)) {
        delete[] *ppbPublicKeyBlob;
        *ppbPublicKeyBlob = NULL;
        goto Error;
    }
    SET_UNALIGNED_VAL32(&pKeyBlob->SigAlgID, SigAlgID);

    // Fill in the other public key blob fields.
    SET_UNALIGNED_VAL32(&pKeyBlob->HashAlgID, uHashAlgId);
    SET_UNALIGNED_VAL32(&pKeyBlob->cbPublicKey, dwKeyLen);
    
    retVal = TRUE;
    goto Exit;

Error:
    SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
Exit:
    END_ENTRYPOINT_VOIDRET;
    return retVal;
}

// Hash and sign a manifest.
SNAPI StrongNameSignatureGeneration(LPCWSTR     wszFilePath,        // [in] valid path to the PE file for the assembly
                                    LPCWSTR     wszKeyContainer,    // [in] desired key container name
                                    BYTE       *pbKeyBlob,          // [in] public/private key blob (optional)
                                    ULONG       cbKeyBlob,
                                    BYTE      **ppbSignatureBlob,   // [out] signature blob
                                    ULONG      *pcbSignatureBlob)
{
    BOOL fRetVal = FALSE;
    BEGIN_ENTRYPOINT_VOIDRET;
    fRetVal = StrongNameSignatureGenerationEx(wszFilePath, wszKeyContainer, pbKeyBlob, cbKeyBlob, ppbSignatureBlob, pcbSignatureBlob, 0);
    END_ENTRYPOINT_VOIDRET;
    return fRetVal;
}

HRESULT FindAssemblySignaturePublicKey(const SN_LOAD_CTX *pLoadCtx,
                                       __out PublicKeyBlob **ppPublicKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT hr;
    StrongNameBufferHolder<PublicKeyBlob> result = NULL;

    IfFailRet(FindPublicKey(pLoadCtx, NULL, 0, &result));

#ifdef FEATURE_STRONGNAME_MIGRATION
    PublicKeyBlob *pSignaturePublicKey = NULL;
    IfFailRet(GetVerifiedSignatureKey((SN_LOAD_CTX*) pLoadCtx, &pSignaturePublicKey));
    
    if(hr != S_FALSE)
    {
        result = pSignaturePublicKey;
    }
#endif // FEATURE_STRONGNAME_MIGRATION
    
    *ppPublicKey = result.Extract();

    return S_OK;
}

SNAPI StrongNameSignatureGenerationEx(LPCWSTR     wszFilePath,        // [in] valid path to the PE file for the assembly
                                      LPCWSTR     wszKeyContainer,    // [in] desired key container name
                                      BYTE       *pbKeyBlob,          // [in] public/private key blob (optional)
                                      ULONG       cbKeyBlob,
                                      BYTE      **ppbSignatureBlob,   // [out] signature blob
                                      ULONG      *pcbSignatureBlob,
                                      DWORD       dwFlags)            // [in] modifer flags
{
    BOOLEAN         retVal = FALSE;

    BEGIN_ENTRYPOINT_VOIDRET;
    HandleStrongNameCspHolder hProv(NULL);
    CapiHashHolder            hHash(NULL);
    NewArrayHolder<BYTE>      pbSig(NULL);
    ULONG                     cbSig = 0;
    SN_LOAD_CTX               sLoadCtx;
    BOOLEAN                   bImageLoaded = FALSE;
    ALG_ID                    uHashAlgId;
    StrongNameBufferHolder<PublicKeyBlob> pSignatureKey = NULL;

    SNLOG((W("StrongNameSignatureGenerationEx(\"%s\", \"%s\", %08X, %08X, %08X, %08X, %08X)\n"), wszFilePath, wszKeyContainer, pbKeyBlob, cbKeyBlob, ppbSignatureBlob, pcbSignatureBlob, dwFlags));

    SN_COMMON_PROLOG();
    
    uHashAlgId = g_uHashAlgId;

    if (pbKeyBlob != NULL && !StrongNameIsValidKeyPair(pbKeyBlob, cbKeyBlob))
        SN_ERROR(E_INVALIDARG);
    if (ppbSignatureBlob != NULL && pcbSignatureBlob == NULL)
        SN_ERROR(E_POINTER);
    
    if (wszFilePath != NULL) {
        // Map the assembly into memory.
        sLoadCtx.m_fReadOnly = FALSE;
        if (!LoadAssembly(&sLoadCtx, wszFilePath))
            goto Error;
        bImageLoaded = TRUE;

        // If we've asked to recalculate the file hashes of linked modules we have
        // to load the metadata engine and search for file references.
        if (dwFlags & SN_SIGN_ALL_FILES)
            if (!RehashModules(&sLoadCtx, wszFilePath))
                goto Error;

        // If no key pair is provided, then we were only called to re-compute the hashes of
        // linked modules in the assembly.
        if (!wszKeyContainer && !pbKeyBlob)
        {
            retVal = TRUE;
            goto Exit;
        }

        HRESULT hr;
        if(FAILED(hr = FindAssemblySignaturePublicKey(&sLoadCtx, &pSignatureKey)))
        {
            SN_ERROR(hr);
        }
        
        // Ecma key has an algorithm of zero, so we ignore that case.
        ALG_ID uKeyHashAlgId = GET_UNALIGNED_VAL32(&pSignatureKey->HashAlgID);
        if(uKeyHashAlgId != 0) 
        {
            uHashAlgId = uKeyHashAlgId;
        }
    }

    if (wszKeyContainer || pbKeyBlob) { // We have a key pair in a container, or in a blob
        // Open a CSP. If a public/private key blob is provided, use CRYPT_VERIFYCONTEXT,
        // otherwise we assume the key container already exists.
        if (pbKeyBlob)
            hProv = LocateCSP(NULL, SN_IGNORE_CONTAINER, uHashAlgId);
        else
            hProv = LocateCSP(wszKeyContainer, SN_OPEN_CONTAINER, uHashAlgId);

        if (hProv.GetValue() == NULL)
            goto Error;

        // If a key blob was provided, import the key pair into the container.
        // This might be the real key or the test-sign key. In the case of test signing,
        // there's no way to specify hash algorithm, so we use the one from the 
        // assembly signature public key (in the metadata table, or the migration attribute).
        if (pbKeyBlob) {
            // The provider holds a reference to the key, so we don't need to 
            // keep one around.
            CapiKeyHolder hKey(NULL); 
            if (!CryptImportKey(hProv,
                                pbKeyBlob,
                                cbKeyBlob,
                                0, 
                                0, 
                                &hKey))
                goto Error;
        }

        // Create a hash object.
        if (!CryptCreateHash(hProv, uHashAlgId, 0, 0, &hHash))
            goto Error;

        // Compute size of the signature blob.
        if (!CryptSignHashW(hHash, g_uKeySpec, NULL, 0, NULL, &cbSig))
            goto Error;

        // If the caller only wants the size of the signature, return it now and
        // exit.
        // RSA signature length is independent of the hash size, so hash algorithm 
        // doesn't matter here (we don't know the algorithm if the assembly path was passed in as NULL)
        if (wszFilePath == NULL) {
            *pcbSignatureBlob = cbSig;
            retVal = TRUE;
            goto Exit;
        }
    }

    // Verify that the public key of the assembly being signed matches the private key we're signing with
    if ((wszKeyContainer != NULL || pbKeyBlob != NULL) && !VerifyKeyMatchesAssembly(pSignatureKey, wszKeyContainer, pbKeyBlob, cbKeyBlob, dwFlags))
    {
        SetLastError(StrongNameErrorInfo());
        goto Error;
    }

    // We set a bit in the header to indicate we're fully signing the assembly.
    if (!(dwFlags & SN_TEST_SIGN))
        sLoadCtx.m_pCorHeader->Flags |= VAL32(COMIMAGE_FLAGS_STRONGNAMESIGNED);
    else
        sLoadCtx.m_pCorHeader->Flags &= ~VAL32(COMIMAGE_FLAGS_STRONGNAMESIGNED);

    // Destroy the old hash object and create a new one
    // because CryptoAPI says you can't reuse a hash once you've signed it
    // Note that this seems to work with MS-based CSPs but breaks on
    // at least newer nCipher CSPs.
    if (!CryptCreateHash(hProv, uHashAlgId, 0, 0, &hHash))
        goto Error;

    // Compute a hash over the image.
    if (!ComputeHash(&sLoadCtx, hHash, CalcHash, NULL))
        goto Error;

    // Allocate the blob.
    pbSig = new (nothrow) BYTE[cbSig];
    if (pbSig == NULL) {
        SetLastError(E_OUTOFMEMORY);
        goto Error;
    }

    // Compute a signature blob over the hash of the manifest.
    if (!CryptSignHashW(hHash, g_uKeySpec, NULL, 0, pbSig, &cbSig))
        goto Error;

    // Check the signature size
    if (sLoadCtx.m_cbSignature != cbSig) {
        SetLastError(CORSEC_E_SIGNATURE_MISMATCH);
        goto Error;
    }

    // If the user hasn't asked for the signature to be returned as a pointer, write it to file.
    if (!ppbSignatureBlob)
    {
        memcpy_s(sLoadCtx.m_pbSignature, sLoadCtx.m_cbSignature, pbSig, cbSig);

        // 
        // Memory-mapped IO in Windows doesn't guarantee that it will update
        // the file's "Modified" timestamp, so we update it ourselves. 
        //
        _ASSERTE(sLoadCtx.m_hFile != INVALID_HANDLE_VALUE);

        FILETIME ft;
        SYSTEMTIME st;

        GetSystemTime(&st);
        
        // We don't care if updating the timestamp fails for any reason.
        if(SystemTimeToFileTime(&st, &ft))
        {
            SetFileTime(sLoadCtx.m_hFile, (LPFILETIME) NULL, (LPFILETIME) NULL, &ft);
        }
    }

    // Unmap the image (automatically recalculates and updates the image
    // checksum).
    bImageLoaded = FALSE;
    if (!UnloadAssembly(&sLoadCtx))
        goto Error;

    if (ppbSignatureBlob) {
        *ppbSignatureBlob = pbSig.Extract();
        *pcbSignatureBlob = cbSig;
    }

    retVal = TRUE;
    goto Exit;

Error:
    SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
Exit:
    if (bImageLoaded)
        UnloadAssembly(&sLoadCtx);
    END_ENTRYPOINT_VOIDRET;
    return retVal;
}

//
// Generate the digest of a delay signed assembly, which can be signed with StrongNameDigestSign.  The digest
// algorithm is determined from the HashAlgID of the assembly's public key blob. 
//
// Parameters:
//  wszFilePath   - path to the delay signed assembly to generate the digest of
//  ppbDigestBlob - on success this will point to a buffer that contains the digest of the wszFilePath
//                  assembly.  This buffer should be freed with StrongNameFreeBuffer.
//  pcbDigestBlob - on success this will point to the size of the digest buffer in *ppbDigestBlob
//  dwFlags       - flags used to control signing.  This is the same set of flags used by
//                  StrongNameSignatureGenerationEx
//

bool StrongNameDigestGenerate_Internal(_In_z_ LPCWSTR                                       wszFilePath,
                                       _Outptr_result_bytebuffer_(*pcbDigestBlob) BYTE**    ppbDigestBlob,
                                       _Out_  ULONG*                                        pcbDigestBlob,
                                       DWORD                                                dwFlags)
{
    // Load up the assembly and find its public key - this tells us which hash algorithm we need to use
    // Note that it cannot be loaded read-only since we need to toggle the fully siged bit in order to
    // calculate the correct hash for the signature.
    StrongNameAssemblyLoadHolder assembly(wszFilePath, false /* read only */);
    if (!assembly.IsLoaded())
    {
        return false;
    }

    // If we were asked to do a full rehashing of all modules that needs to be done before calculating the digest
    if ((dwFlags & SN_SIGN_ALL_FILES) == SN_SIGN_ALL_FILES)
    {
        if (!RehashModules(assembly.GetLoadContext(), wszFilePath))
        {
            return false;
        }
    }

    // During signature verification, the fully signed bit will be set in the assembly's COR header.
    // Therefore, when calculating the digest of the assembly we must toggle this bit in order to make the
    // digest match what will be calculated during verificaiton.  However, we do not want to persist the
    // bit flip on disk, since we're not actually signing the assembly now.  We'll save the current COR
    // flags, flip the bit for digesting, and then restore the COR flags before we finish calculating the
    // digest.
    class AssemblyFlagsHolder
    {
    private:
        IMAGE_COR20_HEADER* m_pHeader;
        DWORD               m_originalFlags;

    public:
        AssemblyFlagsHolder(_In_ IMAGE_COR20_HEADER* pHeader)
            : m_pHeader(pHeader),
              m_originalFlags(pHeader->Flags)
        {
        }

        ~AssemblyFlagsHolder()
        {
            m_pHeader->Flags = m_originalFlags;
        }
    } flagsHolder(assembly.GetLoadContext()->m_pCorHeader);
    assembly.GetLoadContext()->m_pCorHeader->Flags |= VAL32(COMIMAGE_FLAGS_STRONGNAMESIGNED);

    StrongNameBufferHolder<PublicKeyBlob> pPublicKey;
    HRESULT hrPublicKey = FindAssemblySignaturePublicKey(assembly.GetLoadContext(), &pPublicKey);
    if (FAILED(hrPublicKey))
    {
        SetStrongNameErrorInfo(hrPublicKey);
        return false;
    }

    // Generate the digest of the assembly
    HandleStrongNameCspHolder hProv(LocateCSP(nullptr, SN_IGNORE_CONTAINER, pPublicKey->HashAlgID));
    if (!hProv)
    {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        return false;
    }

    CapiHashHolder hHash;
    if (!CryptCreateHash(hProv, pPublicKey->HashAlgID, NULL, 0, &hHash))
    {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        return false;
    }

    if (!ComputeHash(assembly.GetLoadContext(), hHash, CalcHash, nullptr))
    {
        return false;
    }

    // Figure out how big the resulting digest is so that we can pass it back out
    DWORD hashSize = 0;
    DWORD dwordSize = sizeof(hashSize);
    if (!CryptGetHashParam(hHash, HP_HASHSIZE, reinterpret_cast<BYTE*>(&hashSize), &dwordSize, 0))
    {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        return false;
    }

    StrongNameBufferHolder<BYTE> pbHash(new (nothrow)BYTE[hashSize]);
    if (!pbHash)
    {
        SetStrongNameErrorInfo(E_OUTOFMEMORY);
        return false;
    }

    if (!CryptGetHashParam(hHash, HP_HASHVAL, pbHash, &hashSize, 0))
    {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        return false;
    }

    *ppbDigestBlob = pbHash.Extract();
    *pcbDigestBlob = hashSize;
    return true;
}

SNAPI StrongNameDigestGenerate(_In_z_ LPCWSTR                                       wszFilePath,
                               _Outptr_result_bytebuffer_(*pcbDigestBlob) BYTE**    ppbDigestBlob,
                               _Out_  ULONG*                                        pcbDigestBlob,
                               DWORD                                                dwFlags)
{
    BOOLEAN retVal = FALSE;
    BEGIN_ENTRYPOINT_VOIDRET;

    SNLOG((W("StrongNameDigestGenerate(\"%s\", %08X, %08X, %04X)\n"), wszFilePath, ppbDigestBlob, pcbDigestBlob, dwFlags));

    SN_COMMON_PROLOG();
    if (wszFilePath == nullptr)
        SN_ERROR(E_POINTER);
    if (ppbDigestBlob == nullptr)
        SN_ERROR(E_POINTER);
    if (pcbDigestBlob == nullptr)
        SN_ERROR(E_POINTER);

    retVal = StrongNameDigestGenerate_Internal(wszFilePath, ppbDigestBlob, pcbDigestBlob, dwFlags);

    END_ENTRYPOINT_VOIDRET;

Exit:
    return retVal;
}

//
// Sign an the digest of an assembly calculated by StrongNameDigestGenerate
//
// Parameters:
//  wszKeyContainer  - name of the key container that holds the key pair used to generate the signature.  If
//                     both a key container and key blob are specified, the key container name is ignored.
//  pbKeyBlob        - raw key pair to be used to generate the signature.  If both a key pair and a key
//                     container are given, the key blob will be used.
//  cbKeyBlob        - size of the key pair in pbKeyBlob
//  pbDigestBlob     - digest of the assembly, calculated by StrongNameDigestGenerate
//  cbDigestBlob     - size of the digest blob
//  hashAlgId        - algorithm ID of the hash algorithm used to generate the digest blob
//  ppbSignatureBlob - on success this will point to a buffer that contains a signature over the blob.  This
//                     buffer should be freed with StrongNameFreeBuffer.
//  pcbSignatureBlob - on success this will point to the size of the signature blob in *ppbSignatureBlob
//  dwFlags          - flags used to control signing.  This is the same set of flags used by
//                     StrongNameSignatureGenerationEx
//

bool StrongNameDigestSign_Internal(_In_opt_z_ LPCWSTR                                   wszKeyContainer,
                                   _In_reads_bytes_opt_(cbKeyBlob) BYTE*                pbKeyBlob,
                                   ULONG                                                cbKeyBlob,
                                   _In_reads_bytes_(cbDigestBlob) BYTE*                 pbDigestBlob,
                                   ULONG                                                cbDigestBlob,
                                   DWORD                                                hashAlgId,
                                   _Outptr_result_bytebuffer_(*pcbSignatureBlob) BYTE** ppbSignatureBlob,
                                   _Out_ ULONG*                                         pcbSignatureBlob,
                                   DWORD                                                dwFlags)
{
    //
    // Get the key we'll be signing with loaded into CAPI
    //

    HandleStrongNameCspHolder hProv;
    CapiKeyHolder hKey;
    if (pbKeyBlob != nullptr)
    {
        hProv = LocateCSP(nullptr, SN_IGNORE_CONTAINER, hashAlgId);

        if (hProv != NULL)
        {
            if (!CryptImportKey(hProv, pbKeyBlob, cbKeyBlob, 0, 0, &hKey))
            {
                SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
                return false;
            }
        }
    }
    else
    {
        hProv = LocateCSP(wszKeyContainer, SN_OPEN_CONTAINER, hashAlgId);
    }

    if (!hProv)
    {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        return false;
    }

    //
    // Get the pre-calculated digest loaded into a CAPI Hash object
    //

    CapiHashHolder hHash;
    if (!CryptCreateHash(hProv, hashAlgId, 0, 0, &hHash))
    {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        return false;
    }

    DWORD hashSize = 0;
    DWORD cbHashSize = sizeof(hashSize);
    if (!CryptGetHashParam(hHash, HP_HASHSIZE, reinterpret_cast<BYTE*>(&hashSize), &cbHashSize, 0))
    {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        return false;
    }

    if (hashSize != cbDigestBlob)
    {
        SetStrongNameErrorInfo(NTE_BAD_HASH);
        return false;
    }

    if (!CryptSetHashParam(hHash, HP_HASHVAL, pbDigestBlob, 0))
    {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        return false;
    }

    //
    // Sign the hash
    //

    DWORD cbSignature = 0;
    if (!CryptSignHashW(hHash, g_uKeySpec, nullptr, 0, nullptr, &cbSignature))
    {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        return false;
    }

    // CAPI has a quirk where some CSPs do not allow you to sign a hash object once you've asked for the size
    // of the signature. To work in those cases, we must create a new hash object to sign.
    if (!CryptCreateHash(hProv, hashAlgId, 0, 0, &hHash))
    {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        return false;
    }

    if (!CryptGetHashParam(hHash, HP_HASHSIZE, reinterpret_cast<BYTE*>(&hashSize), &cbHashSize, 0))
    {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        return false;
    }

    if (hashSize != cbDigestBlob)
    {
        SetStrongNameErrorInfo(NTE_BAD_HASH);
        return false;
    }

    if (!CryptSetHashParam(hHash, HP_HASHVAL, pbDigestBlob, 0))
    {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        return false;
    }

    // Now that we've got a fresh hash object to sign, we can compute the final signature
    StrongNameBufferHolder<BYTE> pbSignature(new (nothrow)BYTE[cbSignature]);
    if (pbSignature == nullptr)
    {
        SetStrongNameErrorInfo(E_OUTOFMEMORY);
        return false;
    }

    if (!CryptSignHashW(hHash, g_uKeySpec, nullptr, 0, pbSignature, &cbSignature))
    {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        return false;
    }

    *ppbSignatureBlob = pbSignature.Extract();
    *pcbSignatureBlob = cbSignature;
    return true;
}

SNAPI StrongNameDigestSign(_In_opt_z_ LPCWSTR                                   wszKeyContainer,
                           _In_reads_bytes_opt_(cbKeyBlob) BYTE*                pbKeyBlob,
                           ULONG                                                cbKeyBlob,
                           _In_reads_bytes_(cbDigestBlob) BYTE*                 pbDigestBlob,
                           ULONG                                                cbDigestBlob,
                           DWORD                                                hashAlgId,
                           _Outptr_result_bytebuffer_(*pcbSignatureBlob) BYTE** ppbSignatureBlob,
                           _Out_ ULONG*                                         pcbSignatureBlob,
                           DWORD                                                dwFlags)
{
    BOOLEAN retVal = FALSE;

    BEGIN_ENTRYPOINT_VOIDRET;
    SNLOG((W("StrongNameDigestSign(\"%s\", %08X, %04X, %08X, %04X, %04X, %08X, %08X, %04X)\n"), wszKeyContainer, pbKeyBlob, cbKeyBlob, pbDigestBlob, cbDigestBlob, hashAlgId, ppbSignatureBlob, pcbSignatureBlob, dwFlags));
    SN_COMMON_PROLOG();

    if (wszKeyContainer == nullptr && pbKeyBlob == nullptr)
        SN_ERROR(E_POINTER);
    if (pbKeyBlob != nullptr && !StrongNameIsValidKeyPair(pbKeyBlob, cbKeyBlob))
        SN_ERROR(E_INVALIDARG);
    if (pbDigestBlob == nullptr)
        SN_ERROR(E_POINTER);
    if (ppbSignatureBlob == nullptr)
        SN_ERROR(E_POINTER);
    if (pcbSignatureBlob == nullptr)
        SN_ERROR(E_POINTER);

    *ppbSignatureBlob = nullptr;
    *pcbSignatureBlob = 0;

    retVal = StrongNameDigestSign_Internal(wszKeyContainer, pbKeyBlob, cbKeyBlob, pbDigestBlob, cbDigestBlob, hashAlgId, ppbSignatureBlob, pcbSignatureBlob, dwFlags);

    END_ENTRYPOINT_VOIDRET;
Exit:
    return retVal;
}

//
// Embed a digest signature generated with StrongNameDigestSign into a delay signed assembly, completing
// the signing process for that assembly.
//
// Parameters:
//  wszFilePath     - path to the assembly to sign
//  pbSignatureBlob - signature blob to embed in the assembly
//  cbSignatureBlob - size of the signature blob
//

bool StrongNameDigestEmbed_Internal(_In_z_ LPCWSTR                          wszFilePath,
                                    _In_reads_bytes_(cbSignatureBlob) BYTE* pbSignatureBlob,
                                    ULONG                                   cbSignatureBlob)
{
    StrongNameAssemblyLoadHolder assembly(wszFilePath, false);
    if (!assembly.IsLoaded())
    {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        return false;
    }

    memcpy_s(assembly.GetLoadContext()->m_pbSignature, assembly.GetLoadContext()->m_cbSignature, pbSignatureBlob, cbSignatureBlob);
    assembly.GetLoadContext()->m_pCorHeader->Flags |= VAL32(COMIMAGE_FLAGS_STRONGNAMESIGNED);

    FILETIME ft = { 0 };
    SYSTEMTIME st = { 0 };
    GetSystemTime(&st);
    if (SystemTimeToFileTime(&st, &ft))
    {
        SetFileTime(assembly.GetLoadContext()->m_hFile, nullptr, nullptr, &ft);
    }

    return true;
}

SNAPI StrongNameDigestEmbed(_In_z_ LPCWSTR                          wszFilePath,                        // [in] valid path to the PE file for the assembly to update
                            _In_reads_bytes_(cbSignatureBlob) BYTE* pbSignatureBlob,                    // [in] signatuer blob for the assembly
                            ULONG                                   cbSignatureBlob)
{
    BOOLEAN retVal = FALSE;

    BEGIN_ENTRYPOINT_VOIDRET;
    SNLOG((W("StrongNameDigestEmbed(\"%s\", %08X, %04X)\n"), wszFilePath, pbSignatureBlob, cbSignatureBlob));
    SN_COMMON_PROLOG();

    if (wszFilePath == nullptr)
        SN_ERROR(E_POINTER);
    if (pbSignatureBlob == nullptr)
        SN_ERROR(E_POINTER);

    retVal = StrongNameDigestEmbed_Internal(wszFilePath, pbSignatureBlob, cbSignatureBlob);

    END_ENTRYPOINT_VOIDRET;
Exit:
    return retVal;
}

// Create a strong name token from an assembly file.
SNAPI StrongNameTokenFromAssembly(LPCWSTR   wszFilePath,            // [in] valid path to the PE file for the assembly
                                  BYTE    **ppbStrongNameToken,     // [out] strong name token 
                                  ULONG    *pcbStrongNameToken)
{
    BOOL fRetValue = FALSE;
    BEGIN_ENTRYPOINT_VOIDRET;
    fRetValue = StrongNameTokenFromAssemblyEx(wszFilePath,
                                         ppbStrongNameToken,
                                         pcbStrongNameToken,
                                         NULL,
                                         NULL);
    END_ENTRYPOINT_VOIDRET;
    return fRetValue;
}

// Create a strong name token from an assembly file and additionally return the full public key.
SNAPI StrongNameTokenFromAssemblyEx(LPCWSTR   wszFilePath,            // [in] valid path to the PE file for the assembly
                                    BYTE    **ppbStrongNameToken,     // [out] strong name token 
                                    ULONG    *pcbStrongNameToken,
                                    BYTE    **ppbPublicKeyBlob,       // [out] public key blob
                                    ULONG    *pcbPublicKeyBlob)
{
    BOOLEAN         retVal = FALSE;

    BEGIN_ENTRYPOINT_VOIDRET;
    SN_LOAD_CTX     sLoadCtx;
    BOOLEAN         fMapped = FALSE;
    BOOLEAN         fSetErrorInfo = TRUE;
    PublicKeyBlob  *pPublicKey = NULL;
    HRESULT         hrKey = S_OK;

    SNLOG((W("StrongNameTokenFromAssemblyEx(\"%s\", %08X, %08X, %08X, %08X)\n"), wszFilePath, ppbStrongNameToken, pcbStrongNameToken, ppbPublicKeyBlob, pcbPublicKeyBlob));

    SN_COMMON_PROLOG();

    if (wszFilePath == NULL)
        SN_ERROR(E_POINTER);
    if (ppbStrongNameToken == NULL)
        SN_ERROR(E_POINTER);
    if (pcbStrongNameToken == NULL)
        SN_ERROR(E_POINTER);

    // Map the assembly into memory.
    sLoadCtx.m_fReadOnly = TRUE;
    if (!LoadAssembly(&sLoadCtx, wszFilePath))
        goto Error;
    fMapped = TRUE;

    // Read the public key used to sign the assembly from the assembly metadata.
    hrKey = FindPublicKey(&sLoadCtx, NULL, 0, &pPublicKey);
    if (FAILED(hrKey))
    {
        SetStrongNameErrorInfo(hrKey);
        fSetErrorInfo = FALSE;
        goto Error;
    }

    // Unload the assembly.
    fMapped = FALSE;
    if (!UnloadAssembly(&sLoadCtx))
        goto Error;

    // Now we have a public key blob, we can call our more direct API to do the
    // actual work.
    if (!StrongNameTokenFromPublicKey((BYTE*)pPublicKey,
                                      SN_SIZEOF_KEY(pPublicKey),
                                      ppbStrongNameToken,
                                      pcbStrongNameToken)) {
        fSetErrorInfo = FALSE;
        goto Error;
    }

    if (pcbPublicKeyBlob)
        *pcbPublicKeyBlob = SN_SIZEOF_KEY(pPublicKey);
 
    // Return public key information.
    if (ppbPublicKeyBlob)
        *ppbPublicKeyBlob = (BYTE*)pPublicKey;
    else
        delete [] (BYTE*)pPublicKey;

    retVal = TRUE;
    goto Exit;

 Error:
    if (fSetErrorInfo)
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
    if (pPublicKey)
        delete [] (BYTE*)pPublicKey;
    if (fMapped)
        UnloadAssembly(&sLoadCtx);

Exit:
    END_ENTRYPOINT_VOIDRET;
        
    return retVal;
}

bool StrongNameSignatureVerificationEx2_Internal(LPCWSTR wszFilePath,
                                                 BOOLEAN fForceVerification,
                                                 BYTE *pbEcmaPublicKey,
                                                 DWORD cbEcmaPublicKey,
                                                 BOOLEAN *pfWasVerified)
{
    StrongNameAssemblyLoadHolder assembly(wszFilePath, true);
    if (!assembly.IsLoaded())
    {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        return false;
    }
    else
    {
        DWORD dwOutFlags = 0;
        HRESULT hrVerify =  VerifySignature(assembly.GetLoadContext(),
                                            SN_INFLAG_INSTALL | SN_INFLAG_ALL_ACCESS | (fForceVerification ? SN_INFLAG_FORCE_VER : 0),
                                            reinterpret_cast<PublicKeyBlob *>(pbEcmaPublicKey),
                                            &dwOutFlags);
        if (FAILED(hrVerify))
        {
            SetStrongNameErrorInfo(hrVerify);
            return false;
        }

        if (pfWasVerified)
        {
            *pfWasVerified = (dwOutFlags & SN_OUTFLAG_WAS_VERIFIED) != 0;
        }

        return true;
    }
}

//
// Verify the signature of a strongly named assembly, providing a mapping from the ECMA key to a real key
//
// Arguments:
//    wszFilePath         - valid path to the PE file for the assembly
//    fForceVerification  - verify even if settings in the registry disable it
//    pbEcmaPublicKey     - mapping from the ECMA public key to the real key used for verification
//    cbEcmaPublicKey     - length of the real ECMA public key
//    fWasVerified        - [out] set to false if verify succeeded due to registry settings
//    
// Return Value:
//    TRUE if the signature was successfully verified, FALSE otherwise
//

SNAPI StrongNameSignatureVerificationEx2(LPCWSTR wszFilePath,
                                         BOOLEAN fForceVerification,
                                         BYTE *pbEcmaPublicKey,
                                         DWORD cbEcmaPublicKey,
                                         BOOLEAN *pfWasVerified)
{
    BOOLEAN retVal = FALSE;
    BEGIN_ENTRYPOINT_VOIDRET;

    SNLOG((W("StrongNameSignatureVerificationEx2(\"%s\", %d, %08X, %08X, %08X)\n"), wszFilePath, fForceVerification, pbEcmaPublicKey, cbEcmaPublicKey, pfWasVerified));

    SN_COMMON_PROLOG();

    if (wszFilePath == NULL)
        SN_ERROR(E_POINTER);
    if (pbEcmaPublicKey == NULL)
        SN_ERROR(E_POINTER);
    if (!StrongNameIsValidPublicKey(pbEcmaPublicKey, cbEcmaPublicKey, false))
        SN_ERROR(CORSEC_E_INVALID_PUBLICKEY);

    retVal = StrongNameSignatureVerificationEx2_Internal(wszFilePath, fForceVerification, pbEcmaPublicKey, cbEcmaPublicKey, pfWasVerified);

Exit:
    END_ENTRYPOINT_VOIDRET;
    return retVal;
}

// Verify a strong name/manifest against a public key blob.
SNAPI StrongNameSignatureVerificationEx(LPCWSTR     wszFilePath,        // [in] valid path to the PE file for the assembly
                                        BOOLEAN     fForceVerification, // [in] verify even if settings in the registry disable it
                                        BOOLEAN    *pfWasVerified)      // [out] set to false if verify succeeded due to registry settings
{
    BOOLEAN fRet = FALSE;
    BEGIN_ENTRYPOINT_VOIDRET;

    fRet = StrongNameSignatureVerificationEx2(wszFilePath,
                                              fForceVerification,
                                              const_cast<BYTE *>(g_rbTheKey),
                                              COUNTOF(g_rbTheKey),
                                              pfWasVerified);

    END_ENTRYPOINT_VOIDRET;
    return fRet;
}


// Verify a strong name/manifest against a public key blob.
SNAPI StrongNameSignatureVerification(LPCWSTR wszFilePath,      // [in] valid path to the PE file for the assembly
                                      DWORD   dwInFlags,        // [in] flags modifying behaviour
                                      DWORD  *pdwOutFlags)      // [out] additional output info
{
    BOOLEAN     retVal = TRUE;

    BEGIN_ENTRYPOINT_VOIDRET;

    SN_LOAD_CTX sLoadCtx;
    BOOLEAN     fMapped = FALSE;

    SNLOG((W("StrongNameSignatureVerification(\"%s\", %08X, %08X, %08X)\n"), wszFilePath, dwInFlags, pdwOutFlags));

    SN_COMMON_PROLOG();

    if (wszFilePath == NULL)
        SN_ERROR(E_POINTER);

    // Map the assembly into memory.
    sLoadCtx.m_fReadOnly = TRUE;
    if (LoadAssembly(&sLoadCtx, wszFilePath))
    {
        fMapped = TRUE;
    }
    else
    {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        retVal = FALSE;
    }

    // Go to common code to process the verification.
    if (fMapped)
    {
        HRESULT hrVerify = VerifySignature(&sLoadCtx, dwInFlags, reinterpret_cast<PublicKeyBlob *>(const_cast<BYTE *>(g_rbTheKey)), pdwOutFlags);
        if (FAILED(hrVerify))
        {
            SetStrongNameErrorInfo(hrVerify);
            retVal = FALSE;
        }

        // Unmap the image. Only set error information if VerifySignature succeeded, since we do not want to
        // overwrite its error information with the error code from UnloadAssembly.
        if (!UnloadAssembly(&sLoadCtx))
        {
            if (retVal)
                SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
            retVal = FALSE;
        }
    }

    // SN_COMMON_PROLOG requires an Exit location
Exit:
    END_ENTRYPOINT_VOIDRET;
    return retVal;
}


// Verify a strong name/manifest against a public key blob when the assembly is
// already memory mapped.
SNAPI StrongNameSignatureVerificationFromImage(BYTE     *pbBase,             // [in] base address of mapped manifest file
                                               DWORD     dwLength,           // [in] length of mapped image in bytes
                                               DWORD     dwInFlags,          // [in] flags modifying behaviour
                                               DWORD    *pdwOutFlags)        // [out] additional output info
{
    BOOLEAN     retVal = TRUE;

    BEGIN_ENTRYPOINT_VOIDRET

    SN_LOAD_CTX sLoadCtx;
    BOOLEAN     fMapped = FALSE;

    SNLOG((W("StrongNameSignatureVerificationFromImage(%08X, %08X, %08X, %08X)\n"), pbBase, dwLength, dwInFlags, pdwOutFlags));

    SN_COMMON_PROLOG();

    if (pbBase == NULL)
        SN_ERROR(E_POINTER);

    // We don't need to map the image, it's already in memory. But we do need to
    // set up a load context for some of the following routines. LoadAssembly
    // copes with this case for us.
    sLoadCtx.m_pbBase = pbBase;
    sLoadCtx.m_dwLength = dwLength;
    sLoadCtx.m_fReadOnly = TRUE;
    if (LoadAssembly(&sLoadCtx, NULL, dwInFlags))
    {
        fMapped = TRUE;
    }
    else
    {
        SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
        retVal = FALSE;
    }

    if (fMapped)
    {
        // Go to common code to process the verification.
        HRESULT hrVerify = VerifySignature(&sLoadCtx, dwInFlags, reinterpret_cast<PublicKeyBlob *>(const_cast<BYTE *>(g_rbTheKey)), pdwOutFlags);
        if (FAILED(hrVerify))
        {
            SetStrongNameErrorInfo(hrVerify);
            retVal = FALSE;
        }

        // Unmap the image.
        if (!UnloadAssembly(&sLoadCtx))
        {
            SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
            retVal = FALSE;
        }
    }

    // SN_COMMON_PROLOG requires an Exit location
Exit:
    END_ENTRYPOINT_VOIDRET;
    return retVal;
}

// Find portions of an assembly to hash.
BOOLEAN CollectBlob(SN_LOAD_CTX *pLoadCtx, PBYTE pbBlob, DWORD* pcbBlob)
{
    // Calculate the required size
    DWORD cbRequired = 0;
    BOOLEAN bRetval = ComputeHash(pLoadCtx, (HCRYPTHASH)INVALID_HANDLE_VALUE, CalculateSize, &cbRequired);
    if (!bRetval)
        return FALSE;
    if (*pcbBlob < cbRequired) {
        *pcbBlob = cbRequired;
        SetLastError( E_INVALIDARG );
        return FALSE;
    }

    CopyDataBufferDesc buffer = { pbBlob, *pcbBlob };
    if (!ComputeHash(pLoadCtx, (HCRYPTHASH)INVALID_HANDLE_VALUE, CopyData, &buffer))
        return FALSE;
    
    *pcbBlob = cbRequired;
    return TRUE;
}

// ensure that the symbol will be exported properly
extern "C" SNAPI StrongNameGetBlob(LPCWSTR wszFilePath,
                        PBYTE     pbBlob,
                        DWORD    *cbBlob);

SNAPI StrongNameGetBlob(LPCWSTR wszFilePath,      // [in] valid path to the PE file for the assembly
                        PBYTE     pbBlob,         // [in] buffer to fill with blob
                        DWORD    *cbBlob)         // [in/out] size of buffer/number of bytes put into buffer
{
    BOOLEAN     retVal = FALSE;

    BEGIN_ENTRYPOINT_VOIDRET;

    SN_LOAD_CTX sLoadCtx;
    BOOLEAN     fMapped = FALSE;

    SNLOG((W("StrongNameGetBlob(\"%s\", %08X, %08X)\n"), wszFilePath, pbBlob, cbBlob));

    SN_COMMON_PROLOG();

    if (wszFilePath == NULL)
        SN_ERROR(E_POINTER);
    if (pbBlob == NULL)
        SN_ERROR(E_POINTER);
    if (cbBlob == NULL)
        SN_ERROR(E_POINTER);

    // Map the assembly into memory.
    sLoadCtx.m_fReadOnly = TRUE;
    if (!LoadAssembly(&sLoadCtx, wszFilePath, 0, FALSE))
        goto Error;
    fMapped = TRUE;

    if (!CollectBlob(&sLoadCtx, pbBlob, cbBlob))
        goto Error;

    // Unmap the image.
    fMapped = FALSE;
    if (!UnloadAssembly(&sLoadCtx))
        goto Error;

    retVal = TRUE;
    goto Exit;

 Error:
    SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
    if (fMapped)
        UnloadAssembly(&sLoadCtx);
Exit:
    END_ENTRYPOINT_VOIDRET;
    return retVal;
}

// ensure that the symbol will be exported properly
extern "C" SNAPI StrongNameGetBlobFromImage(BYTE     *pbBase,
                                 DWORD     dwLength,
                                 PBYTE     pbBlob,
                                 DWORD    *cbBlob);

SNAPI StrongNameGetBlobFromImage(BYTE     *pbBase,             // [in] base address of mapped manifest file
                                 DWORD     dwLength,           // [in] length of mapped image in bytes
                                 PBYTE     pbBlob,             // [in] buffer to fill with blob
                                 DWORD    *cbBlob)             // [in/out] size of buffer/number of bytes put into buffer
{
    BOOLEAN     retVal = FALSE;

    BEGIN_ENTRYPOINT_VOIDRET;

    SN_LOAD_CTX sLoadCtx;
    BOOLEAN     fMapped = FALSE;


    SNLOG((W("StrongNameGetBlobFromImage(%08X, %08X, %08X, %08X)\n"), pbBase, dwLength, pbBlob, cbBlob));

    SN_COMMON_PROLOG();

    if (pbBase == NULL)
        SN_ERROR(E_POINTER);
    if (pbBlob == NULL)
        SN_ERROR(E_POINTER);
    if (cbBlob == NULL)
        SN_ERROR(E_POINTER);

    // We don't need to map the image, it's already in memory. But we do need to
    // set up a load context for some of the following routines. LoadAssembly
    // copes with this case for us.
    sLoadCtx.m_pbBase = pbBase;
    sLoadCtx.m_dwLength = dwLength;
    sLoadCtx.m_fReadOnly = TRUE;
    if (!LoadAssembly(&sLoadCtx, NULL, 0, FALSE))
        goto Error;
    fMapped = TRUE;

    // Go to common code to process the verification.
    if (!CollectBlob(&sLoadCtx, pbBlob, cbBlob))
        goto Error;

    // Unmap the image.
    fMapped = FALSE;
    if (!UnloadAssembly(&sLoadCtx))
        goto Error;

    retVal = TRUE;
    goto Exit;

 Error:
    SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
    if (fMapped)
        UnloadAssembly(&sLoadCtx);

Exit:
    END_ENTRYPOINT_VOIDRET;

    return retVal;
}


// Verify that two assemblies differ only by signature blob.
SNAPI StrongNameCompareAssemblies(LPCWSTR   wszAssembly1,           // [in] file name of first assembly
                                  LPCWSTR   wszAssembly2,           // [in] file name of second assembly
                                  DWORD    *pdwResult)              // [out] result of comparison
{
    BOOLEAN     retVal = FALSE;

    BEGIN_ENTRYPOINT_VOIDRET;


    SN_LOAD_CTX sLoadCtx1;
    SN_LOAD_CTX sLoadCtx2;
    size_t      dwSkipOffsets[3];
    size_t      dwSkipLengths[3];
    BOOLEAN     bMappedAssem1 = FALSE;
    BOOLEAN     bMappedAssem2 = FALSE;
    BOOLEAN     bIdentical;
    BOOLEAN     bSkipping;
    DWORD       i, j;



    SNLOG((W("StrongNameCompareAssemblies(\"%s\", \"%s\", %08X)\n"), wszAssembly1, wszAssembly2, pdwResult));

    SN_COMMON_PROLOG();

    if (wszAssembly1 == NULL)
        SN_ERROR(E_POINTER);
    if (wszAssembly2 == NULL)
        SN_ERROR(E_POINTER);
    if (pdwResult == NULL)
        SN_ERROR(E_POINTER);

    // Map each assembly.
    sLoadCtx1.m_fReadOnly = TRUE;
    if (!LoadAssembly(&sLoadCtx1, wszAssembly1))
        goto Error;
    bMappedAssem1 = TRUE;

    sLoadCtx2.m_fReadOnly = TRUE;
    if (!LoadAssembly(&sLoadCtx2, wszAssembly2))
        goto Error;
    bMappedAssem2 = TRUE;

    // If the files aren't even the same length then they must be different.
    if (sLoadCtx1.m_dwLength != sLoadCtx2.m_dwLength)
        goto ImagesDiffer;

    // Check that the signatures are located at the same offset and are the same
    // length in each assembly.
    if (sLoadCtx1.m_pCorHeader->StrongNameSignature.VirtualAddress !=
        sLoadCtx2.m_pCorHeader->StrongNameSignature.VirtualAddress)
        goto ImagesDiffer;
    if (sLoadCtx1.m_pCorHeader->StrongNameSignature.Size !=
        sLoadCtx2.m_pCorHeader->StrongNameSignature.Size)
        goto ImagesDiffer;

    // Set up list of image ranges to skip in the upcoming comparison.
    // First there's the signature blob.
    dwSkipOffsets[0] = sLoadCtx1.m_pbSignature - sLoadCtx1.m_pbBase;
    dwSkipLengths[0] = sLoadCtx1.m_cbSignature;

    // Then there's the checksum.
    if (sLoadCtx1.m_pNtHeaders->OptionalHeader.Magic != sLoadCtx2.m_pNtHeaders->OptionalHeader.Magic)
        goto ImagesDiffer;
    if (sLoadCtx1.m_pNtHeaders->OptionalHeader.Magic == VAL16(IMAGE_NT_OPTIONAL_HDR32_MAGIC))
        dwSkipOffsets[1] = (BYTE*)&((IMAGE_NT_HEADERS32*)sLoadCtx1.m_pNtHeaders)->OptionalHeader.CheckSum - sLoadCtx1.m_pbBase;
    else if (sLoadCtx1.m_pNtHeaders->OptionalHeader.Magic == VAL16(IMAGE_NT_OPTIONAL_HDR64_MAGIC))
        dwSkipOffsets[1] = (BYTE*)&((IMAGE_NT_HEADERS64*)sLoadCtx1.m_pNtHeaders)->OptionalHeader.CheckSum - sLoadCtx1.m_pbBase;
    else {
        SetLastError(CORSEC_E_INVALID_IMAGE_FORMAT);
        goto Error;
    }
    dwSkipLengths[1] = sizeof(DWORD);

    // Skip the COM+ 2.0 PE header extension flags field. It's updated by the
    // signing operation.
    dwSkipOffsets[2] = (BYTE*)&sLoadCtx1.m_pCorHeader->Flags - sLoadCtx1.m_pbBase;
    dwSkipLengths[2] = sizeof(DWORD);

    // Compare the two mapped images, skipping the ranges we defined above.
    bIdentical = TRUE;
    for (i = 0; i < sLoadCtx1.m_dwLength; i++) {

        // Determine if we're skipping the check on the current byte.
        bSkipping = FALSE;
        for (j = 0; j < (sizeof(dwSkipOffsets) / sizeof(dwSkipOffsets[0])); j++)
            if ((i >= dwSkipOffsets[j]) && (i < (dwSkipOffsets[j] + dwSkipLengths[j]))) {
                bSkipping = TRUE;
                break;
            }

        // Perform comparisons as desired.
        if (sLoadCtx1.m_pbBase[i] != sLoadCtx2.m_pbBase[i])
            if (bSkipping)
                bIdentical = FALSE;
            else
                goto ImagesDiffer;
    }

    // The assemblies are the same.
    *pdwResult = bIdentical ? SN_CMP_IDENTICAL : SN_CMP_SIGONLY;

    UnloadAssembly(&sLoadCtx1);
    UnloadAssembly(&sLoadCtx2);

    retVal = TRUE;
    goto Exit;

 Error:
    SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
    if (bMappedAssem1)
        UnloadAssembly(&sLoadCtx1);
    if (bMappedAssem2)
        UnloadAssembly(&sLoadCtx2);
    goto Exit;

 ImagesDiffer:
    if (bMappedAssem1)
        UnloadAssembly(&sLoadCtx1);
    if (bMappedAssem2)
        UnloadAssembly(&sLoadCtx2);
    *pdwResult = SN_CMP_DIFFERENT;
    retVal = TRUE;

Exit:
    END_ENTRYPOINT_VOIDRET;

    return retVal;
}


// Compute the size of buffer needed to hold a hash for a given hash algorithm.
SNAPI StrongNameHashSize(ULONG  ulHashAlg,  // [in] hash algorithm
                         DWORD *pcbSize)    // [out] size of the hash in bytes
{
    BOOLEAN     retVal = FALSE;

    BEGIN_ENTRYPOINT_VOIDRET;

    HCRYPTPROV  hProv = NULL;
    HCRYPTHASH  hHash = NULL;
    DWORD       dwSize;


    SNLOG((W("StrongNameHashSize(%08X, %08X)\n"), ulHashAlg, pcbSize));

    SN_COMMON_PROLOG();

    if (pcbSize == NULL)
        SN_ERROR(E_POINTER);

    // Default hashing algorithm ID if necessary.
    if (ulHashAlg == 0)
        ulHashAlg = CALG_SHA1;

    // Find a CSP supporting the required algorithm.
    hProv = LocateCSP(NULL, SN_IGNORE_CONTAINER, ulHashAlg);
    if (!hProv)
        goto Error;

    // Create a hash object.
    if (!CryptCreateHash(hProv, ulHashAlg, 0, 0, &hHash))
        goto Error;

    // And ask for the size of the hash.
    dwSize = sizeof(DWORD);
    if (!CryptGetHashParam(hHash, HP_HASHSIZE, (BYTE*)pcbSize, &dwSize, 0))
        goto Error;

    // Cleanup and exit.
    CryptDestroyHash(hHash);
    FreeCSP(hProv);

    retVal = TRUE;
    goto Exit;

 Error:
    SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
    if (hHash)
        CryptDestroyHash(hHash);
    if (hProv)
        FreeCSP(hProv);

 Exit:

    END_ENTRYPOINT_VOIDRET;

    return retVal;
}


// Compute the size that needs to be allocated for a signature in an assembly.
SNAPI StrongNameSignatureSize(BYTE    *pbPublicKeyBlob,    // [in] public key blob
                              ULONG    cbPublicKeyBlob,
                              DWORD   *pcbSize)            // [out] size of the signature in bytes
{
    BOOLEAN         retVal = FALSE;

    BEGIN_ENTRYPOINT_VOIDRET;

    PublicKeyBlob  *pPublicKey = (PublicKeyBlob*)pbPublicKeyBlob;
    ALG_ID          uHashAlgId;
    ALG_ID          uSignAlgId;
    HCRYPTPROV      hProv = NULL;
    HCRYPTHASH      hHash = NULL;
    HCRYPTKEY       hKey = NULL;
    LPCWSTR         wszKeyContainer = NULL;
    BOOLEAN         bTempContainer = FALSE;
    DWORD           dwKeyLen;
    DWORD           dwBytes;

    SNLOG((W("StrongNameSignatureSize(%08X, %08X, %08X)\n"), pbPublicKeyBlob, cbPublicKeyBlob, pcbSize));

    SN_COMMON_PROLOG();

    if (pbPublicKeyBlob == NULL)
        SN_ERROR(E_POINTER);
    if (!StrongNameIsValidPublicKey(pbPublicKeyBlob, cbPublicKeyBlob, false))
        SN_ERROR(CORSEC_E_INVALID_PUBLICKEY);
    if (pcbSize == NULL)
        SN_ERROR(E_POINTER);

    // Special case neutral key.
    if (SN_IS_NEUTRAL_KEY(pPublicKey))
        pPublicKey = SN_THE_KEY();

    // Determine hashing/signing algorithms.
    uHashAlgId = GET_UNALIGNED_VAL32(&pPublicKey->HashAlgID);
    uSignAlgId = GET_UNALIGNED_VAL32(&pPublicKey->SigAlgID);

    // Default hashing and signing algorithm IDs if necessary.
    if (uHashAlgId == 0)
        uHashAlgId = CALG_SHA1;
    if (uSignAlgId == 0)
        uSignAlgId = CALG_RSA_SIGN;

    // Create a temporary key container name.
    if (!GetKeyContainerName(&wszKeyContainer, &bTempContainer))
        goto Exit;

    // Find a CSP supporting the required algorithms and create a temporary key
    // container.
    hProv = LocateCSP(wszKeyContainer, SN_CREATE_CONTAINER, uHashAlgId, uSignAlgId);
    if (!hProv)
        goto Error;

    // Import the public key (we need to do this in order to determine the key
    // length reliably).
    if (!CryptImportKey(hProv,
                           pPublicKey->PublicKey,
                           GET_UNALIGNED_VAL32(&pPublicKey->cbPublicKey),
                           0, 0, &hKey))
        goto Error;

    // Query the key attributes (it's the length we're interested in).
    dwBytes = sizeof(dwKeyLen);
    if (!CryptGetKeyParam(hKey, KP_KEYLEN, (BYTE*)&dwKeyLen, &dwBytes, 0))
        goto Error;

    // Delete the key container.
    if (LocateCSP(wszKeyContainer, SN_DELETE_CONTAINER) == NULL) {
        SetLastError(CORSEC_E_CONTAINER_NOT_FOUND);
        goto Error;
    }

    // Take shortcut for the typical case
    if ((uSignAlgId == CALG_RSA_SIGN) && (dwKeyLen % 8 == 0)) {
        // The signature size known for CALG_RSA_SIGN
        *pcbSize = dwKeyLen / 8;
    }
    else {
        // Recreate the container so we can create a temporary key pair.
        hProv = LocateCSP(wszKeyContainer, SN_CREATE_CONTAINER, uHashAlgId, uSignAlgId);
        if (!hProv)
            goto Error;

        // Create the temporary key pair.
        if (!CryptGenKey(hProv, g_uKeySpec, dwKeyLen << 16, &hKey))
            goto Error;

        // Create a hash.
        if (!CryptCreateHash(hProv, uHashAlgId, 0, 0, &hHash))
            goto Error;

        // Compute size of the signature blob.
        if (!CryptSignHashW(hHash, g_uKeySpec, NULL, 0, NULL, pcbSize))
            goto Error;
        CryptDestroyHash(hHash);

        if (bTempContainer)
            LocateCSP(wszKeyContainer, SN_DELETE_CONTAINER);
    }

    SNLOG((W("Signature size for  hashalg %08X, %08X key (%08X bits) is %08X bytes\n"), uHashAlgId, uSignAlgId, dwKeyLen, *pcbSize));

    CryptDestroyKey(hKey);
    FreeCSP(hProv);
    FreeKeyContainerName(wszKeyContainer, bTempContainer);

    retVal = TRUE;
    goto Exit;

 Error:
    SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
    if (hHash)
        CryptDestroyHash(hHash);
    if (hKey)
        CryptDestroyKey(hKey);
    if (hProv)
        FreeCSP(hProv);
    if (bTempContainer)
        LocateCSP(wszKeyContainer, SN_DELETE_CONTAINER);
    FreeKeyContainerName(wszKeyContainer, bTempContainer);

 Exit:
    END_ENTRYPOINT_VOIDRET;

    return retVal;
}

// Locate CSP based on criteria specified in the registry (CSP name etc).
// Optionally create or delete a named key container within that CSP.
HCRYPTPROV LocateCSP(LPCWSTR    wszKeyContainer,
                     DWORD      dwAction,
                     ALG_ID     uHashAlgId,
                     ALG_ID     uSignAlgId)
{
    DWORD           i;
    DWORD           dwType;
    WCHAR           wszName[SN_MAX_CSP_NAME + 1];
    DWORD           dwNameLength;
    HCRYPTPROV      hProv;
    BOOLEAN         bFirstAlg;
    BOOLEAN         bFoundHash;
    BOOLEAN         bFoundSign;
    PROV_ENUMALGS   rAlgs;
    HCRYPTPROV      hRetProv;
    DWORD           dwAlgsLen;

    DWORD dwProvType = g_uProvType;

    // If a CSP name has been provided (and we're not opening a CSP just to do a
    // SHA1 hash or a verification), open the CSP directly.
    if (g_bHasCSPName &&
        (dwAction != SN_HASH_SHA1_ONLY))
    {
        if (StrongNameCryptAcquireContext(&hProv,
                                          wszKeyContainer ? wszKeyContainer : NULL,
                                          g_wszCSPName,
                                          dwProvType,
                                          SN_CAC_FLAGS(dwAction)))
            return (dwAction == SN_DELETE_CONTAINER) ? (HCRYPTPROV)~0 : hProv;
        else {
            SNLOG((W("Failed to open CSP '%s': %08X\n"), g_wszCSPName, GetLastError()));
            return NULL;
        }
    }

    // Set up hashing and signing algorithms to look for based upon input
    // parameters. Or if these haven't been supplied use the configured defaults
    // instead.
    if (uHashAlgId == 0)
        uHashAlgId = g_uHashAlgId;
    if (uSignAlgId == 0)
        uSignAlgId = g_uSignAlgId;

    // If default hashing and signing algorithms have been selected (SHA1 and
    // RSA), we select the default CSP for the RSA_FULL type. 
    // For SHA2 and RSA, we select the default CSP For RSA_AES.
    // Otherwise, you just get the first CSP that supports the algorithms 
    // you specified (with no guarantee that the selected CSP is a default of any type). 
    // This is because we have no way of forcing the enumeration to just give us default
    // CSPs.
    bool fUseDefaultCsp = false;
    StrongNameCachedCsp cachedCspNumber = None;

    // We know what container to use for SHA1 algorithms with RSA
    if (((uHashAlgId == CALG_SHA1) && (uSignAlgId == CALG_RSA_SIGN)) ||
        (dwAction == SN_HASH_SHA1_ONLY)) {
        fUseDefaultCsp = true;
        cachedCspNumber = Sha1CachedCsp;
        dwProvType = PROV_RSA_FULL;

        SNLOG((W("Attempting to open default provider\n")));
    }

    // We know what container to use for SHA2 algorithms with RSA
    if ((uHashAlgId == CALG_SHA_256 || uHashAlgId == CALG_SHA_384 || uHashAlgId == CALG_SHA_512)
        && uSignAlgId == CALG_RSA_SIGN) {
        fUseDefaultCsp = true;
        cachedCspNumber = Sha2CachedCsp;
        dwProvType = PROV_RSA_AES;

        SNLOG((W("Attempting to open default SHA2 provider\n")));
    }
    
    if (fUseDefaultCsp)
    {
        // If we're not trying to create/open/delete a key container, see if a
        // CSP is cached.
        if (wszKeyContainer == NULL && dwAction != SN_DELETE_CONTAINER) {
            hProv = LookupCachedCSP(cachedCspNumber);
            if (hProv) {
                SNLOG((W("Found provider in cache\n")));
                return hProv;
            }
        }
        if (StrongNameCryptAcquireContext(&hProv,
                                          wszKeyContainer ? wszKeyContainer : NULL,
                                          NULL,
                                          dwProvType,
                                          SN_CAC_FLAGS(dwAction))) {
            // If we're not trying to create/open/delete a key container, cache
            // the CSP returned.
            if (wszKeyContainer == NULL && dwAction != SN_DELETE_CONTAINER)
                CacheCSP(hProv, cachedCspNumber);
            return (dwAction == SN_DELETE_CONTAINER) ? (HCRYPTPROV)~0 : hProv;
        } else {
            SNLOG((W("Failed to open: %08X\n"), GetLastError()));
            return NULL;
        }
    }

    HRESULT hr = InitStrongNameCriticalSection();
    if (FAILED(hr)) {
        SetLastError(hr);
        return NULL;
    }

    // Some crypto APIs are non thread safe (e.g. enumerating CSP
    // hashing/signing algorithms). Use a mutex to serialize these operations.
    // The following usage is GC-safe and exception-safe:
    {
        CRITSEC_Holder csh(g_rStrongNameMutex);

        for (i = 0; ; i++) {

            // Enumerate all CSPs.
            dwNameLength = sizeof(wszName);
            if (CryptEnumProvidersW(i, 0, 0, &dwType, wszName, &dwNameLength)) {

                // Open the currently selected CSP.
                SNLOG((W("Considering CSP '%s'\n"), wszName));
                if (StrongNameCryptAcquireContext(&hProv,
                                                  NULL,
                                                  wszName,
                                                  dwType,
                                                  CRYPT_SILENT |
                                                  CRYPT_VERIFYCONTEXT |
                                                  (g_bUseMachineKeyset ? CRYPT_MACHINE_KEYSET : 0))) {

                    // Enumerate all the algorithms the CSP supports.
                    bFirstAlg = TRUE;
                    bFoundHash = FALSE;
                    bFoundSign = FALSE;
                    for (;;) {

                        dwAlgsLen = sizeof(rAlgs);
                        if (CryptGetProvParam(hProv,
                                                 PP_ENUMALGS, (BYTE*)&rAlgs, &dwAlgsLen,
                                                 bFirstAlg ? CRYPT_FIRST : 0)) {

                            if (rAlgs.aiAlgid == uHashAlgId)
                                bFoundHash = TRUE;
                            else if (rAlgs.aiAlgid == uSignAlgId)
                                bFoundSign = TRUE;

                            if (bFoundHash && bFoundSign) {

                                // Found a CSP that supports the required
                                // algorithms. Re-open the context with access to
                                // the required key container.

                                SNLOG((W("CSP matches\n")));

                                if (StrongNameCryptAcquireContext(&hRetProv,
                                                                  wszKeyContainer ? wszKeyContainer : NULL,
                                                                  wszName,
                                                                  dwType,
                                                                  CRYPT_SILENT | 
                                                                  SN_CAC_FLAGS(dwAction))) {
                                    CryptReleaseContext(hProv, 0);
                                    return (dwAction == SN_DELETE_CONTAINER) ? (HCRYPTPROV)~0 : hRetProv;
                                } else {
                                    SNLOG((W("Failed to re-open for container: %08X\n"), GetLastError()));
                                    break;
                                }
                            }

                            bFirstAlg = FALSE;

                        } else {
                            _ASSERTE(GetLastError() == ERROR_NO_MORE_ITEMS);
                            break;
                        }

                    }

                    CryptReleaseContext(hProv, 0);

                } else
                    SNLOG((W("Failed to open CSP: %08X\n"), GetLastError()));

            } else if (GetLastError() == ERROR_NO_MORE_ITEMS)
                break;

        }
        // csh for g_rStrongNameMutex goes out of scope here
    }

    // No matching CSP found.
    SetLastError(CORSEC_E_NO_SUITABLE_CSP);
    return NULL;
}


// Release a CSP acquired through LocateCSP.
VOID FreeCSP(HCRYPTPROV hProv)
{
    // If the CSP is currently cached, don't release it yet.
    if (!IsCachedCSP(hProv))
        CryptReleaseContext(hProv, 0);
}

// Locate a cached CSP for this thread.
HCRYPTPROV LookupCachedCSP(StrongNameCachedCsp cspNumber)
{
    SN_THREAD_CTX *pThreadCtx = GetThreadContext();
    if (pThreadCtx == NULL)
        return NULL;
    return pThreadCtx->m_hProv[cspNumber];
}


// Update the CSP cache for this thread (freeing any CSP displaced).
VOID CacheCSP(HCRYPTPROV hProv, StrongNameCachedCsp cspNumber)
{
    SN_THREAD_CTX *pThreadCtx = GetThreadContext();
    if (pThreadCtx == NULL)
        return;
    if (pThreadCtx->m_hProv[cspNumber])
        CryptReleaseContext(pThreadCtx->m_hProv[cspNumber], 0);
    pThreadCtx->m_hProv[cspNumber] = hProv;
}


// Determine whether a given CSP is currently cached.
BOOLEAN IsCachedCSP(HCRYPTPROV hProv)
{
    SN_THREAD_CTX *pThreadCtx = GetThreadContext();
    if (pThreadCtx == NULL)
        return FALSE;
    for (ULONG i = 0; i < CachedCspCount; i++)
    {
        if(pThreadCtx->m_hProv[i] == hProv)
        {
            return TRUE;
        }
    }
    return FALSE;
}

// rehash all files in a multi-module assembly
BOOLEAN RehashModules (SN_LOAD_CTX *pLoadCtx, LPCWSTR wszFilePath) {
    HRESULT             hr;
    ULONG               ulHashAlg;
    mdAssembly          tkAssembly;
    HENUMInternal       hFileEnum;
    mdFile              tkFile;
    LPCSTR              pszFile;
    BYTE               *pbFileHash;
    DWORD               cbFileHash;
    NewArrayHolder<BYTE> pbNewFileHash(NULL);
    DWORD               cbNewFileHash = 0;
    DWORD               cchDirectory;
    DWORD               cchFullFile;
    CHAR                szFullFile[MAX_LONGPATH + 1];
    WCHAR               wszFullFile[MAX_LONGPATH + 1];
    LPCWSTR             pszSlash;
    DWORD               cchFile;
    IMDInternalImport  *pMetaDataImport = NULL;

    // Determine the directory the assembly lives in (this is where we'll
    // look for linked files).
    if (((pszSlash = wcsrchr(wszFilePath, W('\\'))) != NULL) || ((pszSlash = wcsrchr(wszFilePath, W('/'))) != NULL)) {
        cchDirectory = (DWORD) (pszSlash - wszFilePath + 1);
        cchDirectory = WszWideCharToMultiByte(CP_UTF8, 0, wszFilePath, cchDirectory, szFullFile, MAX_LONGPATH, NULL, NULL);
        if (cchDirectory >= MAX_LONGPATH) {
            SNLOG((W("Assembly directory name too long\n")));
            hr = ERROR_BUFFER_OVERFLOW;
            goto Error;
        }
    } else
        cchDirectory = 0;

    // Open the scope on the mapped image.
    if (FAILED(hr = GetMetadataImport(pLoadCtx, &tkAssembly, &pMetaDataImport)))
    {
        goto Error;
    }

    // Determine the hash algorithm used for file references.
    if (FAILED(hr = pMetaDataImport->GetAssemblyProps(
        tkAssembly,           // [IN] The Assembly for which to get the properties
        NULL,                 // [OUT] Pointer to the Originator blob
        NULL,                 // [OUT] Count of bytes in the Originator Blob
        &ulHashAlg,           // [OUT] Hash Algorithm
        NULL,                 // [OUT] Buffer to fill with name
        NULL,                 // [OUT] Assembly MetaData
        NULL)))               // [OUT] Flags
    {
        SNLOG((W("Failed to get assembly 0x%08X info, %08X\n"), tkAssembly, hr));
        goto Error;
    }
    
    // Enumerate all file references.
    if (FAILED(hr = pMetaDataImport->EnumInit(mdtFile, mdTokenNil, &hFileEnum)))
    {
        SNLOG((W("Failed to enumerate linked files, %08X\n"), hr));
        goto Error;
    }

    for (; pMetaDataImport->EnumNext(&hFileEnum, &tkFile); ) {

        // Determine the file name and the location of the hash.
        if (FAILED(hr = pMetaDataImport->GetFileProps(
            tkFile, 
            &pszFile, 
            (const void **)&pbFileHash, 
            &cbFileHash, 
            NULL)))
        {
            SNLOG((W("Failed to get file 0x%08X info, %08X\n"), tkFile, hr));
            goto Error;
        }
        
        // Build the full filename by appending to the assembly directory we
        // calculated earlier.
        cchFile = (DWORD) strlen(pszFile);
        if ((cchFile + cchDirectory) >= COUNTOF(szFullFile)) {
            pMetaDataImport->EnumClose(&hFileEnum);
            SNLOG((W("Linked file name too long (%S)\n"), pszFile));
            hr = ERROR_BUFFER_OVERFLOW;
            goto Error;
        }
        memcpy_s(&szFullFile[cchDirectory], COUNTOF(szFullFile) - cchDirectory, pszFile, cchFile + 1);

        // Allocate enough buffer for the new hash.
        if (cbNewFileHash < cbFileHash) {
            pbNewFileHash = new (nothrow) BYTE[cbFileHash];
            if (pbNewFileHash == NULL) {
                hr = E_OUTOFMEMORY;
                goto Error;
            }
            cbNewFileHash = cbFileHash;
        }

        cchFullFile = WszMultiByteToWideChar(CP_UTF8, 0, szFullFile, -1, wszFullFile, MAX_LONGPATH);
        if (cchFullFile == 0 || cchFullFile >= MAX_LONGPATH) {
            pMetaDataImport->EnumClose(&hFileEnum);
            SNLOG((W("Assembly directory name too long\n")));
            hr = ERROR_BUFFER_OVERFLOW;
            goto Error;
        }

        // Compute a new hash for the file.
        if (FAILED(hr = GetHashFromFileW(wszFullFile,
                                         (unsigned*)&ulHashAlg,
                                         pbNewFileHash,
                                         cbNewFileHash,
                                         &cbNewFileHash))) {
            pMetaDataImport->EnumClose(&hFileEnum);
            SNLOG((W("Failed to get compute file hash, %08X\n"), hr));
            goto Error;
        }

        // The new hash has to be the same size (since we used the same
        // algorithm).
        _ASSERTE(cbNewFileHash == cbFileHash);

        // We make the assumption here that the pointer to the file hash
        // handed to us by the metadata is a direct pointer and not a
        // buffered copy. If this changes, we'll need a new metadata API to
        // support updates of this type.
        memcpy_s(pbFileHash, cbFileHash, pbNewFileHash, cbFileHash);
    }

    pMetaDataImport->EnumClose(&hFileEnum);
    pMetaDataImport->Release();
    return TRUE;

Error:
    if (pMetaDataImport)
        pMetaDataImport->Release();
    if (pbNewFileHash)
        pbNewFileHash.Release();
    SetLastError(hr);
    return FALSE;
}

//
// Check that the public key portion of an assembly's identity matches the private key that it is being
// signed with.
//
// Arguments:
//    pAssemblySignaturePublicKey - Assembly signature public key blob
//    wszKeyContainer             - Key container holding the key the assembly is signed with
//    dwFlags                     - SN_ECMA_SIGN if the assembly is being ECMA signed, SN_TEST_SIGN if it is being test signed
//    
// Return Value:
//    true if the assembly's public key matches the private key in wszKeyContainer, otherwise false
//

bool VerifyKeyMatchesAssembly(PublicKeyBlob * pAssemblySignaturePublicKey, __in_z LPCWSTR wszKeyContainer, BYTE *pbKeyBlob, ULONG cbKeyBlob, DWORD dwFlags)
{
    _ASSERTE(wszKeyContainer != NULL || pbKeyBlob != NULL);

    // If we're test signing, then the assembly's public key will not match the private key by design. 
    // Since there's nothing to check, we can quit early.
    if ((dwFlags & SN_TEST_SIGN) == SN_TEST_SIGN)
    {
        return true;
    }

    if (SN_IS_NEUTRAL_KEY(pAssemblySignaturePublicKey))
    {
        // If we're ECMA signing an assembly with the ECMA public key, then by definition the key matches. 
        if ((dwFlags & SN_ECMA_SIGN) == SN_ECMA_SIGN)
        {
            return true;
        }

        // Swap the real public key in for ECMA signing
        pAssemblySignaturePublicKey = SN_THE_KEY();
    }

    // Otherwise, we need to check that the public key from the key container matches the public key from
    // the assembly.
    StrongNameBufferHolder<BYTE> pbSignaturePublicKey = NULL;
    DWORD cbSignaturePublicKey;
    if (!StrongNameGetPublicKeyEx(wszKeyContainer, pbKeyBlob, cbKeyBlob, &pbSignaturePublicKey, &cbSignaturePublicKey, GET_UNALIGNED_VAL32(&pAssemblySignaturePublicKey->HashAlgID), 0 /*Should be GET_UNALIGNED_VAL32(&pAssemblySignaturePublicKey->HashAlgID) once we support different signature algorithms*/))
    {
        // We failed to get the public key for the key in the given key container. StrongNameGetPublicKey
        // has already set the error information, so we can just return false here without resetting it.
        return false;
    }
    _ASSERTE(!pbSignaturePublicKey.IsNull() && pAssemblySignaturePublicKey != NULL);

    // Do a raw compare on the public key blobs to see if they match
    if (SN_SIZEOF_KEY(reinterpret_cast<PublicKeyBlob *>(pbSignaturePublicKey.GetValue())) == SN_SIZEOF_KEY(pAssemblySignaturePublicKey) &&
        memcmp(static_cast<void *>(pAssemblySignaturePublicKey),
               static_cast<void *>(pbSignaturePublicKey.GetValue()),
               cbSignaturePublicKey) == 0)
    {
        return true;
    }

    SetStrongNameErrorInfo(SN_E_PUBLICKEY_MISMATCH);
    return false;
}

// Map an assembly into memory.
BOOLEAN LoadAssembly(SN_LOAD_CTX *pLoadCtx, LPCWSTR wszFilePath, DWORD inFlags, BOOLEAN fRequireSignature)
{
    DWORD dwError = S_OK;

    // If a filename is not supplied, the image has already been mapped (and the
    // image base and length fields set up correctly).
    if (wszFilePath == NULL)
    {
        pLoadCtx->m_fPreMapped = TRUE;
        pLoadCtx->m_pedecoder = new (nothrow) PEDecoder(pLoadCtx->m_pbBase, static_cast<COUNT_T>(pLoadCtx->m_dwLength));
        if (pLoadCtx->m_pedecoder == NULL) {
            dwError = E_OUTOFMEMORY;
            goto Error;
        }
    }
    else {

        pLoadCtx->m_hMap = INVALID_HANDLE_VALUE;
        pLoadCtx->m_pbBase = NULL;

        // Open the file for reading or writing.
        pLoadCtx->m_hFile = WszCreateFile(wszFilePath,
                                          GENERIC_READ | (pLoadCtx->m_fReadOnly ? 0 : GENERIC_WRITE),
                                          pLoadCtx->m_fReadOnly ? FILE_SHARE_READ : FILE_SHARE_WRITE,
                                          NULL,
                                          OPEN_EXISTING,
                                          0,
                                          NULL);
        if (pLoadCtx->m_hFile == INVALID_HANDLE_VALUE) {
            dwError = HRESULT_FROM_GetLastError();
            goto Error;
        }

        pLoadCtx->m_dwLength = SafeGetFileSize(pLoadCtx->m_hFile, NULL);
        if (pLoadCtx->m_dwLength == 0xffffffff) {
            dwError = HRESULT_FROM_GetLastError();
            goto Error;
        }

        // Create a mapping handle for the file.
        pLoadCtx->m_hMap = WszCreateFileMapping(pLoadCtx->m_hFile, NULL, pLoadCtx->m_fReadOnly ? PAGE_READONLY : PAGE_READWRITE, 0, 0, NULL);
        if (pLoadCtx->m_hMap == NULL) {
            dwError = HRESULT_FROM_GetLastError();
            goto Error;
        }

        // And map it into memory.
        pLoadCtx->m_pbBase = (BYTE*)CLRMapViewOfFile(pLoadCtx->m_hMap, pLoadCtx->m_fReadOnly ? FILE_MAP_READ : FILE_MAP_WRITE, 0, 0, 0);
        if (pLoadCtx->m_pbBase == NULL) {
            dwError = HRESULT_FROM_GetLastError();
            goto Error;
        }
        pLoadCtx->m_pedecoder = new (nothrow) PEDecoder(pLoadCtx->m_pbBase, static_cast<COUNT_T>(pLoadCtx->m_dwLength));
        if (pLoadCtx->m_pedecoder == NULL) {
            dwError = E_OUTOFMEMORY;
            goto Error;
    }
    }

    if (!pLoadCtx->m_pedecoder->HasContents() || !pLoadCtx->m_pedecoder->CheckCORFormat()) {
        dwError = CORSEC_E_INVALID_IMAGE_FORMAT;
        goto Error;
    }
    
    // Locate standard NT image header.
    pLoadCtx->m_pNtHeaders = pLoadCtx->m_pedecoder->GetNTHeaders32();

    if (pLoadCtx->m_pNtHeaders == NULL) {
        dwError = CORSEC_E_INVALID_IMAGE_FORMAT;
        goto Error;
    }

    pLoadCtx->m_pCorHeader = pLoadCtx->m_pedecoder->GetCorHeader();

    if (pLoadCtx->m_pCorHeader == NULL) {
        dwError = CORSEC_E_INVALID_IMAGE_FORMAT;
        goto Error;
    }

    // Set up signature pointer (if we require it).
    if (fRequireSignature && pLoadCtx->m_pedecoder->HasStrongNameSignature()) 
    {
        COUNT_T size = 0;
        BYTE* pbSignature = (BYTE*)pLoadCtx->m_pedecoder->GetStrongNameSignature(&size);

        // Make sure the signature doesn't point back into the header
        if (pbSignature <= reinterpret_cast<BYTE*>(pLoadCtx->m_pCorHeader) &&
            pbSignature > reinterpret_cast<BYTE*>(pLoadCtx->m_pCorHeader) - size)
        {
            dwError = CORSEC_E_INVALID_IMAGE_FORMAT;
            goto Error;
        }
        if (pbSignature >= reinterpret_cast<BYTE*>(pLoadCtx->m_pCorHeader) &&
            pbSignature - sizeof(IMAGE_COR20_HEADER) < reinterpret_cast<BYTE*>(pLoadCtx->m_pCorHeader))
        {
            dwError = CORSEC_E_INVALID_IMAGE_FORMAT;
            goto Error;
        }

        pLoadCtx->m_pbSignature = pbSignature;
        pLoadCtx->m_cbSignature = static_cast<DWORD>(size); 
    }

    return TRUE;

 Error:
    if (!pLoadCtx->m_fPreMapped) {
    if (pLoadCtx->m_pbBase)
        CLRUnmapViewOfFile(pLoadCtx->m_pbBase);
    if (pLoadCtx->m_hMap != INVALID_HANDLE_VALUE)
        CloseHandle(pLoadCtx->m_hMap);
        if (pLoadCtx->m_hFile != INVALID_HANDLE_VALUE)
            CloseHandle(pLoadCtx->m_hFile);
    }
    SetLastError(dwError);
    return FALSE;
}


// Unload an assembly loaded with LoadAssembly (recomputing checksum if
// necessary).
BOOLEAN UnloadAssembly(SN_LOAD_CTX *pLoadCtx)
{
    BOOLEAN             bResult = TRUE;

    if (!pLoadCtx->m_fReadOnly) {

        IMAGE_NT_HEADERS   *pNtHeaders = NULL;
        DWORD               dwCheckSum = 0;

        // We late bind CheckSumMappedFile to avoid bringing in IMAGEHLP unless
        // we need to.
        HMODULE hLibrary = WszLoadLibrary(W("imagehlp.dll"));
        if (hLibrary) {
            IMAGE_NT_HEADERS *(*SN_CheckSumMappedFile)(BYTE*, DWORD, DWORD*, DWORD*);

            if ((*(FARPROC*)&SN_CheckSumMappedFile = GetProcAddress(hLibrary, "CheckSumMappedFile")) != NULL) {
                DWORD               dwOldCheckSum;

                pNtHeaders = SN_CheckSumMappedFile(pLoadCtx->m_pbBase,
                                        pLoadCtx->m_dwLength,
                                        &dwOldCheckSum,
                                        &dwCheckSum);
            }

            FreeLibrary(hLibrary);

        }

        if (pNtHeaders != NULL) {
            if (pNtHeaders->OptionalHeader.Magic == VAL16(IMAGE_NT_OPTIONAL_HDR32_MAGIC))
                ((IMAGE_NT_HEADERS32*)pNtHeaders)->OptionalHeader.CheckSum = VAL32(dwCheckSum);
            else
                if (pNtHeaders->OptionalHeader.Magic == VAL16(IMAGE_NT_OPTIONAL_HDR64_MAGIC))
                    ((IMAGE_NT_HEADERS64*)pNtHeaders)->OptionalHeader.CheckSum = VAL32(dwCheckSum);
        } else
            bResult = FALSE;

        if (!pLoadCtx->m_fPreMapped && !FlushViewOfFile(pLoadCtx->m_pbBase, 0))
            bResult = FALSE;
    }

    if (!pLoadCtx->m_fPreMapped) {
        if (!CLRUnmapViewOfFile(pLoadCtx->m_pbBase))
            bResult = FALSE;

        if (!CloseHandle(pLoadCtx->m_hMap))
            bResult = FALSE;

        if (!CloseHandle(pLoadCtx->m_hFile))
            bResult = FALSE;
    }

    if (pLoadCtx->m_pedecoder != NULL)
    {
        delete (pLoadCtx->m_pedecoder);
        pLoadCtx->m_pedecoder = NULL;
    }

    return bResult;
}

template<class T>
LONG RegQueryValueT(HKEY hKey, LPCWSTR pValueName, T * pData)
{
    DWORD dwLength = sizeof(T);
    
    LONG status = WszRegQueryValueEx(hKey, pValueName, NULL, NULL, (BYTE*) pData, & dwLength);

    return status;
}

// Reads CSP configuration info (name of CSP to use, IDs of hashing/signing
// algorithms) from the registry.
HRESULT ReadRegistryConfig()
{
    HKEY    hKey;
    DWORD   dwLength;

    // Initialize all settings to their default values, in case they've not been
    // specified in the registry.
    g_bHasCSPName = FALSE;
    g_bUseMachineKeyset = TRUE;
    g_uKeySpec = AT_SIGNATURE;
    g_uHashAlgId = CALG_SHA1;
    g_uSignAlgId = CALG_RSA_SIGN;
    g_uProvType = PROV_RSA_FULL;

#ifdef FEATURE_STRONGNAME_DELAY_SIGNING_ALLOWED
    g_pVerificationRecords = NULL;
#endif

    g_fCacheVerify = TRUE;

    // Open the configuration key in the registry.
    if (WszRegOpenKeyEx(HKEY_LOCAL_MACHINE, SN_CONFIG_KEY_W, 0, KEY_READ, &hKey) != ERROR_SUCCESS)
        return S_OK;

    // Read the preferred CSP name.
    {
        // Working set optimization: avoid touching g_wszCSPName (2052 bytes in size) unless registry has value for it
        WCHAR tempCSPName[_countof(g_wszCSPName)];
        dwLength = sizeof(tempCSPName);
            
        tempCSPName[0] = 0;

        // If the registry key value is too long, that means it is invalid.
        VERIFY(WszRegQueryValueEx(hKey, SN_CONFIG_CSP_W, NULL, NULL,
               (BYTE*) tempCSPName, &dwLength) != ERROR_MORE_DATA);
        tempCSPName[COUNTOF(tempCSPName) - 1] = W('\0');   // make sure the string is NULL-terminated
        SNLOG((W("Preferred CSP name: '%s'\n"), tempCSPName));

        if (tempCSPName[0] != W('\0'))
        {
            memcpy(g_wszCSPName, tempCSPName, sizeof(g_wszCSPName));
            g_bHasCSPName = TRUE;
        }
    }

    // Read the machine vs user key container flag.
    DWORD dwUseMachineKeyset = TRUE;
    RegQueryValueT(hKey, SN_CONFIG_MACHINE_KEYSET_W, & dwUseMachineKeyset);
    SNLOG((W("Use machine keyset: %s\n"), dwUseMachineKeyset ? W("TRUE") : W("FALSE")));
    g_bUseMachineKeyset = (BOOLEAN)dwUseMachineKeyset;

    // Read the key spec.
    RegQueryValueT(hKey, SN_CONFIG_KEYSPEC_W, & g_uKeySpec);
    SNLOG((W("Key spec: %08X\n"), g_uKeySpec));

    // Read the provider type
    RegQueryValueT(hKey, SN_CONFIG_PROV_TYPE_W, & g_uProvType);
    SNLOG((W("Provider Type: %08X\n"), g_uProvType));

    // Read the hashing algorithm ID.
    RegQueryValueT(hKey, SN_CONFIG_HASH_ALG_W, & g_uHashAlgId);
    SNLOG((W("Hashing algorithm: %08X\n"), g_uHashAlgId));

    // Read the signing algorithm ID.
    RegQueryValueT(hKey, SN_CONFIG_SIGN_ALG_W, & g_uSignAlgId);
    SNLOG((W("Signing algorithm: %08X\n"), g_uSignAlgId));

    // Read the OK to cache verifications flag.
    DWORD dwCacheVerify = TRUE;
    RegQueryValueT(hKey, SN_CONFIG_CACHE_VERIFY_W, & dwCacheVerify);
    SNLOG((W("OK to cache verifications: %s\n"), dwCacheVerify ? W("TRUE") : W("FALSE")));
    g_fCacheVerify = (BOOLEAN)dwCacheVerify;

    RegCloseKey(hKey);
    
    HRESULT hr = S_OK;
#ifdef FEATURE_STRONGNAME_DELAY_SIGNING_ALLOWED
    // Read verify disable records.
    IfFailRet(ReadVerificationRecords());
#endif // FEATURE_STRONGNAME_DELAY_SIGNING_ALLOWED
    
#ifdef FEATURE_STRONGNAME_MIGRATION
    IfFailRet(ReadRevocationRecords());
#endif // FEATURE_STRONGNAME_MIGRATION
    
    return hr;
}

#ifdef FEATURE_STRONGNAME_DELAY_SIGNING_ALLOWED
// Read verification records from the registry during startup.
HRESULT ReadVerificationRecords()
{
    HKEYHolder hKey;
    WCHAR      wszSubKey[MAX_PATH_FNAME + 1];
    DWORD      cchSubKey;
    SN_VER_REC *pVerificationRecords = NULL;
    HRESULT    hr = S_OK;

    // Open the verification subkey in the registry.
    if (WszRegOpenKeyEx(HKEY_LOCAL_MACHINE, SN_CONFIG_KEY_W W("\\") SN_CONFIG_VERIFICATION_W, 0, KEY_READ, &hKey) != ERROR_SUCCESS)
        return hr;

    // Assembly specific records are represented as subkeys of the key we've
    // just opened.
    for (DWORD i = 0; ; i++) {
        // Get the name of the next subkey.
        cchSubKey = MAX_PATH_FNAME + 1;
        FILETIME sFiletime;
        if (WszRegEnumKeyEx(hKey, i, wszSubKey, &cchSubKey, NULL, NULL, NULL, &sFiletime) != ERROR_SUCCESS)
            break;

        // Open the subkey.
        HKEYHolder hSubKey;
        if (WszRegOpenKeyEx(hKey, wszSubKey, 0, KEY_READ, &hSubKey) == ERROR_SUCCESS) {
            NewArrayHolder<WCHAR> mszUserList(NULL);
            DWORD cbUserList;
            NewArrayHolder<WCHAR> wszTestPublicKey(NULL);
            DWORD cbTestPublicKey;
            NewArrayHolder<WCHAR> wszAssembly(NULL);
            SN_VER_REC *pVerRec;

            // Read a list of valid users, if supplied.
            if ((WszRegQueryValueEx(hSubKey, SN_CONFIG_USERLIST_W, NULL, NULL, NULL, &cbUserList) == ERROR_SUCCESS) &&
                (cbUserList > 0)) {
                mszUserList = new (nothrow) WCHAR[cbUserList / sizeof(WCHAR)];
                if (!mszUserList) {
                    hr = E_OUTOFMEMORY;
                    goto FreeListExit;
                }
                WszRegQueryValueEx(hSubKey, SN_CONFIG_USERLIST_W, NULL, NULL, (BYTE*)mszUserList.GetValue(), &cbUserList);
            }

            // Read the test public key, if supplied
            if ((WszRegQueryValueEx(hSubKey, SN_CONFIG_TESTPUBLICKEY_W, NULL, NULL, NULL, &cbTestPublicKey) == ERROR_SUCCESS) &&
                (cbTestPublicKey > 0)) {
                wszTestPublicKey = new (nothrow) WCHAR[cbTestPublicKey / sizeof(WCHAR)];
                if (!wszTestPublicKey) {
                    hr = E_OUTOFMEMORY;
                    goto FreeListExit;
                }
                WszRegQueryValueEx(hSubKey, SN_CONFIG_TESTPUBLICKEY_W, NULL, NULL, (BYTE*)wszTestPublicKey.GetValue(), &cbTestPublicKey);
            }

            size_t dwSubKeyLen = wcslen(wszSubKey);
            wszAssembly = new (nothrow) WCHAR[dwSubKeyLen+1];
            if (!wszAssembly) {
                hr = E_OUTOFMEMORY;
                goto FreeListExit;
            }
            wcsncpy_s(wszAssembly, dwSubKeyLen+1, wszSubKey, _TRUNCATE);
            wszAssembly[dwSubKeyLen] =  W('\0');

            // We've found a valid entry, add it to the local list.
            pVerRec = new (nothrow) SN_VER_REC;
            if (!pVerRec) {
                hr = E_OUTOFMEMORY;
                goto FreeListExit;
            }

            pVerRec->m_mszUserList = mszUserList;
            pVerRec->m_wszTestPublicKey = wszTestPublicKey;
            pVerRec->m_wszAssembly = wszAssembly;

            mszUserList.SuppressRelease();
            wszTestPublicKey.SuppressRelease();
            wszAssembly.SuppressRelease();

            pVerRec->m_pNext = pVerificationRecords;
            pVerificationRecords = pVerRec;
            SNLOG((W("Verification record for '%s' found in registry\n"), wszSubKey));
        }
    }

    // Initialize the global list of verification records.
    PVOID pv = InterlockedCompareExchangeT(&g_pVerificationRecords, pVerificationRecords, NULL);
    if (pv == NULL)
        return hr;

FreeListExit:
    // Iterate over local list of verification records and free allocated memory.
    SN_VER_REC *pVerRec = pVerificationRecords;
    while (pVerRec) {
        delete [] pVerRec->m_mszUserList;
        delete [] pVerRec->m_wszTestPublicKey;
        delete [] pVerRec->m_wszAssembly;
        SN_VER_REC *tmp = pVerRec->m_pNext;
        delete pVerRec;
        pVerRec = tmp;
    }
    return hr;
}
#endif // FEATURE_STRONGNAME_DELAY_SIGNING_ALLOWED


#ifdef FEATURE_STRONGNAME_MIGRATION

#define SN_REVOCATION_KEY_NAME_W      W("RevokedKeys") // Registry revocation key name
#define SN_REVOKEDKEY_VALUE_NAME_W   W("RevokedKey")  // Registry value name

HRESULT ReadReplacementKeys(HKEY hKey, SN_REPLACEMENT_KEY_REC **ppReplacementRecords)
{
    HRESULT hr = S_OK;
                
    DWORD uValueCount;
    DWORD cchMaxValueNameLen;

    NewArrayHolder<WCHAR> wszValueName(NULL);

    if(RegQueryInfoKey(hKey, NULL, NULL, NULL, NULL, NULL, NULL, &uValueCount, &cchMaxValueNameLen, NULL, NULL, NULL) != ERROR_SUCCESS)
        return hr;

    cchMaxValueNameLen++; // Add 1 for null character

    DWORD cchValueName;
    wszValueName = new (nothrow) WCHAR[cchMaxValueNameLen];
    if (!wszValueName) {
        return E_OUTOFMEMORY;
    }
                
    for (DWORD j = 0; j < uValueCount; j++) {
        cchValueName = cchMaxValueNameLen;
        if (WszRegEnumValue(hKey, j, wszValueName, &cchValueName, NULL, NULL, NULL, NULL) != ERROR_SUCCESS)
            break;

        if(SString::_wcsicmp(wszValueName, SN_REVOKEDKEY_VALUE_NAME_W) == 0) // Skip over the "RevokedKey" value
            continue;

        NewArrayHolder<BYTE> pbReplacementKey(NULL);
        DWORD cbReplacementKey;
        DWORD dwValType;
        if ((WszRegQueryValueEx(hKey, wszValueName, NULL, &dwValType, NULL, &cbReplacementKey) == ERROR_SUCCESS) &&
            (cbReplacementKey > 0) && (dwValType == REG_BINARY)) {
            pbReplacementKey = new (nothrow) BYTE[cbReplacementKey];
            if (!pbReplacementKey) {
                return E_OUTOFMEMORY;
            }
            if(WszRegQueryValueEx(hKey, wszValueName, NULL, NULL, (BYTE*)pbReplacementKey.GetValue(), &cbReplacementKey) == ERROR_SUCCESS)
            {
                NewHolder<SN_REPLACEMENT_KEY_REC> pReplacementRecord(new (nothrow) SN_REPLACEMENT_KEY_REC);
                if (pReplacementRecord == NULL) {
                    return E_OUTOFMEMORY;
                }
                
                pReplacementRecord->m_pbReplacementKey = pbReplacementKey.Extract();
                pReplacementRecord->m_cbReplacementKey = cbReplacementKey;
                // Insert into list
                pReplacementRecord->m_pNext = *ppReplacementRecords;
                *ppReplacementRecords = pReplacementRecord.Extract();
            }
        }
    }

    return hr;
}

// Read revocation records from the registry during startup.
HRESULT ReadRevocationRecordsFromKey(REGSAM samDesired, SN_REVOCATION_REC **ppRevocationRecords)
{
    HKEYHolder          hKey;
    WCHAR               wszSubKey[MAX_PATH_FNAME + 1];
    DWORD               cchSubKey;
    HRESULT             hr = S_OK;

    // Open the revocation subkey in the registry.
    if (WszRegOpenKeyEx(HKEY_LOCAL_MACHINE, SN_CONFIG_KEY_W W("\\") SN_REVOCATION_KEY_NAME_W, 0, samDesired, &hKey) != ERROR_SUCCESS)
        return hr;
    
    // Assembly specific records are represented as subkeys of the key we've
    // just opened.
    for (DWORD i = 0; ; i++) {
        // Read the next subkey
        cchSubKey = MAX_PATH_FNAME + 1; // reset size of buffer, as the following call changes it
        if (WszRegEnumKeyEx(hKey, i, wszSubKey, &cchSubKey, NULL, NULL, NULL, NULL) != ERROR_SUCCESS)
            break;
        
        // Open the subkey.
        HKEYHolder hSubKey;
        if (WszRegOpenKeyEx(hKey, wszSubKey, 0, samDesired, &hSubKey) == ERROR_SUCCESS) {
            NewArrayHolder<BYTE> pbRevokedKey(NULL);
            DWORD cbRevokedKey;
            DWORD dwValType;

            // Read the "RevokedKey" value
            if ((WszRegQueryValueEx(hSubKey, SN_REVOKEDKEY_VALUE_NAME_W, NULL, &dwValType, NULL, &cbRevokedKey) == ERROR_SUCCESS) &&
                (cbRevokedKey > 0) && (dwValType == REG_BINARY)) {
                pbRevokedKey = new (nothrow) BYTE[cbRevokedKey];
                if (!pbRevokedKey) {
                    return E_OUTOFMEMORY;
                }

                if(WszRegQueryValueEx(hSubKey, SN_REVOKEDKEY_VALUE_NAME_W, NULL, NULL, (BYTE*)pbRevokedKey.GetValue(), &cbRevokedKey) == ERROR_SUCCESS)
                {
                    // We've found a valid entry, store it
                    NewHolder<SN_REVOCATION_REC> pRevocationRecord(new (nothrow) SN_REVOCATION_REC);
                    if (pRevocationRecord == NULL) {
                        return E_OUTOFMEMORY;
                    }
                    
                    pRevocationRecord->m_pbRevokedKey = pbRevokedKey.Extract();
                    pRevocationRecord->m_cbRevokedKey = cbRevokedKey;
                    pRevocationRecord->m_pReplacementKeys = NULL;

                    // Insert into list
                    pRevocationRecord->m_pNext = *ppRevocationRecords;
                    *ppRevocationRecords = pRevocationRecord.Extract();

                    IfFailRet(ReadReplacementKeys(hSubKey, &pRevocationRecord->m_pReplacementKeys));

                    SNLOG((W("Revocation record '%s' found in registry\n"), wszSubKey));
                }
            }
        }
    }

    return hr;
}

HRESULT ReadRevocationRecords()
{
    HRESULT             hr = S_OK;

    SYSTEM_INFO systemInfo;
    SN_REVOCATION_REC *pRevocationRecords = NULL;

    GetNativeSystemInfo(&systemInfo);
     // Read both Software\ and Software\WOW6432Node\ on 64-bit systems
    if(systemInfo.wProcessorArchitecture == PROCESSOR_ARCHITECTURE_AMD64)
    {
        IfFailGoto(ReadRevocationRecordsFromKey(KEY_READ | KEY_WOW64_64KEY, &pRevocationRecords), FreeListExit);
        IfFailGoto(ReadRevocationRecordsFromKey(KEY_READ | KEY_WOW64_32KEY, &pRevocationRecords), FreeListExit);
    }
    else
    {
        IfFailGoto(ReadRevocationRecordsFromKey(KEY_READ, &pRevocationRecords), FreeListExit);
    }
    
    // Initialize the global list of verification records.
    PVOID pv = InterlockedCompareExchangeT(&g_pRevocationRecords, pRevocationRecords, NULL);

    if (pv == NULL) // Successfully inserted the list we just created
        return hr;

FreeListExit:
    // Iterate over local list of verification records and free allocated memory.
    SN_REVOCATION_REC *pRevRec = pRevocationRecords;
    while (pRevRec) {
        if(pRevRec->m_pbRevokedKey)
            delete [] pRevRec->m_pbRevokedKey;

        SN_REPLACEMENT_KEY_REC *pKeyRec = pRevRec->m_pReplacementKeys;
        while (pKeyRec) {
            if(pKeyRec->m_pbReplacementKey)
                delete [] pKeyRec->m_pbReplacementKey;
            
            SN_REPLACEMENT_KEY_REC *tmp = pKeyRec->m_pNext;
            delete pKeyRec;
            pKeyRec = tmp;
        }

        SN_REVOCATION_REC *tmp2 = pRevRec->m_pNext;
        delete pRevRec;
        pRevRec = tmp2;
    }
    return hr;
}

#endif // FEATURE_STRONGNAME_MIGRATION

// Check current user name against a multi-string user name list. Return true if
// the name is found (or the list is empty).
BOOLEAN IsValidUser(__in_z WCHAR *mszUserList)
{
    HANDLE          hToken;
    DWORD           dwRetLen;
    TOKEN_USER     *pUser;
    WCHAR           wszUser[1024];
    WCHAR           wszDomain[1024];
    DWORD           cchUser;
    DWORD           cchDomain;
    SID_NAME_USE    eSidUse;
    WCHAR          *wszUserEntry;

    // Empty list implies no user name checking.
    if (mszUserList == NULL)
        return TRUE;

    // Get current user name. Don't cache this to avoid threading/impersonation
    // problems.
    // First look to see if there's a security token on the current thread
    // (maybe we're impersonating). If not, we'll get the token from the
    // process.
    if (!OpenThreadToken(GetCurrentThread(), TOKEN_READ, FALSE, &hToken))
        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_READ, &hToken)) {
            SNLOG((W("Failed to find a security token, error %08X\n"), GetLastError()));
            return FALSE;
        }

    // Get the user SID. (Calculate buffer size first).
    if (!GetTokenInformation(hToken, TokenUser, NULL, 0, &dwRetLen) &&
        GetLastError() != ERROR_INSUFFICIENT_BUFFER) {
        SNLOG((W("Failed to calculate token information buffer size, error %08X\n"), GetLastError()));
        CloseHandle(hToken);
        return FALSE;
    }

    NewArrayHolder<BYTE> pvBuffer = new (nothrow) BYTE[dwRetLen];
    if (pvBuffer == NULL)
    {
        SetLastError(E_OUTOFMEMORY);
        return FALSE;
    }

    if (!GetTokenInformation(hToken, TokenUser, reinterpret_cast<LPVOID>((BYTE*)pvBuffer), dwRetLen, &dwRetLen)) {
        SNLOG((W("Failed to acquire token information, error %08X\n"), GetLastError()));
        CloseHandle(hToken);
        return FALSE;
    }

    pUser = reinterpret_cast<TOKEN_USER *>(pvBuffer.GetValue());

    // Get the user and domain names.
    cchUser = sizeof(wszUser) / sizeof(WCHAR);
    cchDomain = sizeof(wszDomain) / sizeof(WCHAR);
    if (!WszLookupAccountSid(NULL, pUser->User.Sid,
                             wszUser, &cchUser,
                             wszDomain, &cchDomain,
                             &eSidUse)) {
        SNLOG((W("Failed to lookup account information, error %08X\n"), GetLastError()));
        CloseHandle(hToken);
        return FALSE;
    }

    CloseHandle(hToken);

    // Concatenate user and domain name to get a fully qualified account name.
    if (((wcslen(wszUser) + wcslen(wszDomain) + 2) * sizeof(WCHAR)) > sizeof(wszDomain)) {
        SNLOG((W("Fully qualified account name was too long\n")));
        return FALSE;
    }
    wcscat_s(wszDomain, COUNTOF(wszDomain), W("\\"));
    wcscat_s(wszDomain, COUNTOF(wszDomain), wszUser);
    SNLOG((W("Current username is '%s'\n"), wszDomain));

    // Check current user against each name in the multi-string (packed
    // list of nul terminated strings terminated with an additional nul).
    wszUserEntry = mszUserList;
    while (*wszUserEntry) {
        if (!SString::_wcsicmp(wszDomain, wszUserEntry))
            return TRUE;
        wszUserEntry += wcslen(wszUserEntry) + 1;
    }

    // No user name match, fail search.
    SNLOG((W("No username match\n")));

    return FALSE;
}

#ifdef FEATURE_STRONGNAME_DELAY_SIGNING_ALLOWED
// See if there's a verification records for the given assembly.
SN_VER_REC *GetVerificationRecord(__in_z __deref LPWSTR wszAssemblyName, PublicKeyBlob *pPublicKey)
{
    SN_VER_REC *pVerRec;
    SN_VER_REC *pWildcardVerRec = NULL;
    LPWSTR      pAssembly = NULL;
    BYTE       *pbToken;
    DWORD       cbToken;
    WCHAR       wszStrongName[(SN_SIZEOF_TOKEN * 2) + 1];
    DWORD       i;

    // Compress the public key to make for a shorter assembly name.
    if (!StrongNameTokenFromPublicKey((BYTE*)pPublicKey,
                                      SN_SIZEOF_KEY(pPublicKey),
                                      &pbToken,
                                      &cbToken))
        return NULL;

    if (cbToken > SN_SIZEOF_TOKEN)
        return NULL;

    // Turn the token into hex.
    for (i = 0; i < cbToken; i++) {
        static WCHAR *wszHex = W("0123456789ABCDEF");
        wszStrongName[(i * 2) + 0] = wszHex[(pbToken[i] >> 4)];
        wszStrongName[(i * 2) + 1] = wszHex[(pbToken[i] & 0x0F)];
    }
    wszStrongName[i * 2] = W('\0');
    delete[] pbToken;

    // Build the full assembly name.

    size_t nLen = wcslen(wszAssemblyName) + wcslen(W(",")) + wcslen(wszStrongName);
    pAssembly = new (nothrow) WCHAR[nLen +1]; // +1 for NULL
    if (pAssembly == NULL)
            return NULL;
    wcscpy_s(pAssembly, nLen + 1, wszAssemblyName);
    wcscat_s(pAssembly, nLen + 1, W(","));
    wcscat_s(pAssembly, nLen + 1, wszStrongName);

    // Iterate over global list of verification records.
    for (pVerRec = g_pVerificationRecords; pVerRec; pVerRec = pVerRec->m_pNext) {
        // Look for matching assembly name.
        if (!SString::_wcsicmp(pAssembly, pVerRec->m_wszAssembly)) {
            delete[] pAssembly;
            // Check current user against allowed user name list.
            if (IsValidUser(pVerRec->m_mszUserList))
                return pVerRec;
            else
                return NULL;
        } else if (!wcscmp(W("*,*"), pVerRec->m_wszAssembly)) {
            // Found a wildcard record, it'll do if we don't find something more
            // specific.
            if (pWildcardVerRec == NULL)
                pWildcardVerRec = pVerRec;
        } else if (!wcsncmp(W("*,"), pVerRec->m_wszAssembly, 2)) {
            // Found a wildcard record (with a specific strong name). If the
            // strong names match it'll do unless we find something more
            // specific (it overrides "*,*" wildcards though).
            if (!SString::_wcsicmp(wszStrongName, &pVerRec->m_wszAssembly[2]))
                pWildcardVerRec = pVerRec;
        }
    }

    delete[] pAssembly;

    // No match on specific assembly name, see if there's a wildcard entry.
    if (pWildcardVerRec)
        // Check current user against allowed user name list.
        if (IsValidUser(pWildcardVerRec->m_mszUserList))
            return pWildcardVerRec;
        else
            return NULL;

    return NULL;
}
#endif // FEATURE_STRONGNAME_DELAY_SIGNING_ALLOWED

HRESULT 
CallGetMetaDataInternalInterface(
    LPVOID  pData, 
    ULONG   cbData, 
    DWORD   flags, 
    REFIID  riid, 
    LPVOID *ppInterface)
{
#ifdef FEATURE_STRONGNAME_STANDALONE_WINRT
    return E_NOTIMPL; 
#elif STRONGNAME_IN_VM || !FEATURE_STANDALONE_SN
    // We link the GetMetaDataInternalInterface, so just call it
    return GetMetaDataInternalInterface(
        pData, 
        cbData, 
        flags, 
        riid, 
        ppInterface);
#elif FEATURE_CORECLR
    return E_NOTIMPL; 
#else

    // We late bind the metadata function to avoid having a direct dependence on 
    // mscoree.dll unless we absolutely need to.
    
    HRESULT                  hr = S_OK;
    ICLRMetaHost            *pCLRMetaHost = NULL;
    ICLRRuntimeInfo         *pCLRRuntimeInfo = NULL;
    ICLRRuntimeHostInternal *pCLRRuntimeHostInternal = NULL;
    
    HMODULE hLibrary = WszLoadLibrary(MSCOREE_SHIM_W);
    if (hLibrary == NULL)
    {
        hr = HRESULT_FROM_GetLastError();
        SNLOG((W("WszLoadLibrary(\"") MSCOREE_SHIM_W W("\") failed with %08x\n"), hr));
        goto ErrExit;
    }
    
    typedef HRESULT (__stdcall *PFNCLRCreateInstance)(REFCLSID clsid, REFIID riid, /*iid_is(riid)*/ LPVOID *ppInterface);
    PFNCLRCreateInstance pfnCLRCreateInstance = reinterpret_cast<PFNCLRCreateInstance>(GetProcAddress(
        hLibrary, 
        "CLRCreateInstance"));
    if (pfnCLRCreateInstance == NULL)
    {
        hr = HRESULT_FROM_GetLastError();
        SNLOG((W("Couldn't find CLRCreateInstance() in ") MSCOREE_SHIM_W W(": %08x\n"), hr));
        goto ErrExit;
    }
    
    if (FAILED(hr = pfnCLRCreateInstance(CLSID_CLRMetaHost, IID_ICLRMetaHost, (LPVOID *)&pCLRMetaHost)))
    {
        SNLOG((W("Error calling CLRCreateInstance() in ") MSCOREE_SHIM_W W(": %08x\n"), hr));
        goto ErrExit;
    }
    
    if (FAILED(hr = pCLRMetaHost->GetRuntime(
        W("v") VER_PRODUCTVERSION_NO_QFE_STR_L, 
        IID_ICLRRuntimeInfo, 
        (LPVOID *)&pCLRRuntimeInfo)))
    {
        SNLOG((W("Error calling ICLRMetaHost::GetRuntime() in ") MSCOREE_SHIM_W W(": %08x\n"), hr));
        goto ErrExit;
    }
    
    if (FAILED(hr = pCLRRuntimeInfo->GetInterface(
        CLSID_CLRRuntimeHostInternal, 
        IID_ICLRRuntimeHostInternal, 
        (LPVOID *)&pCLRRuntimeHostInternal)))
    {
        SNLOG((W("Error calling ICLRRuntimeInfo::GetInterface() in ") MSCOREE_SHIM_W W(": %08x\n"), hr));
        goto ErrExit;
    }
    
    hr = pCLRRuntimeHostInternal->GetMetaDataInternalInterface(
        (BYTE *)pData, 
        cbData, 
        flags, 
        riid, 
        ppInterface);
    
ErrExit:
    if (pCLRMetaHost != NULL)
    {
        pCLRMetaHost->Release();
    }
    if (pCLRRuntimeInfo != NULL)
    {
        pCLRRuntimeInfo->Release();
    }
    if (pCLRRuntimeHostInternal != NULL)
    {
        pCLRRuntimeHostInternal->Release();
    }
    
    return hr;

#endif
} // CallGetMetaDataInternalInterface

// Load metadata engine and return an importer.
HRESULT 
GetMetadataImport(
    __in const SN_LOAD_CTX   *pLoadCtx, 
    __in mdAssembly          *ptkAssembly, 
    __out IMDInternalImport **ppMetaDataImport)
{
    HRESULT hr = E_FAIL;
    BYTE   *pMetaData = NULL;

    // Locate the COM+ meta data within the header.
    if (pLoadCtx->m_pedecoder->CheckCorHeader())
    {
        pMetaData = (BYTE *)pLoadCtx->m_pedecoder->GetMetadata();
    }

    if (pMetaData == NULL)
    {
        SNLOG((W("Couldn't locate the COM+ header\n")));
        return CORSEC_E_INVALID_IMAGE_FORMAT;
    }

    // Open a metadata scope on the memory directly.
    ReleaseHolder<IMDInternalImport> pMetaDataImportHolder;
    if (FAILED(hr = CallGetMetaDataInternalInterface(
        pMetaData, 
        VAL32(pLoadCtx->m_pCorHeader->MetaData.Size), 
        ofRead, 
        IID_IMDInternalImport, 
        &pMetaDataImportHolder)))
    {
        SNLOG((W("GetMetaDataInternalInterface() failed with %08x\n"), hr));
        return SubstituteErrorIfNotTransient(hr, CORSEC_E_INVALID_IMAGE_FORMAT);
    }

    // Determine the metadata token for the assembly from the scope.
    if (FAILED(hr = pMetaDataImportHolder->GetAssemblyFromScope(ptkAssembly)))
    {
        SNLOG((W("pMetaData->GetAssemblyFromScope() failed with %08x\n"), hr));
        return SubstituteErrorIfNotTransient(hr, CORSEC_E_INVALID_IMAGE_FORMAT);
    }

    *ppMetaDataImport = pMetaDataImportHolder.Extract(); 
    return S_OK;
}
#if STRONGNAME_IN_VM
// Function to form the fully qualified assembly name from the load context
BOOL FormFullyQualifiedAssemblyName(SN_LOAD_CTX   *pLoadCtx, SString &assemblyName)
{
    mdAssembly tkAssembly;
    // Open a metadata scope on the image.
    ReleaseHolder<IMDInternalImport> pMetaDataImport;
    HRESULT hr;
    if (FAILED(hr = GetMetadataImport(pLoadCtx, &tkAssembly, &pMetaDataImport)))
        return FALSE;

    if (pMetaDataImport != NULL)
    {
        PEAssembly::GetFullyQualifiedAssemblyName(pMetaDataImport, tkAssembly, assemblyName);
        return TRUE;
    }
    return FALSE;
}
#endif


// Locate the public key blob located within the metadata of an assembly file
// and return a copy (use delete to deallocate). Optionally get the assembly
// name as well.
HRESULT FindPublicKey(const SN_LOAD_CTX *pLoadCtx,
                      __out_ecount_opt(cchAssemblyName) LPWSTR         wszAssemblyName,
                      DWORD cchAssemblyName,
                      __out PublicKeyBlob **ppPublicKey,
                      DWORD *pcbPublicKey)
{
    HRESULT hr = S_OK;
    *ppPublicKey = NULL;

    // Open a metadata scope on the image.
    mdAssembly tkAssembly;
    ReleaseHolder<IMDInternalImport> pMetaDataImport;
    if (FAILED(hr = GetMetadataImport(pLoadCtx, &tkAssembly, &pMetaDataImport)))
        return hr;

    // Read the public key location from the assembly properties (it's known as
    // the originator property).
    PublicKeyBlob *pKey;
    DWORD dwKeyLen;
    LPCSTR szAssemblyName;
    if (FAILED(hr = pMetaDataImport->GetAssemblyProps(tkAssembly,           // [IN] The Assembly for which to get the properties
                                      (const void **)&pKey, // [OUT] Pointer to the Originator blob
                                      &dwKeyLen,            // [OUT] Count of bytes in the Originator Blob
                                      NULL,                 // [OUT] Hash Algorithm
                                      &szAssemblyName,      // [OUT] Buffer to fill with name
                                      NULL,                 // [OUT] Assembly MetaData
                                                      NULL)))               // [OUT] Flags
    {
        SNLOG((W("Did not get public key property: %08x\n"), hr));
        return SubstituteErrorIfNotTransient(hr, CORSEC_E_MISSING_STRONGNAME);
    }

    if (dwKeyLen == 0)
    {
        SNLOG((W("No public key stored in metadata\n")));
        return CORSEC_E_MISSING_STRONGNAME;
    }

    // Make a copy of the key blob (because we're going to close the metadata scope).
    NewArrayHolder<BYTE> pKeyCopy(new (nothrow) BYTE[dwKeyLen]);
    if (pKeyCopy == NULL)
        return E_OUTOFMEMORY;
    memcpy_s(pKeyCopy, dwKeyLen, pKey, dwKeyLen);

    // Copy the assembly name as well (if it was asked for). We also convert
    // from UTF8 to UNICODE while we're at it.
    if (wszAssemblyName)
        WszMultiByteToWideChar(CP_UTF8, 0, szAssemblyName, -1, wszAssemblyName, cchAssemblyName);

    *ppPublicKey = reinterpret_cast<PublicKeyBlob *>(pKeyCopy.Extract());
    if(pcbPublicKey != NULL)
        *pcbPublicKey = dwKeyLen;

    return S_OK;
}

BYTE HexToByte (WCHAR wc) {
    if (!iswxdigit(wc)) return (BYTE) 0xff;
    if (iswdigit(wc)) return (BYTE) (wc - W('0'));
    if (iswupper(wc)) return (BYTE) (wc - W('A') + 10);
    return (BYTE) (wc - W('a') + 10);
}

// Read the hex string into a PublicKeyBlob structure.
// Caller owns the blob.
PublicKeyBlob *GetPublicKeyFromHex(LPCWSTR wszPublicKeyHexString) {
    size_t cchHex = wcslen(wszPublicKeyHexString);
    size_t cbHex = cchHex / 2;
    if (cchHex % 2 != 0)
        return NULL;

    BYTE *pKey = new (nothrow) BYTE[cbHex];
    if (!pKey)
        return NULL;
    for (size_t i = 0; i < cbHex; i++) {
        pKey[i] = (BYTE) ((HexToByte(*wszPublicKeyHexString) << 4) | HexToByte(*(wszPublicKeyHexString + 1)));
        wszPublicKeyHexString += 2;
    }
    return (PublicKeyBlob*) pKey;
}

// Create a temporary key container name likely to be unique to this process and
// thread. Any existing container with the same name is deleted.
BOOLEAN GetKeyContainerName(LPCWSTR *pwszKeyContainer, BOOLEAN *pbTempContainer)
{
    *pbTempContainer = FALSE;

    if (*pwszKeyContainer != NULL)
        return TRUE;

    GUID guid;
    HRESULT hr = CoCreateGuid(&guid);
    if (FAILED(hr)) {
        SetStrongNameErrorInfo(hr);
        return FALSE;
    }

    WCHAR wszGuid[64];
    if (GuidToLPWSTR(guid, wszGuid, sizeof(wszGuid) / sizeof(WCHAR)) == 0) {
        SetStrongNameErrorInfo(E_UNEXPECTED); // this operation should never fail
        return FALSE;
    }

    // Name is of form '__MSCORSN__<guid>__' where <guid> is a GUID.
    const size_t cchLengthOfKeyContainer = sizeof("__MSCORSN____") + (sizeof(wszGuid) / sizeof(WCHAR)) + 1 /* null */;
    LPWSTR wszKeyContainer = new (nothrow) WCHAR[cchLengthOfKeyContainer];
    if (wszKeyContainer == NULL) {
        SetStrongNameErrorInfo(E_OUTOFMEMORY);
        return FALSE;
    }

    _snwprintf_s(wszKeyContainer, cchLengthOfKeyContainer - 1 /* exclude null */, _TRUNCATE,
             W("__MSCORSN__%s__"),
             wszGuid);

    // Delete any stale container with the same name.
    LocateCSP(wszKeyContainer, SN_DELETE_CONTAINER);

    SNLOG((W("Creating temporary key container name '%s'\n"), wszKeyContainer));

    *pwszKeyContainer = wszKeyContainer;
    *pbTempContainer = TRUE;

    return TRUE;
}


// Free resources allocated by GetKeyContainerName and delete the named
// container.
VOID FreeKeyContainerName(LPCWSTR wszKeyContainer, BOOLEAN bTempContainer)
{
    if (bTempContainer) {
        // Free the name.
        delete [] (WCHAR*)wszKeyContainer;
    }
}

static DWORD GetSpecialKeyFlags(PublicKeyBlob* pKey)
{
    if (SN_IS_THE_KEY(pKey))
        return SN_OUTFLAG_MICROSOFT_SIGNATURE;

    return 0;
}

#ifdef FEATURE_STRONGNAME_MIGRATION

HRESULT VerifyCounterSignature(
    PublicKeyBlob *pSignaturePublicKey,
    ULONG          cbSignaturePublicKey,
    PublicKeyBlob *pIdentityPublicKey,
    BYTE          *pCounterSignature,
    ULONG          cbCounterSignature)
{
    LIMITED_METHOD_CONTRACT;

    HRESULT hr = S_OK;

    HandleStrongNameCspHolder hProv(NULL);
    HandleKeyHolder hKey(NULL);
    HandleHashHolder hHash(NULL);

    hProv = LocateCSP(NULL, SN_IGNORE_CONTAINER, GET_UNALIGNED_VAL32(&pIdentityPublicKey->HashAlgID), GET_UNALIGNED_VAL32(&pIdentityPublicKey->SigAlgID));

    if (!hProv)
    {
        hr = HRESULT_FROM_GetLastError();
        SNLOG((W("Failed to acquire a CSP: %08x"), hr));
        return hr;
    }

    if(SN_IS_NEUTRAL_KEY(pIdentityPublicKey))
    {
        pIdentityPublicKey = reinterpret_cast<PublicKeyBlob *>(const_cast<BYTE *>(g_rbTheKey));
    }

    BYTE *pbRealPublicKey = pIdentityPublicKey->PublicKey;
    DWORD cbRealPublicKey = GET_UNALIGNED_VAL32(&pIdentityPublicKey->cbPublicKey);
    
    if (!CryptImportKey(hProv, pbRealPublicKey, cbRealPublicKey, 0, 0, &hKey))
    {
        hr = HRESULT_FROM_GetLastError();
        SNLOG((W("Failed to import key: %08x"), hr));
        return hr;
    }

    // Create a hash object.
    if (!CryptCreateHash(hProv, GET_UNALIGNED_VAL32(&pIdentityPublicKey->HashAlgID), 0, 0, &hHash))
    {
        hr = HRESULT_FROM_GetLastError();
        SNLOG((W("Failed to create hash: %08x"), hr));
        return hr;
    }

    if (!CryptHashData(hHash, (BYTE*)pSignaturePublicKey, cbSignaturePublicKey, 0))
    {
        hr = HRESULT_FROM_GetLastError();
        SNLOG((W("Failed to compute hash: %08x"), hr));
        return hr;
    }

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
    if (hHash != (HCRYPTHASH)INVALID_HANDLE_VALUE) {
        DWORD   cbHash;
        DWORD   dwRetLen = sizeof(cbHash);
        if (CryptGetHashParam(hHash, HP_HASHSIZE, (BYTE*)&cbHash, &dwRetLen, 0))
        {
            NewArrayHolder<BYTE> pbHash(new (nothrow) BYTE[cbHash]);
            if (pbHash != NULL)
            {
                if (CryptGetHashParam(hHash, HP_HASHVAL, pbHash, &cbHash, 0))
                {
                    SNLOG((W("Computed Hash Value (%u bytes):\n"), cbHash));
                    HexDump(pbHash, cbHash);
                }
                else
                {
                    SNLOG((W("CryptGetHashParam() failed with %08X\n"), GetLastError()));
                }
            }
        }
        else
        {
            SNLOG((W("CryptGetHashParam() failed with %08X\n"), GetLastError()));
        }
    }
#endif // _DEBUG

    // Verify the hash against the signature.
    //DbgCount(dwInFlags & SN_INFLAG_RUNTIME ? W("RuntimeVerify") : W("FusionVerify"));
    if (pCounterSignature != NULL && cbCounterSignature != 0 && 
        CryptVerifySignatureW(hHash, pCounterSignature, cbCounterSignature, hKey, NULL, 0))
    {
        SNLOG((W("Counter-signature verification succeeded\n")));
    }
    else
    {
        SNLOG((W("Counter-signature verification failed\n")));
        hr = CORSEC_E_INVALID_COUNTERSIGNATURE;
    }

    return hr;
}

HRESULT ParseStringArgs(
    CustomAttributeParser &ca,    // The Custom Attribute blob.
    CaArg* pArgs,                 // Array of argument descriptors.
    ULONG cArgs)                  // Count of argument descriptors.
{
    LIMITED_METHOD_CONTRACT;

    HRESULT     hr = S_OK;

    // For each expected arg...
    for (ULONG ix=0; ix<cArgs; ++ix)
    {
        CaArg* pArg = &pArgs[ix];
        if(pArg->type.tag != SERIALIZATION_TYPE_STRING)
        {
            return E_UNEXPECTED; // The blob shouldn't have anything other than strings
        }
        IfFailGo(ca.GetString(&pArg->val.str.pStr, &pArg->val.str.cbStr));
    }
    
ErrExit:
    return hr;
}

HRESULT GetVerifiedSignatureKey(__in SN_LOAD_CTX *pLoadCtx, __out PublicKeyBlob **ppPublicKey, __out_opt DWORD *pcbPublicKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    
    mdAssembly tkAssembly;
    ReleaseHolder<IMDInternalImport> pMetaDataImport;
    IfFailRet(GetMetadataImport(pLoadCtx, &tkAssembly, &pMetaDataImport));

    HRESULT attributeHr;
    void *pAttribute;
    ULONG cbAttribute;
    hr = pMetaDataImport->GetCustomAttributeByName(tkAssembly, g_AssemblySignatureKeyAttribute, const_cast<const void**>(&pAttribute), &cbAttribute);

    if (SUCCEEDED(hr) && hr != S_FALSE)
    {
        CustomAttributeParser parser(pAttribute, cbAttribute);
        IfFailRet(parser.ValidateProlog());

        CaType caTypeString;
        caTypeString.Init(SERIALIZATION_TYPE_STRING);
        
        CaArg args[2];

        CaArg* argPublicKey = &args[0];
        argPublicKey->Init(caTypeString);
        
        CaArg* argCounterSignature = &args[1];
        argCounterSignature->Init(caTypeString);
        
        IfFailRet(ParseStringArgs(parser, args, lengthof(args)));

        StrongNameBufferHolder<PublicKeyBlob> pSignaturePublicKey;
        ULONG cbSignaturePublicKey;
        if (argPublicKey->val.str.pStr == NULL || argPublicKey->val.str.cbStr == 0 ||
            (!GetBytesFromHex(argPublicKey->val.str.pStr, argPublicKey->val.str.cbStr, (BYTE**)(pSignaturePublicKey.GetAddr()), &cbSignaturePublicKey)) ||
            !StrongNameIsValidPublicKey((BYTE*)pSignaturePublicKey.GetValue(), cbSignaturePublicKey, false))
        {
            return CORSEC_E_INVALID_SIGNATUREKEY;
        }
        
        NewArrayHolder<BYTE> pCounterSignature;
        ULONG cbCounterSignature;
        if (argCounterSignature->val.str.pStr == NULL || argCounterSignature->val.str.cbStr == 0 ||
            (!GetBytesFromHex(argCounterSignature->val.str.pStr, argCounterSignature->val.str.cbStr, &pCounterSignature, &cbCounterSignature)))
        {
            return CORSEC_E_INVALID_COUNTERSIGNATURE;
        }

        StrongNameBufferHolder<PublicKeyBlob> pIdentityPublicKey = NULL;
        IfFailRet(FindPublicKey(pLoadCtx, NULL, 0, &pIdentityPublicKey));

        IfFailRet(VerifyCounterSignature(pSignaturePublicKey, cbSignaturePublicKey, pIdentityPublicKey, pCounterSignature, cbCounterSignature));

        *ppPublicKey = pSignaturePublicKey.Extract();
        if (pcbPublicKey != NULL)
            *pcbPublicKey = cbSignaturePublicKey;
    }
    else
    {
        *ppPublicKey = NULL;
        if (pcbPublicKey != NULL)
            *pcbPublicKey = 0;
    }

    return hr;
}

// Checks revocation list against the assembly's public keys.
// If the identity key has been revoked, then the signature key must be non-null and
// must be in the replacement keys list to be allowed.
bool AreKeysAllowedByRevocationList(BYTE* pbAssemblyIdentityKey, DWORD cbAssemblyIdentityKey, BYTE* pbAssemblySignatureKey, DWORD cbAssemblySignatureKey)
{
    LIMITED_METHOD_CONTRACT;

    bool fRevoked = false;

    SN_REVOCATION_REC *pRevocationRec = g_pRevocationRecords;
    while (pRevocationRec)
    {
        if (pRevocationRec->m_cbRevokedKey == cbAssemblyIdentityKey && 
            memcmp(pRevocationRec->m_pbRevokedKey, pbAssemblyIdentityKey, cbAssemblyIdentityKey) == 0)
        {
            fRevoked = true; // Identity key can't be trusted.
            
            if (pbAssemblySignatureKey != NULL)
            {
                SN_REPLACEMENT_KEY_REC *pReplacementKeyRec = pRevocationRec->m_pReplacementKeys;

                while (pReplacementKeyRec)
                {
                    if (pReplacementKeyRec->m_cbReplacementKey == cbAssemblySignatureKey && 
                        memcmp(pReplacementKeyRec->m_pbReplacementKey, pbAssemblySignatureKey, cbAssemblySignatureKey) == 0)
                    {
                        // Signature key was allowed as a replacement for the revoked identity key.
                        return true;
                    }

                    pReplacementKeyRec = pReplacementKeyRec->m_pNext;
                }
            }
            // We didn't find the signature key in the list of allowed replacement keys for this record. 
            // However, we don't return here, because another record might have the same identity key 
            // and allow the signature key as a replacement.
        }

        pRevocationRec = pRevocationRec->m_pNext;
    }

    return !fRevoked; 
}

#endif // FEATURE_STRONGNAME_MIGRATION

// The common code used to verify a signature (taking into account whether skip
// verification is enabled for the given assembly).
HRESULT VerifySignature(__in SN_LOAD_CTX *pLoadCtx, DWORD dwInFlags, PublicKeyBlob *pRealEcmaPublicKey,__out_opt DWORD *pdwOutFlags)
{
    if (pdwOutFlags)
        *pdwOutFlags = 0;

    // Read the public key used to sign the assembly from the assembly metadata.
    // Also get the assembly name, we might need this if we fail the
    // verification and need to look up a verification disablement entry.
    WCHAR           wszSimpleAssemblyName[MAX_PATH_FNAME + 1];
    SString         strFullyQualifiedAssemblyName;
    BOOL            bSuccess = FALSE;
#if STRONGNAME_IN_VM
    BOOL            bAssemblyNameFormed = FALSE;
    BOOL            bVerificationBegun = FALSE;
#endif
    
    HandleKeyHolder             hKey(NULL);
    HandleHashHolder            hHash(NULL);
    HandleStrongNameCspHolder   hProv(NULL);

    StrongNameBufferHolder<PublicKeyBlob> pAssemblyIdentityKey;
    DWORD cbAssemblyIdentityKey;
    HRESULT hr = FindPublicKey(pLoadCtx,
                               wszSimpleAssemblyName,
                               sizeof(wszSimpleAssemblyName) / sizeof(WCHAR),
                               &pAssemblyIdentityKey,
                               &cbAssemblyIdentityKey);
    if (FAILED(hr))
        return hr;

    BOOL isEcmaKey = SN_IS_NEUTRAL_KEY(pAssemblyIdentityKey);
    // If we're handed the ECMA key, we translate it to the real key at this point.
    // Note: gcc gets confused with the complexity of StrongNameBufferHolder<> and 
    // won't auto-convert pAssemblyIdentityKey to type PublicKeyBlob*, so cast it explicitly.
    PublicKeyBlob *pRealPublicKey = isEcmaKey ? pRealEcmaPublicKey : static_cast<PublicKeyBlob*>(pAssemblyIdentityKey);

// An assembly can specify a signature public key in an attribute. 
// If one is present, we verify the signature using that public key.
#ifdef FEATURE_STRONGNAME_MIGRATION
    StrongNameBufferHolder<PublicKeyBlob> pAssemblySignaturePublicKey;
    DWORD cbAssemblySignaturePublicKey;
    IfFailRet(GetVerifiedSignatureKey(pLoadCtx, &pAssemblySignaturePublicKey, &cbAssemblySignaturePublicKey));
    if(hr != S_FALSE) // Attribute was found
    {
        pRealPublicKey = pAssemblySignaturePublicKey;
    }
    
#endif // FEATURE_STRONGNAME_MIGRATION

    DWORD dwSpecialKeys = GetSpecialKeyFlags(pRealPublicKey);

    // If this isn't the first time we've been called for this assembly and we
    // know it was fully signed and we're confident it couldn't have been
    // tampered with in the meantime, we can just skip the verification.
    if (!(dwInFlags & SN_INFLAG_FORCE_VER) &&
        !(dwInFlags & SN_INFLAG_INSTALL) &&
        (pLoadCtx->m_pCorHeader->Flags & VAL32(COMIMAGE_FLAGS_STRONGNAMESIGNED)) &&
        ((dwInFlags & SN_INFLAG_ADMIN_ACCESS) || g_fCacheVerify))
    {
        SNLOG((W("Skipping verification due to cached result\n")));
        DbgCount(dwInFlags & SN_INFLAG_RUNTIME ? W("RuntimeSkipCache") : W("FusionSkipCache"));
        return S_OK;
    }

#ifdef FEATURE_STRONGNAME_DELAY_SIGNING_ALLOWED
    // If we're not forcing verification, let's see if there's a skip
    // verification entry for this assembly. If there is we can skip all the
    // hard work and just lie about the strong name now. The exception is if the
    // assembly is marked as fully signed, in which case we have to force a
    // verification to see if they're telling the truth.
    StrongNameBufferHolder<PublicKeyBlob> pTestKey = NULL;
    SN_VER_REC *pVerRec = GetVerificationRecord(wszSimpleAssemblyName, pAssemblyIdentityKey);
    if (!(dwInFlags & SN_INFLAG_FORCE_VER) && !(pLoadCtx->m_pCorHeader->Flags & VAL32(COMIMAGE_FLAGS_STRONGNAMESIGNED)))
    {
        if (pVerRec != NULL)
        {
            if (pVerRec->m_wszTestPublicKey)
            {
                // substitute the public key with the test public key.
                pTestKey = GetPublicKeyFromHex(pVerRec->m_wszTestPublicKey);
                if (pTestKey != NULL)
                {

                    SNLOG((W("Using test public key for verification due to registry entry\n")));
                    DbgCount(dwInFlags & SN_INFLAG_RUNTIME ? W("RuntimeSkipDelay") : W("FusionSkipDelay"));

                    // If the assembly was not ECMA signed, then we need to update the key that it will be
                    // verified with as well.
                    if (!isEcmaKey)
                    {
                        // When test signing, there's no way to specify a hash algorithm. 
                        // So instead of defaulting to SHA1, we pick the algorithm the assembly 
                        // would've been signed with, if the test key wasn't present.
                        // Thus we use the same algorithm when verifying the signature.
                        SET_UNALIGNED_VAL32(&pTestKey->HashAlgID, GET_UNALIGNED_VAL32(&pRealPublicKey->HashAlgID));

                        pRealPublicKey = pTestKey;
                    }
                }
            }
            else
            {
                SNLOG((W("Skipping verification due to registry entry\n")));
                DbgCount(dwInFlags & SN_INFLAG_RUNTIME ? W("RuntimeSkipDelay") : W("FusionSkipDelay"));
                if (pdwOutFlags)
                {
                    *pdwOutFlags |= dwSpecialKeys;
                }
                return S_OK;
            }
        }
    }
#endif // FEATURE_STRONGNAME_DELAY_SIGNING_ALLOWED

#ifdef FEATURE_STRONGNAME_MIGRATION
    if(!isEcmaKey) // We should never revoke the ecma key, as it is tied strongly to the runtime
    {
        if(!AreKeysAllowedByRevocationList((BYTE*)pAssemblyIdentityKey.GetValue(), cbAssemblyIdentityKey, (BYTE*)pAssemblySignaturePublicKey.GetValue(), cbAssemblySignaturePublicKey))
        {
            if(pAssemblySignaturePublicKey == NULL)
            {
                SNLOG((W("Verification failed. Assembly public key has been revoked\n")));
            }
            else
            {
                SNLOG((W("Verification failed. Assembly identity key has been revoked, an the assembly signature key isn't in the replacement key list\n")));
            }

            hr = CORSEC_E_INVALID_STRONGNAME;
            goto Error;
        }
    }

#endif // FEATURE_STRONGNAME_MIGRATION

#ifdef FEATURE_CORECLR
    // TritonTODO: check with security team on this
    if (pLoadCtx->m_pbSignature == NULL)
    {
        hr = CORSEC_E_MISSING_STRONGNAME;
        goto Error;
    }
#endif //FEATURE_CORECLR

#if STRONGNAME_IN_VM
    bVerificationBegun = TRUE;
    // SN verification start event
    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_SECURITY_KEYWORD))
    {
        // form the fully qualified assembly name using the load context
        bAssemblyNameFormed = FormFullyQualifiedAssemblyName(pLoadCtx, strFullyQualifiedAssemblyName);
        if(bAssemblyNameFormed)
        {
            ETW::SecurityLog::StrongNameVerificationStart(dwInFlags,(LPWSTR)strFullyQualifiedAssemblyName.GetUnicode());
        }
    }
#endif // STRONGNAME_IN_VM

    ALG_ID uHashAlgId = GET_UNALIGNED_VAL32(&pRealPublicKey->HashAlgID);
    ALG_ID uSignAlgId = GET_UNALIGNED_VAL32(&pRealPublicKey->SigAlgID);

    // Default hashing and signing algorithm IDs if necessary.
    if (uHashAlgId == 0)
        uHashAlgId = CALG_SHA1;
    if (uSignAlgId == 0)
        uSignAlgId = CALG_RSA_SIGN;

    // Find a CSP supporting the required algorithms.
    hProv = LocateCSP(NULL, SN_IGNORE_CONTAINER, uHashAlgId, uSignAlgId);
    if (!hProv)
    {
        hr = HRESULT_FROM_GetLastError();
        SNLOG((W("Failed to acquire a CSP: %08x"), hr));
        goto Error;
    }

    BYTE *pbRealPublicKey;
    pbRealPublicKey = pRealPublicKey->PublicKey;
    DWORD cbRealPublicKey;
    cbRealPublicKey = GET_UNALIGNED_VAL32(&pRealPublicKey->cbPublicKey);
    
    if (!CryptImportKey(hProv, pbRealPublicKey, cbRealPublicKey, 0, 0, &hKey))
    {
        hr = HRESULT_FROM_GetLastError();
        SNLOG((W("Failed to import key: %08x"), hr));
        goto Error;
    }

    // Create a hash object.
    
    if (!CryptCreateHash(hProv, uHashAlgId, 0, 0, &hHash))
    {
        hr = HRESULT_FROM_GetLastError();
        SNLOG((W("Failed to create hash: %08x"), hr));
        goto Error;
    }

    // Compute a hash over the image.
    if (!ComputeHash(pLoadCtx, hHash, CalcHash, NULL))
    {
        hr = HRESULT_FROM_GetLastError();
        SNLOG((W("Failed to compute hash: %08x"), hr));
        goto Error;
    }

    // Verify the hash against the signature.
    DbgCount(dwInFlags & SN_INFLAG_RUNTIME ? W("RuntimeVerify") : W("FusionVerify"));
    if (pLoadCtx->m_pbSignature != NULL && pLoadCtx->m_cbSignature != 0 && 
        CryptVerifySignatureW(hHash, pLoadCtx->m_pbSignature, pLoadCtx->m_cbSignature, hKey, NULL, 0))
    {
        SNLOG((W("Verification succeeded (for real)\n")));
        if (pdwOutFlags)
        {
            *pdwOutFlags |= dwSpecialKeys | SN_OUTFLAG_WAS_VERIFIED;
        }
        bSuccess = TRUE;
    }
    else
    {
        SNLOG((W("Verification failed\n")));
        hr = CORSEC_E_INVALID_STRONGNAME;
    }

Error:

#if STRONGNAME_IN_VM
    // SN verification end event
    if(bVerificationBegun &&
        ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, TRACE_LEVEL_VERBOSE, CLR_SECURITY_KEYWORD))
    {
        // form the fully qualified assembly name using the load context if it has not yet been formed
        if(!bAssemblyNameFormed)
        {
            strFullyQualifiedAssemblyName.Clear();
            bAssemblyNameFormed = FormFullyQualifiedAssemblyName(pLoadCtx, strFullyQualifiedAssemblyName);
        }
        if(bAssemblyNameFormed)
        {
            ETW::SecurityLog::StrongNameVerificationStop(dwInFlags,(ULONG)hr, (LPWSTR)strFullyQualifiedAssemblyName.GetUnicode());
        }  
    }
#endif // STRONGNAME_IN_VM
 
    if (bSuccess)
        return S_OK;
    else
        return hr;
}

// Compute a hash over the elements of an assembly manifest file that should
// remain static (skip checksum, Authenticode signatures and strong name
// signature blob).
// This function can also be used to get the blob of bytes that would be 
// hashed without actually hashing.
BOOLEAN ComputeHash(SN_LOAD_CTX *pLoadCtx, HCRYPTHASH hHash, HashFunc func, void* cookie)
{
    union {
        IMAGE_NT_HEADERS32  m_32;
        IMAGE_NT_HEADERS64  m_64;
    }                       sHeaders;
    IMAGE_SECTION_HEADER   *pSections;
    ULONG                   i;
    BYTE                   *pbSig = pLoadCtx->m_pbSignature;
    DWORD                   cbSig = pLoadCtx->m_cbSignature;

#define LIMIT_CHECK(_start, _length, _fileStart, _fileLength)               \
        do { if (((_start) < (_fileStart))              ||                  \
                 (((_start)+(_length)) < (_start))      ||                  \
                 (((_start)+(_length)) < (_fileStart))  ||                  \
                 (((_start)+(_length)) > ((_fileStart)+(_fileLength))) )    \
        { SetLastError(CORSEC_E_INVALID_IMAGE_FORMAT); return FALSE; }  } while (false)
        
#define FILE_LIMIT_CHECK(_start, _length) LIMIT_CHECK(_start, _length, pLoadCtx->m_pbBase, pLoadCtx->m_dwLength)
        
#define SN_HASH(_start, _length) do { if (!func(hHash, (_start), (_length), 0, cookie)) return FALSE; } while (false)
    
#define SN_CHECK_AND_HASH(_start, _length) do { FILE_LIMIT_CHECK(_start, _length); SN_HASH(_start, _length); } while (false)

    // Make sure the file size doesn't wrap around.
    if (pLoadCtx->m_pbBase + pLoadCtx->m_dwLength <= pLoadCtx->m_pbBase)
    {
        SetLastError(CORSEC_E_INVALID_IMAGE_FORMAT);
        return FALSE;
    }

    // Make sure the signature is completely contained within the file.
    FILE_LIMIT_CHECK(pbSig, cbSig);

    // Hash the DOS header if it exists.
    if ((BYTE*)pLoadCtx->m_pNtHeaders != pLoadCtx->m_pbBase)
        SN_CHECK_AND_HASH(pLoadCtx->m_pbBase, (DWORD)((BYTE*)pLoadCtx->m_pNtHeaders - pLoadCtx->m_pbBase));

    // Add image headers minus the checksum and security data directory.
    if (pLoadCtx->m_pNtHeaders->OptionalHeader.Magic == VAL16(IMAGE_NT_OPTIONAL_HDR32_MAGIC)) {
        sHeaders.m_32 = *((IMAGE_NT_HEADERS32*)pLoadCtx->m_pNtHeaders);
        sHeaders.m_32.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_SECURITY].VirtualAddress = 0;
        sHeaders.m_32.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_SECURITY].Size = 0;
        sHeaders.m_32.OptionalHeader.CheckSum = 0;
        SN_HASH((BYTE*)&sHeaders.m_32, sizeof(sHeaders.m_32));
    } else if (pLoadCtx->m_pNtHeaders->OptionalHeader.Magic == VAL16(IMAGE_NT_OPTIONAL_HDR64_MAGIC)) {
        sHeaders.m_64 = *((IMAGE_NT_HEADERS64*)pLoadCtx->m_pNtHeaders);
        sHeaders.m_64.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_SECURITY].VirtualAddress = 0;
        sHeaders.m_64.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_SECURITY].Size = 0;
        sHeaders.m_64.OptionalHeader.CheckSum = 0;
        SN_HASH((BYTE*)&sHeaders.m_64, sizeof(sHeaders.m_64));
    } else {
        SetLastError(CORSEC_E_INVALID_IMAGE_FORMAT);
        return FALSE;
    }

    // Then the section headers.
    pSections = IMAGE_FIRST_SECTION(pLoadCtx->m_pNtHeaders);
    SN_CHECK_AND_HASH((BYTE*)pSections, VAL16(pLoadCtx->m_pNtHeaders->FileHeader.NumberOfSections) * sizeof(IMAGE_SECTION_HEADER));

    // Finally, add data from each section.
    for (i = 0; i < VAL16(pLoadCtx->m_pNtHeaders->FileHeader.NumberOfSections); i++) {
        BYTE   *pbData = pLoadCtx->m_pbBase + VAL32(pSections[i].PointerToRawData);
        DWORD   cbData = VAL32(pSections[i].SizeOfRawData);

        // We need to exclude the strong name signature blob from the hash. The
        // blob could intersect the section in a number of ways.

        if ((pbSig + cbSig) <= pbData || pbSig >= (pbData + cbData))
            // No intersection at all. Hash all data.
            SN_CHECK_AND_HASH(pbData, cbData);
        else if (pbSig == pbData && cbSig == cbData)
            // Signature consumes entire block. Hash no data.
            ;
        else if (pbSig == pbData)
            // Signature at start. Hash end.
            SN_CHECK_AND_HASH(pbData + cbSig, cbData - cbSig);
        else if ((pbSig + cbSig) == (pbData + cbData))
            // Signature at end. Hash start.
            SN_CHECK_AND_HASH(pbData, cbData - cbSig);
        else {
            // Signature in the middle. Hash head and tail.
            SN_CHECK_AND_HASH(pbData, (DWORD)(pbSig - pbData));
            SN_CHECK_AND_HASH(pbSig + cbSig, cbData - (DWORD)(pbSig + cbSig - pbData));
        }
    }

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
    if (hHash != (HCRYPTHASH)INVALID_HANDLE_VALUE) {
        DWORD   cbHash;
        DWORD   dwRetLen = sizeof(cbHash);
        if (CryptGetHashParam(hHash, HP_HASHSIZE, (BYTE*)&cbHash, &dwRetLen, 0))
        {
            NewArrayHolder<BYTE> pbHash(new (nothrow) BYTE[cbHash]);
            if (pbHash != NULL)
            {
                if (CryptGetHashParam(hHash, HP_HASHVAL, pbHash, &cbHash, 0))
                {
                    SNLOG((W("Computed Hash Value (%u bytes):\n"), cbHash));
                    HexDump(pbHash, cbHash);
                }
                else
                {
                    SNLOG((W("CryptGetHashParam() failed with %08X\n"), GetLastError()));
                }
            }
        }
        else
        {
            SNLOG((W("CryptGetHashParam() failed with %08X\n"), GetLastError()));
        }
    }
#endif // _DEBUG

    return TRUE;

#undef SN_CHECK_AND_HASH    
#undef SN_HASH
#undef FILE_LIMIT_CHECK
#undef LIMIT_CHECK
}


SNAPI_(DWORD) GetHashFromAssemblyFile(LPCSTR szFilePath, // [IN] location of file to be hashed
                                      unsigned int *piHashAlg, // [IN/OUT] constant specifying the hash algorithm (set to 0 if you want the default)
                                      BYTE   *pbHash,    // [OUT] hash buffer
                                      DWORD  cchHash,    // [IN]  max size of buffer
                                      DWORD  *pchHash)   // [OUT] length of hash byte array
{
    BOOL retVal = FALSE;

    BEGIN_ENTRYPOINT_NOTHROW;
    // Convert filename to wide characters and call the W version of this
    // function.

    MAKE_WIDEPTR_FROMANSI(wszFilePath, szFilePath);
    retVal = GetHashFromAssemblyFileW(wszFilePath, piHashAlg, pbHash, cchHash, pchHash);
    END_ENTRYPOINT_NOTHROW;
    return retVal;
}

SNAPI_(DWORD) GetHashFromAssemblyFileW(LPCWSTR wszFilePath, // [IN] location of file to be hashed
                                       unsigned int *piHashAlg, // [IN/OUT] constant specifying the hash algorithm (set to 0 if you want the default)
                                       BYTE   *pbHash,    // [OUT] hash buffer
                                       DWORD  cchHash,    // [IN]  max size of buffer
                                       DWORD  *pchHash)   // [OUT] length of hash byte array
{
    HRESULT         hr;

    BEGIN_ENTRYPOINT_NOTHROW;

    SN_LOAD_CTX     sLoadCtx;
    BYTE           *pbMetaData = NULL;
    DWORD           cbMetaData;

    sLoadCtx.m_fReadOnly = TRUE;
    if (!LoadAssembly(&sLoadCtx, wszFilePath, 0, FALSE))
        IfFailGo(HRESULT_FROM_GetLastError());

    if (sLoadCtx.m_pedecoder->CheckCorHeader())
    {
        pbMetaData = (BYTE *)sLoadCtx.m_pedecoder->GetMetadata();
    }
    if (pbMetaData == NULL) {
        UnloadAssembly(&sLoadCtx);
        IfFailGo(E_INVALIDARG);
    }
    cbMetaData = VAL32(sLoadCtx.m_pCorHeader->MetaData.Size);

    hr = GetHashFromBlob(pbMetaData, cbMetaData, piHashAlg, pbHash, cchHash, pchHash);

    UnloadAssembly(&sLoadCtx);
ErrExit:
    
    END_ENTRYPOINT_NOTHROW;
    return hr;
}
    
SNAPI_(DWORD) GetHashFromFile(LPCSTR szFilePath, // [IN] location of file to be hashed
                              unsigned int *piHashAlg, // [IN/OUT] constant specifying the hash algorithm (set to 0 if you want the default)
                              BYTE   *pbHash,    // [OUT] hash buffer
                              DWORD  cchHash,    // [IN]  max size of buffer
                              DWORD  *pchHash)   // [OUT] length of hash byte array
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;
    
    HANDLE hFile = CreateFileA(szFilePath,
                               GENERIC_READ,
                               FILE_SHARE_READ,
                               NULL,
                               OPEN_EXISTING,
                               FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN,
                               NULL);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        hr = HRESULT_FROM_GetLastError();
    }
    else
    {
        hr = GetHashFromHandle(hFile, piHashAlg, pbHash, cchHash, pchHash);
        CloseHandle(hFile);
    }
    END_ENTRYPOINT_NOTHROW;
    return hr;
}

SNAPI_(DWORD) GetHashFromFileW(LPCWSTR wszFilePath, // [IN] location of file to be hashed
                               unsigned int *piHashAlg, // [IN/OUT] constant specifying the hash algorithm (set to 0 if you want the default)
                               BYTE   *pbHash,    // [OUT] hash buffer
                               DWORD  cchHash,    // [IN]  max size of buffer
                               DWORD  *pchHash)   // [OUT] length of hash byte array
{
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    HANDLE hFile = WszCreateFile(wszFilePath,
                                 GENERIC_READ,
                                 FILE_SHARE_READ,
                                 NULL,
                                 OPEN_EXISTING,
                                 FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN,
                                 NULL);
    if (hFile == INVALID_HANDLE_VALUE)
        IfFailGo(HRESULT_FROM_GetLastError());

    hr = GetHashFromHandle(hFile, piHashAlg, pbHash, cchHash, pchHash);
    CloseHandle(hFile);
ErrExit:
    
    END_ENTRYPOINT_NOTHROW;
    return hr;
}

SNAPI_(DWORD) GetHashFromHandle(HANDLE hFile,      // [IN] handle of file to be hashed
                                unsigned int *piHashAlg, // [IN/OUT] constant specifying the hash algorithm (set to 0 if you want the default)
                                BYTE   *pbHash,    // [OUT] hash buffer
                                DWORD  cchHash,    // [IN]  max size of buffer
                                DWORD  *pchHash)   // [OUT] length of hash byte array
{
    HRESULT hr;

    BEGIN_ENTRYPOINT_NOTHROW;

    PBYTE pbBuffer = NULL;
    DWORD dwFileLen = SafeGetFileSize(hFile, 0);
    if (dwFileLen == 0xffffffff)
        IfFailGo(HRESULT_FROM_GetLastError());

    if (SetFilePointer(hFile, 0, NULL, FILE_BEGIN) == 0xFFFFFFFF)
        IfFailGo(HRESULT_FROM_GetLastError());

    DWORD dwResultLen;
    pbBuffer = new (nothrow) BYTE[dwFileLen];
    IfNullGo(pbBuffer);

    if (ReadFile(hFile, pbBuffer, dwFileLen, &dwResultLen, NULL))
        hr = GetHashFromBlob(pbBuffer, dwResultLen, piHashAlg, pbHash, cchHash, pchHash);
    else
        hr = HRESULT_FROM_GetLastError();

    delete[] pbBuffer;

ErrExit:
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

SNAPI_(DWORD) GetHashFromBlob(BYTE   *pbBlob,       // [IN] pointer to memory block to hash
                              DWORD  cchBlob,       // [IN] length of blob
                              unsigned int *piHashAlg,  // [IN/OUT] constant specifying the hash algorithm (set to 0 if you want the default)
                              BYTE   *pbHash,       // [OUT] hash buffer
                              DWORD  cchHash,       // [IN]  max size of buffer
                              DWORD  *pchHash)      // [OUT] length of hash byte array
{
    HRESULT    hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    HandleStrongNameCspHolder   hProv(NULL);
    CapiHashHolder              hHash(NULL);

    if (!piHashAlg || !pbHash || !pchHash)
        IfFailGo(E_INVALIDARG);

    if (!(*piHashAlg))
        *piHashAlg = CALG_SHA1;

    *pchHash = cchHash;

    hProv = LocateCSP(NULL, SN_IGNORE_CONTAINER, *piHashAlg);

    if (!hProv ||
        (!CryptCreateHash(hProv, *piHashAlg, 0, 0, &hHash)) ||
        (!CryptHashData(hHash, pbBlob, cchBlob, 0)) ||
        (!CryptGetHashParam(hHash, HP_HASHVAL, pbHash, pchHash, 0)))
        hr = HRESULT_FROM_GetLastError();

ErrExit:
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

#endif // #ifndef DACCESS_COMPILE

#else // !defined(FEATURE_CORECLR)

#define InitStrongName() S_OK

#endif // !defined(FEATURE_CORECLR)


// Free buffer allocated by routines below.
SNAPI_(VOID) StrongNameFreeBuffer(BYTE *pbMemory)            // [in] address of memory to free
{
    BEGIN_ENTRYPOINT_VOIDRET;

    SNLOG((W("StrongNameFreeBuffer(%08X)\n"), pbMemory));

    if (pbMemory != (BYTE*)SN_THE_KEY() && pbMemory != g_rbNeutralPublicKey)
        delete [] pbMemory;
    END_ENTRYPOINT_VOIDRET;

}

#ifndef DACCESS_COMPILE
// Retrieve per-thread context, lazily allocating it if necessary.
SN_THREAD_CTX *GetThreadContext()
{
    SN_THREAD_CTX *pThreadCtx = (SN_THREAD_CTX*)ClrFlsGetValue(TlsIdx_StrongName);
    if (pThreadCtx == NULL) {
        pThreadCtx = new (nothrow) SN_THREAD_CTX;
        if (pThreadCtx == NULL)
            return NULL;
        pThreadCtx->m_dwLastError = S_OK;
#if !defined(FEATURE_CORECLR)
        for (ULONG i = 0; i < CachedCspCount; i++)
        {
            pThreadCtx->m_hProv[i] = NULL;
        }
#endif // !FEATURE_CORECLR

        EX_TRY {
            ClrFlsSetValue(TlsIdx_StrongName, pThreadCtx);
        }
        EX_CATCH {
            delete pThreadCtx;
            pThreadCtx = NULL;
        }
        EX_END_CATCH (SwallowAllExceptions);
    }
    return pThreadCtx;
}

// Set the per-thread last error code.
VOID SetStrongNameErrorInfo(DWORD dwStatus)
{
    SN_THREAD_CTX *pThreadCtx = GetThreadContext();
    if (pThreadCtx == NULL)
        // We'll return E_OUTOFMEMORY when we attempt to get the error.
        return;
    pThreadCtx->m_dwLastError = dwStatus;
}

#endif // !DACCESS_COMPILE

// Return last error.
SNAPI_(DWORD) StrongNameErrorInfo(VOID)
{
    HRESULT hr = E_FAIL;
    
    BEGIN_ENTRYPOINT_NOTHROW;

#ifndef DACCESS_COMPILE
    SN_THREAD_CTX *pThreadCtx = GetThreadContext();
    if (pThreadCtx == NULL)
        hr = E_OUTOFMEMORY;
    else
        hr = pThreadCtx->m_dwLastError;
#else
    hr = E_FAIL;
#endif // #ifndef DACCESS_COMPILE
    END_ENTRYPOINT_NOTHROW;

    return hr;
}


// Create a strong name token from a public key blob.
SNAPI StrongNameTokenFromPublicKey(BYTE    *pbPublicKeyBlob,        // [in] public key blob
                                   ULONG    cbPublicKeyBlob,
                                   BYTE   **ppbStrongNameToken,     // [out] strong name token 
                                   ULONG   *pcbStrongNameToken)
{
    BOOLEAN         retVal = FALSE;

    BEGIN_ENTRYPOINT_VOIDRET;

#ifndef DACCESS_COMPILE

#ifndef FEATURE_CORECLR
    HCRYPTPROV      hProv = NULL;
    HCRYPTHASH      hHash = NULL;
    HCRYPTKEY       hKey  = NULL;
    DWORD           dwHashLen;
    DWORD           dwRetLen;
    NewArrayHolder<BYTE> pHash(NULL);
#else // !FEATURE_CORECLR
    SHA1Hash        sha1;
    BYTE            *pHash = NULL;
#endif // !FEATURE_CORECLR

    DWORD           i;
    DWORD           cbKeyBlob;
    PublicKeyBlob   *pPublicKey = NULL;
    DWORD dwHashLenMinusTokenSize = 0;

    SNLOG((W("StrongNameTokenFromPublicKey(%08X, %08X, %08X, %08X)\n"), pbPublicKeyBlob, cbPublicKeyBlob, ppbStrongNameToken, pcbStrongNameToken));

#if STRONGNAME_IN_VM
    FireEtwSecurityCatchCall_V1(GetClrInstanceId());
#endif // STRONGNAME_IN_VM

    SN_COMMON_PROLOG();

    if (pbPublicKeyBlob == NULL)
        SN_ERROR(E_POINTER);
    if (!StrongNameIsValidPublicKey(pbPublicKeyBlob, cbPublicKeyBlob, false))
        SN_ERROR(CORSEC_E_INVALID_PUBLICKEY);
    if (ppbStrongNameToken == NULL)
        SN_ERROR(E_POINTER);
    if (pcbStrongNameToken == NULL)
        SN_ERROR(E_POINTER);

    // Allocate a buffer for the output token.
    *ppbStrongNameToken = new (nothrow) BYTE[SN_SIZEOF_TOKEN];
    if (*ppbStrongNameToken == NULL) {
        SetStrongNameErrorInfo(E_OUTOFMEMORY);
        goto Exit;
    }
    *pcbStrongNameToken = SN_SIZEOF_TOKEN;

    // We cache a couple of common cases.
    if (SN_IS_NEUTRAL_KEY(pbPublicKeyBlob)) {
        memcpy_s(*ppbStrongNameToken, *pcbStrongNameToken, g_rbNeutralPublicKeyToken, SN_SIZEOF_TOKEN);
        retVal = TRUE;
        goto Exit;
    }
    if (cbPublicKeyBlob == SN_SIZEOF_THE_KEY() &&
        memcmp(pbPublicKeyBlob, SN_THE_KEY(), cbPublicKeyBlob) == 0) {
        memcpy_s(*ppbStrongNameToken, *pcbStrongNameToken, SN_THE_KEYTOKEN(), SN_SIZEOF_TOKEN);
        retVal = TRUE;
        goto Exit;
    }
#ifdef FEATURE_CORECLR
    if (SN_IS_THE_SILVERLIGHT_PLATFORM_KEY(pbPublicKeyBlob))
    {
        memcpy_s(*ppbStrongNameToken, *pcbStrongNameToken, SN_THE_SILVERLIGHT_PLATFORM_KEYTOKEN(), SN_SIZEOF_TOKEN);
        retVal = TRUE;
        goto Exit;
    }

    if (SN_IS_THE_SILVERLIGHT_KEY(pbPublicKeyBlob))
    {
        memcpy_s(*ppbStrongNameToken, *pcbStrongNameToken, SN_THE_SILVERLIGHT_KEYTOKEN(), SN_SIZEOF_TOKEN);
        retVal = TRUE;
        goto Exit;
    }

#ifdef FEATURE_WINDOWSPHONE

    if (SN_IS_THE_MICROSOFT_PHONE_KEY(pbPublicKeyBlob))
    {
        memcpy_s(*ppbStrongNameToken, *pcbStrongNameToken, SN_THE_MICROSOFT_PHONE_KEYTOKEN(), SN_SIZEOF_TOKEN);
        retVal = TRUE;
        goto Exit;
    }

    if (SN_IS_THE_MICROSOFT_XNA_KEY(pbPublicKeyBlob))
    {
        memcpy_s(*ppbStrongNameToken, *pcbStrongNameToken, SN_THE_MICROSOFT_XNA_KEYTOKEN(), SN_SIZEOF_TOKEN);
        retVal = TRUE;
        goto Exit;
    }

#endif //FEATURE_WINDOWSPHONE
#endif //FEATURE_CORECLR

    // To compute the correct public key token, we need to make sure the public key blob
    // was not padded with extra bytes that CAPI CryptImportKey would've ignored.
    // Without this round trip, we would blindly compute the hash over the padded bytes
    // which could make finding a public key token collision a significantly easier task
    // since an attacker wouldn't need to work hard on generating valid key pairs before hashing.
    if (cbPublicKeyBlob <= sizeof(PublicKeyBlob)) {
        SetLastError(CORSEC_E_INVALID_PUBLICKEY);
        goto Error;
    }

    // Check that the blob type is PUBLICKEYBLOB.
    pPublicKey = (PublicKeyBlob*) pbPublicKeyBlob;

    if (pPublicKey->PublicKey + GET_UNALIGNED_VAL32(&pPublicKey->cbPublicKey) < pPublicKey->PublicKey) {
        SetLastError(CORSEC_E_INVALID_PUBLICKEY);
        goto Error;
    }

    if (cbPublicKeyBlob < SN_SIZEOF_KEY(pPublicKey)) {
        SetLastError(CORSEC_E_INVALID_PUBLICKEY);
        goto Error;
    }

    if (*(BYTE*) pPublicKey->PublicKey /* PUBLICKEYSTRUC->bType */ != PUBLICKEYBLOB) {
        SetLastError(CORSEC_E_INVALID_PUBLICKEY);
        goto Error;
    }

#ifndef FEATURE_CORECLR

    // Look for a CSP to hash the public key.
    hProv = LocateCSP(NULL, SN_HASH_SHA1_ONLY);
    if (!hProv)
        goto Error;

    if (!CryptImportKey(hProv, 
                           pPublicKey->PublicKey, 
                           GET_UNALIGNED_VAL32(&pPublicKey->cbPublicKey),
                           0, 
                           0, 
                           &hKey))
        goto Error;

    cbKeyBlob = sizeof(DWORD);
    if (!CryptExportKey(hKey, 0, PUBLICKEYBLOB, 0, NULL, &cbKeyBlob))
        goto Error;

    if ((offsetof(PublicKeyBlob, PublicKey) + cbKeyBlob) != cbPublicKeyBlob) {
        SetLastError(CORSEC_E_INVALID_PUBLICKEY);
        goto Error;
    }

    // Create a hash object.
    if (!CryptCreateHash(hProv, CALG_SHA1, 0, 0, &hHash))
        goto Error;

    // Compute a hash over the public key.
    if (!CryptHashData(hHash, pbPublicKeyBlob, cbPublicKeyBlob, 0))
        goto Error;

    // Get the length of the hash.
    dwRetLen = sizeof(dwHashLen);
    if (!CryptGetHashParam(hHash, HP_HASHSIZE, (BYTE*)&dwHashLen, &dwRetLen, 0))
        goto Error;

    // Allocate a temporary block to hold the hash.
    pHash = new (nothrow) BYTE[dwHashLen];
    if (pHash == NULL)
    {
        SetLastError(E_OUTOFMEMORY);
        goto Error;
    }

    // Read the hash value.
    if (!CryptGetHashParam(hHash, HP_HASHVAL, pHash, &dwHashLen, 0))
        goto Error;

    // We no longer need the hash object or the provider.
    CryptDestroyHash(hHash);
    CryptDestroyKey(hKey);
    FreeCSP(hProv);

    // Take the last few bytes of the hash value for our token. (These are the
    // low order bytes from a network byte order point of view). Reverse the
    // order of these bytes in the output buffer to get host byte order.
    _ASSERTE(dwHashLen >= SN_SIZEOF_TOKEN);
    if (!ClrSafeInt<DWORD>::subtraction(dwHashLen, SN_SIZEOF_TOKEN, dwHashLenMinusTokenSize))
    {
        SetLastError(COR_E_OVERFLOW);
        goto Error;
    }

#else // !FEATURE_CORECLR

    // Compute a hash over the public key.
    sha1.AddData(pbPublicKeyBlob, cbPublicKeyBlob);
    pHash = sha1.GetHash();
    static_assert(SHA1_HASH_SIZE >= SN_SIZEOF_TOKEN, "SN_SIZEOF_TOKEN must be smaller or equal to the SHA1_HASH_SIZE");
    dwHashLenMinusTokenSize = SHA1_HASH_SIZE - SN_SIZEOF_TOKEN;
    
#endif // !FEATURE_CORECLR

    // Take the last few bytes of the hash value for our token. (These are the
    // low order bytes from a network byte order point of view). Reverse the
    // order of these bytes in the output buffer to get host byte order.
    for (i = 0; i < SN_SIZEOF_TOKEN; i++)
        (*ppbStrongNameToken)[SN_SIZEOF_TOKEN - (i + 1)] = pHash[i + dwHashLenMinusTokenSize];

    retVal = TRUE;
    goto Exit;

 Error:
    SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());
#ifndef FEATURE_CORECLR
    if (hHash)
        CryptDestroyHash(hHash);
    if (hKey)
        CryptDestroyKey(hKey);
    if (hProv)
        FreeCSP(hProv);
#endif // !FEATURE_CORECLR

    if (*ppbStrongNameToken) {
        delete [] *ppbStrongNameToken;
        *ppbStrongNameToken = NULL;
    }
Exit:
#else
    DacNotImpl();
#endif // #ifndef DACCESS_COMPILE
    END_ENTRYPOINT_VOIDRET;

    return retVal;

}
