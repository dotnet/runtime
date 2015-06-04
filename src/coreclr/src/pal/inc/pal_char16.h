//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
#if defined (PLATFORM_UNIX) && defined(__GNUC__)
#undef wchar_t
#define wchar_t __wchar_16_cpp__
#endif // PLATFORM_UNIX

// Set up the wchar_t type (which got preprocessed to __wchar_16_cpp__).
// In C++11, the standard gives us char16_t, which is what we want (and matches types with u"")
// In C, this doesn't exist, so use unsigned short.

#if !defined(_WCHAR_T_DEFINED) || !defined(_MSC_VER)
#if defined (PLATFORM_UNIX)
#if defined(__cplusplus)
typedef char16_t wchar_t;
#else
typedef unsigned short wchar_t;
#endif // __cplusplus
#endif // PLATFORM_UNIX
#ifndef _WCHAR_T_DEFINED
#define _WCHAR_T_DEFINED
#endif // !_WCHAR_T_DEFINED
#endif // !_WCHAR_T_DEFINED || !_MSC_VER
#endif // !PAL_STDCPP_COMPAT

