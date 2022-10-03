// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*
 *
 * COM+99 EE to Debugger Interface Implementation
 *
 */

#include "common.h"
#include "dbginterface.h"
#include "eedbginterfaceimpl.h"
#include "virtualcallstub.h"
#include "contractimpl.h"

#ifdef DEBUGGING_SUPPORTED

#ifndef DACCESS_COMPILE

//
// Cleanup any global data used by this interface.
//
void EEDbgInterfaceImpl::Terminate(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (g_pEEDbgInterfaceImpl)
    {
        delete g_pEEDbgInterfaceImpl;
        g_pEEDbgInterfaceImpl = NULL;
    }
}

#endif // #ifndef DACCESS_COMPILE

Thread* EEDbgInterfaceImpl::GetThread(void)
{
    LIMITED_METHOD_CONTRACT;
// Since this may be called from a Debugger Interop Hijack, the EEThread may be bogus.
// Thus we can't use contracts. If we do fix that, then the contract below would be nice...
#if 0
    CONTRACT(Thread *)
    {
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;
#endif

    return ::GetThreadNULLOk();
}

#ifndef DACCESS_COMPILE

StackWalkAction EEDbgInterfaceImpl::StackWalkFramesEx(Thread* pThread,
                                                      PREGDISPLAY pRD,
                                                      PSTACKWALKFRAMESCALLBACK pCallback,
                                                      VOID* pData,
                                                      unsigned int flags)
{
    CONTRACTL
    {
        DISABLED(NOTHROW); // FIX THIS when StackWalkFramesEx gets fixed.
        DISABLED(GC_TRIGGERS); // We cannot predict if pCallback will trigger or not.
                               // Disabled is not a bug in this case.
        PRECONDITION(CheckPointer(pThread));
    }
    CONTRACTL_END;

    return pThread->StackWalkFramesEx(pRD, pCallback, pData, flags);
}

Frame *EEDbgInterfaceImpl::GetFrame(CrawlFrame *pCF)
{
    CONTRACT(Frame *)
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pCF));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    RETURN pCF->GetFrame();
}

bool EEDbgInterfaceImpl::InitRegDisplay(Thread* pThread,
                                        const PREGDISPLAY pRD,
                                        const PCONTEXT pctx,
                                        bool validContext)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pThread));
        PRECONDITION(CheckPointer(pRD));
        if (validContext)
        {
            PRECONDITION(CheckPointer(pctx));
        }
    }
    CONTRACTL_END;

    return pThread->InitRegDisplay(pRD, pctx, validContext);
}

BOOL EEDbgInterfaceImpl::IsStringObject(Object* o)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(o));
    }
    CONTRACTL_END;

    return o->GetMethodTable() == g_pStringClass;
}

BOOL EEDbgInterfaceImpl::IsTypedReference(MethodTable* pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    return pMT == g_TypedReferenceMT;
}

WCHAR* EEDbgInterfaceImpl::StringObjectGetBuffer(StringObject* so)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(so));
    }
    CONTRACTL_END;

    return so->GetBuffer();
}

DWORD EEDbgInterfaceImpl::StringObjectGetStringLength(StringObject* so)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(so));
    }
    CONTRACTL_END;

    return so->GetStringLength();
}

void* EEDbgInterfaceImpl::GetObjectFromHandle(OBJECTHANDLE handle)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    void *v;

    *((OBJECTREF *)&v) = *(OBJECTREF *)handle;

    return v;
}

OBJECTHANDLE EEDbgInterfaceImpl::GetHandleFromObject(void *obj,
                                              bool fStrongNewRef,
                                              AppDomain *pAppDomain)
{
    CONTRACTL
    {
        THROWS;  // From CreateHandle
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pAppDomain));
    }
    CONTRACTL_END;

    OBJECTHANDLE oh;

    if (fStrongNewRef)
    {
        oh = pAppDomain->CreateStrongHandle(ObjectToOBJECTREF((Object *)obj));

        LOG((LF_CORDB, LL_INFO1000, "EEI::GHFO: Given objectref 0x%x,"
            "created strong handle 0x%x!\n", obj, oh));
    }
    else
    {
        oh = pAppDomain->CreateLongWeakHandle( ObjectToOBJECTREF((Object *)obj));

        LOG((LF_CORDB, LL_INFO1000, "EEI::GHFO: Given objectref 0x%x,"
            "created long weak handle 0x%x!\n", obj, oh));
    }

    return oh;
}

void EEDbgInterfaceImpl::DbgDestroyHandle(OBJECTHANDLE oh,
                                          bool fStrongNewRef)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO1000, "EEI::GHFO: Destroyed given handle 0x%x,"
        "fStrong: 0x%x!\n", oh, fStrongNewRef));

    if (fStrongNewRef)
    {
        DestroyStrongHandle(oh);
    }
    else
    {
        DestroyLongWeakHandle(oh);
    }
}


