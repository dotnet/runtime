// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// Strong name APIs which are not exposed publicly but are used by CLR code
// 

#include "common.h"
#include "strongnameinternal.h"
#include "strongnameholders.h"
#include "thekey.h"
#include "ecmakey.h"

//---------------------------------------------------------------------------------------
//
// Check to see if a public key blob is the ECMA public key blob
//
// Arguments:
//   pbKey - public key blob to check
//   cbKey - size in bytes of pbKey
//

bool StrongNameIsEcmaKey(__in_ecount(cbKey) const BYTE *pbKey, DWORD cbKey)
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
// Check to see if a public key blob is the TheKey public key blob
//
// Arguments:
//   pbKey - public key blob to check
//   cbKey - size in bytes of pbKey
//
bool StrongNameIsTheKey(__in_ecount(cbKey) const BYTE *pbKey, DWORD cbKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // The key should be the same size as the TheKey key
    if (cbKey != sizeof(g_rbTheKey))
    {
        return false;
    }

    return (memcmp(pbKey, g_rbTheKey, sizeof(g_rbTheKey)) == 0);
}

//---------------------------------------------------------------------------------------
//
// Check to see if a public key blob is the Silverlight Platform public key blob
//
// Arguments:
//   pbKey - public key blob to check
//   cbKey - size in bytes of pbKey
//

bool StrongNameIsSilverlightPlatformKey(__in_ecount(cbKey) const BYTE *pbKey, DWORD cbKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // The key should be the same size as the ECMA key
    if (cbKey != sizeof(g_rbTheSilverlightPlatformKey))
    {
        return false;
    }

    const PublicKeyBlob *pKeyBlob = reinterpret_cast<const PublicKeyBlob *>(pbKey);
    return StrongNameIsSilverlightPlatformKey(*pKeyBlob);
}

//---------------------------------------------------------------------------------------
//
// Check to see if a public key blob is the Silverlight Platform public key blob
//
// Arguments:
//   keyPublicKey - Key to check to see if it matches the ECMA key
//

bool StrongNameIsSilverlightPlatformKey(const PublicKeyBlob &keyPublicKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return StrongNameSizeOfPublicKey(keyPublicKey) == sizeof(g_rbTheSilverlightPlatformKey) &&
           memcmp(reinterpret_cast<const BYTE *>(&keyPublicKey), g_rbTheSilverlightPlatformKey, sizeof(g_rbTheSilverlightPlatformKey)) == 0;
}

//---------------------------------------------------------------------------------------
//
// Verify that a public key blob looks like a reasonable public key
//
// Arguments:
//   pbBuffer     - buffer to verify the format of
//   cbBuffer     - size of pbBuffer
//   fImportKeys  - do a more extensive check by attempting to import the keys
//

bool StrongNameIsValidPublicKey(__in_ecount(cbBuffer) const BYTE *pbBuffer, DWORD cbBuffer, bool fImportKeys)
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
    return StrongNameIsValidPublicKey(*pkeyPublicKey, fImportKeys);
}

//---------------------------------------------------------------------------------------
//
// Verify that a public key blob looks like a reasonable public key.
// 
// Arguments:
//   keyPublicKey - key blob to verify 
//   fImportKeys  - do a more extensive check by verifying that the key data imports into CAPI
// 
// Notes:
//    This can be a very expensive operation, since it involves importing keys.  
//

bool StrongNameIsValidPublicKey(const PublicKeyBlob &keyPublicKey, bool fImportKeys)
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

#if (defined(CROSSGEN_COMPILE) && !defined(PLATFORM_UNIX))
    // Make sure the public key blob imports properly
    if (fImportKeys)
    {
        CapiProviderHolder hProv;
        if (!StrongNameCryptAcquireContext(&hProv, NULL, NULL, PROV_RSA_FULL, CRYPT_VERIFYCONTEXT))
        {
            return false;
        }

        CapiKeyHolder hKey;
        if (!CryptImportKey(hProv, keyPublicKey.PublicKey, GET_UNALIGNED_VAL32(&keyPublicKey.cbPublicKey), NULL, 0, &hKey))
        {
            return false;
        }
    }
