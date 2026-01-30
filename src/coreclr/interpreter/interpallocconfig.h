// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTERPALLOCCONFIG_H_
#define _INTERPALLOCCONFIG_H_

#include "../jitshared/allocconfig.h"
#include "failures.h"

#include <cstdlib>
#include <cstring>

// InterpAllocatorConfig implements IAllocatorConfig for the interpreter.
// It uses direct malloc/free for memory allocation since the interpreter
// doesn't have a JIT host.

class InterpAllocatorConfig : public IAllocatorConfig
{
public:
    InterpAllocatorConfig() = default;

    // The interpreter always uses direct malloc/free, so this returns false.
    // In DEBUG builds, this could be controlled by a configuration setting.
    virtual bool bypassHostAllocator() override
    {
        return false; // We always use malloc, so no "bypass" concept applies
    }

    // The interpreter doesn't currently support fault injection.
    virtual bool shouldInjectFault() override
    {
        return false;
    }

    // Allocates a block of memory using malloc.
    virtual void* allocateHostMemory(size_t size, size_t* pActualSize) override
    {
        if (pActualSize != nullptr)
        {
            *pActualSize = size;
        }
        if (size == 0)
        {
            size = 1;
        }
        void* p = malloc(size);
        if (p == nullptr)
        {
            NOMEM();
        }
        return p;
    }

    // Frees a block of memory previously allocated by allocateHostMemory.
    virtual void freeHostMemory(void* block, size_t size) override
    {
        (void)size; // unused
        free(block);
    }

    // Fills a memory block with an uninitialized pattern for DEBUG builds.
    virtual void fillWithUninitializedPattern(void* block, size_t size) override
    {
#if defined(DEBUG)
        // Use 0xCD pattern (same as MSVC debug heap) to help catch use-before-init bugs
        memset(block, 0xCD, size);
#else
        (void)block;
        (void)size;
#endif
    }
};

#endif // _INTERPALLOCCONFIG_H_
