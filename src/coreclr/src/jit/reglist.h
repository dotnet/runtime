//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
