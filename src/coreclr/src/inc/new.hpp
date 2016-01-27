// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//

#ifndef __new__hpp
#define __new__hpp

struct NoThrow { int x; };
extern const NoThrow nothrow;

void * __cdecl operator new(size_t n, const NoThrow&);
void * __cdecl operator new[](size_t n, const NoThrow&);

#ifdef _DEBUG
void DisableThrowCheck();
#endif

#endif
