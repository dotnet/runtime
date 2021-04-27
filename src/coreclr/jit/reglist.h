// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef REGLIST_H
#define REGLIST_H

#include "target.h"
#include "tinyarray.h"

// The "regList" type is a small set of registerse
#ifdef TARGET_X86
typedef TinyArray<unsigned short, regNumber, REGNUM_BITS> regList;
#else
// The regList is unused for all other targets.
#endif // TARGET*

#endif // REGLIST_H
