// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// Strong name APIs which are not exposed publicly, but are built into StrongName.lib
// 

#ifndef _STRONGNAME_INTERNAL_H
#define _STRONGNAME_INTERNAL_H

#include <strongname.h>

// Determine the number of bytes in a public key
DWORD StrongNameSizeOfPublicKey(const PublicKeyBlob &keyPublicKey);

bool StrongNameIsValidPublicKey(__in_ecount(cbPublicKeyBlob) const BYTE *pbPublicKeyBlob, DWORD cbPublicKeyBlob);
bool StrongNameIsValidPublicKey(const PublicKeyBlob &keyPublicKey);

// Determine if a public key is the ECMA key
bool StrongNameIsEcmaKey(__in_ecount(cbKey) const BYTE *pbKey, DWORD cbKey);
bool StrongNameIsEcmaKey(const PublicKeyBlob &keyPublicKey);

#endif // !_STRONGNAME_INTERNAL_H
