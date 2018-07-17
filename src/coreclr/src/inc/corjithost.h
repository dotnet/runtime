// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __CORJITHOST_H__
#define __CORJITHOST_H__

// ICorJitHost
//
// ICorJitHost provides the interface that the JIT uses to access some functionality that
// would normally be provided by the operating system. This is intended to allow for
// host-specific policies re: memory allocation, configuration value access, etc. It is
// expected that the `ICorJitHost` value provided to `jitStartup` lives at least as
// long as the JIT itself.
class ICorJitHost
{
public:
    // Allocate memory of the given size in bytes.
    virtual void* allocateMemory(size_t size) = 0;

    // Frees memory previous obtained by a call to `ICorJitHost::allocateMemory`.
    virtual void freeMemory(void* block) = 0;

    // Return an integer config value for the given key, if any exists.
    virtual int getIntConfigValue(
        const wchar_t* name, 
        int defaultValue
        ) = 0;

    // Return a string config value for the given key, if any exists.
    virtual const wchar_t* getStringConfigValue(
        const wchar_t* name
        ) = 0;

    // Free a string ConfigValue returned by the runtime.
    // JITs using the getStringConfigValue query are required
    // to return the string values to the runtime for deletion.
    // This avoids leaking the memory in the JIT.
    virtual void freeStringConfigValue(
        const wchar_t* value
        ) = 0;

    // Allocate memory slab of the given size in bytes. The host is expected to pool
    // these for a good performance.
    virtual void* allocateSlab(size_t size, size_t* pActualSize)
    {
        *pActualSize = size;
        return allocateMemory(size);
    }

    // Free memory slab of the given size in bytes.
    virtual void freeSlab(void* slab, size_t actualSize)
    {
        freeMemory(slab);
    }
};

#endif
