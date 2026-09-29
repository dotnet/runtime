// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _METADATAINITIALSIZE_H_
#define _METADATAINITIALSIZE_H_

#include "cor.h"

// A dispenser hint for the initial allocation of metadata tables and heaps.
EXTERN_GUID(MetaDataInitialSize, 0x2675b6bf, 0xf504, 0x4cb4, 0xa4, 0xd5, 0x08, 0x4e, 0xea, 0x77, 0x0d, 0xdc);

typedef enum CorMetaDataInitialSize
{
    MDInitialSizeDefault = 0,
    MDInitialSizeMinimal = 1
} CorMetaDataInitialSize;

#endif // _METADATAINITIALSIZE_H_
