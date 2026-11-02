// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef EXTERNALMEMORYHANDLE_HPP
#define EXTERNALMEMORYHANDLE_HPP

#include "common.h"
#include "gcinterface.h"

class ExternalMemoryHandle;
typedef DPTR(ExternalMemoryHandle) PTR_ExternalMemoryHandle;

// Represents a handle to external memory that can be scanned by the garbage collector.
class ExternalMemoryHandle final
{
    friend struct cdac_data<ExternalMemoryHandle>;
    friend struct _DacGlobals;

public:
    // Create a reference to external memory. The memory should point to a value that represents PTR_MethodTable.
    // So for example, if pMT is a struct, pMemory is the value of the struct.
    // If pMT is a class type, pMemory is the PTR_Object pointing to the instance of the class on the GC heap.
    ExternalMemoryHandle(PTR_MethodTable pMT, PTR_VOID pMemory, UINT gcFlags)
        : m_pNext(nullptr), m_pMT(pMT), m_pMemory(pMemory), m_gcFlags(gcFlags)
    {
    }

    void GCScanRoot(promote_func *fn, ScanContext *sc);

    // Next pointer for SList linkage.
    PTR_ExternalMemoryHandle m_pNext;

#ifndef DACCESS_COMPILE
    static void Init();
    static ExternalMemoryHandle* Add(PTR_MethodTable pMT, PTR_VOID pMemory, UINT gcFlags);
    static void Remove(ExternalMemoryHandle* handle DEBUG_ARG(bool isEESuspended = false));
#endif
    static void GCScanRoots(promote_func *fn, ScanContext *sc);

private:
    PTR_MethodTable m_pMT;
    PTR_VOID m_pMemory;
    UINT m_gcFlags;

    static CrstStatic s_crst;
    SVAL_DECL(SListTail<ExternalMemoryHandle>, s_handles);
};

template<>
struct cdac_data<ExternalMemoryHandle>
{
    static constexpr size_t Next = offsetof(ExternalMemoryHandle, m_pNext);
    static constexpr size_t MethodTable = offsetof(ExternalMemoryHandle, m_pMT);
    static constexpr size_t Memory = offsetof(ExternalMemoryHandle, m_pMemory);
    static constexpr size_t GCFlags = offsetof(ExternalMemoryHandle, m_gcFlags);
#ifndef DACCESS_COMPILE
    // s_handles is exported to the classic DAC via SVAL_DECL, so under DACCESS_COMPILE it is wrapped
    // in __GlobalVal<T>, which does not support taking the address of a member. This is only used by
    // the (non-DAC) cDAC contract descriptor generator, so it is unneeded -- and would not compile -- when
    // DACCESS_COMPILE is defined.
    static constexpr PTR_ExternalMemoryHandle* HandlesHead = &ExternalMemoryHandle::s_handles.m_pHead;
#endif
};

#endif // EXTERNALMEMORYHANDLE_HPP
