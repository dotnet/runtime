// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "rhassert.h"
#include "slist.h"
#include "holder.h"
#include "Crst.h"
#include "rhbinder.h"
#include "RWLock.h"
#include "RuntimeInstance.h"
#include "event.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "gcrhinterface.h"
#include "shash.h"
#include "TypeManager.h"
#include "MethodTable.h"
#include "varint.h"

#include "CommonMacros.inl"
#include "slist.inl"
#include "MethodTable.inl"

#ifdef  FEATURE_GC_STRESS
enum HijackType { htLoop, htCallsite };
bool ShouldHijackForGcStress(uintptr_t CallsiteIP, HijackType ht);
#endif // FEATURE_GC_STRESS

#include "shash.inl"

#ifndef DACCESS_COMPILE
COOP_PINVOKE_HELPER(uint8_t *, RhSetErrorInfoBuffer, (uint8_t * pNewBuffer))
{
    return (uint8_t *) PalSetWerDataBuffer(pNewBuffer);
}
#endif // DACCESS_COMPILE


ThreadStore *   RuntimeInstance::GetThreadStore()
{
    return m_pThreadStore;
}

COOP_PINVOKE_HELPER(uint8_t *, RhFindMethodStartAddress, (void * codeAddr))
{
    return dac_cast<uint8_t *>(GetRuntimeInstance()->FindMethodStartAddress(dac_cast<PTR_VOID>(codeAddr)));
}

PTR_UInt8 RuntimeInstance::FindMethodStartAddress(PTR_VOID ControlPC)
{
    ICodeManager * pCodeManager = FindCodeManagerByAddress(ControlPC);
    MethodInfo methodInfo;
    if (pCodeManager != NULL && pCodeManager->FindMethodInfo(ControlPC, &methodInfo))
    {
        return (PTR_UInt8)pCodeManager->GetMethodStartAddress(&methodInfo);
    }

    return NULL;
}

ICodeManager * RuntimeInstance::FindCodeManagerByAddress(PTR_VOID pvAddress)
{
    ReaderWriterLock::ReadHolder read(&m_ModuleListLock);

    // TODO: ICodeManager support in DAC
#ifndef DACCESS_COMPILE
    for (CodeManagerEntry * pEntry = m_CodeManagerList.GetHead(); pEntry != NULL; pEntry = pEntry->m_pNext)
    {
        if (dac_cast<TADDR>(pvAddress) - dac_cast<TADDR>(pEntry->m_pvStartRange) < pEntry->m_cbRange)
            return pEntry->m_pCodeManager;
    }
#endif

    return NULL;
}

#ifndef DACCESS_COMPILE

// Find the code manager containing the given address, which might be a return address from a managed function. The
// address may be to another managed function, or it may be to an unmanaged function. The address may also refer to
// an MethodTable.
ICodeManager * RuntimeInstance::FindCodeManagerForClasslibFunction(PTR_VOID address)
{
    // Try looking up the code manager assuming the address is for code first. This is expected to be most common.
    ICodeManager * pCodeManager = FindCodeManagerByAddress(address);
    if (pCodeManager != NULL)
        return pCodeManager;

    ASSERT_MSG(!Thread::IsHijackTarget(address), "not expected to be called with hijacked return address");

    return NULL;
}

void * RuntimeInstance::GetClasslibFunctionFromCodeAddress(PTR_VOID address, ClasslibFunctionId functionId)
{
    // Find the code manager for the given address, which is an address into some managed module. It could
    // be code, or it could be an MethodTable. No matter what, it's an address into a managed module in some non-Rtm
    // type system.
    ICodeManager * pCodeManager = FindCodeManagerForClasslibFunction(address);

    // If the address isn't in a managed module then we have no classlib function.
    if (pCodeManager == NULL)
    {
        return NULL;
    }

    return pCodeManager->GetClasslibFunction(functionId);
}

