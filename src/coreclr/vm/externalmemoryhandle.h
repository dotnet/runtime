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
    // pMemory has the representation of a managed local declared with type.
    // Value types are inline, reference types are object-reference slots, and byrefs are pointer slots.
    ExternalMemoryHandle(TypeHandle type, PTR_VOID pMemory)
        : m_pNext(nullptr), m_type(type), m_pMemory(pMemory)
    {
    }

    void GCScanRoot(promote_func *fn, ScanContext *sc);

    // Next pointer for SList linkage.
    PTR_ExternalMemoryHandle m_pNext;

#ifndef DACCESS_COMPILE
    static void Init();
    static ExternalMemoryHandle* Add(TypeHandle type, PTR_VOID pMemory);
    static void Remove(ExternalMemoryHandle* handle DEBUG_ARG(bool isEESuspended = false));
#endif
    static void GCScanRoots(promote_func *fn, ScanContext *sc);

private:
    TypeHandle m_type;
    PTR_VOID m_pMemory;

    static CrstStatic s_crst;
    SVAL_DECL(SListTail<ExternalMemoryHandle>, s_handles);
};

template<>
struct cdac_data<ExternalMemoryHandle>
{
    static constexpr size_t Next = offsetof(ExternalMemoryHandle, m_pNext);
    static constexpr size_t TypeHandle = offsetof(ExternalMemoryHandle, m_type);
    static constexpr size_t Memory = offsetof(ExternalMemoryHandle, m_pMemory);
#ifndef DACCESS_COMPILE
    // s_handles is exported to the classic DAC via SVAL_DECL, so under DACCESS_COMPILE it is wrapped
    // in __GlobalVal<T>, which does not support taking the address of a member. This is only used by
    // the (non-DAC) cDAC contract descriptor generator, so it is unneeded -- and would not compile -- when
    // DACCESS_COMPILE is defined.
    static constexpr PTR_ExternalMemoryHandle* HandlesHead = &ExternalMemoryHandle::s_handles.m_pHead;
#endif
};

#endif // EXTERNALMEMORYHANDLE_HPP
