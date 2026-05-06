// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Strong name APIs which are not exposed publicly, but are built into StrongName.lib
//

#ifndef _STRONGNAME_INTERNAL_H
#define _STRONGNAME_INTERNAL_H

extern BYTE const* const g_coreLibPublicKey;
extern const ULONG g_coreLibPublicKeyLen;

// Public key blob binary format.
typedef struct {
    unsigned int SigAlgID;       // (ALG_ID) signature algorithm used to create the signature
    unsigned int HashAlgID;      // (ALG_ID) hash algorithm used to create the signature
    ULONG        cbPublicKey;    // length of the key in bytes
    BYTE         PublicKey[1];   // variable length byte array containing the key value in format output by CryptoAPI
} PublicKeyBlob;

struct StrongNameToken
{
    static constexpr ULONG SIZEOF_TOKEN = 8;
    BYTE m_token[SIZEOF_TOKEN];
};

HRESULT StrongNameTokenFromPublicKey(BYTE* pbPublicKeyBlob,  // [in] public key blob
    ULONG    cbPublicKeyBlob,
    StrongNameToken* token     // [out] strong name token
);

#endif // !_STRONGNAME_INTERNAL_H
