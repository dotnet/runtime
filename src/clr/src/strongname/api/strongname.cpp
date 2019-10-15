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

// We cache a couple of things on a per thread basis: the last error encountered
// and (potentially) CSP contexts. The following structure tracks these and is
// allocated lazily as needed.
struct SN_THREAD_CTX {
    DWORD       m_dwLastError;
};

#endif // !DACCESS_COMPILE

// Macro containing common code used at the start of most APIs.
#define SN_COMMON_PROLOG() do {                             \
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


// Silverlight platform key
#define SN_THE_SILVERLIGHT_PLATFORM_KEYTOKEN() ((PublicKeyBlob*)g_rbTheSilverlightPlatformKeyToken)
#define SN_IS_THE_SILVERLIGHT_PLATFORM_KEY(_pk) (SN_SIZEOF_KEY((PublicKeyBlob*)(_pk)) == sizeof(g_rbTheSilverlightPlatformKey) && \
                                memcmp((_pk), g_rbTheSilverlightPlatformKey, sizeof(g_rbTheSilverlightPlatformKey)) == 0)

// Silverlight key
#define SN_IS_THE_SILVERLIGHT_KEY(_pk) (SN_SIZEOF_KEY((PublicKeyBlob*)(_pk)) == sizeof(g_rbTheSilverlightKey) && \
                                memcmp((_pk), g_rbTheSilverlightKey, sizeof(g_rbTheSilverlightKey)) == 0)

#define SN_THE_SILVERLIGHT_KEYTOKEN() ((PublicKeyBlob*)g_rbTheSilverlightKeyToken)


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

    SHA1Hash        sha1;
    BYTE            *pHash = NULL;
    DWORD           i;
    PublicKeyBlob   *pPublicKey = NULL;
    DWORD dwHashLenMinusTokenSize = 0;

    SNLOG((W("StrongNameTokenFromPublicKey(%08X, %08X, %08X, %08X)\n"), pbPublicKeyBlob, cbPublicKeyBlob, ppbStrongNameToken, pcbStrongNameToken));

#if STRONGNAME_IN_VM
    FireEtwSecurityCatchCall_V1(GetClrInstanceId());
#endif // STRONGNAME_IN_VM

    SN_COMMON_PROLOG();

    if (pbPublicKeyBlob == NULL)
        SN_ERROR(E_POINTER);
    if (!StrongNameIsValidPublicKey(pbPublicKeyBlob, cbPublicKeyBlob))
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

    // Compute a hash over the public key.
    sha1.AddData(pbPublicKeyBlob, cbPublicKeyBlob);
    pHash = sha1.GetHash();
    static_assert(SHA1_HASH_SIZE >= SN_SIZEOF_TOKEN, "SN_SIZEOF_TOKEN must be smaller or equal to the SHA1_HASH_SIZE");
    dwHashLenMinusTokenSize = SHA1_HASH_SIZE - SN_SIZEOF_TOKEN;

    // Take the last few bytes of the hash value for our token. (These are the
    // low order bytes from a network byte order point of view). Reverse the
    // order of these bytes in the output buffer to get host byte order.
    for (i = 0; i < SN_SIZEOF_TOKEN; i++)
        (*ppbStrongNameToken)[SN_SIZEOF_TOKEN - (i + 1)] = pHash[i + dwHashLenMinusTokenSize];

    retVal = TRUE;
    goto Exit;

 Error:
    SetStrongNameErrorInfo(HRESULT_FROM_GetLastError());

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
