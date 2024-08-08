// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// sha256.h
//

//
// contains implementation of sha256 hash algorithm
//
//*****************************************************************************
#ifndef __sha256__h__
#define __sha256__h__

HRESULT Sha256Hash(BYTE* pSrc, DWORD srcSize, BYTE* pDst, DWORD dstSize);

#endif // __sha256__h__
