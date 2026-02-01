// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _JITALLOCCONFIG_H_
#define _JITALLOCCONFIG_H_

#include "../jitshared/allocconfig.h"

// Forward declarations
class ICorJitHost;
extern ICorJitHost* g_jitHost;

// JitAllocatorConfig implements IAllocatorConfig for the JIT compiler.
// It uses the JIT host (g_jitHost) for memory allocation and supports
// JIT-specific debugging features like direct allocation bypass and
// fault injection.

class JitAllocatorConfig : public IAllocatorConfig
{
public:
    JitAllocatorConfig() = default;

    // Returns true if the JIT should bypass the JIT host and use direct malloc/free.
    // This is controlled by JitConfig.JitDirectAlloc() in DEBUG builds.
    virtual bool bypassHostAllocator() override;

    // Returns true if the JIT should inject faults for testing.
    // This is controlled by JitConfig.ShouldInjectFault() in DEBUG builds.
    virtual bool shouldInjectFault() override;

    // Allocates a block of memory from the JIT host.
    virtual void* allocateHostMemory(size_t size, size_t* pActualSize) override;

    // Frees a block of memory previously allocated by allocateHostMemory.
    virtual void freeHostMemory(void* block, size_t size) override;

    // Fills a memory block with the uninitialized pattern for DEBUG builds.
    virtual void fillWithUninitializedPattern(void* block, size_t size) override;

    // Called when allocation fails - calls NOMEM() which does not return.
    virtual void outOfMemory() override;
};

// Global JIT allocator configuration instance.
// This is used by all ArenaAllocators created by the JIT.
extern JitAllocatorConfig g_jitAllocatorConfig;

#endif // _JITALLOCCONFIG_H_
