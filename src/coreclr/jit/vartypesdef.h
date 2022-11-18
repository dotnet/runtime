// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/
#ifndef _VARTYPESDEF_H_
#define _VARTYPESDEF_H_
/*****************************************************************************/

enum var_types : BYTE
{
#define DEF_TP(tn, nm, jitType, verType, sz, sze, asze, st, al, tf) TYP_##tn,
#include "typelist.h"
#undef DEF_TP
    TYP_COUNT
};

/*****************************************************************************/
#endif // _VARTYPESDEF_H_
/*****************************************************************************/
