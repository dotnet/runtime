// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _ICorJitHostImpl
#define _ICorJitHostImpl

// ICorJitHost
//
// ICorJitHost provides the interface that the JIT uses to access some functionality that
// would normally be provided by the operating system. This is intended to allow for
// host-specific policies re: memory allocation, configuration value access, etc. It is
// expected that the `ICorJitHost` value provided to `jitStartup` lives at least as
// long as the JIT itself.

// ICorJitHostImpl: declare for implementation all the members of the ICorJitHost interface (which are
// specified as pure virtual methods). This is done once, here, and all implementations share it,
// to avoid duplicated declarations. This file is #include'd within all the ICorJitHost implementation
// classes.
//
// NOTE: this file is in exactly the same order, with exactly the same whitespace, as the ICorJitHost
// interface declaration (with the "virtual" and "= 0" syntax removed). This is to make it easy to compare
// against the interface declaration.

public:
    // Allocate memory of the given size in bytes. All bytes of the returned block
    // must be initialized to zero. If `usePageAllocator` is true, the implementation
    // should use an allocator that deals in OS pages if one exists.
    void* allocateMemory(size_t size, bool usePageAllocator = false);

    // Frees memory previous obtained by a call to `ICorJitHost::allocateMemory`. The
    // value of the `usePageAllocator` parameter must match the value that was
    // provided to the call to used to allocate the memory.
    void freeMemory(void* block, bool usePageAllocator = false);

    // Return an integer config value for the given key, if any exists.
    int getIntConfigValue(
        const wchar_t* name, 
        int defaultValue
        );

    // Return a string config value for the given key, if any exists.
    const wchar_t* getStringConfigValue(
        const wchar_t* name
        );

    // Free a string ConfigValue returned by the runtime.
    // JITs using the getStringConfigValue query are required
    // to return the string values to the runtime for deletion.
    // This avoids leaking the memory in the JIT.
    void freeStringConfigValue(
        const wchar_t* value
        );

#endif
