// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTOPSSHARED_H_
#define _INTOPSSHARED_H_

#define OPDEF(a,b,c,d,e,f) a,
typedef enum
{
#include "intops.def"
    INTOP_LAST
} InterpOpcode;
#undef OPDEF

#endif