OBJECTHANDLE EEDbgInterfaceImpl::GetThreadException(Thread *pThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pThread));
    }
    CONTRACTL_END;

    OBJECTHANDLE oh = pThread->GetThrowableAsHandle();

    if (oh != NULL)
    {
        return oh;
    }

    // Return the last thrown object if there's no current throwable.
    // This logic is similar to UpdateCurrentThrowable().
    return pThread->m_LastThrownObjectHandle;
}

bool EEDbgInterfaceImpl::IsThreadExceptionNull(Thread *pThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pThread));
    }
    CONTRACTL_END;

    //
    // We're assuming that the handle on the
    // thread is a strong handle and we're goona check it for
    // NULL. We're also assuming something about the
    // implementation of the handle here, too.
    //
    OBJECTHANDLE h = pThread->GetThrowableAsHandle();
    if (h == NULL)
    {
        return true;
    }

    void *pThrowable = *((void**)h);

    return (pThrowable == NULL);
}

void EEDbgInterfaceImpl::ClearThreadException(Thread *pThread)
{
    //
    // If one day there is a continuable exception, then this will have to be
    // implemented properly.
    //
    //
    LIMITED_METHOD_CONTRACT;
}

bool EEDbgInterfaceImpl::StartSuspendForDebug(AppDomain *pAppDomain,
                                              BOOL fHoldingThreadStoreLock)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB,LL_INFO1000, "EEDbgII:SSFD: start suspend on AD:0x%x\n",
        pAppDomain));

    bool result = Thread::SysStartSuspendForDebug(pAppDomain);

    return result;
}

bool EEDbgInterfaceImpl::SweepThreadsForDebug(bool forceSync)
{
    CONTRACTL
    {
        NOTHROW;
        DISABLED(GC_TRIGGERS);  // Called by unmanaged threads.
    }
    CONTRACTL_END;

    return Thread::SysSweepThreadsForDebug(forceSync);
}

void EEDbgInterfaceImpl::ResumeFromDebug(AppDomain *pAppDomain)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Thread::SysResumeFromDebug(pAppDomain);
}

void EEDbgInterfaceImpl::MarkThreadForDebugSuspend(Thread* pRuntimeThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pRuntimeThread));
    }
    CONTRACTL_END;

    pRuntimeThread->MarkForDebugSuspend();
}

void EEDbgInterfaceImpl::MarkThreadForDebugStepping(Thread* pRuntimeThread,
                                                    bool onOff)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pRuntimeThread));
    }
    CONTRACTL_END;

    pRuntimeThread->MarkDebuggerIsStepping(onOff);
}

void EEDbgInterfaceImpl::SetThreadFilterContext(Thread *thread,
                                                CONTEXT *context)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(thread));
    }
    CONTRACTL_END;

    thread->SetFilterContext(context);
}

CONTEXT *EEDbgInterfaceImpl::GetThreadFilterContext(Thread *thread)
{
    CONTRACT(CONTEXT *)
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(thread));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    RETURN thread->GetFilterContext();
}

#ifdef FEATURE_INTEROP_DEBUGGING

VOID * EEDbgInterfaceImpl::GetThreadDebuggerWord()
{
    return TlsGetValue(g_debuggerWordTLSIndex);
}

void EEDbgInterfaceImpl::SetThreadDebuggerWord(VOID *dw)
{
    TlsSetValue(g_debuggerWordTLSIndex, dw);
}

#endif

BOOL EEDbgInterfaceImpl::IsManagedNativeCode(const BYTE *address)
{
    WRAPPER_NO_CONTRACT;
    return ExecutionManager::IsManagedCode((PCODE)address);
}

PCODE EEDbgInterfaceImpl::GetNativeCodeStartAddress(PCODE address)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(address != NULL);

    return ExecutionManager::GetCodeStartAddress(address);
}

MethodDesc *EEDbgInterfaceImpl::GetNativeCodeMethodDesc(const PCODE address)
{
    CONTRACT(MethodDesc *)
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(address != NULL);
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    RETURN ExecutionManager::GetCodeMethodDesc(address);
}

#ifndef USE_GC_INFO_DECODER
// IsInPrologOrEpilog doesn't seem to be used for code that uses GC_INFO_DECODER
BOOL EEDbgInterfaceImpl::IsInPrologOrEpilog(const BYTE *address,
                                            size_t* prologSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    *prologSize = 0;

    EECodeInfo codeInfo((PCODE)address);

    if (codeInfo.IsValid())
    {
        GCInfoToken gcInfoToken = codeInfo.GetGCInfoToken();

        if (codeInfo.GetCodeManager()->IsInPrologOrEpilog(codeInfo.GetRelOffset(), gcInfoToken, prologSize))
        {
            return TRUE;
        }
    }

    return FALSE;
}
#endif // USE_GC_INFO_DECODER

