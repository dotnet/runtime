// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    include/pal/shm.hpp

Abstract:
    C++ typesafe accessors for shared memory routines



--*/

#ifndef _SHM_HPP_
#define _SHM_HPP_

#include "shmemory.h"

//
// Some compilers (e.g., HPUX/IA64) warn about using NULL to initialize
// something of type SHMPTR, since SHMPTR is defined as DWORD_PTR, which
// isn't considered a pointer type...
//

#define SHMNULL 0

#ifndef _DEBUG

inline
void *
ShmPtrToPtrFast(SHMPTR shmptr)
{
    void *pv = NULL;
    
    if (SHMNULL != shmptr)
    {
        int segment = shmptr >> 24;

        if (segment < shm_numsegments)
        {
            pv = reinterpret_cast<void*>(
                reinterpret_cast<DWORD_PTR>(shm_segment_bases[(uint)segment].Load())
                + (shmptr & 0x00FFFFFF)
                );
        }
        else
        {
            pv = SHMPtrToPtr(shmptr);
        }
    }

    return pv;
}

//
// We could use a function template here to avoid the cast / macro
//

#define SHMPTR_TO_TYPED_PTR(type, shmptr) reinterpret_cast<type*>(ShmPtrToPtrFast((shmptr)))

#else

#define SHMPTR_TO_TYPED_PTR(type, shmptr) reinterpret_cast<type*>(SHMPtrToPtr((shmptr)))

#endif

/* Set ptr to NULL if shmPtr == 0, else set ptr to SHMPTR_TO_TYPED_PTR(type, shmptr) 
   return FALSE if SHMPTR_TO_TYPED_PTR returns NULL ptr from non null shmptr, 
   TRUE otherwise */
#define SHMPTR_TO_TYPED_PTR_BOOL(type, ptr, shmptr) \
    ((shmptr != 0) ? ((ptr = SHMPTR_TO_TYPED_PTR(type, shmptr)) != NULL) : ((ptr = NULL) == NULL))




#endif // _SHM_HPP_

