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
#include "comdelegate.h"
#include "ceeload.h"
#include "eeconfig.h"
#include "dbginterface.h"
#include "stubgen.h"
#include "appdomain.inl"
#include "callingconvention.h"
#include "customattribute.h"
#include "typeparse.h"

#ifndef CROSSGEN_COMPILE

struct UM2MThunk_Args
{
    UMEntryThunk *pEntryThunk;
    void *pAddr;
    void *pThunkArgs;
    int argLen;
};

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

        m_crst.Init(CrstLeafLock, CRST_UNSAFE_ANYMODE);
    }

    UMEntryThunk *GetUMEntryThunk()
    {
        WRAPPER_NO_CONTRACT;

        if (m_count < m_threshold)
            return NULL;

        CrstHolder ch(&m_crst);

        UMEntryThunk *pThunk = m_pHead;

        if (pThunk == NULL)
            return NULL;

        m_pHead = m_pHead->m_pNextFreeThunk;
        --m_count;

        return pThunk;
    }

    void AddToList(UMEntryThunk *pThunk)
    {
        CONTRACTL
        {
            NOTHROW;
        }
        CONTRACTL_END;

        CrstHolder ch(&m_crst);

#if defined(HOST_OSX) && defined(HOST_ARM64)
        auto jitWriteEnableHolder = PAL_JITWriteEnable(true);
#endif // defined(HOST_OSX) && defined(HOST_ARM64)

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
    UMEntryThunk *m_pHead;
    UMEntryThunk *m_pTail;
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
        DestroyMarshInfo(i->m_pThunk->GetUMThunkMarshInfo());
        UMEntryThunk::FreeUMEntryThunk(i->m_pThunk);
    }
}

UMEntryThunk *UMEntryThunkCache::GetUMEntryThunk(MethodDesc *pMD)
{
    CONTRACT (UMEntryThunk *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    UMEntryThunk *pThunk;

    CrstHolder ch(&m_crst);

    const CacheElement *pElement = m_hash.LookupPtr(pMD);
    if (pElement != NULL)
    {
        pThunk = pElement->m_pThunk;
    }
    else
    {
        // cache miss -> create a new thunk
#if defined(HOST_OSX) && defined(HOST_ARM64)
        auto jitWriteEnableHolder = PAL_JITWriteEnable(true);
#endif // defined(HOST_OSX) && defined(HOST_ARM64)
        pThunk = UMEntryThunk::CreateUMEntryThunk();
        Holder<UMEntryThunk *, DoNothing, UMEntryThunk::FreeUMEntryThunk> umHolder;
        umHolder.Assign(pThunk);

        UMThunkMarshInfo *pMarshInfo = (UMThunkMarshInfo *)(void *)(m_pDomain->GetStubHeap()->AllocMem(S_SIZE_T(sizeof(UMThunkMarshInfo))));
        Holder<UMThunkMarshInfo *, DoNothing, UMEntryThunkCache::DestroyMarshInfo> miHolder;
        miHolder.Assign(pMarshInfo);

        pMarshInfo->LoadTimeInit(pMD);
        pThunk->LoadTimeInit(NULL, NULL, pMarshInfo, pMD);

        // add it to the cache
        CacheElement element;
        element.m_pMD = pMD;
        element.m_pThunk = pThunk;
        m_hash.Add(element);

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

// Disable from a place that is calling into managed code via a UMEntryThunk.
extern "C" VOID STDCALL UMThunkStubRareDisableWorker(Thread *pThread, UMEntryThunk *pUMEntryThunk)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    // Do not add a CONTRACT here.  We haven't set up SEH.

    // WARNING!!!!
    // when we start executing here, we are actually in cooperative mode.  But we
    // haven't synchronized with the barrier to reentry yet.  So we are in a highly
    // dangerous mode.  If we call managed code, we will potentially be active in
    // the GC heap, even as GC's are occuring!

    // We must do the following in this order, because otherwise we would be constructing
    // the exception for the abort without synchronizing with the GC.  Also, we have no
    // CLR SEH set up, despite the fact that we may throw a ThreadAbortException.
    pThread->RareDisablePreemptiveGC();
    pThread->HandleThreadAbort();

#ifdef DEBUGGING_SUPPORTED
    // If the debugger is attached, we use this opportunity to see if
    // we're disabling preemptive GC on the way into the runtime from
    // unmanaged code. We end up here because
    // Increment/DecrementTraceCallCount() will bump
    // g_TrapReturningThreads for us.
    if (CORDebuggerTraceCall())
        g_pDebugInterface->TraceCall((const BYTE *)pUMEntryThunk->GetManagedTarget());
#endif // DEBUGGING_SUPPORTED
}

PCODE TheUMEntryPrestubWorker(UMEntryThunk * pUMEntryThunk)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;

    Thread * pThread = GetThreadNULLOk();
    if (pThread == NULL)
        pThread = CreateThreadBlockThrow();

    GCX_COOP_THREAD_EXISTS(pThread);

    if (pThread->IsAbortRequested())
        pThread->HandleThreadAbort();

    UMEntryThunk::DoRunTimeInit(pUMEntryThunk);

    return (PCODE)pUMEntryThunk->GetCode();
}

void RunTimeInit_Wrapper(LPVOID /* UMThunkMarshInfo * */ ptr)
{
    WRAPPER_NO_CONTRACT;

    UMEntryThunk::DoRunTimeInit((UMEntryThunk*)ptr);
}


// asm entrypoint
void STDCALL UMEntryThunk::DoRunTimeInit(UMEntryThunk* pUMEntryThunk)
{

    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(pUMEntryThunk));
    }
    CONTRACTL_END;

    INSTALL_MANAGED_EXCEPTION_DISPATCHER;
    // this method is called by stubs which are called by managed code,
    // so we need an unwind and continue handler so that our internal
    // exceptions don't leak out into managed code.
    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    {
        GCX_PREEMP();

#if defined(HOST_OSX) && defined(HOST_ARM64)
        auto jitWriteEnableHolder = PAL_JITWriteEnable(true);
#endif // defined(HOST_OSX) && defined(HOST_ARM64)

        pUMEntryThunk->RunTimeInit();
    }

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
    UNINSTALL_MANAGED_EXCEPTION_DISPATCHER;
}

