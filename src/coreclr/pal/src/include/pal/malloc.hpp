// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#include <stdlib.h>
#include <new>

namespace CorUnix{
    inline void *
    InternalMalloc(
        size_t szSize
        )
    {
        void *pvMem;

        if (szSize == 0)
        {
            // malloc may return null for a requested size of zero bytes. Force a nonzero size to get a valid pointer.
            szSize = 1;
        }

        pvMem = (void*)malloc(szSize);
        return pvMem;
    }

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

    // 1 arg case.
    template<class T, class A1>
    T* InternalNew(A1 arg1)
    {
        INTERNAL_NEW_COMMON();
        return new (pMem) T(arg1);
    }

    // 2 args case.
    template<class T, class A1, class A2>
    T* InternalNew(A1 arg1, A2 arg2)
    {
        INTERNAL_NEW_COMMON();
        return new (pMem) T(arg1, arg2);
    }

    // 3 args case.
    template<class T, class A1, class A2, class A3>
    T* InternalNew(A1 arg1, A2 arg2, A3 arg3)
    {
        INTERNAL_NEW_COMMON();
        return new (pMem) T(arg1, arg2, arg3);
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
            free(p);
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
            free(pRealMem);
        }
    }
}

#endif // _MALLOC_HPP