//
// Given a collection of native offsets of a certain function, determine if each falls
// within an exception filter or handler.
//
void EEDbgInterfaceImpl::DetermineIfOffsetsInFilterOrHandler(const BYTE *functionAddress,
                                                                  DebugOffsetToHandlerInfo *pOffsetToHandlerInfo,
                                                                  unsigned offsetToHandlerInfoLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    EECodeInfo codeInfo((PCODE)functionAddress);

    if (!codeInfo.IsValid())
    {
        return;
    }

    // Loop through all the exception handling clause information for the method
    EH_CLAUSE_ENUMERATOR pEnumState;
    unsigned EHCount = codeInfo.GetJitManager()->InitializeEHEnumeration(codeInfo.GetMethodToken(), &pEnumState);
    if (EHCount == 0)
    {
        return;
    }

    for (ULONG i=0; i < EHCount; i++)
    {
        EE_ILEXCEPTION_CLAUSE EHClause;
        codeInfo.GetJitManager()->GetNextEHClause(&pEnumState, &EHClause);

        // Check each EH clause against each offset of interest.
        // Note that this could be time consuming for very long methods ( O(n^2) ).
        // We could make this linear if we could guarantee that the two lists are sorted.
        for (ULONG j=0; j < offsetToHandlerInfoLength; j++)
        {
            SIZE_T offs = pOffsetToHandlerInfo[j].offset;

            // those with -1 indicate slots to skip
            if (offs == (SIZE_T) -1)
            {
                continue;
            }
            // For a filter, the handler comes directly after it so check from start of filter
            // to end of handler
            if (IsFilterHandler(&EHClause))
            {
                if (offs >= EHClause.FilterOffset && offs < EHClause.HandlerEndPC)
                {
                    pOffsetToHandlerInfo[j].isInFilterOrHandler = TRUE;
                }
            }
            // For anything else, only care about handler range
            else if (offs >= EHClause.HandlerStartPC && offs < EHClause.HandlerEndPC)
            {
                pOffsetToHandlerInfo[j].isInFilterOrHandler = TRUE;
            }
        }
    }
}
#endif // #ifndef DACCESS_COMPILE

void EEDbgInterfaceImpl::GetMethodRegionInfo(const PCODE    pStart,
                                             PCODE        * pCold,
                                             size_t *hotSize,
                                             size_t *coldSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pCold));
        PRECONDITION(CheckPointer(hotSize));
        PRECONDITION(CheckPointer(coldSize));
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    IJitManager::MethodRegionInfo methodRegionInfo = {NULL, 0, NULL, 0};

    EECodeInfo codeInfo(pStart);

    if (codeInfo.IsValid() != NULL)
    {
        codeInfo.GetMethodRegionInfo(&methodRegionInfo);
    }

    *pCold    = methodRegionInfo.coldStartAddress;
    *hotSize  = methodRegionInfo.hotSize;
    *coldSize = methodRegionInfo.coldSize;
}

#if defined(FEATURE_EH_FUNCLETS)
DWORD EEDbgInterfaceImpl::GetFuncletStartOffsets(const BYTE *pStart, DWORD* pStartOffsets, DWORD dwLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pStart));
    }
    CONTRACTL_END;

    EECodeInfo codeInfo((PCODE)pStart);
    _ASSERTE(codeInfo.IsValid());

    return codeInfo.GetJitManager()->GetFuncletStartOffsets(codeInfo.GetMethodToken(), pStartOffsets, dwLength);
}

StackFrame EEDbgInterfaceImpl::FindParentStackFrame(CrawlFrame* pCF)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pCF));
    }
    CONTRACTL_END;

#if defined(DACCESS_COMPILE)
    DacNotImpl();
    return StackFrame();

#else  // !DACCESS_COMPILE
    return ExceptionTracker::FindParentStackFrameForStackWalk(pCF);

#endif // !DACCESS_COMPILE
}
#endif // FEATURE_EH_FUNCLETS

#ifndef DACCESS_COMPILE
size_t EEDbgInterfaceImpl::GetFunctionSize(MethodDesc *pFD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pFD));
    }
    CONTRACTL_END;

    PCODE methodStart = pFD->GetNativeCode();

    if (methodStart == NULL)
        return 0;

    EECodeInfo codeInfo(methodStart);
    GCInfoToken gcInfoToken = codeInfo.GetGCInfoToken();
    return codeInfo.GetCodeManager()->GetFunctionSize(gcInfoToken);
}
#endif //!DACCESS_COMPILE

PCODE EEDbgInterfaceImpl::GetFunctionAddress(MethodDesc *pFD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pFD));
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return pFD->GetNativeCode();
}

#ifndef DACCESS_COMPILE

void EEDbgInterfaceImpl::DisablePreemptiveGC(void)
{
    CONTRACTL
    {
        NOTHROW;
        DISABLED(GC_TRIGGERS); // Disabled because disabled in RareDisablePreemptiveGC()
    }
    CONTRACTL_END;

    ::GetThread()->DisablePreemptiveGC();
}