#endif // DACCESS_COMPILE

PTR_UInt8 RuntimeInstance::GetTargetOfUnboxingAndInstantiatingStub(PTR_VOID ControlPC)
{
    ICodeManager * pCodeManager = FindCodeManagerByAddress(ControlPC);
    if (pCodeManager != NULL)
    {
        PTR_UInt8 pData = (PTR_UInt8)pCodeManager->GetAssociatedData(ControlPC);
        if (pData != NULL)
        {
            uint8_t flags = *pData++;

            if ((flags & (uint8_t)AssociatedDataFlags::HasUnboxingStubTarget) != 0)
                return pData + *dac_cast<PTR_Int32>(pData);
        }
    }

    return NULL;
}

GPTR_IMPL_INIT(RuntimeInstance, g_pTheRuntimeInstance, NULL);

PTR_RuntimeInstance GetRuntimeInstance()
{
    return g_pTheRuntimeInstance;
}

void RuntimeInstance::EnumAllStaticGCRefs(void * pfnCallback, void * pvCallbackData)
{
    for (TypeManagerList::Iterator iter = m_TypeManagerList.Begin(); iter != m_TypeManagerList.End(); iter++)
    {
        iter->m_pTypeManager->EnumStaticGCRefs(pfnCallback, pvCallbackData);
    }
}

void RuntimeInstance::SetLoopHijackFlags(uint32_t flag)
{
    for (TypeManagerList::Iterator iter = m_TypeManagerList.Begin(); iter != m_TypeManagerList.End(); iter++)
    {
        iter->m_pTypeManager->SetLoopHijackFlag(flag);
    }
}

RuntimeInstance::OsModuleList* RuntimeInstance::GetOsModuleList()
{
    return dac_cast<DPTR(OsModuleList)>(dac_cast<TADDR>(this) + offsetof(RuntimeInstance, m_OsModuleList));
}

ReaderWriterLock& RuntimeInstance::GetTypeManagerLock()
{
    return m_ModuleListLock;
}

#ifndef DACCESS_COMPILE

RuntimeInstance::RuntimeInstance() :
    m_pThreadStore(NULL),
    m_conservativeStackReportingEnabled(false),
    m_pUnboxingStubsRegion(NULL)
{
}

RuntimeInstance::~RuntimeInstance()
{
    if (NULL != m_pThreadStore)
    {
        delete m_pThreadStore;
        m_pThreadStore = NULL;
    }
}

HANDLE  RuntimeInstance::GetPalInstance()
{
    return m_hPalInstance;
}

void RuntimeInstance::EnableConservativeStackReporting()
{
    m_conservativeStackReportingEnabled = true;
}

bool RuntimeInstance::RegisterCodeManager(ICodeManager * pCodeManager, PTR_VOID pvStartRange, uint32_t cbRange)
{
    CodeManagerEntry * pEntry = new (nothrow) CodeManagerEntry();
    if (NULL == pEntry)
        return false;

    pEntry->m_pvStartRange = pvStartRange;
    pEntry->m_cbRange = cbRange;
    pEntry->m_pCodeManager = pCodeManager;

    {
        ReaderWriterLock::WriteHolder write(&m_ModuleListLock);

        m_CodeManagerList.PushHead(pEntry);
    }

    return true;
}

void RuntimeInstance::UnregisterCodeManager(ICodeManager * pCodeManager)
{
    CodeManagerEntry * pEntry = NULL;

    {
        ReaderWriterLock::WriteHolder write(&m_ModuleListLock);

        for (CodeManagerList::Iterator i = m_CodeManagerList.Begin(), end = m_CodeManagerList.End(); i != end; i++)
        {
            if (i->m_pCodeManager == pCodeManager)
            {
                pEntry = *i;

                m_CodeManagerList.Remove(i);
                break;
            }
        }
    }

    ASSERT(pEntry != NULL);
    delete pEntry;
}

