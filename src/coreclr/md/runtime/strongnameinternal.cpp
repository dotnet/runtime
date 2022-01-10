// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Strong name APIs which are not exposed publicly but are used by CLR code
//

#include "stdafx.h"
#include "strongnameinternal.h"
#include "thekey.h"
#include "ecmakey.h"
#include "sha1.h"

//---------------------------------------------------------------------------------------
//
// Check to see if a public key blob is the ECMA public key blob
//
// Arguments:
//   pbKey - public key blob to check
//   cbKey - size in bytes of pbKey
//

bool StrongNameIsEcmaKey(_In_reads_(cbKey) const BYTE *pbKey, DWORD cbKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // The key should be the same size as the ECMA key
    if (cbKey != sizeof(g_rbNeutralPublicKey))
    {
        return false;
    }

    const PublicKeyBlob *pKeyBlob = reinterpret_cast<const PublicKeyBlob *>(pbKey);
    return StrongNameIsEcmaKey(*pKeyBlob);
}

//---------------------------------------------------------------------------------------
//
// Check to see if a public key blob is the ECMA public key blob
//
// Arguments:
//   keyPublicKey - Key to check to see if it matches the ECMA key
//

bool StrongNameIsEcmaKey(const PublicKeyBlob &keyPublicKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return StrongNameSizeOfPublicKey(keyPublicKey) == sizeof(g_rbNeutralPublicKey) &&
           memcmp(reinterpret_cast<const BYTE *>(&keyPublicKey), g_rbNeutralPublicKey, sizeof(g_rbNeutralPublicKey)) == 0;
}

//---------------------------------------------------------------------------------------
//
// Verify that a public key blob looks like a reasonable public key
//
// Arguments:
//   pbBuffer     - buffer to verify the format of
//   cbBuffer     - size of pbBuffer
//

bool StrongNameIsValidPublicKey(_In_reads_(cbBuffer) const BYTE *pbBuffer, DWORD cbBuffer)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(pbBuffer));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // The buffer must be at least as large as the public key structure
    if (cbBuffer < sizeof(PublicKeyBlob))
    {
        return false;
    }

    // The buffer must be the same size as the structure header plus the trailing key data
    const PublicKeyBlob *pkeyPublicKey = reinterpret_cast<const PublicKeyBlob *>(pbBuffer);
    if (GET_UNALIGNED_VAL32(&pkeyPublicKey->cbPublicKey) != cbBuffer - offsetof(PublicKeyBlob, PublicKey))
    {
        return false;
    }

    // The buffer itself looks reasonable, but the public key structure needs to be validated as well
    return StrongNameIsValidPublicKey(*pkeyPublicKey);
}

//---------------------------------------------------------------------------------------
//
// Verify that a public key blob looks like a reasonable public key.
//
// Arguments:
//   keyPublicKey - key blob to verify
//
// Notes:
//    This can be a very expensive operation, since it involves importing keys.
//

bool StrongNameIsValidPublicKey(const PublicKeyBlob &keyPublicKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // The ECMA key doesn't look like a valid key so it will fail the below checks. If we were passed that
    // key, then we can skip them
    if (StrongNameIsEcmaKey(keyPublicKey))
    {
        return true;
    }

    // If a hash algorithm is specified, it must be a sensible value
    bool fHashAlgorithmValid = GET_ALG_CLASS(GET_UNALIGNED_VAL32(&keyPublicKey.HashAlgID)) == ALG_CLASS_HASH &&
                               GET_ALG_SID(GET_UNALIGNED_VAL32(&keyPublicKey.HashAlgID)) >= ALG_SID_SHA1;
    if (keyPublicKey.HashAlgID != 0 && !fHashAlgorithmValid)
    {
        return false;
    }

    // If a signature algorithm is specified, it must be a sensible value
    bool fSignatureAlgorithmValid = GET_ALG_CLASS(GET_UNALIGNED_VAL32(&keyPublicKey.SigAlgID)) == ALG_CLASS_SIGNATURE;
    if (keyPublicKey.SigAlgID != 0 && !fSignatureAlgorithmValid)
    {
        return false;
    }

    // The key blob must indicate that it is a PUBLICKEYBLOB
    if (keyPublicKey.PublicKey[0] != PUBLICKEYBLOB)
    {
        return false;
    }

    return true;
}


//---------------------------------------------------------------------------------------
//
// Determine the number of bytes that a public key blob occupies, including the key portion
//
// Arguments:
//   keyPublicKey - key blob to calculate the size of
//

DWORD StrongNameSizeOfPublicKey(const PublicKeyBlob &keyPublicKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return offsetof(PublicKeyBlob, PublicKey) +     // Size of the blob header plus
           GET_UNALIGNED_VAL32(&keyPublicKey.cbPublicKey);  // the number of bytes in the key
}