void EEDbgInterfaceImpl::EnablePreemptiveGC(void)
{
    CONTRACTL
    {
        NOTHROW;
        DISABLED(GC_TRIGGERS); // Disabled because disabled in RareEnablePreemptiveGC()
    }
    CONTRACTL_END;

    ::GetThread()->EnablePreemptiveGC();
}

bool EEDbgInterfaceImpl::IsPreemptiveGCDisabled(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return ::GetThread()->PreemptiveGCDisabled() != 0;
}

DWORD EEDbgInterfaceImpl::MethodDescIsStatic(MethodDesc *pFD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pFD));
    }
    CONTRACTL_END;

    return pFD->IsStatic();
}

#endif // #ifndef DACCESS_COMPILE

Module *EEDbgInterfaceImpl::MethodDescGetModule(MethodDesc *pFD)
{
    CONTRACT(Module *)
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pFD));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    RETURN pFD->GetModule();
}

#ifndef DACCESS_COMPILE

COR_ILMETHOD* EEDbgInterfaceImpl::MethodDescGetILHeader(MethodDesc *pFD)
{
    CONTRACT(COR_ILMETHOD *)
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pFD));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    if (pFD->IsIL())
    {
        RETURN pFD->GetILHeader();
    }

    RETURN NULL;
}

ULONG EEDbgInterfaceImpl::MethodDescGetRVA(MethodDesc *pFD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pFD));
    }
    CONTRACTL_END;

    return pFD->GetRVA();
}

MethodDesc *EEDbgInterfaceImpl::FindLoadedMethodRefOrDef(Module* pModule,
                                                          mdToken memberRef)
{
    CONTRACT(MethodDesc *)
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pModule));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    // Must have a MemberRef or a MethodDef
    mdToken tkType = TypeFromToken(memberRef);
    _ASSERTE((tkType == mdtMemberRef) || (tkType == mdtMethodDef));

    if (tkType == mdtMemberRef)
    {
        RETURN pModule->LookupMemberRefAsMethod(memberRef);
    }

    RETURN pModule->LookupMethodDef(memberRef);
}

MethodDesc *EEDbgInterfaceImpl::LoadMethodDef(Module* pModule,
                                              mdMethodDef methodDef,
                                              DWORD numGenericArgs,
                                              TypeHandle *pGenericArgs,
                                              TypeHandle *pOwnerType)
{
    CONTRACT(MethodDesc *)
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pModule));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    _ASSERTE(TypeFromToken(methodDef) == mdtMethodDef);

    // The generic class and method args are sent as one array
    // by the debugger.  We now split this into two by finding out how
    // many generic args are for the class and how many for the
    // method.  The actual final checks are done in MemberLoader::GetMethodDescFromMethodDef.

    DWORD numGenericClassArgs = 0;
    TypeHandle *pGenericClassArgs = NULL;
    DWORD nGenericMethodArgs = 0;
    TypeHandle *pGenericMethodArgs = NULL;
    mdTypeDef typeDef = 0;

    TypeHandle thOwner;

    BOOL forceRemotable = FALSE;
    if (numGenericArgs != 0)
    {
        HRESULT hr = pModule->GetMDImport()->GetParentToken(methodDef, &typeDef);
        if (FAILED(hr))
            COMPlusThrowHR(E_INVALIDARG);

        TypeHandle thClass = LoadClass(pModule, typeDef);
        _ASSERTE(!thClass.IsNull());

        numGenericClassArgs = thClass.GetNumGenericArgs();
        if (numGenericArgs < numGenericClassArgs)
        {
            COMPlusThrowHR(COR_E_TARGETPARAMCOUNT);
        }
        pGenericClassArgs = (numGenericClassArgs > 0) ? pGenericArgs : NULL;
        nGenericMethodArgs = (numGenericArgs >= numGenericClassArgs) ? (numGenericArgs - numGenericClassArgs) : 0;
        pGenericMethodArgs = (nGenericMethodArgs > 0) ? (pGenericArgs + numGenericClassArgs) : NULL;

#ifdef FEATURE_COMINTEROP
        if (numGenericClassArgs > 0)
        {
            thOwner = ClassLoader::LoadGenericInstantiationThrowing(pModule, typeDef, Instantiation(pGenericClassArgs, numGenericClassArgs));
            forceRemotable = false;
        }
#endif // FEATURE_COMINTEROP
    }

    MethodDesc *pRes = MemberLoader::GetMethodDescFromMethodDef(pModule,
                                                                methodDef,
                                                                Instantiation(pGenericClassArgs, numGenericClassArgs),
                                                                Instantiation(pGenericMethodArgs, nGenericMethodArgs),
                                                                forceRemotable);

    // The ownerType is extra information that augments the specification of an interface MD.
    // It is only needed if generics code sharing is supported, because otherwise MDs are
    // fully self-describing.
    if (pOwnerType != NULL)
    {
        if (numGenericClassArgs != 0)
        {
            if (thOwner.IsNull())
                *pOwnerType = ClassLoader::LoadGenericInstantiationThrowing(pModule, typeDef, Instantiation(pGenericClassArgs, numGenericClassArgs));
            else
                *pOwnerType = thOwner;
        }
        else
        {
            *pOwnerType = TypeHandle(pRes->GetMethodTable());
        }
    }
    RETURN (pRes);

}


