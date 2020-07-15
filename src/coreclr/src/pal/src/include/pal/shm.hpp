// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

#define SHMPTR_TO_TYPED_PTR(type, shmptr) reinterpret_cast<type*>(shmptr)

/* Set ptr to NULL if shmPtr == 0, else set ptr to SHMPTR_TO_TYPED_PTR(type, shmptr)
   return FALSE if SHMPTR_TO_TYPED_PTR returns NULL ptr from non null shmptr,
   TRUE otherwise */
#define SHMPTR_TO_TYPED_PTR_BOOL(type, ptr, shmptr) \
    ((shmptr != 0) ? ((ptr = SHMPTR_TO_TYPED_PTR(type, shmptr)) != NULL) : ((ptr = NULL) == NULL))

#endif // _SHM_HPP_

