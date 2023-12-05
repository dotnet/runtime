// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Strong name APIs which are not exposed publicly but are used by CLR code
//

#include "stdafx.h"
#include "strongnameinternal.h"
#include "sha1.h"

// Common keys used by libraries we ship are included here.

namespace
{
    // The byte values of the ECMA pseudo public key and its token.
    const BYTE g_rbNeutralPublicKey[] = { 0, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0 };
    const BYTE g_rbNeutralPublicKeyToken[] = { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 };

    // The byte values of the real public keys and their corresponding tokens
    // for assemblies we ship.
    // These blobs allow us to skip the token calculation for our assemblies.
    static const BYTE g_rbMicrosoftKey[] =
    {
        0x00,0x24,0x00,0x00,0x04,0x80,0x00,0x00,0x94,0x00,0x00,0x00,0x06,0x02,0x00,0x00,
        0x00,0x24,0x00,0x00,0x52,0x53,0x41,0x31,0x00,0x04,0x00,0x00,0x01,0x00,0x01,0x00,
        0x07,0xd1,0xfa,0x57,0xc4,0xae,0xd9,0xf0,0xa3,0x2e,0x84,0xaa,0x0f,0xae,0xfd,0x0d,
        0xe9,0xe8,0xfd,0x6a,0xec,0x8f,0x87,0xfb,0x03,0x76,0x6c,0x83,0x4c,0x99,0x92,0x1e,
        0xb2,0x3b,0xe7,0x9a,0xd9,0xd5,0xdc,0xc1,0xdd,0x9a,0xd2,0x36,0x13,0x21,0x02,0x90,
        0x0b,0x72,0x3c,0xf9,0x80,0x95,0x7f,0xc4,0xe1,0x77,0x10,0x8f,0xc6,0x07,0x77,0x4f,
        0x29,0xe8,0x32,0x0e,0x92,0xea,0x05,0xec,0xe4,0xe8,0x21,0xc0,0xa5,0xef,0xe8,0xf1,
        0x64,0x5c,0x4c,0x0c,0x93,0xc1,0xab,0x99,0x28,0x5d,0x62,0x2c,0xaa,0x65,0x2c,0x1d,
        0xfa,0xd6,0x3d,0x74,0x5d,0x6f,0x2d,0xe5,0xf1,0x7e,0x5e,0xaf,0x0f,0xc4,0x96,0x3d,
        0x26,0x1c,0x8a,0x12,0x43,0x65,0x18,0x20,0x6d,0xc0,0x93,0x34,0x4d,0x5a,0xd2,0x93
    };

    static const BYTE g_rbMicrosoftKeyToken[] = {0xb0,0x3f,0x5f,0x7f,0x11,0xd5,0x0a,0x3a};

    static const BYTE g_rbTheSilverlightPlatformKey[] =
    {
        0x00,0x24,0x00,0x00,0x04,0x80,0x00,0x00,0x94,0x00,0x00,0x00,0x06,0x02,0x00,0x00,
        0x00,0x24,0x00,0x00,0x52,0x53,0x41,0x31,0x00,0x04,0x00,0x00,0x01,0x00,0x01,0x00,
        0x8d,0x56,0xc7,0x6f,0x9e,0x86,0x49,0x38,0x30,0x49,0xf3,0x83,0xc4,0x4b,0xe0,0xec,
        0x20,0x41,0x81,0x82,0x2a,0x6c,0x31,0xcf,0x5e,0xb7,0xef,0x48,0x69,0x44,0xd0,0x32,
        0x18,0x8e,0xa1,0xd3,0x92,0x07,0x63,0x71,0x2c,0xcb,0x12,0xd7,0x5f,0xb7,0x7e,0x98,
        0x11,0x14,0x9e,0x61,0x48,0xe5,0xd3,0x2f,0xba,0xab,0x37,0x61,0x1c,0x18,0x78,0xdd,
        0xc1,0x9e,0x20,0xef,0x13,0x5d,0x0c,0xb2,0xcf,0xf2,0xbf,0xec,0x3d,0x11,0x58,0x10,
        0xc3,0xd9,0x06,0x96,0x38,0xfe,0x4b,0xe2,0x15,0xdb,0xf7,0x95,0x86,0x19,0x20,0xe5,
        0xab,0x6f,0x7d,0xb2,0xe2,0xce,0xef,0x13,0x6a,0xc2,0x3d,0x5d,0xd2,0xbf,0x03,0x17,
        0x00,0xae,0xc2,0x32,0xf6,0xc6,0xb1,0xc7,0x85,0xb4,0x30,0x5c,0x12,0x3b,0x37,0xab
    };

    static const BYTE g_rbTheSilverlightPlatformKeyToken[] = {0x7c,0xec,0x85,0xd7,0xbe,0xa7,0x79,0x8e};

    static const BYTE g_rbTheSilverlightKey[] =
    {
        0x00,0x24,0x00,0x00,0x04,0x80,0x00,0x00,0x94,0x00,0x00,0x00,0x06,0x02,0x00,0x00,
        0x00,0x24,0x00,0x00,0x52,0x53,0x41,0x31,0x00,0x04,0x00,0x00,0x01,0x00,0x01,0x00,
        0xb5,0xfc,0x90,0xe7,0x02,0x7f,0x67,0x87,0x1e,0x77,0x3a,0x8f,0xde,0x89,0x38,0xc8,
        0x1d,0xd4,0x02,0xba,0x65,0xb9,0x20,0x1d,0x60,0x59,0x3e,0x96,0xc4,0x92,0x65,0x1e,
        0x88,0x9c,0xc1,0x3f,0x14,0x15,0xeb,0xb5,0x3f,0xac,0x11,0x31,0xae,0x0b,0xd3,0x33,
        0xc5,0xee,0x60,0x21,0x67,0x2d,0x97,0x18,0xea,0x31,0xa8,0xae,0xbd,0x0d,0xa0,0x07,
        0x2f,0x25,0xd8,0x7d,0xba,0x6f,0xc9,0x0f,0xfd,0x59,0x8e,0xd4,0xda,0x35,0xe4,0x4c,
        0x39,0x8c,0x45,0x43,0x07,0xe8,0xe3,0x3b,0x84,0x26,0x14,0x3d,0xae,0xc9,0xf5,0x96,
        0x83,0x6f,0x97,0xc8,0xf7,0x47,0x50,0xe5,0x97,0x5c,0x64,0xe2,0x18,0x9f,0x45,0xde,
        0xf4,0x6b,0x2a,0x2b,0x12,0x47,0xad,0xc3,0x65,0x2b,0xf5,0xc3,0x08,0x05,0x5d,0xa9
    };

