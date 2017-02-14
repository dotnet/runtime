// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// Strong name APIs which are not exposed publicly, but are built into StrongName.lib
// 

#ifndef _STRONGNAME_INTERNAL_H
#define _STRONGNAME_INTERNAL_H

#include <strongname.h>

#include <wincrypt.h>

// NTDDI_VERSION is currently defined as XP SP2.
// Strongname api's that use this are supported on XP SP3 and later, so we can use them.
#ifndef ALG_SID_SHA_256 
#define ALG_SID_SHA_256                 12
#define ALG_SID_SHA_384                 13
#define ALG_SID_SHA_512                 14
#define CALG_SHA_256            (ALG_CLASS_HASH | ALG_TYPE_ANY | ALG_SID_SHA_256)
#define CALG_SHA_384            (ALG_CLASS_HASH | ALG_TYPE_ANY | ALG_SID_SHA_384)
#define CALG_SHA_512            (ALG_CLASS_HASH | ALG_TYPE_ANY | ALG_SID_SHA_512)
#endif //ALG_SID_SHA_256

// Determine the number of bytes in a public key
DWORD StrongNameSizeOfPublicKey(const PublicKeyBlob &keyPublicKey);

bool StrongNameIsValidPublicKey(__in_ecount(cbPublicKeyBlob) const BYTE *pbPublicKeyBlob, DWORD cbPublicKeyBlob, bool fImporKey);
bool StrongNameIsValidPublicKey(const PublicKeyBlob &keyPublicKey, bool fImportKey);

// Determine if a public key is the ECMA key
bool StrongNameIsEcmaKey(__in_ecount(cbKey) const BYTE *pbKey, DWORD cbKey);
bool StrongNameIsEcmaKey(const PublicKeyBlob &keyPublicKey);

bool StrongNameIsTheKey(__in_ecount(cbKey) const BYTE *pbKey, DWORD cbKey);

#if defined(CROSSGEN_COMPILE) && !defined(PLATFORM_UNIX)

// Verify the format of a public key blob
bool StrongNameIsValidKeyPair(__in_ecount(cbKeyPair) const BYTE *pbKeyPair, DWORD cbKeyPair);

bool GetBytesFromHex(LPCUTF8 szHexString, ULONG cchHexString, BYTE** buffer, ULONG *cbBufferSize);

bool StrongNameCryptAcquireContext(HCRYPTPROV *phProv, LPCWSTR pwszContainer, LPCWSTR pwszProvider, DWORD dwProvType, DWORD dwFlags);
#endif // (CROSSGEN_COMPILE && !PLATFORM_UNIX)

bool StrongNameIsSilverlightPlatformKey(__in_ecount(cbKey) const BYTE *pbKey, DWORD cbKey);
bool StrongNameIsSilverlightPlatformKey(const PublicKeyBlob &keyPublicKey);

#endif // !_STRONGNAME_INTERNAL_H
