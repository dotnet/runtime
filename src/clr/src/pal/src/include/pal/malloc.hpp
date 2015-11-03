//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    pal/malloc.hpp

Abstract:
    Declarations for suspension safe memory allocation functions



--*/

#ifndef _MALLOC_HPP
#define _MALLOC_HPP

#include "pal/corunix.hpp"
#include "pal/thread.hpp"

#include <stdarg.h>

#if HAVE_ALLOCA_H
#include <alloca.h>
#endif  // HAVE_ALLOCA_H  

extern "C"
{
    void *
    __cdecl
    PAL_realloc(
        void* pvMemblock,
        size_t szSize
        );

    void *
    __cdecl
    PAL_malloc(
        size_t szSize
        );

    void
    __cdecl
    PAL_free(
        void *pvMem
        );

    char *
    __cdecl
    PAL__strdup(
        const char *c_szStr
        );
}

inline void* operator new(size_t, void* p) throw () { return p; }
inline void* operator new[](size_t, void* p) throw () { return p; }

namespace CorUnix{

    void *
    InternalRealloc(
        void *pvMemblock,
        size_t szSize
        );

    void *
    InternalMalloc(
        size_t szSize
        );

    void
    InternalFree(
        void *pvMem
        );

    char *
    InternalStrdup(
        const char *c_szStr
        );  

    // Define common code for "new" style allocators below.
#define INTERNAL_NEW_COMMON()                    \
        T *pMem = (T*)InternalMalloc(sizeof(T)); \
        if (pMem == NULL)                        \
            return NULL;

    // Define "new" style allocators (which allocate then call a constructor) for different numbers of
    // constructor arguments. Added based on usage.

    // Default constructor (0 args) case.
    template<class T>
    T* InternalNew()
    {
        INTERNAL_NEW_COMMON();
        return new (pMem) T();
    }

    // 2 args case.
    template<class T, class A1, class A2>
    T* InternalNew(A1 arg1, A2 arg2)
    {
        INTERNAL_NEW_COMMON();
        return new (pMem) T(arg1, arg2);
    }

    // 4 args case.
    template<class T, class A1, class A2, class A3, class A4>
    T* InternalNew(A1 arg1, A2 arg2, A3 arg3, A4 arg4)
    {
        INTERNAL_NEW_COMMON();
        return new (pMem) T(arg1, arg2, arg3, arg4);
    }

    // 5 args case.
    template<class T, class A1, class A2, class A3, class A4, class A5>
    T* InternalNew(A1 arg1, A2 arg2, A3 arg3, A4 arg4, A5 arg5)
    {
        INTERNAL_NEW_COMMON();
        return new (pMem) T(arg1, arg2, arg3, arg4, arg5);
    }

    template<class T> T* InternalNewArray(size_t cElements)
    {
        size_t cbSize = (cElements * sizeof(T)) + sizeof(size_t);
        T *pMem;

        pMem = (T*)InternalMalloc(cbSize);

        if (pMem == NULL)
            return NULL;

        *(size_t*)pMem = cElements;
        pMem = (T*)((size_t*)pMem + 1);

        return new (pMem) T[cElements]();
    }

    template<class T> void InternalDelete(T *p)
    {
        if (p)
        {
            p->~T();
            InternalFree(p);
        }
    }

    template<class T> void InternalDeleteArray(T *p)
    {
        if (p)
        {
            size_t *pRealMem = (size_t*)p - 1;
            size_t cElements = *pRealMem;
            for (size_t i = 0; i < cElements; i++)
                p[i].~T();
            InternalFree(pRealMem);
        }
    }
}

#endif // _MALLOC_HPP