extern "C" bool __stdcall RegisterCodeManager(ICodeManager * pCodeManager, PTR_VOID pvStartRange, uint32_t cbRange)
{
    return GetRuntimeInstance()->RegisterCodeManager(pCodeManager, pvStartRange, cbRange);
}

extern "C" void __stdcall UnregisterCodeManager(ICodeManager * pCodeManager)
{
    return GetRuntimeInstance()->UnregisterCodeManager(pCodeManager);
}

bool RuntimeInstance::RegisterUnboxingStubs(PTR_VOID pvStartRange, uint32_t cbRange)
{
    ASSERT(pvStartRange != NULL && cbRange > 0);

    UnboxingStubsRegion * pEntry = new (nothrow) UnboxingStubsRegion();
    if (NULL == pEntry)
        return false;

    pEntry->m_pRegionStart = pvStartRange;
    pEntry->m_cbRegion = cbRange;

    do
    {
        pEntry->m_pNextRegion = m_pUnboxingStubsRegion;
    }
    while (PalInterlockedCompareExchangePointer((void *volatile *)&m_pUnboxingStubsRegion, pEntry, pEntry->m_pNextRegion) != pEntry->m_pNextRegion);

    return true;
}

bool RuntimeInstance::IsUnboxingStub(uint8_t* pCode)
{
    UnboxingStubsRegion * pCurrent = m_pUnboxingStubsRegion;
    while (pCurrent != NULL)
    {
        uint8_t* pUnboxingStubsRegion = dac_cast<uint8_t*>(pCurrent->m_pRegionStart);
        if (pCode >= pUnboxingStubsRegion && pCode < (pUnboxingStubsRegion + pCurrent->m_cbRegion))
            return true;

        pCurrent = pCurrent->m_pNextRegion;
    }

    return false;
}

extern "C" bool __stdcall RegisterUnboxingStubs(PTR_VOID pvStartRange, uint32_t cbRange)
{
    return GetRuntimeInstance()->RegisterUnboxingStubs(pvStartRange, cbRange);
}

bool RuntimeInstance::RegisterTypeManager(TypeManager * pTypeManager)
{
    TypeManagerEntry * pEntry = new (nothrow) TypeManagerEntry();
    if (NULL == pEntry)
        return false;

    pEntry->m_pTypeManager = pTypeManager;

    {
        ReaderWriterLock::WriteHolder write(&m_ModuleListLock);

        m_TypeManagerList.PushHead(pEntry);
    }

    return true;
}

COOP_PINVOKE_HELPER(TypeManagerHandle, RhpCreateTypeManager, (HANDLE osModule, void* pModuleHeader, PTR_PTR_VOID pClasslibFunctions, uint32_t nClasslibFunctions))
{
    TypeManager * typeManager = TypeManager::Create(osModule, pModuleHeader, pClasslibFunctions, nClasslibFunctions);
    GetRuntimeInstance()->RegisterTypeManager(typeManager);

    return TypeManagerHandle::Create(typeManager);
}

COOP_PINVOKE_HELPER(void*, RhpRegisterOsModule, (HANDLE hOsModule))
{
    RuntimeInstance::OsModuleEntry * pEntry = new (nothrow) RuntimeInstance::OsModuleEntry();
    if (NULL == pEntry)
        return nullptr; // Return null on failure.

    pEntry->m_osModule = hOsModule;

    {
        RuntimeInstance *pRuntimeInstance = GetRuntimeInstance();
        ReaderWriterLock::WriteHolder write(&pRuntimeInstance->GetTypeManagerLock());

        pRuntimeInstance->GetOsModuleList()->PushHead(pEntry);
    }

    return hOsModule; // Return non-null on success
}

RuntimeInstance::TypeManagerList& RuntimeInstance::GetTypeManagerList()
{
    return m_TypeManagerList;
}

