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
};

/*****************************************************************************/
#endif // _GENTREEOPSDEF_H_
/*****************************************************************************/
