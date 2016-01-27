// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


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

inline void AppDomainLoaderAllocator::Init(AppDomain *pAppDomain) 
{
    WRAPPER_NO_CONTRACT;
    m_Id.Init(pAppDomain);
    LoaderAllocator::Init((BaseDomain *)pAppDomain);
}

inline void LoaderAllocatorID::Init(AppDomain *pAppDomain)
{
    m_type = LAT_AppDomain;
    m_pAppDomain = pAppDomain;
}

inline void AssemblyLoaderAllocator::Init(AppDomain* pAppDomain)
{
    m_Id.Init();
    LoaderAllocator::Init((BaseDomain *)pAppDomain);
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

inline void LoaderAllocatorID::SetDomainAssembly(DomainAssembly* pAssembly)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_type == LAT_Assembly);
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

inline DomainAssembly* LoaderAllocatorID::GetDomainAssembly()
{
	LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(m_type == LAT_Assembly);
    return m_pDomainAssembly;
}

inline AppDomain *LoaderAllocatorID::GetAppDomain()
{
	LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(m_type == LAT_AppDomain);
    return m_pAppDomain;
}

inline BOOL LoaderAllocatorID::IsCollectible()
{
    LIMITED_METHOD_DAC_CONTRACT; 
    return m_type == LAT_Assembly;
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

inline LoaderAllocatorID* AppDomainLoaderAllocator::Id()
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
#endif //  _LOADER_ALLOCATOR_I