// static
bool RuntimeInstance::Initialize(HANDLE hPalInstance)
{
    NewHolder<RuntimeInstance> pRuntimeInstance = new (nothrow) RuntimeInstance();
    if (NULL == pRuntimeInstance)
        return false;

    CreateHolder<ThreadStore>  pThreadStore = ThreadStore::Create(pRuntimeInstance);
    if (NULL == pThreadStore)
        return false;

    pThreadStore.SuppressRelease();
    pRuntimeInstance.SuppressRelease();

    pRuntimeInstance->m_pThreadStore = pThreadStore;
    pRuntimeInstance->m_hPalInstance = hPalInstance;

    ASSERT_MSG(g_pTheRuntimeInstance == NULL, "multi-instances are not supported");
    g_pTheRuntimeInstance = pRuntimeInstance;

    return true;
}

void RuntimeInstance::Destroy()
{
    delete this;
}

bool RuntimeInstance::ShouldHijackLoopForGcStress(uintptr_t CallsiteIP)
{
#ifdef FEATURE_GC_STRESS
    return ShouldHijackForGcStress(CallsiteIP, htLoop);
#else // FEATURE_GC_STRESS
    UNREFERENCED_PARAMETER(CallsiteIP);
    return false;
#endif // FEATURE_GC_STRESS
}

bool RuntimeInstance::ShouldHijackCallsiteForGcStress(uintptr_t CallsiteIP)
{
#ifdef FEATURE_GC_STRESS
    return ShouldHijackForGcStress(CallsiteIP, htCallsite);
#else // FEATURE_GC_STRESS
    UNREFERENCED_PARAMETER(CallsiteIP);
    return false;
#endif // FEATURE_GC_STRESS
}

COOP_PINVOKE_HELPER(uint32_t, RhGetGCDescSize, (MethodTable* pEEType))
{
    return RedhawkGCInterface::GetGCDescSize(pEEType);
}

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH
EXTERN_C void RhpInitialDynamicInterfaceDispatch();

COOP_PINVOKE_HELPER(void *, RhNewInterfaceDispatchCell, (MethodTable * pInterface, int32_t slotNumber))
{
    InterfaceDispatchCell * pCell = new (nothrow) InterfaceDispatchCell[2];
    if (pCell == NULL)
        return NULL;

    // Due to the synchronization mechanism used to update this indirection cell we must ensure the cell's alignment is twice that of a pointer.
    // Fortunately, Windows heap guarantees this alignment.
    ASSERT(IS_ALIGNED(pCell, 2 * POINTER_SIZE));
    ASSERT(IS_ALIGNED(pInterface, (InterfaceDispatchCell::IDC_CachePointerMask + 1)));

    pCell[0].m_pStub = (uintptr_t)&RhpInitialDynamicInterfaceDispatch;
    pCell[0].m_pCache = ((uintptr_t)pInterface) | InterfaceDispatchCell::IDC_CachePointerIsInterfacePointerOrMetadataToken;
    pCell[1].m_pStub = 0;
    pCell[1].m_pCache = (uintptr_t)slotNumber;

    return pCell;
}
#endif // FEATURE_CACHED_INTERFACE_DISPATCH

COOP_PINVOKE_HELPER(PTR_UInt8, RhGetThreadLocalStorageForDynamicType, (uint32_t uOffset, uint32_t tlsStorageSize, uint32_t numTlsCells))
{
    Thread * pCurrentThread = ThreadStore::GetCurrentThread();

    PTR_UInt8 pResult = pCurrentThread->GetThreadLocalStorageForDynamicType(uOffset);
    if (pResult != NULL || tlsStorageSize == 0 || numTlsCells == 0)
        return pResult;

    ASSERT(tlsStorageSize > 0 && numTlsCells > 0);
    return pCurrentThread->AllocateThreadLocalStorageForDynamicType(uOffset, tlsStorageSize, numTlsCells);
}

#endif // DACCESS_COMPILE
