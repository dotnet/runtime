// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "externalmemoryhandle.h"
#include "siginfo.hpp"

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
