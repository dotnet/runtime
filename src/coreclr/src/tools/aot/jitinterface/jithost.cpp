// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>

#include "dllexport.h"

class JitConfigProvider
{
public:
    virtual int STDMETHODCALLTYPE getIntConfigValue(
        const wchar_t* name, 
        int defaultValue
        ) = 0;

    virtual int STDMETHODCALLTYPE getStringConfigValue(
        const wchar_t* name,
        wchar_t* retBuffer,
        int retBufferLength
        ) = 0;
};

// Native implementation of the JIT host.
// The native implementation calls into JitConfigProvider (implemented on the managed side) to get the actual
// configuration values.
// This dance is necessary because RyuJIT calls into the JitHost as part of the process shutdown (to free up
// strings). JitHost therefore can't be implemented in managed code (because managed runtime might have
// already shut down).

class JitHost
{
    JitConfigProvider* pConfigProvider;

public:
    JitHost(JitConfigProvider* pConfigProvider)
        : pConfigProvider(pConfigProvider) { }

    virtual void* allocateMemory(size_t size)
    {
        return malloc(size);
    }

    virtual void freeMemory(void* block)
    {
        free(block);
    }

    virtual int getIntConfigValue(
        const wchar_t* name, 
        int defaultValue
        )
    {
        return pConfigProvider->getIntConfigValue(name, defaultValue);
    }

    virtual const wchar_t* getStringConfigValue(
        const wchar_t* name
        )
    {
        // Find out the required length of the buffer
        int numCharacters = pConfigProvider->getStringConfigValue(name, nullptr, 0);
        if (numCharacters == 0)
            return nullptr;

        // Allocate extra char for the null terminator
        wchar_t* retBuffer = (wchar_t*)calloc(numCharacters + 1, sizeof(wchar_t));
        pConfigProvider->getStringConfigValue(name, retBuffer, numCharacters);

        return retBuffer;
    }

    virtual void freeStringConfigValue(
        wchar_t* value
        )
    {
        free(value);
    }

    virtual void* allocateSlab(size_t size, size_t* pActualSize)
    {
        *pActualSize = size;
        return allocateMemory(size);
    }

    virtual void freeSlab(void* slab, size_t actualSize)
    {
        freeMemory(slab);
    }
};

DLL_EXPORT void* GetJitHost(JitConfigProvider* pConfigProvider)
{
    return new JitHost(pConfigProvider);
}
