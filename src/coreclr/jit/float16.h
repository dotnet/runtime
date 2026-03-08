// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 Both simd.cpp, gentree.cpp, and utils.cpp need a definition of float16_t
 but do not share a common header.

 Defining here so as to not create accidental implicit include dependencies.
 This definition can be removed once .NET moves to C++23 support.
******************************************************************************/

#ifndef _FLOAT16_H_
#define _FLOAT16_H_

#include <cstdint>

typedef uint16_t float16_t;

#endif // _FLOAT16_H_
