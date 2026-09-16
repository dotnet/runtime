// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "externalmemoryhandle.h"
#include "siginfo.hpp"

CrstStatic ExternalMemoryHandle::s_crst;
SListTail<ExternalMemoryHandle> ExternalMemoryHandle::s_handles;

#ifndef DACCESS_COMPILE

void ExternalMemoryHandle::Init()
{
    STANDARD_VM_CONTRACT;

    s_crst.Init(CrstExternalMemoryHandle, CrstFlags(CRST_UNSAFE_COOPGC | CRST_TAKEN_DURING_SHUTDOWN));
}

ExternalMemoryHandle* ExternalMemoryHandle::Add(PTR_MethodTable pMT, PTR_VOID pMemory, UINT gcFlags)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(pMT != nullptr);
    _ASSERTE(pMemory != nullptr);

    ExternalMemoryHandle* handle = new ExternalMemoryHandle(pMT, pMemory, gcFlags);

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

void ExternalMemoryHandle::Cleanup()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    CrstHolder lock(&s_crst);

    while (!s_handles.IsEmpty())
    {
        delete s_handles.RemoveHead();
    }
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
    for (ExternalMemoryHandle* handle = s_handles.GetHead(); handle != nullptr; handle = SListTail<ExternalMemoryHandle>::GetNext(handle))
    {
        handle->GCScanRoot(fn, sc);
    }
}

PTR_ExternalMemoryHandle ExternalMemoryHandle::GetHead()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return s_handles.GetHead();
}

void ExternalMemoryHandle::GCScanRoot(promote_func *fn, ScanContext *sc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    PTR_VOID fromAddress = m_pMemory;
    if (m_pMT->IsValueType())
    {
        ReportPointersFromValueType(fn, sc, m_pMT, m_pMemory);
    }
    else
    {
        if (m_gcFlags != 0)
        {
            _ASSERTE(m_gcFlags & GC_CALL_INTERIOR);
            PromoteCarefully(fn, (PTR_PTR_Object)m_pMemory, sc, m_gcFlags | CHECK_APP_DOMAIN);
        }
        else
        {
            (*fn)((PTR_PTR_Object)m_pMemory, sc, 0);
        }

        PTR_VOID toAddress = m_pMemory;
        LOG((LF_GC, INFO3, "External Memory Handle promoted" FMT_ADDR "to" FMT_ADDR "\n",
            DBG_ADDR(fromAddress), DBG_ADDR(toAddress)));
    }
}

#ifdef DACCESS_COMPILE

void ExternalMemoryHandle::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    m_pMT.EnumMem();
    m_pMT->EnumMemoryRegions(flags);

    TSIZE_T size = m_pMT->IsValueType() ? m_pMT->GetNumInstanceFieldBytes() : TARGET_POINTER_SIZE;
    DacEnumMemoryRegion(m_pMemory.GetAddr(), size);
}

#endif // DACCESS_COMPILE
