// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================================
// File: argslot.h
//

// ============================================================================
// Contains the ARG_SLOT type.


#ifndef __ARG_SLOT_H__
#define __ARG_SLOT_H__

// The ARG_SLOT must be big enough to represent all pointer and basic types (except for 80-bit fp values).
// So, it's guaranteed to be at least 64-bit.
typedef unsigned __int64 ARG_SLOT;
#define SIZEOF_ARG_SLOT 8

#if BIGENDIAN
// Returns the address of the payload inside the argslot
inline BYTE* ArgSlotEndiannessFixup(ARG_SLOT* pArg, UINT cbSize) {
    LIMITED_METHOD_CONTRACT;

    BYTE* pBuf = (BYTE*)pArg;
    switch (cbSize) {
    case 1:
        pBuf += 7;
        break;
    case 2:
        pBuf += 6;
        break;
    case 4:
        pBuf += 4;
        break;
    }
    return pBuf;
}
#else
#define ArgSlotEndiannessFixup(pArg, cbSize) ((BYTE *)(pArg))
#endif

#endif  // __ARG_SLOT_H__
