// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __JITHOST_H__
#define __JITHOST_H__

// Common implementation of ICorJitHost that respects CLR host policies.
class JitHost : public ICorJitHost
{
private:
    static JitHost theJitHost;

    JitHost() {}
    JitHost(const JitHost& other) = delete;
    JitHost& operator=(const JitHost& other) = delete;

public:
    virtual void* allocateMemory(size_t size, bool usePageAllocator);
    virtual void freeMemory(void* block, bool usePageAllocator);
    virtual int getIntConfigValue(const wchar_t* name, int defaultValue);
    virtual const wchar_t* getStringConfigValue(const wchar_t* name);
    virtual void freeStringConfigValue(const wchar_t* value);

    static ICorJitHost* getJitHost();
};

#endif // __JITHOST_H__