#else // (CROSSGEN_COMPILE && !PLATFORM_UNIX)
    _ASSERTE(!fImportKeys);
#endif // (CROSSGEN_COMPILE && !PLATFORM_UNIX)

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

#if (defined(CROSSGEN_COMPILE) && !defined(PLATFORM_UNIX))

//---------------------------------------------------------------------------------------
//
// Check to see if the value held in a buffer is a full strong name key pair
//
// Arguments:
//    pbBuffer - Blob to check
//    cbBuffer - Size of the buffer in bytes
//
// Return Value:
//    true if the buffer represents a full strong name key pair, false otherwise
//

bool StrongNameIsValidKeyPair(__in_ecount(cbKeyPair) const BYTE *pbKeyPair, DWORD cbKeyPair)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pbKeyPair));
    }
    CONTRACTL_END;

    // Key pairs are just CAPI PRIVATEKEYBLOBs, so see if CAPI can import the blob
    CapiProviderHolder hProv;
    if (!StrongNameCryptAcquireContext(&hProv, NULL, NULL, PROV_RSA_FULL, CRYPT_VERIFYCONTEXT))
    {
        return false;
    }

    CapiKeyHolder hKey;
    if (!CryptImportKey(hProv, pbKeyPair, cbKeyPair, NULL, 0, &hKey))
    {
        return false;
    }

    return true;
}


BYTE HexToByteA (char c) {
    LIMITED_METHOD_CONTRACT;

    if (!isxdigit(c)) return (BYTE) 0xff;
    if (isdigit(c)) return (BYTE) (c - '0');
    if (isupper(c)) return (BYTE) (c - 'A' + 10);
    return (BYTE) (c - 'a' + 10);
}
    
// Read the hex string into a buffer
// Caller owns the buffer. 
// Returns NULL if the string contains non-hex characters, or doesn't contain a multiple of 2 characters.
bool GetBytesFromHex(LPCUTF8 szHexString, ULONG cchHexString, BYTE** buffer, ULONG *cbBufferSize) {
    LIMITED_METHOD_CONTRACT;

    ULONG cchHex = cchHexString;
    if (cchHex % 2 != 0)
        return false;
    *cbBufferSize = cchHex / 2;
    NewArrayHolder<BYTE> tempBuffer(new (nothrow) BYTE[*cbBufferSize]);
    if (tempBuffer == NULL)
        return false;

    for (ULONG i = 0; i < *cbBufferSize; i++) {
        BYTE msn = HexToByteA(*szHexString);
        BYTE lsn = HexToByteA(*(szHexString + 1));
        if(msn == 0xFF || lsn == 0xFF)
        {
            return false;
        }

        tempBuffer[i] = (BYTE) ( (msn << 4) | lsn );
        szHexString += 2;
    }

    *buffer = tempBuffer.Extract();
    return true;
}

// Helper method to call CryptAcquireContext, making sure we have a valid set of flags
bool StrongNameCryptAcquireContext(HCRYPTPROV *phProv, LPCWSTR pwszContainer, LPCWSTR pwszProvider, DWORD dwProvType, DWORD dwFlags)
{
    LIMITED_METHOD_CONTRACT;

#if defined(CRYPT_VERIFYCONTEXT) && defined(CRYPT_MACHINE_KEYSET)
    // Specifying both verify context (for an ephemeral key) and machine keyset (for a persisted machine key)
    // does not make sense.  Additionally, Widows is beginning to lock down against uses of MACHINE_KEYSET
    // (for instance in the app container), even if verify context is present.   Therefore, if we're using
    // an ephemeral key, strip out MACHINE_KEYSET from the flags.
    if ((dwFlags & CRYPT_VERIFYCONTEXT) && (dwFlags & CRYPT_MACHINE_KEYSET))
    {
        dwFlags &= ~CRYPT_MACHINE_KEYSET;
    }
#endif // defined(CRYPT_VERIFYCONTEXT) && defined(CRYPT_MACHINE_KEYSET)

    return !!WszCryptAcquireContext(phProv, pwszContainer, pwszProvider, dwProvType, dwFlags);
}

#endif // (CROSSGEN_COMPILE && !PLATFORM_UNIX)

