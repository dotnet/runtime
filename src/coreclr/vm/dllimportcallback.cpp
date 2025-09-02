// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: DllImportCallback.cpp
//

//


#include "common.h"

#include "threads.h"
#include "excep.h"
#include "object.h"
#include "dllimportcallback.h"
#include "mlinfo.h"
#include "ceeload.h"
#include "eeconfig.h"
#include "dbginterface.h"
#include "stubgen.h"
#include "appdomain.inl"

#ifdef FEATURE_PERFMAP
#include "perfmap.h"
#endif

class UMEntryThunkFreeList
{
public:
    UMEntryThunkFreeList(size_t threshold) :
        m_threshold(threshold),
        m_count(0),
        m_pHead(NULL),
        m_pTail(NULL)
    {
        WRAPPER_NO_CONTRACT;

        m_crst.Init(CrstUMEntryThunkFreeListLock, CRST_UNSAFE_ANYMODE);
    }

    UMEntryThunkData *GetUMEntryThunk()
    {
        WRAPPER_NO_CONTRACT;

        if (m_count < m_threshold)
            return NULL;

        CrstHolder ch(&m_crst);

        UMEntryThunkData *pThunk = m_pHead;

        if (pThunk == NULL)
            return NULL;

        m_pHead = m_pHead->m_pNextFreeThunk;
        --m_count;

        return pThunk;
    }

    void AddToList(UMEntryThunkData *pThunk)
    {
        CONTRACTL
        {
            NOTHROW;
        }
        CONTRACTL_END;

        CrstHolder ch(&m_crst);

        if (m_pHead == NULL)
        {
            m_pHead = pThunk;
            m_pTail = pThunk;
        }
        else
        {
            m_pTail->m_pNextFreeThunk = pThunk;
            m_pTail = pThunk;
        }

        pThunk->m_pNextFreeThunk = NULL;

        ++m_count;
    }

private:
    // Used to delay reusing freed thunks
    size_t m_threshold;
    size_t m_count;
    UMEntryThunkData *m_pHead;
    UMEntryThunkData *m_pTail;
    CrstStatic m_crst;
};

#define DEFAULT_THUNK_FREE_LIST_THRESHOLD 64

static UMEntryThunkFreeList s_thunkFreeList(DEFAULT_THUNK_FREE_LIST_THRESHOLD);

PCODE UMThunkMarshInfo::GetExecStubEntryPoint()
{
    LIMITED_METHOD_CONTRACT;

    return m_pILStub;
}

UMEntryThunkCache::UMEntryThunkCache(AppDomain *pDomain) :
    m_crst(CrstUMEntryThunkCache),
    m_pDomain(pDomain)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(pDomain != NULL);
}

UMEntryThunkCache::~UMEntryThunkCache()
{
    WRAPPER_NO_CONTRACT;

    for (SHash<ThunkSHashTraits>::Iterator i = m_hash.Begin(); i != m_hash.End(); i++)
    {
        // UMEntryThunks in this cache own UMThunkMarshInfo in 1-1 fashion
        DestroyMarshInfo((*i)->GetUMThunkMarshInfo());
        UMEntryThunkData::FreeUMEntryThunk(*i);
    }
}

UMEntryThunkData *UMEntryThunkCache::GetUMEntryThunk(MethodDesc *pMD)
{
    CONTRACT (UMEntryThunkData *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    CrstHolder ch(&m_crst);

    UMEntryThunkData *pThunk = m_hash.Lookup(pMD);
    if (pThunk == NULL)
    {
        // cache miss -> create a new thunk
        pThunk = UMEntryThunkData::CreateUMEntryThunk();
        Holder<UMEntryThunkData *, DoNothing, UMEntryThunkData::FreeUMEntryThunk> umHolder;
        umHolder.Assign(pThunk);

        UMThunkMarshInfo *pMarshInfo = (UMThunkMarshInfo *)(void *)(m_pDomain->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(UMThunkMarshInfo))));
        Holder<UMThunkMarshInfo *, DoNothing, UMEntryThunkCache::DestroyMarshInfo> miHolder;
        miHolder.Assign(pMarshInfo);

        pMarshInfo->LoadTimeInit(pMD);

        pThunk->LoadTimeInit((PCODE)NULL, NULL, pMarshInfo, pMD);

        // add it to the cache
        m_hash.Add(pThunk);

        miHolder.SuppressRelease();
        umHolder.SuppressRelease();
    }

    RETURN pThunk;
}

