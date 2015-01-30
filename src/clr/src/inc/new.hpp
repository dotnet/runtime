//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