TypeHandle EEDbgInterfaceImpl::FindLoadedClass(Module *pModule,
                                             mdTypeDef classToken)
{
    CONTRACT(TypeHandle)
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACT_END;

    RETURN ClassLoader::LookupTypeDefOrRefInModule(pModule, classToken);

}

TypeHandle EEDbgInterfaceImpl::FindLoadedInstantiation(Module *pModule,
                                                       mdTypeDef typeDef,
                                                       DWORD ntypars,
                                                       TypeHandle *inst)
{
    // Lookup operations run the class loader in non-load mode.
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();


    // scan violation:  asserts that this can be suppressed since there is currently
    // work on dac-izing all this code and as a result the issue will become moot.
    CONTRACT_VIOLATION(FaultViolation);

    return ClassLoader::LoadGenericInstantiationThrowing(pModule, typeDef, Instantiation(inst, ntypars),
                                                        ClassLoader::DontLoadTypes);
}

TypeHandle EEDbgInterfaceImpl::FindLoadedFnptrType(TypeHandle *inst,
                                                   DWORD ntypars)
{
    // Lookup operations run the class loader in non-load mode.
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    //<TODO> : CALLCONV? </TODO>
    return ClassLoader::LoadFnptrTypeThrowing(0, ntypars, inst,
                                              // <TODO> should this be FailIfNotLoaded? - NO - although we may
                                              // want to debug unrestored VCs, we can't do it because the debug API
                                              // is not set up to handle them </TODO>
                                              // == FailIfNotLoadedOrNotRestored
                                              ClassLoader::DontLoadTypes);
}

TypeHandle EEDbgInterfaceImpl::FindLoadedPointerOrByrefType(CorElementType et,
                                                            TypeHandle elemtype)
{
    // Lookup operations run the class loader in non-load mode.
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    return ClassLoader::LoadPointerOrByrefTypeThrowing(et, elemtype,
                                                       // <TODO> should this be FailIfNotLoaded? - NO - although we may
                                                       // want to debug unrestored VCs, we can't do it because the debug API
                                                       // is not set up to handle them </TODO>
                                                       // == FailIfNotLoadedOrNotRestored
                                                       ClassLoader::DontLoadTypes);
}

TypeHandle EEDbgInterfaceImpl::FindLoadedArrayType(CorElementType et,
                                                   TypeHandle elemtype,
                                                   unsigned rank)
{
    // Lookup operations run the class loader in non-load mode.
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    if (elemtype.IsNull())
        return TypeHandle();
    else
        return ClassLoader::LoadArrayTypeThrowing(elemtype, et, rank,
                                                  // <TODO> should this be FailIfNotLoaded? - NO - although we may
                                                  // want to debug unrestored VCs, we can't do it because the debug API
                                                  // is not set up to handle them </TODO>
                                                  // == FailIfNotLoadedOrNotRestored
                                                  ClassLoader::DontLoadTypes );
}


TypeHandle EEDbgInterfaceImpl::FindLoadedElementType(CorElementType et)
{
    // Lookup operations run the class loader in non-load mode.
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    MethodTable *m = CoreLibBinder::GetElementType(et);

    return TypeHandle(m);
}

TypeHandle EEDbgInterfaceImpl::LoadClass(Module *pModule,
                                       mdTypeDef classToken)
{
    CONTRACT(TypeHandle)
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACT_END;

    RETURN ClassLoader::LoadTypeDefOrRefThrowing(pModule, classToken,
                                                          ClassLoader::ThrowIfNotFound,
                                                          ClassLoader::PermitUninstDefOrRef);

}

TypeHandle EEDbgInterfaceImpl::LoadInstantiation(Module *pModule,
                                                 mdTypeDef typeDef,
                                                 DWORD ntypars,
                                                 TypeHandle *inst)
{
    CONTRACT(TypeHandle)
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACT_END;

    RETURN ClassLoader::LoadGenericInstantiationThrowing(pModule, typeDef, Instantiation(inst, ntypars));
}

TypeHandle EEDbgInterfaceImpl::LoadArrayType(CorElementType et,
                                             TypeHandle elemtype,
                                             unsigned rank)
{
    CONTRACT(TypeHandle)
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACT_END;

    if (elemtype.IsNull())
        RETURN TypeHandle();
    else
        RETURN ClassLoader::LoadArrayTypeThrowing(elemtype, et, rank);
}

