// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/
#ifndef _GENTREEOPSDEF_H_
#define _GENTREEOPSDEF_H_
/*****************************************************************************/

enum genTreeOps : BYTE
{
#define GTNODE(en, st, cm, ivn, ok) GT_##en,
#include "gtlist.h"

    GT_COUNT,

#ifdef TARGET_64BIT
    // GT_CNS_NATIVELONG is the gtOper symbol for GT_CNS_LNG or GT_CNS_INT, depending on the target.
    // For the 64-bit targets we will only use GT_CNS_INT as it used to represent all the possible sizes
    GT_CNS_NATIVELONG = GT_CNS_INT,
#else
    // For the 32-bit targets we use a GT_CNS_LNG to hold a 64-bit integer constant and GT_CNS_INT for all others.
    // In the future when we retarget the JIT for x86 we should consider eliminating GT_CNS_LNG
    GT_CNS_NATIVELONG = GT_CNS_LNG,
#endif
};

/*****************************************************************************/
#endif // _GENTREEOPSDEF_H_
/*****************************************************************************/
