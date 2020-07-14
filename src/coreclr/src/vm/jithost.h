// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __JITHOST_H__
#define __JITHOST_H__

// Common implementation of ICorJitHost that respects CLR host policies.
class JitHost : public ICorJitHost
{
private:
    static JitHost s_theJitHost;

    struct Slab
    {
        Slab * pNext;
        size_t size;
        Thread* affinity;
    };

    CrstStatic m_jitSlabAllocatorCrst;
    Slab* m_pCurrentCachedList;
    Slab* m_pPreviousCachedList;
    size_t m_totalCached;
    DWORD m_lastFlush;

    JitHost() {}
    JitHost(const JitHost& other) = delete;
    JitHost& operator=(const JitHost& other) = delete;

    void init();
    void reclaim();

public:
    virtual void* allocateMemory(size_t size);
    virtual void freeMemory(void* block);
    virtual int getIntConfigValue(const WCHAR* name, int defaultValue);
    virtual const WCHAR* getStringConfigValue(const WCHAR* name);
    virtual void freeStringConfigValue(const WCHAR* value);
    virtual void* allocateSlab(size_t size, size_t* pActualSize);
    virtual void freeSlab(void* slab, size_t actualSize);

    static void Init() { s_theJitHost.init(); }
    static void Reclaim() { s_theJitHost.reclaim(); }

    static ICorJitHost* getJitHost() { return &s_theJitHost; }
};

#endif // __JITHOST_H__