TypeHandle EEDbgInterfaceImpl::LoadPointerOrByrefType(CorElementType et,
                                                      TypeHandle elemtype)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    return ClassLoader::LoadPointerOrByrefTypeThrowing(et, elemtype);
}

TypeHandle EEDbgInterfaceImpl::LoadFnptrType(TypeHandle *inst,
                                             DWORD ntypars)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    /* @TODO : CALLCONV? */
    return ClassLoader::LoadFnptrTypeThrowing(0, ntypars, inst);
}

TypeHandle EEDbgInterfaceImpl::LoadElementType(CorElementType et)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    MethodTable *m = CoreLibBinder::GetElementType(et);

    if (m == NULL)
    {
        return TypeHandle();
    }

    return TypeHandle(m);
}


HRESULT EEDbgInterfaceImpl::GetMethodImplProps(Module *pModule,
                                               mdToken tk,
                                               DWORD *pRVA,
                                               DWORD *pImplFlags)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;

    return pModule->GetMDImport()->GetMethodImplProps(tk, pRVA, pImplFlags);
}

HRESULT EEDbgInterfaceImpl::GetParentToken(Module *pModule,
                                           mdToken tk,
                                           mdToken *pParentToken)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;

    return pModule->GetMDImport()->GetParentToken(tk, pParentToken);
}

void EEDbgInterfaceImpl::MarkDebuggerAttached(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    g_CORDebuggerControlFlags |= DBCF_ATTACHED;
    g_CORDebuggerControlFlags &= ~DBCF_PENDING_ATTACH;
}

void EEDbgInterfaceImpl::MarkDebuggerUnattached(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    g_CORDebuggerControlFlags &= ~DBCF_ATTACHED;
}


#ifdef EnC_SUPPORTED

// Apply an EnC edit to the specified module
HRESULT EEDbgInterfaceImpl::EnCApplyChanges(EditAndContinueModule *pModule,
                                            DWORD cbMetadata,
                                            BYTE *pMetadata,
                                            DWORD cbIL,
                                            BYTE *pIL)
{
    LOG((LF_ENC, LL_INFO100, "EncApplyChanges\n"));
    CONTRACTL
    {
        DISABLED(THROWS);
        DISABLED(GC_TRIGGERS);
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;

    return pModule->ApplyEditAndContinue(cbMetadata, pMetadata, cbIL, pIL);
}

// Remap execution to the latest version of an edited method
// This function should never return.
void EEDbgInterfaceImpl::ResumeInUpdatedFunction(EditAndContinueModule *pModule,
                                                 MethodDesc *pFD,
                                                 void *debuggerFuncHandle,
                                                 SIZE_T resumeIP,
                                                 CONTEXT *pContext)
{
    CONTRACTL
    {
        DISABLED(THROWS);
        DISABLED(GC_TRIGGERS);
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;

    pModule->ResumeInUpdatedFunction(pFD,
                                     debuggerFuncHandle,
                                     resumeIP,
                                     pContext);
}

#endif // EnC_SUPPORTED

bool EEDbgInterfaceImpl::CrawlFrameIsGcSafe(CrawlFrame *pCF)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pCF));
    }
    CONTRACTL_END;

    return pCF->IsGcSafe();
}

bool EEDbgInterfaceImpl::IsStub(const BYTE *ip)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // IsStub will catch any exceptions and return false.
    return StubManager::IsStub((PCODE) ip) != FALSE;
}

#endif // #ifndef DACCESS_COMPILE

// static
bool EEDbgInterfaceImpl::DetectHandleILStubs(Thread *thread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return thread->DetectHandleILStubsForDebugger();
}

bool EEDbgInterfaceImpl::TraceStub(const BYTE *ip,
                                   TraceDestination *trace)
{
#ifndef DACCESS_COMPILE
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return StubManager::TraceStub((PCODE) ip, trace) != FALSE;
#else
    DacNotImpl();
    return false;
#endif // #ifndef DACCESS_COMPILE
}

#ifndef DACCESS_COMPILE

bool EEDbgInterfaceImpl::FollowTrace(TraceDestination *trace)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return StubManager::FollowTrace(trace) != FALSE;
}

bool EEDbgInterfaceImpl::TraceFrame(Thread *thread,
                                    Frame *frame,
                                    BOOL fromPatch,
                                    TraceDestination *trace,
                                    REGDISPLAY *regs)
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_TRIGGERS);  // This is not a bug - the debugger can call this on an un-managed thread.
        PRECONDITION(CheckPointer(frame));
    }
    CONTRACTL_END;

    bool fResult = frame->TraceFrame(thread, fromPatch, trace, regs) != FALSE;

#ifdef _DEBUG
    StubManager::DbgWriteLog("Doing TraceFrame on frame=0x%p (fromPatch=%d), yields:\n", frame, fromPatch);
    if (fResult)
    {
        SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;
        FAULT_NOT_FATAL();
        SString buffer;
        StubManager::DbgWriteLog("  td=%S\n", trace->DbgToString(buffer));
    }
    else
    {
        StubManager::DbgWriteLog("  false (this frame does not expect to call managed code).\n");
    }
