// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef STANDARDPCH_H
#define STANDARDPCH_H

// The point of a PCH file is to never reparse files that never change.
// Only include files here that will almost NEVER change. Headers for the project
// itself are probably inappropriate, because if you change them, the entire
// project will require a recompile. Generally just put SDK style stuff here...

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif // WIN32_LEAN_AND_MEAN
#include <windows.h>

#ifdef INTERNAL_BUILD
// There are a few features that reference Microsoft internal resources. We can't build these
// in the open source version.
#define USE_MSVCDIS

// Disable CoreDisTools until coredistools.dll is statically-linked to the CRT, or until it is delayload linked.
//#define USE_COREDISTOOLS
#endif // INTERNAL_BUILD

// JIT_BUILD disables certain PAL_TRY debugging and Contracts features. Set this just as the JIT sets it.
#define JIT_BUILD

#ifdef _MSC_VER
// Defining this prevents:
//   error C2338 : / RTCc rejects conformant code, so it isn't supported by the C++ Standard Library.
//   Either remove this compiler option, or define _ALLOW_RTCc_IN_STL to acknowledge that you have received this
//   warning.
#ifndef _ALLOW_RTCc_IN_STL
#define _ALLOW_RTCc_IN_STL
#endif

#define MSC_ONLY(x) x
#else // !_MSC_VER
#define MSC_ONLY(x)
#endif // !_MSC_VER

#ifndef _CRT_SECURE_NO_WARNINGS
#define _CRT_SECURE_NO_WARNINGS
#endif // _CRT_SECURE_NO_WARNINGS

#define _CRT_RAND_S

#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <stddef.h>
#include <malloc.h>
#include <assert.h>
#include <wchar.h>
#include <specstrings.h>
#include <math.h>
#include <limits.h>
#include <ctype.h>
#include <stdarg.h>

#include <utility>
#include <string>
#include <algorithm>
#include <vector>

template<typename T, typename U>
constexpr auto max(T&& t, U&& u) -> decltype(t > u ? t : u)
{
    return t > u ? t : u;
}

template<typename T, typename U>
constexpr auto min(T&& t, U&& u) -> decltype(t < u ? t : u)
{
    return t < u ? t : u;
}

#ifdef USE_MSVCDIS
#define DISLIB
#include "..\external\msvcdis\inc\msvcdis.h"
#include "..\external\msvcdis\inc\disx86.h"
#include "..\external\msvcdis\inc\disarm64.h"
#endif // USE_MSVCDIS

#ifndef W
#ifdef TARGET_UNIX
#define W(str) u##str
#else // TARGET_UNIX
#define W(str) L##str
#endif // TARGET_UNIX
#endif // !W

#ifdef TARGET_UNIX
#ifndef DIRECTORY_SEPARATOR_CHAR_A
#define DIRECTORY_SEPARATOR_CHAR_A '/'
#endif
#ifndef DIRECTORY_SEPARATOR_STR_A
#define DIRECTORY_SEPARATOR_STR_A "/"
#endif
#ifndef DIRECTORY_SEPARATOR_STR_W
#define DIRECTORY_SEPARATOR_STR_W W("/")
#endif
#else // TARGET_UNIX
#ifndef DIRECTORY_SEPARATOR_CHAR_A
#define DIRECTORY_SEPARATOR_CHAR_A '\\'
#endif
#ifndef DIRECTORY_SEPARATOR_STR_A
#define DIRECTORY_SEPARATOR_STR_A "\\"
#endif
#ifndef DIRECTORY_SEPARATOR_STR_W
#define DIRECTORY_SEPARATOR_STR_W W("\\")
#endif
#endif // TARGET_UNIX

#ifdef TARGET_UNIX
#define PLATFORM_SHARED_LIB_SUFFIX_A PAL_SHLIB_SUFFIX
#else // !TARGET_UNIX
#define PLATFORM_SHARED_LIB_SUFFIX_A ".dll"
#endif // !TARGET_UNIX

#define DEFAULT_REAL_JIT_NAME_A MAKEDLLNAME_A("clrjit2")
#define DEFAULT_REAL_JIT_NAME_W MAKEDLLNAME_W("clrjit2")

#if !defined(_MSC_VER) && !defined(__llvm__)
static inline void __debugbreak()
{
  DebugBreak();
}
#endif

#include <minipal/utils.h>

#endif // STANDARDPCH_H
