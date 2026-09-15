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

public:
    // Create a reference to external memory. The memory should point to a value that represents PTR_MethodTable.
    // So for example, if pMT is a struct, pMemory is the value of the struct.
    // If pMT is a class type, pMemory is the PTR_Object pointing to the instance of the class on the GC heap.
    ExternalMemoryHandle(PTR_MethodTable pMT, PTR_VOID pMemory, UINT gcFlags)
        : m_pNext(nullptr), m_pMT(pMT), m_pMemory(pMemory), m_gcFlags(gcFlags)
    {
    }

    void GCScanRoot(promote_func *fn, ScanContext *sc);

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    // Next pointer for SList linkage.
    PTR_ExternalMemoryHandle m_pNext;

private:
    PTR_MethodTable m_pMT;
    PTR_VOID m_pMemory;
    UINT m_gcFlags;
};

template<>
struct cdac_data<ExternalMemoryHandle>
{
    static constexpr size_t Next = offsetof(ExternalMemoryHandle, m_pNext);
    static constexpr size_t MethodTable = offsetof(ExternalMemoryHandle, m_pMT);
    static constexpr size_t Memory = offsetof(ExternalMemoryHandle, m_pMemory);
    static constexpr size_t GCFlags = offsetof(ExternalMemoryHandle, m_gcFlags);
};

#endif // EXTERNALMEMORYHANDLE_HPP
