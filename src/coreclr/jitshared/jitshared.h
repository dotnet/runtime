// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _JITSHARED_H_
#define _JITSHARED_H_

// This file contains common definitions shared between the JIT and interpreter.

#include "jitassert.h"

// MEASURE_MEM_ALLOC controls whether memory allocation statistics are collected.
// When enabled, the arena allocator tracks allocations by category (memory kind)
// and can report aggregate statistics at shutdown.
//
// Set to 1 in DEBUG builds by default. Can be set to 1 in retail builds as well
// for performance analysis.

#ifdef DEBUG
#define MEASURE_MEM_ALLOC 1 // Collect memory allocation stats.
#else
#define MEASURE_MEM_ALLOC 0 // You can set this to 1 to get memory stats in retail, as well
#endif

#endif // _JITSHARED_H_