#endif
    return fResult;
}

bool EEDbgInterfaceImpl::TraceManager(Thread *thread,
                                      StubManager *stubManager,
                                      TraceDestination *trace,
                                      CONTEXT *context,
                                      BYTE **pRetAddr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(stubManager));
    }
    CONTRACTL_END;

    bool fResult = false;

    EX_TRY
    {
        fResult =  stubManager->TraceManager(thread, trace, context, pRetAddr) != FALSE;
    }
    EX_CATCH
    {
        // We never expect TraceManager() to fail and throw an exception,
        // so we should never hit this assertion.
        _ASSERTE(!"Fail to trace a stub through TraceManager()");
        fResult = false;
    }
    EX_END_CATCH(SwallowAllExceptions);

#ifdef _DEBUG
    StubManager::DbgWriteLog("Doing TraceManager on %s (0x%p) for IP=0x%p, yields:\n", stubManager->DbgGetName(), stubManager, GetIP(context));
    if (fResult)
    {
        // Should never be on helper thread
        FAULT_NOT_FATAL();
        SString buffer;
        StubManager::DbgWriteLog("  td=%S\n", trace->DbgToString(buffer));
    }
    else
    {
        StubManager::DbgWriteLog("  false (this stub does not expect to call managed code).\n");
    }
#endif
    return fResult;
}

void EEDbgInterfaceImpl::EnableTraceCall(Thread *thread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(thread));
    }
    CONTRACTL_END;

    thread->IncrementTraceCallCount();
}

void EEDbgInterfaceImpl::DisableTraceCall(Thread *thread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(thread));
    }
    CONTRACTL_END;

    thread->DecrementTraceCallCount();
}

void EEDbgInterfaceImpl::GetRuntimeOffsets(SIZE_T *pTLSIndex,
                                           SIZE_T *pTLSEEThreadOffset,
                                           SIZE_T *pTLSIsSpecialOffset,
                                           SIZE_T *pTLSCantStopOffset,
                                           SIZE_T *pEEThreadStateOffset,
                                           SIZE_T *pEEThreadStateNCOffset,
                                           SIZE_T *pEEThreadPGCDisabledOffset,
                                           DWORD  *pEEThreadPGCDisabledValue,
                                           SIZE_T *pEEThreadFrameOffset,
                                           SIZE_T *pEEThreadMaxNeededSize,
                                           DWORD  *pEEThreadSteppingStateMask,
                                           DWORD  *pEEMaxFrameValue,
                                           SIZE_T *pEEThreadDebuggerFilterContextOffset,
                                           SIZE_T *pEEFrameNextOffset,
                                           DWORD  *pEEIsManagedExceptionStateMask)
{
    LIMITED_METHOD_CONTRACT;

#ifdef TARGET_WINDOWS
    *pTLSIndex = _tls_index;
    *pTLSEEThreadOffset = Thread::GetOffsetOfThreadStatic(&gCurrentThreadInfo.m_pThread);
    *pTLSIsSpecialOffset = Thread::GetOffsetOfThreadStatic(&t_ThreadType);
    *pTLSCantStopOffset = Thread::GetOffsetOfThreadStatic(&t_CantStopCount);
#else
    *pTLSIndex = (SIZE_T)-1;
    *pTLSEEThreadOffset = (SIZE_T)-1;
    *pTLSIsSpecialOffset = (SIZE_T)-1;
    *pTLSCantStopOffset = (SIZE_T)-1;
#endif
    *pEEThreadStateOffset = Thread::GetOffsetOfState();
    *pEEThreadStateNCOffset = Thread::GetOffsetOfStateNC();
    *pEEThreadPGCDisabledOffset = Thread::GetOffsetOfGCFlag();
    *pEEThreadPGCDisabledValue = 1; // A little obvious, but just in case...
    *pEEThreadFrameOffset = Thread::GetOffsetOfCurrentFrame();
    *pEEThreadMaxNeededSize = sizeof(Thread);
    *pEEThreadDebuggerFilterContextOffset = Thread::GetOffsetOfDebuggerFilterContext();
    *pEEThreadSteppingStateMask = Thread::TSNC_DebuggerIsStepping;
    *pEEMaxFrameValue = (DWORD)(size_t)FRAME_TOP; // <TODO> should this be size_t for 64bit?</TODO>
    *pEEFrameNextOffset = Frame::GetOffsetOfNextLink();
    *pEEIsManagedExceptionStateMask = Thread::TSNC_DebuggerIsManagedException;
}

void EEDbgInterfaceImpl::DebuggerModifyingLogSwitch (int iNewLevel,
                                                     const WCHAR *pLogSwitchName)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
}