UMEntryThunk* UMEntryThunk::CreateUMEntryThunk()
{
    CONTRACT (UMEntryThunk*)
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    UMEntryThunk * p;

    p = s_thunkFreeList.GetUMEntryThunk();

    if (p == NULL)
        p = (UMEntryThunk *)(void *)SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap()->AllocMem(S_SIZE_T(sizeof(UMEntryThunk)));

    RETURN p;
}

void UMEntryThunk::Terminate()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_code.Poison();

    if (GetObjectHandle())
    {
#if defined(HOST_OSX) && defined(HOST_ARM64)
        auto jitWriteEnableHolder = PAL_JITWriteEnable(true);
#endif // defined(HOST_OSX) && defined(HOST_ARM64)

        DestroyLongWeakHandle(GetObjectHandle());
        m_pObjectHandle = 0;
    }

    s_thunkFreeList.AddToList(this);
}

VOID UMEntryThunk::FreeUMEntryThunk(UMEntryThunk* p)
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

#endif // CROSSGEN_COMPILE

//-------------------------------------------------------------------------
// This function is used to report error when we call collected delegate.
// But memory that was allocated for thunk can be reused, due to it this
// function will not be called in all cases of the collected delegate call,
// also it may crash while trying to report the problem.
//-------------------------------------------------------------------------
VOID __fastcall UMEntryThunk::ReportViolation(UMEntryThunk* pEntryThunk)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pEntryThunk));
    }
    CONTRACTL_END;

    MethodDesc* pMethodDesc = pEntryThunk->GetMethod();

    SString namespaceOrClassName;
    SString methodName;
    SString moduleName;

    pMethodDesc->GetMethodInfoNoSig(namespaceOrClassName, methodName);
    moduleName.SetUTF8(pMethodDesc->GetModule()->GetSimpleName());

    SString message;

    message.Printf(W("A callback was made on a garbage collected delegate of type '%s!%s::%s'."),
        moduleName.GetUnicode(),
        namespaceOrClassName.GetUnicode(),
        methodName.GetUnicode());

    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_FAILFAST, message.GetUnicode());
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
    dwStubFlags |= NDIRECTSTUB_FL_REVERSE_INTEROP;  // could be either delegate interop or not--that info is passed in from the caller

