// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef CORHDR_H
#define CORHDR_H

#include "pal.h"

typedef struct IMAGE_COR_VTABLEFIXUP // From CoreCLR's corhdr.h
{
    std::uint32_t      RVA;                    // Offset of v-table array in image.
    std::uint16_t      Count;                  // How many entries at location.
    std::uint16_t      Type;                   // COR_VTABLE_xxx type of entries.
} IMAGE_COR_VTABLEFIXUP;

#define RidFromToken(tk) ((ULONG32) ((tk) & 0x00ffffff))
#define IsNilToken(tk) ((RidFromToken(tk)) == 0)

using mdToken = std::uint32_t;

#endif
