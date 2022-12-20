// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __COMMON_TYPES_H__
#define __COMMON_TYPES_H__

#include <cstddef>
#include <cstdint>
#include <stdlib.h>
#include <stdio.h>
#include <new>

// Implement pure virtual for Unix (for -p:LinkStandardCPlusPlusLibrary=false the default),
// to avoid linker requiring __cxa_pure_virtual.
#ifdef TARGET_WINDOWS
#define PURE_VIRTUAL = 0;
#else
// `while(true);` is to satisfy the missing `return` statement. It will be optimized away by the compiler.
#define PURE_VIRTUAL { assert(!"pure virtual function called"); while(true); }
#endif

using std::nothrow;
using std::size_t;
using std::uintptr_t;
using std::intptr_t;

typedef wchar_t             WCHAR;
typedef void *              HANDLE;

typedef uint32_t            UInt32_BOOL;    // windows 4-byte BOOL, 0 -> false, everything else -> true
#define UInt32_FALSE        0
#define UInt32_TRUE         1

#endif // __COMMON_TYPES_H__
