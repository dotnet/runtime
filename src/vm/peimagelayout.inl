// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

#ifndef PEIMAGEVIEW_INL_
#define PEIMAGEVIEW_INL_

#include "util.hpp"
#include "peimage.h"

inline const SString &PEImageLayout::GetPath()
{
    LIMITED_METHOD_CONTRACT;
    return m_pOwner?m_pOwner->GetPath():SString::Empty();
}

inline void PEImageLayout::AddRef()
{
    CONTRACT_VOID
    {
        PRECONDITION(m_refCount>0 && m_refCount < COUNT_T_MAX);
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    FastInterlockIncrement(&m_refCount);

    RETURN;
}

inline ULONG PEImageLayout::Release()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

#ifdef DACCESS_COMPILE
    // when DAC accesses layouts via PEImage it does not addref
    if (m_pOwner)
        return m_refCount;
#endif

    ULONG result=FastInterlockDecrement(&m_refCount);
    if (result == 0 )
    {
        delete this;
    }
    return result;
}


inline PEImageLayout::~PEImageLayout()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
}

inline PEImageLayout::PEImageLayout()
    : m_refCount(1)
    , m_pOwner(NULL)
{
    LIMITED_METHOD_CONTRACT;
}

inline void PEImageLayout::Startup()
{
    CONTRACT_VOID
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckStartup());
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    if (CheckStartup())
        RETURN;

    RETURN;
}

inline CHECK PEImageLayout::CheckStartup()
{
    WRAPPER_NO_CONTRACT;
    CHECK_OK;
}

inline BOOL PEImageLayout::CompareBase(UPTR base, UPTR mapping)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer((PEImageLayout *)mapping));
        PRECONDITION(CheckPointer((PEImageLayout *)(base<<1),NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    if (base==NULL) //we were searching for 'Any'
        return TRUE;
    return ((PEImageLayout*)mapping)->GetBase()==((PEImageLayout*)(base<<1))->GetBase();
  
}

#endif //PEIMAGEVIEW_INL_
