// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Strong name APIs which are not exposed publicly, but are built into StrongName.lib
//

#ifndef _STRONGNAME_INTERNAL_H
#define _STRONGNAME_INTERNAL_H

// Public key blob binary format.
typedef struct {
    unsigned int SigAlgID;       // (ALG_ID) signature algorithm used to create the signature
    unsigned int HashAlgID;      // (ALG_ID) hash algorithm used to create the signature
    ULONG        cbPublicKey;    // length of the key in bytes
    BYTE         PublicKey[1];   // variable length byte array containing the key value in format output by CryptoAPI
} PublicKeyBlob;

// Determine the number of bytes in a public key
DWORD StrongNameSizeOfPublicKey(const PublicKeyBlob &keyPublicKey);

bool StrongNameIsValidPublicKey(_In_reads_(cbPublicKeyBlob) const BYTE *pbPublicKeyBlob, DWORD cbPublicKeyBlob);
bool StrongNameIsValidPublicKey(const PublicKeyBlob &keyPublicKey);

// Determine if a public key is the ECMA key
bool StrongNameIsEcmaKey(_In_reads_(cbKey) const BYTE *pbKey, DWORD cbKey);
bool StrongNameIsEcmaKey(const PublicKeyBlob &keyPublicKey);

HRESULT StrongNameTokenFromPublicKey(BYTE* pbPublicKeyBlob,  // [in] public key blob
    ULONG    cbPublicKeyBlob,
    BYTE** ppbStrongNameToken,     // [out] strong name token
    ULONG* pcbStrongNameToken);

VOID StrongNameFreeBuffer(BYTE* pbMemory);

#endif // !_STRONGNAME_INTERNAL_H
