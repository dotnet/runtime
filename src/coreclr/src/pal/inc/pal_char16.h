// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    pal_char16.h

Abstract:

This file is used to define the wchar_t type as a 16-bit type on Unix.



--*/

// The unix compilers use a 32-bit wchar_t, so we must make a 16 bit wchar_t.
// The windows compilers, gcc and MSVC, both define a 16 bit wchar_t.

// Note : wchar_t is a built-in type in C++, gcc/llvm ignores any attempts to
// typedef it. Using the preprocessor here, we make sure gcc sees
// __wchar_16_cpp__ instead of wchar_t. This is apparently not necessary under
// vc++, for whom wchar_t is already a typedef instead of a built-in.

#ifndef PAL_STDCPP_COMPAT
#undef wchar_t
#undef __WCHAR_TYPE__
#define __WCHAR_TYPE__ __wchar_16_cpp__
#define wchar_t __wchar_16_cpp__

// Set up the wchar_t type (which got preprocessed to __wchar_16_cpp__).
// In C++11, the standard gives us char16_t, which is what we want (and matches types with u"")
// In C, this doesn't exist, so use unsigned short.
// **** WARNING: Linking C and C++ objects will break with -fstrict-aliasing with GCC/Clang
//               due to conditional typedef
#if !defined(_WCHAR_T_DEFINED) || !defined(_MSC_VER)
#if defined(__cplusplus)
#undef __WCHAR_TYPE__
#define __WCHAR_TYPE__ char16_t
typedef char16_t wchar_t;
#else
#undef __WCHAR_TYPE__
#define __WCHAR_TYPE__ unsigned short
typedef unsigned short wchar_t;
#endif // __cplusplus

#ifndef _WCHAR_T_DEFINED
#define _WCHAR_T_DEFINED
#endif // !_WCHAR_T_DEFINED
#endif // !_WCHAR_T_DEFINED || !_MSC_VER
#endif // !PAL_STDCPP_COMPAT