// Size in bytes of strong name token.
#define SN_SIZEOF_TOKEN     8

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
VOID StrongNameFreeBuffer(BYTE *pbMemory)            // [in] address of memory to free
{
    if (pbMemory != (BYTE*)SN_THE_KEY() && pbMemory != g_rbNeutralPublicKey)
        delete [] pbMemory;
}


// Create a strong name token from a public key blob.
HRESULT StrongNameTokenFromPublicKey(BYTE    *pbPublicKeyBlob,        // [in] public key blob
                                   ULONG    cbPublicKeyBlob,
                                   BYTE   **ppbStrongNameToken,     // [out] strong name token
                                   ULONG   *pcbStrongNameToken)
{
    HRESULT         hr = S_OK;

#ifndef DACCESS_COMPILE

    SHA1Hash        sha1;
    BYTE            *pHash = NULL;
    DWORD           i;
    PublicKeyBlob   *pPublicKey = NULL;
    DWORD dwHashLenMinusTokenSize = 0;

    if (!StrongNameIsValidPublicKey(pbPublicKeyBlob, cbPublicKeyBlob))
    {
        hr = CORSEC_E_INVALID_PUBLICKEY;
        goto Exit;
    }

    // Allocate a buffer for the output token.
    *ppbStrongNameToken = new (nothrow) BYTE[SN_SIZEOF_TOKEN];
    if (*ppbStrongNameToken == NULL) {
        hr = E_OUTOFMEMORY;
        goto Exit;
    }
    *pcbStrongNameToken = SN_SIZEOF_TOKEN;

    // We cache a couple of common cases.
    if (SN_IS_NEUTRAL_KEY(pbPublicKeyBlob)) {
        memcpy_s(*ppbStrongNameToken, *pcbStrongNameToken, g_rbNeutralPublicKeyToken, SN_SIZEOF_TOKEN);
        goto Exit;
    }
    if (cbPublicKeyBlob == SN_SIZEOF_THE_KEY() &&
        memcmp(pbPublicKeyBlob, SN_THE_KEY(), cbPublicKeyBlob) == 0) {
        memcpy_s(*ppbStrongNameToken, *pcbStrongNameToken, SN_THE_KEYTOKEN(), SN_SIZEOF_TOKEN);
        goto Exit;
    }

    if (SN_IS_THE_SILVERLIGHT_PLATFORM_KEY(pbPublicKeyBlob))
    {
        memcpy_s(*ppbStrongNameToken, *pcbStrongNameToken, SN_THE_SILVERLIGHT_PLATFORM_KEYTOKEN(), SN_SIZEOF_TOKEN);
        goto Exit;
    }

    if (SN_IS_THE_SILVERLIGHT_KEY(pbPublicKeyBlob))
    {
        memcpy_s(*ppbStrongNameToken, *pcbStrongNameToken, SN_THE_SILVERLIGHT_KEYTOKEN(), SN_SIZEOF_TOKEN);
        goto Exit;
    }

    // To compute the correct public key token, we need to make sure the public key blob
    // was not padded with extra bytes that CAPI CryptImportKey would've ignored.
    // Without this round trip, we would blindly compute the hash over the padded bytes
    // which could make finding a public key token collision a significantly easier task
    // since an attacker wouldn't need to work hard on generating valid key pairs before hashing.
    if (cbPublicKeyBlob <= sizeof(PublicKeyBlob)) {
        hr = CORSEC_E_INVALID_PUBLICKEY;
        goto Error;
    }

    // Check that the blob type is PUBLICKEYBLOB.
    pPublicKey = (PublicKeyBlob*) pbPublicKeyBlob;

    if (pPublicKey->PublicKey + GET_UNALIGNED_VAL32(&pPublicKey->cbPublicKey) < pPublicKey->PublicKey) {
        hr = CORSEC_E_INVALID_PUBLICKEY;
        goto Error;
    }

    if (cbPublicKeyBlob < SN_SIZEOF_KEY(pPublicKey)) {
        hr = CORSEC_E_INVALID_PUBLICKEY;
        goto Error;
    }

    if (*(BYTE*) pPublicKey->PublicKey /* PUBLICKEYSTRUC->bType */ != PUBLICKEYBLOB) {
        hr = CORSEC_E_INVALID_PUBLICKEY;
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

    goto Exit;

 Error:
    if (*ppbStrongNameToken) {
        delete [] *ppbStrongNameToken;
        *ppbStrongNameToken = NULL;
    }
Exit:
#else
    DacNotImpl();
#endif // #ifndef DACCESS_COMPILE

    return hr;
}