// FailFast if a method marked UnmanagedCallersOnlyAttribute is
// invoked directly from managed code. UMThunkStub.asm check the
// mode and call this function to failfast.
extern "C" VOID STDCALL ReversePInvokeBadTransition()
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    // Fail
    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(
                                             COR_E_EXECUTIONENGINE,
                                             W("Invalid Program: attempted to call a UnmanagedCallersOnly method from managed code.")
                                            );
}

//-------------------------------------------------------------------------
// This function is used to report error when we call collected delegate.
// But memory that was allocated for thunk can be reused, due to it this
// function will not be called in all cases of the collected delegate call,
// also it may crash while trying to report the problem.
//-------------------------------------------------------------------------
VOID CallbackOnCollectedDelegate(UMEntryThunkData* pEntryThunkData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pEntryThunkData));
    }
    CONTRACTL_END;

    MethodDesc* pMethodDesc = pEntryThunkData->GetMethod();

    SString namespaceOrClassName;
    SString methodName;
    pMethodDesc->GetMethodInfoNoSig(namespaceOrClassName, methodName);

    SString message;
    message.Printf("A callback was made on a garbage collected delegate of type '%s!%s::%s'.",
        pMethodDesc->GetModule()->GetSimpleName(),
        namespaceOrClassName.GetUTF8(),
        methodName.GetUTF8());

    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_FAILFAST, message.GetUnicode());
}

#ifdef FEATURE_INTERPRETER
PLATFORM_THREAD_LOCAL UMEntryThunkData * t_MostRecentUMEntryThunkData;

UMEntryThunkData * GetMostRecentUMEntryThunkData()
{
    LIMITED_METHOD_CONTRACT;

    UMEntryThunkData * result = t_MostRecentUMEntryThunkData;
    t_MostRecentUMEntryThunkData = nullptr;
    return result;
}
#endif

PCODE TheUMEntryPrestubWorker(UMEntryThunkData * pUMEntryThunkData)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;

    Thread * pThread = GetThreadNULLOk();
    if (pThread == NULL)
    {
        CREATETHREAD_IF_NULL_FAILFAST(pThread, W("Failed to setup new thread during reverse P/Invoke"));
    }

#ifdef FEATURE_INTERPRETER
    PCODE pInterpreterTarget = pUMEntryThunkData->GetInterpreterTarget();
    if (pInterpreterTarget != (PCODE)0)
    {
        t_MostRecentUMEntryThunkData = pUMEntryThunkData;
        return pInterpreterTarget;
    }
#endif // FEATURE_INTERPRETER

    // Verify the current thread isn't in COOP mode.
    if (pThread->PreemptiveGCDisabled())
        ReversePInvokeBadTransition();

    if (pUMEntryThunkData->IsCollectedDelegate())
        CallbackOnCollectedDelegate(pUMEntryThunkData);

    INSTALL_MANAGED_EXCEPTION_DISPATCHER;
    // this method is called by stubs which are called by managed code,
    // so we need an unwind and continue handler so that our internal
    // exceptions don't leak out into managed code.
    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    pUMEntryThunkData->RunTimeInit();

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
    UNINSTALL_MANAGED_EXCEPTION_DISPATCHER;

    return (PCODE)pUMEntryThunkData->GetCode();
}

UMEntryThunkData* UMEntryThunkData::CreateUMEntryThunk()
{
    CONTRACT (UMEntryThunkData*)
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    UMEntryThunkData * pData = s_thunkFreeList.GetUMEntryThunk();

    if (pData == NULL)
    {
        static_assert(sizeof(UMEntryThunk) == sizeof(StubPrecode));
        LoaderAllocator *pLoaderAllocator = SystemDomain::GetGlobalLoaderAllocator();
        AllocMemTracker amTracker;
        AllocMemTracker *pamTracker = &amTracker;

        pData = (UMEntryThunkData *)pamTracker->Track(pLoaderAllocator->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(UMEntryThunkData))));
        UMEntryThunk* pThunk = (UMEntryThunk*)pamTracker->Track(pLoaderAllocator->GetNewStubPrecodeHeap()->AllocStub());
#ifdef FEATURE_PERFMAP
        PerfMap::LogStubs(__FUNCTION__, "UMEntryThunk", (PCODE)pThunk, sizeof(UMEntryThunk), PerfMapStubType::IndividualWithinBlock);
#endif
        pData->m_pUMEntryThunk = pThunk;
        pThunk->Init(pThunk, dac_cast<TADDR>(pData), NULL, dac_cast<TADDR>(PRECODE_UMENTRY_THUNK));
        pamTracker->SuppressRelease();
    }

    RETURN pData;
}

