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
// Verify that a public key blob looks like a reasonable public key
//
// Arguments:
//   pbBuffer     - buffer to verify the format of
//   cbBuffer     - size of pbBuffer
//

bool StrongNameIsValidPublicKey(__in_ecount(cbBuffer) const BYTE *pbBuffer, DWORD cbBuffer)
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
