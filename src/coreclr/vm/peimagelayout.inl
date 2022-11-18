// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#ifndef PEIMAGEVIEW_INL_
#define PEIMAGEVIEW_INL_

#include "util.hpp"
#include "peimage.h"

inline void PEImageLayout::AddRef()
{
    CONTRACT_VOID
    {
        PRECONDITION(m_refCount>0 && m_refCount < COUNT_T_MAX);
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    InterlockedIncrement(&m_refCount);

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

    ULONG result=InterlockedDecrement(&m_refCount);
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