void UMEntryThunkData::Terminate()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    // TheUMEntryPrestub includes diagnostic for collected delegates
    m_pUMEntryThunk->SetTargetUnconditional(TheUMThunkPreStub());

    FlushCacheForDynamicMappedStub(m_pUMEntryThunk, sizeof(UMEntryThunk));

    OBJECTHANDLE pObjectHandle = m_pObjectHandle;

    // Set m_pObjectHandle indicate the collected state
    m_pObjectHandle = (OBJECTHANDLE)-1;

    if (pObjectHandle != NULL)
    {
        DestroyLongWeakHandle(pObjectHandle);
    }

    s_thunkFreeList.AddToList(this);
}

VOID UMEntryThunkData::FreeUMEntryThunk(UMEntryThunkData* p)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(p));
    }
    CONTRACTL_END;

    p->Terminate();
}

UMThunkMarshInfo::~UMThunkMarshInfo()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    FillMemory(this, sizeof(*this), 0xcc);
#endif
}

MethodDesc* UMThunkMarshInfo::GetILStubMethodDesc(MethodDesc* pInvokeMD, PInvokeStaticSigInfo* pSigInfo, DWORD dwStubFlags)
{
    STANDARD_VM_CONTRACT;

    MethodDesc* pStubMD = NULL;
    dwStubFlags |= PINVOKESTUB_FL_REVERSE_INTEROP;  // could be either delegate interop or not--that info is passed in from the caller

#if defined(DEBUGGING_SUPPORTED)
    // Combining the next two lines, and eliminating jitDebuggerFlags, leads to bad codegen in x86 Release builds using Visual C++ 19.00.24215.1.
    CORJIT_FLAGS jitDebuggerFlags = GetDebuggerCompileFlags(pSigInfo->GetModule(), CORJIT_FLAGS());
    if (jitDebuggerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE))
    {
        dwStubFlags |= PINVOKESTUB_FL_GENERATEDEBUGGABLEIL;
    }
#endif // DEBUGGING_SUPPORTED

    pStubMD = PInvoke::CreateCLRToNativeILStub(
        pSigInfo,
        dwStubFlags,
        pInvokeMD // may be NULL
        );

    return pStubMD;
}

//----------------------------------------------------------
// This initializer is called during load time.
// It does not do any stub initialization or sigparsing.
// The RunTimeInit() must be called subsequently to fully
// UMThunkMarshInfo.
//----------------------------------------------------------
VOID UMThunkMarshInfo::LoadTimeInit(MethodDesc* pMD)
{
    LIMITED_METHOD_CONTRACT;
    PRECONDITION(pMD != NULL);

    LoadTimeInit(pMD->GetSignature(), pMD->GetModule(), pMD);
}

VOID UMThunkMarshInfo::LoadTimeInit(Signature sig, Module * pModule, MethodDesc * pMD)
{
    LIMITED_METHOD_CONTRACT;

    FillMemory(this, sizeof(UMThunkMarshInfo), 0); // Prevent problems with partial deletes

    // This will be overwritten by the actual code pointer (or NULL) at the end of UMThunkMarshInfo::RunTimeInit()
    m_pILStub = (PCODE)1;

    m_pMD = pMD;
    m_pModule = pModule;
    m_sig = sig;
}

//----------------------------------------------------------
// This initializer finishes the init started by LoadTimeInit.
// It does stub creation and can throw an exception.
//
// It can safely be called multiple times and by concurrent
// threads.
//----------------------------------------------------------
VOID UMThunkMarshInfo::RunTimeInit()
{
    STANDARD_VM_CONTRACT;

    // Nothing to do if already inited
    if (IsCompletelyInited())
        return;

    MethodDesc * pMD = GetMethod();

    PInvokeStaticSigInfo sigInfo;

    if (pMD != NULL)
        new (&sigInfo) PInvokeStaticSigInfo(pMD);
    else
        new (&sigInfo) PInvokeStaticSigInfo(GetSignature(), GetModule());

    DWORD dwStubFlags = 0;

    if (sigInfo.IsDelegateInterop())
        dwStubFlags |= PINVOKESTUB_FL_DELEGATE;

    MethodDesc* pStubMD = GetILStubMethodDesc(pMD, &sigInfo, dwStubFlags);
    PCODE pFinalILStub = JitILStub(pStubMD);

    // Must be the last thing we set!
    InterlockedCompareExchangeT<PCODE>(&m_pILStub, pFinalILStub, (PCODE)1);
}