#if defined(DEBUGGING_SUPPORTED)
    // Combining the next two lines, and eliminating jitDebuggerFlags, leads to bad codegen in x86 Release builds using Visual C++ 19.00.24215.1.
    CORJIT_FLAGS jitDebuggerFlags = GetDebuggerCompileFlags(pSigInfo->GetModule(), CORJIT_FLAGS());
    if (jitDebuggerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE))
    {
        dwStubFlags |= NDIRECTSTUB_FL_GENERATEDEBUGGABLEIL;
    }
#endif // DEBUGGING_SUPPORTED

    pStubMD = NDirect::CreateCLRToNativeILStub(
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

#ifndef CROSSGEN_COMPILE
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

    PCODE pFinalILStub = NULL;
    MethodDesc* pStubMD = NULL;

    MethodDesc * pMD = GetMethod();

    // Lookup NGened stub - currently we only support ngening of reverse delegate invoke interop stubs
    if (pMD != NULL && pMD->IsEEImpl())
    {
        DWORD dwStubFlags = NDIRECTSTUB_FL_NGENEDSTUB | NDIRECTSTUB_FL_REVERSE_INTEROP | NDIRECTSTUB_FL_DELEGATE;

#if defined(DEBUGGING_SUPPORTED)
        // Combining the next two lines, and eliminating jitDebuggerFlags, leads to bad codegen in x86 Release builds using Visual C++ 19.00.24215.1.
        CORJIT_FLAGS jitDebuggerFlags = GetDebuggerCompileFlags(GetModule(), CORJIT_FLAGS());
        if (jitDebuggerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE))
        {
            dwStubFlags |= NDIRECTSTUB_FL_GENERATEDEBUGGABLEIL;
        }
#endif // DEBUGGING_SUPPORTED

        pFinalILStub = GetStubForInteropMethod(pMD, dwStubFlags, &pStubMD);
    }

    if (pFinalILStub == NULL)
    {
        PInvokeStaticSigInfo sigInfo;

        if (pMD != NULL)
            new (&sigInfo) PInvokeStaticSigInfo(pMD);
        else
            new (&sigInfo) PInvokeStaticSigInfo(GetSignature(), GetModule());

        DWORD dwStubFlags = 0;

        if (sigInfo.IsDelegateInterop())
            dwStubFlags |= NDIRECTSTUB_FL_DELEGATE;

        pStubMD = GetILStubMethodDesc(pMD, &sigInfo, dwStubFlags);
        pFinalILStub = JitILStub(pStubMD);
    }

    // Must be the last thing we set!
    InterlockedCompareExchangeT<PCODE>(&m_pILStub, pFinalILStub, (PCODE)1);
}

#ifdef _DEBUG
void STDCALL LogUMTransition(UMEntryThunk* thunk)
{
    CONTRACTL
    {
        NOTHROW;
        DEBUG_ONLY;
        GC_NOTRIGGER;
        ENTRY_POINT;
        if (GetThreadNULLOk()) MODE_PREEMPTIVE; else MODE_ANY;
        DEBUG_ONLY;
        PRECONDITION(CheckPointer(thunk));
        PRECONDITION((GetThreadNULLOk() != NULL) ? (!GetThread()->PreemptiveGCDisabled()) : TRUE);
    }
    CONTRACTL_END;

    BEGIN_ENTRYPOINT_VOIDRET;

    void** retESP = ((void**) &thunk) + 4;

    MethodDesc* method = thunk->GetMethod();
    if (method)
    {
        LOG((LF_STUBS, LL_INFO1000000, "UNMANAGED -> MANAGED Stub To Method = %s::%s SIG %s Ret Address ESP = 0x%x ret = 0x%x\n",
            method->m_pszDebugClassName,
            method->m_pszDebugMethodName,
            method->m_pszDebugMethodSignature, retESP, *retESP));
    }

    END_ENTRYPOINT_VOIDRET;

    }
