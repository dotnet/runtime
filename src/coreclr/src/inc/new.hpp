// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//

#ifndef __new__hpp
#define __new__hpp

#if defined(_MSC_VER) && _MSC_VER < 1900
#define NOEXCEPT
#else
#define NOEXCEPT noexcept
#endif

struct NoThrow { int x; };
extern const NoThrow nothrow;

void * __cdecl operator new(size_t n, const NoThrow&) NOEXCEPT;
void * __cdecl operator new[](size_t n, const NoThrow&) NOEXCEPT;

#ifdef _DEBUG
void DisableThrowCheck();
#endif

#endif