    static const BYTE g_rbTheSilverlightKeyToken[] = {0x31,0xBF,0x38,0x56,0xAD,0x36,0x4E,0x35};

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
}

BYTE const* const g_coreLibPublicKey = g_rbTheSilverlightPlatformKey;
const ULONG g_coreLibPublicKeyLen = ARRAY_SIZE(g_rbTheSilverlightPlatformKey);

// Determine the size of a PublicKeyBlob structure given the size of the key
// portion.
#define SN_SIZEOF_KEY(_pKeyBlob) (offsetof(PublicKeyBlob, PublicKey) + GET_UNALIGNED_VAL32(&(_pKeyBlob)->cbPublicKey))


#define SN_MICROSOFT_KEY() ((PublicKeyBlob*)g_rbMicrosoftKey)
#define SN_SIZEOF_MICROSOFT_KEY() sizeof(g_rbMicrosoftKey)

#define SN_MICROSOFT_KEYTOKEN() ((PublicKeyBlob*)g_rbMicrosoftKeyToken)

// Determine if the given public key blob is the neutral key.
#define SN_IS_NEUTRAL_KEY(_pk) (SN_SIZEOF_KEY((PublicKeyBlob*)(_pk)) == sizeof(g_rbNeutralPublicKey) && \
                                memcmp((_pk), g_rbNeutralPublicKey, sizeof(g_rbNeutralPublicKey)) == 0)

#define SN_IS_MICROSOFT_KEY(_pk) (SN_SIZEOF_KEY((PublicKeyBlob*)(_pk)) == sizeof(g_rbMicrosoftKey) && \
                                memcmp((_pk), g_rbMicrosoftKey, sizeof(g_rbMicrosoftKey)) == 0)


// Silverlight platform key
#define SN_THE_SILVERLIGHT_PLATFORM_KEYTOKEN() ((PublicKeyBlob*)g_rbTheSilverlightPlatformKeyToken)
#define SN_IS_THE_SILVERLIGHT_PLATFORM_KEY(_pk) (SN_SIZEOF_KEY((PublicKeyBlob*)(_pk)) == sizeof(g_rbTheSilverlightPlatformKey) && \
                                memcmp((_pk), g_rbTheSilverlightPlatformKey, sizeof(g_rbTheSilverlightPlatformKey)) == 0)

// Silverlight key
#define SN_IS_THE_SILVERLIGHT_KEY(_pk) (SN_SIZEOF_KEY((PublicKeyBlob*)(_pk)) == sizeof(g_rbTheSilverlightKey) && \
                                memcmp((_pk), g_rbTheSilverlightKey, sizeof(g_rbTheSilverlightKey)) == 0)

#define SN_THE_SILVERLIGHT_KEYTOKEN() ((PublicKeyBlob*)g_rbTheSilverlightKeyToken)

// Create a strong name token from a public key blob.
HRESULT StrongNameTokenFromPublicKey(BYTE    *pbPublicKeyBlob,        // [in] public key blob
                                   ULONG    cbPublicKeyBlob,
                                   BYTE(&tokenBuffer)[SN_SIZEOF_TOKEN]     // [out] strong name token
)
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

    // We cache a couple of common cases.
    if (SN_IS_NEUTRAL_KEY(pbPublicKeyBlob)) {
        memcpy_s(tokenBuffer, SN_SIZEOF_TOKEN, g_rbNeutralPublicKeyToken, SN_SIZEOF_TOKEN);
        goto Exit;
    }
    if (cbPublicKeyBlob == SN_SIZEOF_MICROSOFT_KEY() &&
        memcmp(pbPublicKeyBlob, SN_MICROSOFT_KEY(), cbPublicKeyBlob) == 0) {
        memcpy_s(tokenBuffer, SN_SIZEOF_TOKEN, SN_MICROSOFT_KEYTOKEN(), SN_SIZEOF_TOKEN);
        goto Exit;
    }

    if (SN_IS_THE_SILVERLIGHT_PLATFORM_KEY(pbPublicKeyBlob))
    {
        memcpy_s(tokenBuffer, SN_SIZEOF_TOKEN, SN_THE_SILVERLIGHT_PLATFORM_KEYTOKEN(), SN_SIZEOF_TOKEN);
        goto Exit;
    }

    if (SN_IS_THE_SILVERLIGHT_KEY(pbPublicKeyBlob))
    {
        memcpy_s(tokenBuffer, SN_SIZEOF_TOKEN, SN_THE_SILVERLIGHT_KEYTOKEN(), SN_SIZEOF_TOKEN);
        goto Exit;
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
        (tokenBuffer)[SN_SIZEOF_TOKEN - (i + 1)] = pHash[i + dwHashLenMinusTokenSize];

    goto Exit;

Exit:
#else
    DacNotImpl();
#endif // #ifndef DACCESS_COMPILE

    return hr;
}
