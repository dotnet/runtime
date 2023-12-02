// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Strong name APIs which are not exposed publicly, but are built into StrongName.lib
//

#ifndef _STRONGNAME_INTERNAL_H
#define _STRONGNAME_INTERNAL_H

HRESULT StrongNameTokenFromPublicKey(BYTE* pbPublicKeyBlob,  // [in] public key blob
    ULONG    cbPublicKeyBlob,
    BYTE** ppbStrongNameToken,     // [out] strong name token
    ULONG* pcbStrongNameToken);

VOID StrongNameFreeBuffer(BYTE* pbMemory);

#endif // !_STRONGNAME_INTERNAL_H
