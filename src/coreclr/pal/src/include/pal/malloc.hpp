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
    // Define "new" style allocators (which allocate then call a constructor).
    template<class T, class... Ts>
    T* InternalNew(Ts... args)
    {
        T* pMem = (T*)malloc(sizeof(T));

        if (pMem == NULL)
            return NULL;

        return new (pMem) T(args...);
    }

    template<class T> T* InternalNewArray(size_t cElements)
    {
        size_t cbSize = (cElements * sizeof(T)) + sizeof(size_t);
        T *pMem;

        pMem = (T*)malloc(cbSize);

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
