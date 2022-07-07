// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// We would like to allow "util" collection classes to be usable both
// from the VM and from the JIT.  The latter case presents a
// difficulty, because in the (x86, soon to be cross-platform) JIT
// compiler, we require allocation to be done using a "no-release"
// (aka, arena-style) allocator that is provided as methods of the
// JIT's Compiler type.

// To allow utilcode collection classes to deal with this, they may be
// written to do allocation and freeing via an instance of the
// "IAllocator" class defined in this file.
//
#ifndef _IALLOCATOR_DEFINED_
#define _IALLOCATOR_DEFINED_

#include "contract.h"
#include "safemath.h"

class IAllocator
{
  public:
    virtual void* Alloc(size_t sz) = 0;

    // Allocate space for an array of "elems" elements, each of size "elemSize".
    virtual void* ArrayAlloc(size_t elems, size_t elemSize) = 0;

    virtual void  Free(void* p) = 0;
};

#endif // _IALLOCATOR_DEFINED_