#endif

bool TryGetCallingConventionFromUnmanagedCallersOnly(MethodDesc* pMD, CorInfoCallConvExtension* pCallConv)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(pMD != NULL && pMD->HasUnmanagedCallersOnlyAttribute());

    // Validate usage
    COMDelegate::ThrowIfInvalidUnmanagedCallersOnlyUsage(pMD);

    BYTE* pData = NULL;
    LONG cData = 0;

    bool nativeCallableInternalData = false;
    HRESULT hr = pMD->GetCustomAttribute(WellKnownAttribute::UnmanagedCallersOnly, (const VOID **)(&pData), (ULONG *)&cData);
    if (hr == S_FALSE)
    {
        hr = pMD->GetCustomAttribute(WellKnownAttribute::NativeCallableInternal, (const VOID **)(&pData), (ULONG *)&cData);
        nativeCallableInternalData = SUCCEEDED(hr);
    }

    IfFailThrow(hr);

    _ASSERTE(cData > 0);

    CustomAttributeParser ca(pData, cData);

    // UnmanagedCallersOnly and NativeCallableInternal each
    // have optional named arguments.
    CaNamedArg namedArgs[2];

    // For the UnmanagedCallersOnly scenario.
    CaType caCallConvs;

    // Define attribute specific optional named properties
    if (nativeCallableInternalData)
    {
        namedArgs[0].InitI4FieldEnum("CallingConvention", "System.Runtime.InteropServices.CallingConvention", (ULONG)(CorPinvokeMap)0);
    }
    else
    {
        caCallConvs.Init(SERIALIZATION_TYPE_SZARRAY, SERIALIZATION_TYPE_TYPE, SERIALIZATION_TYPE_UNDEFINED, NULL, 0);
        namedArgs[0].Init("CallConvs", SERIALIZATION_TYPE_SZARRAY, caCallConvs);
    }

    // Define common optional named properties
    CaTypeCtor caEntryPoint(SERIALIZATION_TYPE_STRING);
    namedArgs[1].Init("EntryPoint", SERIALIZATION_TYPE_STRING, caEntryPoint);

    InlineFactory<SArray<CaValue>, 4> caValueArrayFactory;
    DomainAssembly* domainAssembly = pMD->GetLoaderModule()->GetDomainAssembly();
    IfFailThrow(Attribute::ParseAttributeArgumentValues(
        pData,
        cData,
        &caValueArrayFactory,
        NULL,
        0,
        namedArgs,
        lengthof(namedArgs),
        domainAssembly));

    // If the value isn't defined, then return without setting anything.
    if (namedArgs[0].val.type.tag == SERIALIZATION_TYPE_UNDEFINED)
        return false;

    CorInfoCallConvExtension callConvLocal;
    if (nativeCallableInternalData)
    {
        callConvLocal = (CorInfoCallConvExtension)(namedArgs[0].val.u4 << 8);
    }
    else
    {
        CallConvBuilder callConvBuilder;

        CaValue* arrayOfTypes = &namedArgs[0].val;
        for (ULONG i = 0; i < arrayOfTypes->arr.length; i++)
        {
            CaValue& typeNameValue = arrayOfTypes->arr[i];

            if (!callConvBuilder.AddFullyQualifiedTypeName(typeNameValue.str.cbStr, typeNameValue.str.pStr))
            {
                // We found a second base calling convention.
                return false;
            }
        }

        callConvLocal = callConvBuilder.GetCurrentCallConv();
        if (callConvLocal == CallConvBuilder::UnsetValue)
        {
            callConvLocal = MetaSig::GetDefaultUnmanagedCallingConvention();
        }
    }
    *pCallConv = callConvLocal;
    return true;
}
#endif // CROSSGEN_COMPILE
