// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef REGLIST_H
#define REGLIST_H

#include "target.h"
#include "tinyarray.h"

// The "regList" type is a small set of registerse
#ifdef _TARGET_X86_
typedef TinyArray<unsigned short, regNumber, REGNUM_BITS> regList;
#else
// The regList is unused for all other targets.
#endif // _TARGET_*

#endif // REGLIST_H