HRESULT EEDbgInterfaceImpl::SetIPFromSrcToDst(Thread *pThread,
                                              SLOT addrStart,
                                              DWORD offFrom,
                                              DWORD offTo,
                                              bool fCanSetIPOnly,
                                              PREGDISPLAY pReg,
                                              PCONTEXT pCtx,
                                              void *pDji,
                                              EHRangeTree *pEHRT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    return ::SetIPFromSrcToDst(pThread,
                               addrStart,
                               offFrom,
                               offTo,
                               fCanSetIPOnly,
                               pReg,
                               pCtx,
                               pDji,
                               pEHRT);

}

void EEDbgInterfaceImpl::SetDebugState(Thread *pThread,
                                       CorDebugThreadState state)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pThread));
    }
    CONTRACTL_END;

    _ASSERTE(state == THREAD_SUSPEND || state == THREAD_RUN);

    LOG((LF_CORDB,LL_INFO10000,"EEDbg:Setting thread 0x%x (ID:0x%x) to 0x%x\n", pThread, pThread->GetThreadId(), state));

    if (state == THREAD_SUSPEND)
    {
        pThread->SetThreadStateNC(Thread::TSNC_DebuggerUserSuspend);
    }
    else
    {
        pThread->ResetThreadStateNC(Thread::TSNC_DebuggerUserSuspend);
    }
}

void EEDbgInterfaceImpl::SetAllDebugState(Thread *et,
                                          CorDebugThreadState state)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Thread *pThread = NULL;

    while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
    {
        if (pThread != et)
        {
            SetDebugState(pThread, state);
        }
    }
}

// This is pretty much copied from VM\COMSynchronizable's
// INT32 __stdcall ThreadNative::GetThreadState, so propagate changes
// to both functions
// This just gets the user state from the EE's perspective (hence "partial").
CorDebugUserState EEDbgInterfaceImpl::GetPartialUserState(Thread *pThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pThread));
    }
    CONTRACTL_END;

    Thread::ThreadState ts = pThread->GetSnapshotState();
    unsigned ret = 0;

    if (ts & Thread::TS_Background)
    {
        ret |= (unsigned)USER_BACKGROUND;
    }

    if (ts & Thread::TS_Unstarted)
    {
        ret |= (unsigned)USER_UNSTARTED;
    }

    // Don't report a StopRequested if the thread has actually stopped.
    if (ts & Thread::TS_Dead)
    {
        ret |= (unsigned)USER_STOPPED;
    }

    if (ts & Thread::TS_Interruptible)
    {
        ret |= (unsigned)USER_WAIT_SLEEP_JOIN;
    }

    LOG((LF_CORDB,LL_INFO1000, "EEDbgII::GUS: thread 0x%x (id:0x%x)"
        " userThreadState is 0x%x\n", pThread, pThread->GetThreadId(), ret));

    return (CorDebugUserState)ret;
}

#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void
EEDbgInterfaceImpl::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    DAC_ENUM_VTHIS();
}

#endif

unsigned EEDbgInterfaceImpl::GetSizeForCorElementType(CorElementType etyp)
{
    WRAPPER_NO_CONTRACT;

    return (::GetSizeForCorElementType(etyp));
}


#ifndef DACCESS_COMPILE
/*
 * ObjIsInstanceOf
 *
 * This method supplies the internal VM implementation of this method to the
 * debugger left-side.
 *
 */
BOOL EEDbgInterfaceImpl::ObjIsInstanceOf(Object *pElement, TypeHandle toTypeHnd)
{
    WRAPPER_NO_CONTRACT;

    return (::ObjIsInstanceOf(pElement, toTypeHnd));
}
#endif

/*
 * ClearAllDebugInterfaceReferences
 *
 * This method is called by the debugging part of the runtime to notify
 * that the debugger resources are no longer valid and any internal references
 * to it must be null'ed out.
 *
 * Parameters:
 *   None.
 *
 * Returns:
 *   None.
 *
 */
void EEDbgInterfaceImpl::ClearAllDebugInterfaceReferences()
{
    LIMITED_METHOD_CONTRACT;
}

#ifndef DACCESS_COMPILE
#ifdef _DEBUG
/*
 * ObjectRefFlush
 *
 * Flushes all debug tracking information for object referencing.
 *
 * Parameters:
 *   pThread - The target thread to flush object references of.
 *
 * Returns:
 *   None.
 *
 */
void EEDbgInterfaceImpl::ObjectRefFlush(Thread *pThread)
{
    WRAPPER_NO_CONTRACT;

    Thread::ObjectRefFlush(pThread);
}
#endif
#endif

#ifndef DACCESS_COMPILE

BOOL AdjustContextForJITHelpers(EXCEPTION_RECORD *pExceptionRecord, CONTEXT *pContext);
BOOL EEDbgInterfaceImpl::AdjustContextForJITHelpersForDebugger(CONTEXT* context)
{
    WRAPPER_NO_CONTRACT;
    return AdjustContextForJITHelpers(nullptr, context);
}
#endif

#endif // DEBUGGING_SUPPORTED
