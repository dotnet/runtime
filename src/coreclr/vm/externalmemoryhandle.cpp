// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "externalmemoryhandle.h"
#include "siginfo.hpp"

CrstStatic ExternalMemoryHandle::s_crst;
SVAL_IMPL(SListTail<ExternalMemoryHandle>, ExternalMemoryHandle, s_handles);

#ifndef DACCESS_COMPILE

void ExternalMemoryHandle::Init()
{
    STANDARD_VM_CONTRACT;

    s_crst.Init(CrstExternalMemoryHandle, CrstFlags(CRST_UNSAFE_COOPGC | CRST_TAKEN_DURING_SHUTDOWN));
}

ExternalMemoryHandle* ExternalMemoryHandle::Add(TypeHandle type, PTR_VOID pMemory)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(!type.IsNull());
    _ASSERTE(pMemory != nullptr);

    ExternalMemoryHandle* handle = new ExternalMemoryHandle(type, pMemory);

    {
        CrstHolder lock(&s_crst);
        s_handles.InsertTail(handle);
    }

    return handle;
}

void ExternalMemoryHandle::Remove(ExternalMemoryHandle* handle DEBUG_ARG(bool isEESuspended))
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(handle != nullptr);
    _ASSERTE(isEESuspended || (GetThreadNULLOk() != nullptr && GetThread()->PreemptiveGCDisabled()));

    bool removed;
    {
        CrstHolder lock(&s_crst);
        removed = s_handles.RemoveFirst(handle);
    }

    _ASSERTE(removed);

    delete handle;
}

#endif // !DACCESS_COMPILE

void ExternalMemoryHandle::GCScanRoots(promote_func *fn, ScanContext *sc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // The caller (GCToEEInterface::GcScanRoots) only invokes this outside the concurrent mark phase of
    // a background GC, so the EE is always suspended for a GC (or, for the DAC, the target process is
    // stopped) whenever this list is walked, and the list cannot be mutated concurrently with this scan.
    // s_handles is read through the DAC global table (SVAL_DECL), so materialize a local copy of the
    // list head/tail before traversing -- the DAC __GlobalVal wrapper does not proxy member calls.
    SListTail<ExternalMemoryHandle> handles = s_handles;
    for (ExternalMemoryHandle* handle = handles.GetHead(); handle != nullptr; handle = SListTail<ExternalMemoryHandle>::GetNext(handle))
    {
        handle->GCScanRoot(fn, sc);
    }
}

void ExternalMemoryHandle::GCScanRoot(promote_func *fn, ScanContext *sc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE
    if (sc->promotion)
    {
        // Native storage has no object header to preserve its type's loader allocator.
        GcReportLoaderAllocator(fn, sc, m_type.GetLoaderAllocator());
    }
#endif // !DACCESS_COMPILE

    PTR_VOID fromAddress = m_pMemory;
    if (m_type.IsByRef())
    {
        PromoteCarefully(fn, (PTR_PTR_Object)m_pMemory, sc, GC_CALL_INTERIOR | CHECK_APP_DOMAIN);
    }
    else if (m_type.IsValueType())
    {
        ReportPointersFromValueType(fn, sc, m_type.GetMethodTable(), m_pMemory);
    }
    else if (!m_type.IsTypeDesc())
    {
        (*fn)((PTR_PTR_Object)m_pMemory, sc, 0);
    }
    else
    {
        _ASSERTE(m_type.IsPointer() || m_type.GetSignatureCorElementType() == ELEMENT_TYPE_FNPTR);
    }

    PTR_VOID toAddress = m_pMemory;
    LOG((LF_GC, INFO3, "External Memory Handle promoted" FMT_ADDR "to" FMT_ADDR "\n",
        DBG_ADDR(fromAddress), DBG_ADDR(toAddress)));
}
