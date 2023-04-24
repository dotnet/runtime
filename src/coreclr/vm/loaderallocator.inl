// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef _LOADER_ALLOCATOR_I
#define _LOADER_ALLOCATOR_I

#include "assembly.hpp"

#ifndef DACCESS_COMPILE
inline LOADERALLOCATORREF LoaderAllocator::GetExposedObject()
{
    LIMITED_METHOD_CONTRACT;
    OBJECTREF loaderAllocatorObject = (m_hLoaderAllocatorObjectHandle != NULL) ? ObjectFromHandle(m_hLoaderAllocatorObjectHandle) : NULL;
    return (LOADERALLOCATORREF)loaderAllocatorObject;
}
#endif

inline void GlobalLoaderAllocator::Init(BaseDomain *pDomain)
{
    LoaderAllocator::Init(pDomain, m_ExecutableHeapInstance);
}

inline BOOL LoaderAllocatorID::Equals(LoaderAllocatorID *pId)
{
    LIMITED_METHOD_CONTRACT;
    if (GetType() != pId->GetType())
        return false;

    return GetValue() == pId->GetValue();
}

inline void LoaderAllocatorID::Init()
{
    LIMITED_METHOD_CONTRACT;
    m_type = LAT_Assembly;
};

inline void LoaderAllocatorID::AddDomainAssembly(DomainAssembly* pAssembly)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_type == LAT_Assembly);

    // Link domain assembly together
    if (m_pDomainAssembly != NULL)
    {
        pAssembly->SetNextDomainAssemblyInSameALC(m_pDomainAssembly);
    }
    m_pDomainAssembly = pAssembly;
}

inline VOID* LoaderAllocatorID::GetValue()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_pValue;
}

inline COUNT_T LoaderAllocatorID::Hash()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return (COUNT_T)(SIZE_T)GetValue();
}

inline LoaderAllocatorType LoaderAllocatorID::GetType()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_type;
}

inline DomainAssemblyIterator LoaderAllocatorID::GetDomainAssemblyIterator()
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(m_type == LAT_Assembly);
    return DomainAssemblyIterator(m_pDomainAssembly);
}

inline LoaderAllocatorID* AssemblyLoaderAllocator::Id()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return &m_Id;
}

inline LoaderAllocatorID* GlobalLoaderAllocator::Id()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return &m_Id;
}

/* static */
FORCEINLINE BOOL LoaderAllocator::GetHandleValueFast(LOADERHANDLE handle, OBJECTREF *pValue)
{
    LIMITED_METHOD_CONTRACT;

    // If the slot value does have the low bit set, then it is a simple pointer to the value
    // Otherwise, we will need a more complicated operation to get the value.
    if ((((UINT_PTR)handle) & 1) != 0)
    {
        *pValue = *((OBJECTREF *)(((UINT_PTR)handle) - 1));
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}

FORCEINLINE BOOL LoaderAllocator::GetHandleValueFastPhase2(LOADERHANDLE handle, OBJECTREF *pValue)
{
    SUPPORTS_DAC;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    if (handle == 0)
        return FALSE;

    /* This is lockless access to the handle table, be careful */
    OBJECTREF loaderAllocatorAsObjectRef = ObjectFromHandle(m_hLoaderAllocatorObjectHandle);

    // If the managed loader allocator has been collected, then the handles associated with it are dead as well.
    if (loaderAllocatorAsObjectRef == NULL)
        return FALSE;

    LOADERALLOCATORREF loaderAllocator = dac_cast<LOADERALLOCATORREF>(loaderAllocatorAsObjectRef);
    PTRARRAYREF handleTable = loaderAllocator->GetHandleTable();
    UINT_PTR index = (((UINT_PTR)handle) >> 1) - 1;
    *pValue = handleTable->GetAt(index);

    return TRUE;
}

FORCEINLINE OBJECTREF LoaderAllocator::GetHandleValueFastCannotFailType2(LOADERHANDLE handle)
{
    SUPPORTS_DAC;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    /* This is lockless access to the handle table, be careful */
    OBJECTREF loaderAllocatorAsObjectRef = ObjectFromHandle(m_hLoaderAllocatorObjectHandle);
    LOADERALLOCATORREF loaderAllocator = dac_cast<LOADERALLOCATORREF>(loaderAllocatorAsObjectRef);
    PTRARRAYREF handleTable = loaderAllocator->GetHandleTable();
    UINT_PTR index = (((UINT_PTR)handle) >> 1) - 1;

    return handleTable->GetAt(index);
}

inline bool SegmentedHandleIndexStack::Push(DWORD value)
{
    LIMITED_METHOD_CONTRACT;

    if (m_TOSIndex == Segment::Size)
    {
        Segment* segment;

        if (m_freeSegment == NULL)
        {
            segment = new (nothrow) Segment();
            if (segment == NULL)
            {
                return false;
            }
        }
        else
        {
            segment = m_freeSegment;
            m_freeSegment = NULL;
        }

        segment->m_prev = m_TOSSegment;
        m_TOSSegment = segment;

        m_TOSIndex = 0;
    }

    m_TOSSegment->m_data[m_TOSIndex++] = value;
    return true;
}

inline DWORD SegmentedHandleIndexStack::Pop()
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(!IsEmpty());

    if (m_TOSIndex == 0)
    {
        Segment* prevSegment = m_TOSSegment->m_prev;
        _ASSERTE(prevSegment != NULL);

        delete m_freeSegment;
        m_freeSegment = m_TOSSegment;

        m_TOSSegment = prevSegment;
        m_TOSIndex = Segment::Size;
    }

    return m_TOSSegment->m_data[--m_TOSIndex];
}

inline bool SegmentedHandleIndexStack::IsEmpty()
{
    LIMITED_METHOD_CONTRACT;

    return (m_TOSSegment == NULL) || ((m_TOSIndex == 0) && (m_TOSSegment->m_prev == NULL));
}

#endif //  _LOADER_ALLOCATOR_I

